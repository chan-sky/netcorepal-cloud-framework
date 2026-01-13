using System;
using System.Collections.Generic;
using System.IO;
using NetCorePal.Extensions.CodeAnalysis.Tools;
using Xunit;

namespace NetCorePal.Extensions.CodeAnalysis.Tools.UnitTests;

public class AppCsContentGeneratorTests
{
    [Fact]
    public void GenerateAppCsContent_ContainsProjectDirectivesAndAssemblyNames()
    {
        // Arrange
        var projects = new List<string>
        {
            "/tmp/ProjA/ProjA.csproj",
            "/tmp/ProjB/ProjB.csproj",
            "/tmp/ProjB/ProjB.csproj" // duplicate to verify distinct
        };
        var outputPath = Path.Combine(Path.GetTempPath(), $"arch-{Guid.NewGuid():N}.html");
        var title = "My Title \"Quoted\"";

        // Act
        var content = AppCsContentGenerator.GenerateAppCsContent(projects, outputPath, title);

        // Assert
        // #:project lines
        Assert.Contains("#:project /tmp/ProjA/ProjA.csproj", content);
        Assert.Contains("#:project /tmp/ProjB/ProjB.csproj", content);

        // assemblyNames array contains distinct dlls
        Assert.Contains("\"ProjA.dll\"", content);
        Assert.Contains("\"ProjB.dll\"", content);

        // VisualizationHtmlBuilder called with escaped title
        Assert.Contains("VisualizationHtmlBuilder.GenerateVisualizationHtml", content);
        Assert.Contains("My Title \"\"Quoted\"\"", content); // verbatim escaped

        // Output path is normalized and verbatim string used
        var normalized = Path.GetFullPath(outputPath).Replace("\\", "/");
        // We expect the normalized path verbatim in the generated content
        Assert.Contains(normalized, content.Replace("\\", "/"));
    }
}
