using System.CommandLine;
using System.IO;
using NetCorePal.Extensions.CodeAnalysis.Tools;
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
    public async Task Main_GenerateCommand_WithNonExistentProject_CallsExit()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), "non-existent.csproj");
        var exitCalled = false;
        var exitCode = 0;
        
        var mockExitHandler = new MockExitHandler(code => {
            exitCalled = true;
            exitCode = code;
        });
        
        var originalHandler = Program.ExitHandler;
        Program.ExitHandler = mockExitHandler;
        
        try
        {
            var args = new[] { "generate", "--project", nonExistentPath, "--output", _tempOutputPath };

            // Act
            await Program.Main(args);

            // Assert
            Assert.True(exitCalled, "Exit should have been called");
            Assert.Equal(1, exitCode);
        }
        finally
        {
            Program.ExitHandler = originalHandler;
        }
    }

    private class MockExitHandler : IExitHandler
    {
        private readonly Action<int> _onExit;
        
        public MockExitHandler(Action<int> onExit)
        {
            _onExit = onExit;
        }
        
        public void Exit(int exitCode) => _onExit(exitCode);
    }

        
}
