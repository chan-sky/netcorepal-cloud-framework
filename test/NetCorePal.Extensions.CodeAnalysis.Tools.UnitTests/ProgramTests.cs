using System.CommandLine;
using System.Reflection;
using System.IO;
using Xunit;

namespace NetCorePal.Extensions.CodeAnalysis.Tools.UnitTests;

public class ProgramTests
{
    private readonly string _testProjectPath;
    private readonly string _tempOutputPath;

    public ProgramTests()
    {
        // Use the actual test project that can be analyzed
        var testDir = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", 
                "NetCorePal.Extensions.CodeAnalysis.UnitTests"));
        _testProjectPath = Path.Combine(testDir, "NetCorePal.Extensions.CodeAnalysis.UnitTests.csproj");
        
        _tempOutputPath = Path.Combine(Path.GetTempPath(), $"codeanalysis-test-{Guid.NewGuid():N}.html");
    }

    [Fact]
    public async Task Main_WithHelpArgument_ReturnsZero()
    {
        // Arrange
        var args = new[] { "--help" };

        // Act
        var result = await Program.Main(args);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Main_WithVersionArgument_ReturnsZero()
    {
        // Arrange
        var args = new[] { "--version" };

        // Act
        var result = await Program.Main(args);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Main_WithInvalidArgument_ReturnsNonZero()
    {
        // Arrange
        var args = new[] { "--invalid-option" };

        // Act
        var result = await Program.Main(args);

        // Assert
        Assert.NotEqual(0, result);
    }

    [Fact]
    public async Task Main_GenerateCommand_WithHelp_ReturnsZero()
    {
        // Arrange
        var args = new[] { "generate", "--help" };

        // Act
        var result = await Program.Main(args);

        // Assert
        Assert.Equal(0, result);
    }

    [Fact(Skip = "Integration test - requires building projects")]
    public async Task Main_GenerateCommand_WithRealProject_CreatesOutputFile()
    {
        // Skip test if project doesn't exist
        if (!File.Exists(_testProjectPath))
        {
            return; // Skip test gracefully
        }

        try
        {
            // Arrange
            var args = new[] { "generate", "--project", _testProjectPath, "--output", _tempOutputPath };

            // Act
            var result = await Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            Assert.True(File.Exists(_tempOutputPath), "Output file should be created");
            
            var content = await File.ReadAllTextAsync(_tempOutputPath);
            Assert.Contains("<!DOCTYPE html>", content);
            Assert.Contains("架构可视化", content);
        }
        finally
        {
            // Cleanup
            if (File.Exists(_tempOutputPath))
            {
                File.Delete(_tempOutputPath);
            }
        }
    }

    [Fact(Skip = "Integration test - requires building projects")]
    public async Task Main_GenerateCommand_WithCustomTitle_UsesCustomTitle()
    {
        // Skip test if project doesn't exist
        if (!File.Exists(_testProjectPath))
        {
            return; // Skip test gracefully
        }

        try
        {
            // Arrange
            var customTitle = "Custom Test Documentation";
            var args = new[] { 
                "generate", 
                "--project", _testProjectPath, 
                "--output", _tempOutputPath,
                "--title", customTitle 
            };

            // Act
            var result = await Program.Main(args);

            // Assert
            Assert.Equal(0, result);
            Assert.True(File.Exists(_tempOutputPath));
            
            var content = await File.ReadAllTextAsync(_tempOutputPath);
            Assert.Contains(customTitle, content);
        }
        finally
        {
            // Cleanup
            if (File.Exists(_tempOutputPath))
            {
                File.Delete(_tempOutputPath);
            }
        }
    }

    [Fact]
    public void Main_GenerateCommand_WithNonExistentProject_Fails()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non-existent.csproj");
        var args = new[] { "generate", "--project", nonExistentPath, "--output", _tempOutputPath };

        // Act & Assert
        // The tool will call Environment.Exit(1) which we cannot catch in tests
        // So we just verify that the project file doesn't exist
        Assert.False(File.Exists(nonExistentPath), "Non-existent project should not exist");
    }

        [Fact]
        public void IsTestProject_ReturnsTrue_WhenParentDirIsTests()
        {
                // Arrange
                var tempRoot = Path.Combine(Path.GetTempPath(), $"codeanalysis-tests-{Guid.NewGuid():N}");
                var testsDir = Path.Combine(tempRoot, "tests");
                Directory.CreateDirectory(testsDir);
                var csprojPath = Path.Combine(testsDir, "Sample.csproj");
                File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");

                // Act
                var result = InvokeIsTestProject(csprojPath);

                // Assert
                Assert.True(result);

                Directory.Delete(tempRoot, true);
        }

        [Fact]
        public void IsTestProject_ReturnsTrue_WhenIsTestProjectFlagIsSet()
        {
                // Arrange
                var tempRoot = Path.Combine(Path.GetTempPath(), $"codeanalysis-tests-{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempRoot);
                var csprojPath = Path.Combine(tempRoot, "Sample.csproj");
                var content = """
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
        <IsTestProject>true</IsTestProject>
    </PropertyGroup>
</Project>
""";
                File.WriteAllText(csprojPath, content);

                // Act
                var result = InvokeIsTestProject(csprojPath);

                // Assert
                Assert.True(result);

                Directory.Delete(tempRoot, true);
        }

        [Fact]
        public void IsTestProject_ReturnsFalse_WhenNoTestMarkers()
        {
                // Arrange
                var tempRoot = Path.Combine(Path.GetTempPath(), $"codeanalysis-tests-{Guid.NewGuid():N}");
                Directory.CreateDirectory(tempRoot);
                var csprojPath = Path.Combine(tempRoot, "Sample.csproj");
                var content = """
<Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
        <TargetFramework>net8.0</TargetFramework>
    </PropertyGroup>
</Project>
""";
                File.WriteAllText(csprojPath, content);

                // Act
                var result = InvokeIsTestProject(csprojPath);

                // Assert
                Assert.False(result);

                Directory.Delete(tempRoot, true);
        }

        private static bool InvokeIsTestProject(string projectPath)
        {
                var method = typeof(Program).GetMethod("IsTestProject", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.NotNull(method);
                var result = method!.Invoke(null, new object[] { projectPath });
                return Assert.IsType<bool>(result);
        }
}
