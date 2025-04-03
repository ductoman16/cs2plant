using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using cs2plant.Services;
using Xunit;

namespace cs2plant.Tests.Services;

public sealed class ClassAnalyzerTests : IDisposable
{
    private readonly ClassAnalyzer _analyzer;
    private readonly MSBuildProjectLoader _projectLoader;
    private readonly List<string> _tempFiles = new();

    public ClassAnalyzerTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _analyzer = new ClassAnalyzer(loggerFactory.CreateLogger<ClassAnalyzer>());
        _projectLoader = new MSBuildProjectLoader(loggerFactory.CreateLogger<MSBuildProjectLoader>());
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
            {
                File.Delete(file);
            }
        }
    }

    [Fact]
    public async Task AnalyzeClassesAsync_WithValidProject_ReturnsClassInfo()
    {
        // Arrange
        var (projectPath, codePath) = CreateTestProject();
        var project = _projectLoader.LoadProject(projectPath, CancellationToken.None);
        project.Should().NotBeNull();

        // Act
        var result = await _analyzer.AnalyzeClassesAsync(project!, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(1);

        var classInfo = result[0];
        classInfo.Name.Should().Be("TestClass");
        classInfo.Properties.Should().HaveCount(3);
        classInfo.Methods.Should().HaveCount(3);

        classInfo.Properties.Should().Contain(p => p.Name == "Id" && p.Type == "int" && p.HasGet && p.HasInit);
        classInfo.Properties.Should().Contain(p => p.Name == "Name" && p.Type == "string" && p.HasGet && p.HasSet);
        classInfo.Properties.Should().Contain(p => p.Name == "Description" && p.Type == "string" && p.HasGet);

        classInfo.Methods.Should().Contain(m => m.Name == "DoSomething" && m.ReturnType == "void" && !m.IsAsync && !m.IsStatic && !m.IsOverride);
        classInfo.Methods.Should().Contain(m => m.Name == "GetDataAsync" && m.ReturnType == "Task<string>" && m.IsAsync && !m.IsStatic && !m.IsOverride);
        classInfo.Methods.Should().Contain(m => m.Name == "Create" && m.ReturnType == "TestClass" && !m.IsAsync && m.IsStatic && !m.IsOverride);
    }

    [Fact]
    public async Task AnalyzeClassesAsync_WithInvalidSyntax_LogsWarningAndReturnsEmptyList()
    {
        // Arrange
        var (projectPath, codePath) = CreateTestProject("invalid c# code");
        var project = _projectLoader.LoadProject(projectPath, CancellationToken.None);
        project.Should().NotBeNull();

        // Act
        var result = await _analyzer.AnalyzeClassesAsync(project!, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task AnalyzeClassesAsync_WithNullCompilation_LogsWarningAndReturnsEmptyList()
    {
        // Arrange
        var (projectPath, _) = CreateTestProject();
        var project = _projectLoader.LoadProject(projectPath, CancellationToken.None);

        // Act
        var result = await _analyzer.AnalyzeClassesAsync(null!, CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    private (string ProjectPath, string CodePath) CreateTestProject(string? code = null)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);

        var projectPath = Path.Combine(tempDir, "TestProject.csproj");
        var codePath = Path.Combine(tempDir, "TestClass.cs");

        _tempFiles.Add(projectPath);
        _tempFiles.Add(codePath);

        File.WriteAllText(projectPath, """
            <Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                    <TargetFramework>net8.0</TargetFramework>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>
            </Project>
            """);

        code ??= """
            using System;
            using System.Threading.Tasks;

            namespace TestProject;

            public class TestClass
            {
                public int Id { get; init; }
                public string Name { get; set; } = string.Empty;
                public string Description { get; }

                public void DoSomething()
                {
                    Console.WriteLine("Doing something...");
                }

                public async Task<string> GetDataAsync()
                {
                    await Task.Delay(100);
                    return "Data";
                }

                public static TestClass Create()
                {
                    return new TestClass();
                }
            }
            """;

        File.WriteAllText(codePath, code);

        return (projectPath, codePath);
    }
} 