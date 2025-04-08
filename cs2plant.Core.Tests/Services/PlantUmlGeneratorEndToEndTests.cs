using FluentAssertions;
using Microsoft.Extensions.Logging;
using cs2plant.Models;
using cs2plant.Services;
using Xunit.Abstractions;
using NSubstitute;

namespace cs2plant.Core.Tests.Services;

[Trait("Category", "Integration")]
public class PlantUmlGeneratorEndToEndTests : IDisposable
{
    private readonly ILogger<PlantUmlGenerator> _logger;
    private readonly PlantUmlGenerator _generator;
    private readonly string _tempFilePath;
    private readonly ITestOutputHelper _output;
    private readonly string _plantUmlJarPath;

    public PlantUmlGeneratorEndToEndTests(ITestOutputHelper output)
    {
        _output = output;
        _logger = Substitute.For<ILogger<PlantUmlGenerator>>();
        _generator = new PlantUmlGenerator(_logger);
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"test-diagram-{Guid.NewGuid()}.puml");
        _plantUmlJarPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "cs2plant.Core", "Resources", "plantuml.jar");
    }

    [Fact]
    public void GenerateDiagram_ShouldContainCompleteUmlStructure()
    {
        // Arrange
        var dependencies = new List<ProjectDependency>
        {
            new ProjectDependency(
                ProjectName: "cs2plant.Core",
                ProjectPath: "/path/to/core",
                TargetFramework: "net8.0",
                PackageReferences: new[] { "Microsoft.Extensions.Logging" },
                ProjectReferences: Array.Empty<string>(),
                Classes: new[]
                {
                    new ClassInfo(
                        Name: "TestService",
                        Namespace: "cs2plant.Core.Services",
                        Visibility: Visibility.Public,
                        IsSealed: true,
                        IsRecord: false,
                        IsAbstract: false,
                        IsStatic: false,
                        BaseTypes: Array.Empty<string>(),
                        Properties: new[]
                        {
                            new PropertyInfo(
                                Name: "Name",
                                Type: "string",
                                Visibility: Visibility.Public,
                                IsStatic: false,
                                IsVirtual: false,
                                IsOverride: false,
                                HasGet: true,
                                HasSet: true,
                                HasInit: false)
                        },
                        Methods: Array.Empty<MethodInfo>(),
                        TypeParameters: Array.Empty<TypeParameterInfo>(),
                        Relationships: Array.Empty<RelationshipInfo>(),
                        NestedClasses: Array.Empty<ClassInfo>())
                })
        };

        // Act
        var result = _generator.GenerateDiagram(dependencies);

        // Assert
        result.Should().Contain("@startuml");
        result.Should().Contain("package \"Project Dependencies\" {");
        result.Should().Contain("component \"cs2plant.Core\"");
        result.Should().Contain("note right");
        result.Should().Contain("Packages:");
        result.Should().Contain("- Microsoft.Extensions.Logging");
        result.Should().Contain("end note");
        result.Should().Contain("}");
        result.Should().Contain("@enduml");
        result.Should().Contain("namespace cs2plant.Core.Services {");
        result.Should().Contain("class TestService <<sealed>> {");
        result.Should().Contain("+ Name : string { get set }");
        result.Should().Contain("}");
        result.Should().EndWith("@enduml");
    }

    [Fact]
    public void GenerateDiagram_WithComplexProject_GeneratesValidPlantUml()
    {
        // Arrange
        var dependencies = new List<ProjectDependency>
        {
            new ProjectDependency(
                ProjectName: "cs2plant.Core",
                ProjectPath: "/path/to/core",
                TargetFramework: "net8.0",
                PackageReferences: new[] { "Microsoft.Extensions.Logging" },
                ProjectReferences: new[] { "cs2plant.Web" },
                Classes: new[]
                {
                    new ClassInfo(
                        Name: "TestService",
                        Namespace: "cs2plant.Core.Services",
                        Visibility: Visibility.Public,
                        IsSealed: true,
                        IsRecord: false,
                        IsAbstract: false,
                        IsStatic: false,
                        BaseTypes: Array.Empty<string>(),
                        Properties: new[]
                        {
                            new PropertyInfo(
                                Name: "Name",
                                Type: "string",
                                Visibility: Visibility.Public,
                                IsStatic: false,
                                IsVirtual: false,
                                IsOverride: false,
                                HasGet: true,
                                HasSet: true,
                                HasInit: false)
                        },
                        Methods: Array.Empty<MethodInfo>(),
                        TypeParameters: Array.Empty<TypeParameterInfo>(),
                        Relationships: Array.Empty<RelationshipInfo>(),
                        NestedClasses: Array.Empty<ClassInfo>())
                }),
            new ProjectDependency(
                ProjectName: "cs2plant.Web",
                ProjectPath: "/path/to/web",
                TargetFramework: "net8.0",
                PackageReferences: new[] { "Microsoft.AspNetCore" },
                ProjectReferences: Array.Empty<string>(),
                Classes: new[]
                {
                    new ClassInfo(
                        Name: "Startup",
                        Namespace: "cs2plant.Web",
                        Visibility: Visibility.Public,
                        IsSealed: false,
                        IsRecord: false,
                        IsAbstract: false,
                        IsStatic: false,
                        BaseTypes: Array.Empty<string>(),
                        Properties: new[]
                        {
                            new PropertyInfo(
                                Name: "Configuration",
                                Type: "IConfiguration",
                                Visibility: Visibility.Public,
                                IsStatic: false,
                                IsVirtual: false,
                                IsOverride: false,
                                HasGet: true,
                                HasSet: true,
                                HasInit: false)
                        },
                        Methods: Array.Empty<MethodInfo>(),
                        TypeParameters: Array.Empty<TypeParameterInfo>(),
                        Relationships: Array.Empty<RelationshipInfo>(),
                        NestedClasses: Array.Empty<ClassInfo>())
                })
        };

        // Act
        var result = _generator.GenerateDiagram(dependencies);

        // Assert
        result.Should().Contain("@startuml");
        result.Should().Contain("package \"Project Dependencies\" {");
        result.Should().Contain("component \"cs2plant.Core\"");
        result.Should().Contain("component \"cs2plant.Web\"");
        result.Should().Contain("\"cs2plant.Core\" --> \"cs2plant.Web\"");
        result.Should().Contain("note right");
        result.Should().Contain("Packages:");
        result.Should().Contain("- Microsoft.Extensions.Logging");
        result.Should().Contain("end note");
        result.Should().Contain("}");
        result.Should().Contain("@enduml");
        result.Should().Contain("namespace cs2plant.Core.Services {");
        result.Should().Contain("class TestService <<sealed>> {");
        result.Should().Contain("+ Name : string { get set }");
        result.Should().Contain("}");
        result.Should().Contain("namespace cs2plant.Web {");
        result.Should().Contain("class Startup {");
        result.Should().Contain("+ Configuration : IConfiguration { get set }");
        result.Should().Contain("}");
        result.Should().EndWith("@enduml");
    }

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }
} 