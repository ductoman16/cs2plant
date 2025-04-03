using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using cs2plant.Services;
using Xunit;

namespace cs2plant.Tests.Services;

public sealed class MSBuildProjectLoaderTests : IDisposable
{
    private readonly ILogger<MSBuildProjectLoader> _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<MSBuildProjectLoader>();
    private readonly MSBuildProjectLoader _loader;
    private readonly string _tempSolutionPath;
    private readonly string _tempProjectPath;
    private readonly string _tempProject2Path;
    private readonly string _tempProjectInSubdirPath;

    public MSBuildProjectLoaderTests()
    {
        _loader = new MSBuildProjectLoader(_logger);
        
        // Create temporary solution and project files
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        
        _tempSolutionPath = Path.Combine(tempDir, "Test.sln");
        _tempProjectPath = Path.Combine(tempDir, "TestProject.csproj");
        _tempProject2Path = Path.Combine(tempDir, "TestProject2.csproj");
        
        var subDir = Path.Combine(tempDir, "SubDir");
        Directory.CreateDirectory(subDir);
        _tempProjectInSubdirPath = Path.Combine(subDir, "TestProject3.csproj");
        
        File.WriteAllText(_tempSolutionPath, """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            VisualStudioVersion = 17.0.31903.59
            MinimumVisualStudioVersion = 10.0.40219.1
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "TestProject", "TestProject.csproj", "{12345678-1234-1234-1234-123456789012}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "TestProject2", "TestProject2.csproj", "{22345678-1234-1234-1234-123456789012}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "TestProject3", "SubDir\TestProject3.csproj", "{32345678-1234-1234-1234-123456789012}"
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

        File.WriteAllText(_tempProject2Path, """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>
            </Project>
            """);

        File.WriteAllText(_tempProjectInSubdirPath, """
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
        if (File.Exists(_tempProject2Path))
        {
            File.Delete(_tempProject2Path);
        }
        if (File.Exists(_tempProjectInSubdirPath))
        {
            File.Delete(_tempProjectInSubdirPath);
        }
        var directory = Path.GetDirectoryName(_tempSolutionPath);
        if (directory != null && Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void GetProjectsFromSolution_WithValidSolution_ReturnsProjectPaths()
    {
        // Act
        var result = _loader.GetProjectsFromSolution(_tempSolutionPath).ToList();

        // Assert
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().Contain(_tempProjectPath);
            result.Should().Contain(_tempProject2Path);
            result.Should().Contain(_tempProjectInSubdirPath);
        }
    }

    [Fact]
    public void GetProjectsFromSolution_WithDifferentProjectGuids_ReturnsProjectPaths()
    {
        // Arrange
        var solutionContent = """
            Microsoft Visual Studio Solution File, Format Version 12.00
            # Visual Studio Version 17
            VisualStudioVersion = 17.0.31903.59
            MinimumVisualStudioVersion = 10.0.40219.1
            Project("{9A19103F-16F7-4668-BE54-9A1E7A4F7556}") = "TestProject", "TestProject.csproj", "{12345678-1234-1234-1234-123456789012}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "TestProject2", "TestProject2.csproj", "{22345678-1234-1234-1234-123456789012}"
            EndProject
            Project("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "TestProject3", "SubDir\TestProject3.csproj", "{32345678-1234-1234-1234-123456789012}"
            EndProject
            Global
                GlobalSection(SolutionConfigurationPlatforms) = preSolution
                    Debug|Any CPU = Debug|Any CPU
                    Release|Any CPU = Release|Any CPU
                EndGlobalSection
            EndGlobal
            """;
        File.WriteAllText(_tempSolutionPath, solutionContent);

        // Act
        var result = _loader.GetProjectsFromSolution(_tempSolutionPath).ToList();

        // Assert
        using (new AssertionScope())
        {
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Should().Contain(_tempProjectPath);
            result.Should().Contain(_tempProject2Path);
            result.Should().Contain(_tempProjectInSubdirPath);
        }
    }

    [Fact]
    public void LoadProject_WithValidProject_ReturnsProject()
    {
        // Act
        var result = _loader.LoadProject(_tempProjectPath, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result?.FilePath.Should().Be(_tempProjectPath);
    }
} 