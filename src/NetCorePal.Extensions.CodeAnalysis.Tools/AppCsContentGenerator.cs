using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NetCorePal.Extensions.CodeAnalysis.Tools;

internal static class AppCsContentGenerator
{
    internal static string GenerateAppCsContent(List<string> projectPaths, string outputPath, string title)
    {
        var sb = new StringBuilder();

        // Add #:project directives for each project
        foreach (var projectPath in projectPaths)
        {
            sb.AppendLine($"#:project {projectPath}");
        }

        sb.AppendLine();
        sb.AppendLine("using NetCorePal.Extensions.CodeAnalysis;");
        sb.AppendLine("using System.IO;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Reflection;");
        sb.AppendLine();
        sb.AppendLine();
        sb.AppendLine("var baseDir = AppDomain.CurrentDomain.BaseDirectory;");

        // Generate assembly names from project paths
        var assemblyNames = projectPaths
            .Select(p => Path.GetFileNameWithoutExtension(p) + ".dll")
            .Distinct()
            .ToList();

        sb.AppendLine("var assemblyNames = new[]");
        sb.AppendLine("{");
        foreach (var assemblyName in assemblyNames)
        {
            sb.AppendLine($"    \"{assemblyName}\",");
        }
        sb.AppendLine("};");
        sb.AppendLine();

        sb.AppendLine("var assemblies = assemblyNames");
        sb.AppendLine("    .Select(name => Path.Combine(baseDir, name))");
        sb.AppendLine("    .Where(File.Exists)");
        sb.AppendLine("    .Select(Assembly.LoadFrom)");
        sb.AppendLine("    .Distinct()");
        sb.AppendLine("    .ToArray();");
        sb.AppendLine();

        sb.AppendLine("var result = CodeFlowAnalysisHelper.GetResultFromAssemblies(assemblies);");

        // Use verbatim strings with proper escaping for title and outputPath
        var escapedTitle = title.Replace("\"", "\"\"");
        var normalizedOutputPath = Path.GetFullPath(outputPath);
        var escapedOutputPath = normalizedOutputPath.Replace("\"", "\"\"");

        sb.AppendLine($"var html = VisualizationHtmlBuilder.GenerateVisualizationHtml(result, @\"{escapedTitle}\");");
        sb.AppendLine($"File.WriteAllText(@\"{escapedOutputPath}\", html);");

        return sb.ToString();
    }
}
