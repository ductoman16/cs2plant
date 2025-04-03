using FluentAssertions;
using Microsoft.Extensions.Logging;
using cs2plant.Services;
using Xunit;

namespace cs2plant.Tests.Services;

public sealed class MSBuildDependencyAnalyzerTests : IDisposable
{
    private readonly ILogger<MSBuildDependencyAnalyzer> _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<MSBuildDependencyAnalyzer>();
    private readonly MSBuildProjectLoader _projectLoader;
    private readonly ClassAnalyzer _classAnalyzer;
    private readonly MSBuildDependencyAnalyzer _analyzer;
    private readonly string _tempSolutionPath;
    private readonly string _tempProjectPath;

    public MSBuildDependencyAnalyzerTests()
    {
        _projectLoader = new MSBuildProjectLoader(LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<MSBuildProjectLoader>());
        _classAnalyzer = new ClassAnalyzer(LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ClassAnalyzer>());
        _analyzer = new MSBuildDependencyAnalyzer(_projectLoader, _logger, _classAnalyzer);

        // Create temporary solution and project files
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        
        _tempSolutionPath = Path.Combine(tempDir, "Test.sln");
        _tempProjectPath = Path.Combine(tempDir, "TestProject.csproj");
        
        File.WriteAllText(_tempSolutionPath, """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            VisualStudioVersion = 17.0.31903.59
            MinimumVisualStudioVersion = 10.0.40219.1
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "TestProject", "TestProject.csproj", "{12345678-1234-1234-1234-123456789012}"
            EndProject
            Global
                GlobalSection(SolutionConfigurationPlatforms) = preSolution
                    Debug|Any CPU = Debug|Any CPU
                    Release|Any CPU = Release|Any CPU
                EndGlobalSection
            EndGlobal
            """);
            
        File.WriteAllText(_tempProjectPath, """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>
            </Project>
            """);
    }

    public void Dispose()
    {
        if (File.Exists(_tempSolutionPath))
        {
            File.Delete(_tempSolutionPath);
        }
        if (File.Exists(_tempProjectPath))
        {
            File.Delete(_tempProjectPath);
        }
        var directory = Path.GetDirectoryName(_tempSolutionPath);
        if (directory != null && Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithValidSolution_ReturnsProjectDependencies()
    {
        // Act
        var result = await _analyzer.AnalyzeProjectAsync(_tempSolutionPath);

        // Assert
        result.Should().NotBeNull();
        result.Should().NotBeEmpty();
        result.Should().ContainSingle()
            .Which.ProjectName.Should().Be("TestProject");
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithProjectLoadError_LogsWarningAndContinues()
    {
        // Arrange
        var solutionPath = "invalid/path/to/solution.sln";

        // Act
        var result = await _analyzer.AnalyzeProjectAsync(solutionPath, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeProjectAsync_WithCancellation_StopsProcessing()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var result = await _analyzer.AnalyzeProjectAsync(_tempSolutionPath, cts.Token);

        // Assert
        result.Should().BeEmpty();
    }
} 