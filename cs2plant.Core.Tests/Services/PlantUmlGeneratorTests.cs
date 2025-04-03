using FluentAssertions;
using Microsoft.Extensions.Logging;
using cs2plant.Models;
using cs2plant.Services;
using Xunit;

namespace cs2plant.Tests.Services;

public sealed class PlantUmlGeneratorTests
{
    private readonly PlantUmlGenerator _generator;

    public PlantUmlGeneratorTests()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        _generator = new PlantUmlGenerator(loggerFactory.CreateLogger<PlantUmlGenerator>());
    }

    [Fact]
    public void GenerateDiagram_WithNullDependencies_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<ProjectDependency>? dependencies = null;

        // Act
        var act = () => _generator.GenerateDiagram(dependencies!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("dependencies");
    }

    [Fact]
    public void GenerateDiagram_WithEmptyDependencies_GeneratesValidPlantUml()
    {
        // Arrange
        var dependencies = Array.Empty<ProjectDependency>();

        // Act
        var result = _generator.GenerateDiagram(dependencies);

        // Assert
        result.Should().NotBeNullOrWhiteSpace();
        result.Should().StartWith("@startuml");
        result.Should().EndWith("@enduml\n");
    }

    [Fact]
    public void GenerateDiagram_WithSingleProject_GeneratesValidPlantUml()
    {
        // Arrange
        var dependencies = new[]
        {
            new ProjectDependency(
                "TestProject",
                "/path/to/project",
                "net8.0",
                new[] { "Package1@1.0.0", "Package2@2.0.0" },
                Array.Empty<string>(),
                Array.Empty<ClassInfo>())
        };

        // Act
        var result = _generator.GenerateDiagram(dependencies);

        // Assert
        result.Should().Contain("@startuml");
        result.Should().Contain("skinparam componentStyle rectangle");
        result.Should().Contain("[TestProject]");
        result.Should().Contain("note bottom of [TestProject]");
        result.Should().Contain("- Package1@1.0.0");
        result.Should().Contain("- Package2@2.0.0");
        result.Should().Contain("@enduml");
    }

    [Fact]
    public void GenerateDiagram_WithProjectReferences_GeneratesValidPlantUml()
    {
        // Arrange
        var dependencies = new[]
        {
            new ProjectDependency(
                "Project.A",
                "/path/to/a",
                "net8.0",
                Array.Empty<string>(),
                new[] { "Project.B" },
                Array.Empty<ClassInfo>()),
            new ProjectDependency(
                "Project.B",
                "/path/to/b",
                "net8.0",
                Array.Empty<string>(),
                Array.Empty<string>(),
                Array.Empty<ClassInfo>())
        };

        // Act
        var result = _generator.GenerateDiagram(dependencies);

        // Assert
        result.Should().Contain("@startuml");
        result.Should().Contain("[Project.A]");
        result.Should().Contain("[Project.B]");
        result.Should().Contain("[Project.B] <-- [Project.A]");
        result.Should().Contain("@enduml");
    }

    [Fact]
    public void GenerateDiagram_WithClassInfo_GeneratesClassDiagram()
    {
        // Arrange
        var dependencies = new[]
        {
            new ProjectDependency(
                "TestProject",
                "/path/to/project",
                "net8.0",
                Array.Empty<string>(),
                Array.Empty<string>(),
                new[]
                {
                    new ClassInfo(
                        "TestService",
                        "TestProject.Services",
                        true, // IsSealed
                        false, // IsRecord
                        new[] { "ITestService" },
                        new[]
                        {
                            new PropertyInfo("Id", "int", true, false, true),
                            new PropertyInfo("Name", "string", true, true, false)
                        },
                        new[]
                        {
                            new MethodInfo("ProcessData", "Task<bool>", true, false, false,
                                new[] { new ParameterInfo("data", "string") })
                        })
                })
        };

        // Act
        var result = _generator.GenerateDiagram(dependencies);

        // Assert
        result.Should().Contain("class TestService <<sealed>> {");
        result.Should().Contain("  + Id: int { get init }");
        result.Should().Contain("  + Name: string { get set }");
        result.Should().Contain("  + async ProcessData(data: string): Task<bool>");
        result.Should().Contain("}");
        result.Should().Contain("interface ITestService");
        result.Should().Contain("ITestService <|.. TestService");
        result.Should().Contain("namespace TestProject.Services {");
    }
} 