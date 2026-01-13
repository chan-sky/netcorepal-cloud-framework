using System.CommandLine;
using System.Reflection;
using System.Xml.Linq;
using System.Text;
using NetCorePal.Extensions.CodeAnalysis;

namespace NetCorePal.Extensions.CodeAnalysis.Tools;

public interface IExitHandler
{
    void Exit(int exitCode);
}

public class EnvironmentExitHandler : IExitHandler
{
    public void Exit(int exitCode) => Environment.Exit(exitCode);
}

public class Program
{
    private const int AnalysisTimeoutMinutes = 5;
    internal static IExitHandler ExitHandler { get; set; } = new EnvironmentExitHandler();
    
    

    public static async Task<int> Main(string[] args)
    {
        var rootCommand =
            new RootCommand(
                "NetCorePal Code Analysis Tool - Generate architecture visualization HTML files from .NET projects");

        var generateCommand = new Command("generate", "Generate HTML visualization from projects");

        var solutionOption = new Option<FileInfo>(
            name: "--solution",
            description: "Solution file to analyze (.sln)")
        {
            IsRequired = false
        };
        solutionOption.AddAlias("-s");

        var projectOption = new Option<FileInfo[]>(
            name: "--project",
            description: "Project files to analyze (.csproj)")
        {
            IsRequired = false,
            AllowMultipleArgumentsPerToken = true
        };
        projectOption.AddAlias("-p");

        var outputOption = new Option<FileInfo>(
            name: "--output",
            description: "Output HTML file path")
        {
            IsRequired = false
        };
        outputOption.AddAlias("-o");
        outputOption.SetDefaultValue(new FileInfo("architecture-visualization.html"));

        var titleOption = new Option<string>(
            name: "--title",
            description: "HTML page title")
        {
            IsRequired = false
        };
        titleOption.AddAlias("-t");
        titleOption.SetDefaultValue("架构可视化");

        var verboseOption = new Option<bool>(
            name: "--verbose",
            description: "Enable verbose output")
        {
            IsRequired = false
        };
        verboseOption.AddAlias("-v");

        var includeTestsOption = new Option<bool>(
            name: "--include-tests",
            description: "Include test projects when analyzing (default: false)")
        {
            IsRequired = false
        };
        // no short alias to avoid ambiguity

        generateCommand.AddOption(solutionOption);
        generateCommand.AddOption(projectOption);
        generateCommand.AddOption(outputOption);
        generateCommand.AddOption(titleOption);
        generateCommand.AddOption(verboseOption);
        generateCommand.AddOption(includeTestsOption);
        

        generateCommand.SetHandler(
            async (solution, projects, output, title, verbose, includeTests) =>
            {
                await GenerateVisualization(solution, projects, output, title, verbose, includeTests);
            }, solutionOption, projectOption, outputOption, titleOption, verboseOption, includeTestsOption);

        rootCommand.AddCommand(generateCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task GenerateVisualization(FileInfo? solutionFile, FileInfo[]? projectFiles,
        FileInfo outputFile, string title, bool verbose, bool includeTests)
    {
        try
        {
            if (verbose)
            {
                Console.WriteLine($"NetCorePal Code Analysis Tool v{ProjectAnalysisHelpers.GetVersion()}");
                Console.WriteLine($"Output file: {outputFile.FullName}");
                Console.WriteLine($"Title: {title}");
                Console.WriteLine($"Include tests: {includeTests}");
                Console.WriteLine();
            }

            // Determine projects to analyze
            var projectsToAnalyze = new List<string>();

            if (projectFiles?.Length > 0)
            {
                // Project files specified
                if (verbose)
                    Console.WriteLine("Using specified projects:");

                foreach (var projectFile in projectFiles)
                {
                    if (!projectFile.Exists)
                    {
                        Console.Error.WriteLine($"Error: Project file not found: {projectFile.FullName}");
                        ExitHandler.Exit(1);
                    }
                    if (!includeTests && ProjectAnalysisHelpers.IsTestProject(projectFile.FullName, verbose))
                    {
                        if (verbose)
                            Console.WriteLine($"  Skipping test project: {projectFile.FullName}");
                        continue;
                    }
                    projectsToAnalyze.Add(projectFile.FullName);
                    if (verbose)
                        Console.WriteLine($"  {projectFile.FullName}");
                }
            }
            else if (solutionFile != null)
            {
                // Solution file specified
                if (!solutionFile.Exists)
                {
                    Console.Error.WriteLine($"Error: Solution file not found: {solutionFile.FullName}");
                    ExitHandler.Exit(1);
                }

                if (verbose)
                    Console.WriteLine($"Analyzing solution: {solutionFile.FullName}");

                var solutionDir = Path.GetDirectoryName(solutionFile.FullName)!;
                var projectPaths = ProjectAnalysisHelpers.GetProjectPathsFromSolution(solutionFile.FullName, solutionDir);
                
                // Skip IsTestProject check entirely when includeTests is true
                List<string> filtered;
                if (includeTests)
                {
                    filtered = projectPaths;
                }
                else
                {
                    filtered = projectPaths.Where(p => !ProjectAnalysisHelpers.IsTestProject(p)).ToList();
                    if (verbose)
                    {
                        var excluded = projectPaths.Count - filtered.Count;
                        if (excluded > 0)
                            Console.WriteLine($"Excluded {excluded} test project(s) by default.");
                    }
                }
                projectsToAnalyze.AddRange(filtered);

                if (verbose)
                {
                    Console.WriteLine($"Found {projectPaths.Count} projects:");
                    foreach (var projectPath in projectPaths)
                    {
                        Console.WriteLine($"  {Path.GetFileName(projectPath)}");
                    }
                }
            }
            else
            {
                // Auto-discover solution or projects in current directory
                if (verbose)
                    Console.WriteLine("Auto-discovering solution or projects in current directory...");

                await AutoDiscoverProjects(projectsToAnalyze, verbose, includeTests);
            }

            if (projectsToAnalyze.Count == 0)
            {
                Console.Error.WriteLine(
                    "Error: No projects found to analyze. Please specify --solution or --project options.");
                ExitHandler.Exit(1);
            }

            // Get all project dependencies recursively
            var allProjects = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int totalMissing = 0;
            if (!verbose)
            {
                Console.WriteLine("Collecting project dependencies...");
            }
            foreach (var projectPath in projectsToAnalyze)
            {
                totalMissing += ProjectAnalysisHelpers.CollectProjectDependencies(projectPath, allProjects, verbose, includeTests);
            }

            if (verbose)
            {
                Console.WriteLine();
                Console.WriteLine($"Total projects to analyze (including dependencies): {allProjects.Count}");
                if (totalMissing > 0)
                {
                    Console.WriteLine($"⚠️  Warning: {totalMissing} project dependencies could not be found");
                }
            }
            else if (totalMissing > 0)
            {
                Console.WriteLine($"⚠️  Warning: {totalMissing} project dependencies could not be found (use --verbose for details)");
            }

            // Generate app.cs file in an isolated temp folder to avoid inheriting cwd/global.json
            var tempWorkDir = Path.Combine(Path.GetTempPath(), $"netcorepal-analysis-{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempWorkDir);
            var tempAppCsPath = Path.Combine(tempWorkDir, "app.cs");
            var absoluteOutputPath = Path.GetFullPath(outputFile.FullName);
            var appCsContent = AppCsContentGenerator.GenerateAppCsContent(allProjects.ToList(), absoluteOutputPath, title);

            if (verbose)
            {
                Console.WriteLine();
                Console.WriteLine($"Generated app.cs at: {tempAppCsPath}");
                Console.WriteLine("Content:");
                Console.WriteLine("========================================");
                Console.WriteLine(appCsContent);
                Console.WriteLine("========================================");
                Console.WriteLine();
            }

            await File.WriteAllTextAsync(tempAppCsPath, appCsContent);

            try
            {
                Console.WriteLine("Starting analysis...");
                // Run dotnet run app.cs in an isolated temp directory to avoid project launchSettings/global.json in cwd
                var workingDir = tempWorkDir;
                var runArgs = $"run {tempAppCsPath} --no-launch-profile";
                if (verbose)
                {
                    Console.WriteLine($"Executing: dotnet {runArgs}");
                    Console.WriteLine($"WorkingDirectory: {workingDir}");
                }

                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = runArgs,
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = System.Diagnostics.Process.Start(processStartInfo);
                if (process == null)
                {
                    Console.Error.WriteLine("Failed to start dotnet run process");
                    ExitHandler.Exit(1);
                    return; // Add return for null-safety analysis
                }

                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                var outputTask = Task.Run(async () =>
                {
                    using var reader = process.StandardOutput;
                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        outputBuilder.AppendLine(line);
                        if (verbose)
                        {
                            Console.WriteLine(line);
                        }
                    }
                });

                var errorTask = Task.Run(async () =>
                {
                    using var reader = process.StandardError;
                    string? line;
                    while ((line = await reader.ReadLineAsync()) != null)
                    {
                        errorBuilder.AppendLine(line);
                        if (verbose)
                        {
                            Console.Error.WriteLine(line);
                        }
                    }
                });

                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(AnalysisTimeoutMinutes));

                try
                {
                    await Task.WhenAll(
                        process.WaitForExitAsync(timeoutCts.Token),
                        outputTask,
                        errorTask
                    );

                    if (process.ExitCode != 0)
                    {
                        var error = errorBuilder.ToString();
                        Console.Error.WriteLine($"Analysis failed with exit code {process.ExitCode}:");
                        Console.Error.WriteLine(error);
                        ExitHandler.Exit(1);
                    }

                    if (verbose)
                    {
                        Console.WriteLine();
                        Console.WriteLine("Analysis completed successfully");
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.Error.WriteLine($"Analysis process timed out after {AnalysisTimeoutMinutes} minutes");
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.Kill(true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Failed to kill analysis process: {ex.Message}");
                    }
                    ExitHandler.Exit(1);
                }

                // Check if output file was created
                if (File.Exists(absoluteOutputPath))
                {
                    Console.WriteLine($"✅ HTML visualization generated successfully: {absoluteOutputPath}");

                    if (verbose)
                    {
                        var fileInfo = new FileInfo(absoluteOutputPath);
                        Console.WriteLine($"File size: {fileInfo.Length:N0} bytes");
                    }
                }
                else
                {
                    Console.Error.WriteLine($"Error: Output file was not created: {absoluteOutputPath}");
                    ExitHandler.Exit(1);
                }
            }
            finally
            {
                // Clean up temporary app.cs file
                try
                {
                    if (Directory.Exists(tempWorkDir))
                    {
                        Directory.Delete(tempWorkDir, recursive: true);
                        if (verbose)
                            Console.WriteLine($"Cleaned up temporary folder: {tempWorkDir}");
                    }
                }
                catch (Exception ex)
                {
                    if (verbose)
                        Console.WriteLine($"Warning: Failed to delete temporary folder: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            if (verbose)
            {
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            ExitHandler.Exit(1);
        }
    }

    // Helpers moved to ProjectAnalysisHelpers

    

    private static async Task AutoDiscoverProjects(List<string> projectsToAnalyze, bool verbose, bool includeTests)
    {
        var currentDir = Directory.GetCurrentDirectory();

        // Prefer .slnx first, then .sln (top directory only)
        var slnxFiles = Directory.GetFiles(currentDir, "*.slnx", SearchOption.TopDirectoryOnly);
        if (slnxFiles.Length > 0)
        {
            var solutionFile = slnxFiles[0];
            Console.WriteLine($"Using solution (.slnx): {Path.GetFileName(solutionFile)}");

            var solutionDir = Path.GetDirectoryName(solutionFile)!;
            var projectPaths = ProjectAnalysisHelpers.GetProjectPathsFromSolution(solutionFile, solutionDir);
            var filtered = includeTests ? projectPaths : projectPaths.Where(p => !ProjectAnalysisHelpers.IsTestProject(p)).ToList();
            if (!includeTests && verbose)
            {
                var excluded = projectPaths.Count - filtered.Count;
                if (excluded > 0)
                    Console.WriteLine($"Excluded {excluded} test project(s) by default.");
            }
            Console.WriteLine($"Projects to analyze ({filtered.Count}):");
            foreach (var projectPath in filtered)
            {
                Console.WriteLine($"  {Path.GetFileName(projectPath)}");
            }
            projectsToAnalyze.AddRange(filtered);
            return;
        }

        var slnFiles = Directory.GetFiles(currentDir, "*.sln", SearchOption.TopDirectoryOnly);
        if (slnFiles.Length > 0)
        {
            var solutionFile = slnFiles[0];
            Console.WriteLine($"Using solution (.sln): {Path.GetFileName(solutionFile)}");

            var solutionDir = Path.GetDirectoryName(solutionFile)!;
            var projectPaths = ProjectAnalysisHelpers.GetProjectPathsFromSolution(solutionFile, solutionDir);
            var filtered = includeTests ? projectPaths : projectPaths.Where(p => !ProjectAnalysisHelpers.IsTestProject(p)).ToList();
            if (!includeTests && verbose)
            {
                var excluded = projectPaths.Count - filtered.Count;
                if (excluded > 0)
                    Console.WriteLine($"Excluded {excluded} test project(s) by default.");
            }
            Console.WriteLine($"Projects to analyze ({filtered.Count}):");
            foreach (var projectPath in filtered)
            {
                Console.WriteLine($"  {Path.GetFileName(projectPath)}");
            }
            projectsToAnalyze.AddRange(filtered);
            return;
        }

        // Look for project files
        var projectFiles = Directory.GetFiles(currentDir, "*.csproj", SearchOption.TopDirectoryOnly);
        if (projectFiles.Length > 0)
        {
            var filtered = includeTests ? projectFiles : projectFiles.Where(p => !ProjectAnalysisHelpers.IsTestProject(p)).ToArray();
            Console.WriteLine($"Using projects ({filtered.Length}):");

            foreach (var projectFile in filtered)
            {
                Console.WriteLine($"  {Path.GetFileName(projectFile)}");
                projectsToAnalyze.Add(projectFile);
            }

            return;
        }

        // No solution or projects found
        Console.WriteLine("  No solution or project files found in current directory.");
    }

    // Helpers moved to ProjectAnalysisHelpers

    // Helpers moved to ProjectAnalysisHelpers

    // Helpers moved to ProjectAnalysisHelpers
}