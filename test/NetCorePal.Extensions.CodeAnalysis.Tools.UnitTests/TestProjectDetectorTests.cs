using System;
using System.IO;
using NetCorePal.Extensions.CodeAnalysis.Tools;
using Xunit;
using System.Reflection;

namespace NetCorePal.Extensions.CodeAnalysis.Tools.UnitTests;

public class ProjectAnalysisHelpersTests
{
    [Fact]
    public void IsTestProject_ReturnsTrue_WhenParentDirIsTests()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"codeanalysis-tests-{Guid.NewGuid():N}");
        var testsDir = Path.Combine(tempRoot, "tests");
        Directory.CreateDirectory(testsDir);
        var csprojPath = Path.Combine(testsDir, "Sample.csproj");
        File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        try
        {
            var result = ProjectAnalysisHelpers.IsTestProject(csprojPath);
            Assert.True(result);
        }
        finally { Directory.Delete(tempRoot, true); }
    }

    [Fact]
    public void IsTestProject_ReturnsTrue_WhenParentDirIsTests_DifferentCasing()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"codeanalysis-tests-{Guid.NewGuid():N}");
        var testsDir = Path.Combine(tempRoot, "TeStS");
        Directory.CreateDirectory(testsDir);
        var csprojPath = Path.Combine(testsDir, "Sample.csproj");
        File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        try
        {
            var result = ProjectAnalysisHelpers.IsTestProject(csprojPath);
            Assert.True(result);
        }
        finally { Directory.Delete(tempRoot, true); }
    }

    [Fact]
    public void IsTestProject_ReturnsTrue_WhenAncestorDirIsTests()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"codeanalysis-tests-{Guid.NewGuid():N}");
        var ancestorTestsDir = Path.Combine(tempRoot, "tests");
        var subDir = Path.Combine(ancestorTestsDir, "submodule");
        Directory.CreateDirectory(subDir);
        var csprojPath = Path.Combine(subDir, "Sample.csproj");
        File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        try
        {
            var result = ProjectAnalysisHelpers.IsTestProject(csprojPath);
            Assert.True(result);
        }
        finally { Directory.Delete(tempRoot, true); }
    }

    [Fact]
    public void IsTestProject_ReturnsFalse_WhenDirNameIsTesting()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"codeanalysis-tests-{Guid.NewGuid():N}");
        var testingDir = Path.Combine(tempRoot, "testing");
        Directory.CreateDirectory(testingDir);
        var csprojPath = Path.Combine(testingDir, "Sample.csproj");
        File.WriteAllText(csprojPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        try
        {
            var result = ProjectAnalysisHelpers.IsTestProject(csprojPath);
            Assert.False(result);
        }
        finally { Directory.Delete(tempRoot, true); }
    }

    [Fact]
    public void IsTestProject_ReturnsTrue_WhenIsTestProjectFlagIsUppercaseAndSpaced()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"codeanalysis-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var csprojPath = Path.Combine(tempRoot, "Sample.csproj");
        var content = """
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsTestProject>  TRUE  </IsTestProject>
  </PropertyGroup>
  
</Project>
""";
        File.WriteAllText(csprojPath, content);
        try
        {
            var result = ProjectAnalysisHelpers.IsTestProject(csprojPath);
            Assert.True(result);
        }
        finally { Directory.Delete(tempRoot, true); }
    }

    [Fact]
    public void IsTestProject_ReturnsTrue_WhenIsTestProjectFlagIsSet()
    {
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
        try
        {
            var result = ProjectAnalysisHelpers.IsTestProject(csprojPath);
            Assert.True(result);
        }
        finally { Directory.Delete(tempRoot, true); }
    }

    [Fact]
    public void IsTestProject_ReturnsFalse_WhenNoTestMarkers()
    {
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
        try
        {
            var result = ProjectAnalysisHelpers.IsTestProject(csprojPath);
            Assert.False(result);
        }
        finally { Directory.Delete(tempRoot, true); }
    }
}

public class ProjectAnalysisHelpersAdditionalTests
{
    [Fact]
    public void GetVersion_Returns_AssemblyInformationalVersion()
    {
        var expected = typeof(ProjectAnalysisHelpers).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(ProjectAnalysisHelpers).Assembly.GetName().Version?.ToString();

        var actual = ProjectAnalysisHelpers.GetVersion();
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetProjectDependencies_Parses_ProjectReference_Includes()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"codeanalysis-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var csprojPath = Path.Combine(tempRoot, "Sample.csproj");
        var content = """
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="../Lib/Lib.csproj" />
    <ProjectReference Include="..\\Util\\Util.csproj" />
  </ItemGroup>
</Project>
""";
        File.WriteAllText(csprojPath, content);
        try
        {
            var deps = ProjectAnalysisHelpers.GetProjectDependencies(csprojPath);
            Assert.Contains("../Lib/Lib.csproj", deps);
            Assert.Contains(deps, s => s.EndsWith("Util.csproj", StringComparison.OrdinalIgnoreCase));
        }
        finally { Directory.Delete(tempRoot, true); }
    }

    [Fact]
    public void CollectProjectDependencies_Counts_Missing_Dependency()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"codeanalysis-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var mainCsproj = Path.Combine(tempRoot, "Main.csproj");
        var content = """
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="./Missing/Missing.csproj" />
  </ItemGroup>
</Project>
""";
        File.WriteAllText(mainCsproj, content);
        try
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var missing = ProjectAnalysisHelpers.CollectProjectDependencies(mainCsproj, set, verbose: true, includeTests: true);
            Assert.Equal(1, missing);
            Assert.Contains(mainCsproj, set);
        }
        finally { Directory.Delete(tempRoot, true); }
    }

    [Fact]
    public void GetProjectPathsFromSolution_Sln_Parses_Project_Path()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"codeanalysis-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);
        var projDir = Path.Combine(tempRoot, "src", "App");
        Directory.CreateDirectory(projDir);
        var projPath = Path.Combine(projDir, "App.csproj");
        File.WriteAllText(projPath, "<Project Sdk=\"Microsoft.NET.Sdk\"></Project>");
        var slnPath = Path.Combine(tempRoot, "Sample.sln");
        var slnContent = $"Project(\"{{FAKE-GUID}}\") = \"App\", \"src\\App\\App.csproj\", \"{{GUID}}\"";
        File.WriteAllText(slnPath, slnContent);
        try
        {
            var list = ProjectAnalysisHelpers.GetProjectPathsFromSolution(slnPath, tempRoot);
            Assert.Single(list);
            Assert.Equal(projPath, list[0]);
        }
        finally { Directory.Delete(tempRoot, true); }
    }
}
