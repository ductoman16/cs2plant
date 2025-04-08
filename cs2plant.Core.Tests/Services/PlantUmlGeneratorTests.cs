using FluentAssertions;
using Microsoft.Extensions.Logging;
using cs2plant.Models;
using cs2plant.Services;
using Xunit;
using System.Collections.Generic;
using NSubstitute;

namespace cs2plant.Tests.Services;

public sealed class PlantUmlGeneratorTests
{
    private readonly ILogger<PlantUmlGenerator> _logger;
    private readonly PlantUmlGenerator _generator;

    public PlantUmlGeneratorTests()
    {
        _logger = Substitute.For<ILogger<PlantUmlGenerator>>();
        _generator = new PlantUmlGenerator(_logger);
    }

    [Fact]
    public void GenerateDiagram_WithNullDependencies_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<ProjectDependency>? dependencies = null;

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
        result.Should().Contain("\"TestProject\"");
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
        result.Should().Contain("\"Project.A\"");
        result.Should().Contain("\"Project.B\"");
        result.Should().Contain("\"Project.A\" --> \"Project.B\"");
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
                        Visibility.Public,
                        true, // IsSealed
                        false, // IsRecord
                        false, // IsAbstract
                        false, // IsStatic
                        new[] { "ITestService" },
                        new[]
                        {
                            new PropertyInfo("Id", "int", Visibility.Public, false, false, false, true, false, true),
                            new PropertyInfo("Name", "string", Visibility.Public, false, false, false, true, true, false)
                        },
                        new[]
                        {
                            new MethodInfo("ProcessData", "Task<bool>", Visibility.Public, true, false, false, false, false,
                                new[] { new ParameterInfo("data", "string") },
                                Array.Empty<TypeParameterInfo>())
                        },
                        Array.Empty<TypeParameterInfo>(),
                        new[] { new RelationshipInfo("ITestService", RelationshipType.Implementation) },
                        Array.Empty<ClassInfo>())
                })
        };

        // Act
        var result = _generator.GenerateDiagram(dependencies);

        // Assert
        result.Should().Contain("class TestService <<sealed>>  {");
        result.Should().Contain("  + Id: int { get init }");
        result.Should().Contain("  + Name: string { get set }");
        result.Should().Contain("  + async ProcessData(data: string): Task<bool>");
        result.Should().Contain("}");
        result.Should().Contain("interface ITestService");
        result.Should().Contain("TestService --|> ITestService");
        result.Should().Contain("namespace TestProject.Services {");
    }

    [Fact]
    public void GenerateDiagram_WithVisibilityModifiers_GeneratesCorrectSymbols()
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
                        "TestClass",
                        "TestProject",
                        Visibility.Public,
                        false, // IsSealed
                        false, // IsRecord
                        false, // IsAbstract
                        false, // IsStatic
                        Array.Empty<string>(),
                        new[]
                        {
                            new PropertyInfo("PublicProp", "string", Visibility.Public, false, false, false, true, false, false),
                            new PropertyInfo("PrivateProp", "string", Visibility.Private, false, false, false, true, false, false),
                            new PropertyInfo("ProtectedProp", "string", Visibility.Protected, false, false, false, true, false, false),
                            new PropertyInfo("InternalProp", "string", Visibility.Internal, false, false, false, true, false, false),
                            new PropertyInfo("ProtectedInternalProp", "string", Visibility.ProtectedInternal, false, false, false, true, false, false),
                            new PropertyInfo("PrivateProtectedProp", "string", Visibility.PrivateProtected, false, false, false, true, false, false)
                        },
                        new[]
                        {
                            new MethodInfo("PublicMethod", "void", Visibility.Public, false, false, false, false, false, Array.Empty<ParameterInfo>(), Array.Empty<TypeParameterInfo>()),
                            new MethodInfo("PrivateMethod", "void", Visibility.Private, false, false, false, false, false, Array.Empty<ParameterInfo>(), Array.Empty<TypeParameterInfo>()),
                            new MethodInfo("ProtectedMethod", "void", Visibility.Protected, false, false, false, false, false, Array.Empty<ParameterInfo>(), Array.Empty<TypeParameterInfo>()),
                            new MethodInfo("InternalMethod", "void", Visibility.Internal, false, false, false, false, false, Array.Empty<ParameterInfo>(), Array.Empty<TypeParameterInfo>()),
                            new MethodInfo("ProtectedInternalMethod", "void", Visibility.ProtectedInternal, false, false, false, false, false, Array.Empty<ParameterInfo>(), Array.Empty<TypeParameterInfo>()),
                            new MethodInfo("PrivateProtectedMethod", "void", Visibility.PrivateProtected, false, false, false, false, false, Array.Empty<ParameterInfo>(), Array.Empty<TypeParameterInfo>())
                        },
                        Array.Empty<TypeParameterInfo>(),
                        Array.Empty<RelationshipInfo>(),
                        Array.Empty<ClassInfo>())
                })
        };

        // Act
        var result = _generator.GenerateDiagram(dependencies);

        // Assert
        result.Should().Contain("class TestClass  {");
        result.Should().Contain("  + PublicProp: string");
        result.Should().Contain("  - PrivateProp: string");
        result.Should().Contain("  # ProtectedProp: string");
        result.Should().Contain("  ~ InternalProp: string");
        result.Should().Contain("  # ProtectedInternalProp: string");
        result.Should().Contain("  -# PrivateProtectedProp: string");
        result.Should().Contain("  + PublicMethod(): void");
        result.Should().Contain("  - PrivateMethod(): void");
        result.Should().Contain("  # ProtectedMethod(): void");
        result.Should().Contain("  ~ InternalMethod(): void");
        result.Should().Contain("  # ProtectedInternalMethod(): void");
        result.Should().Contain("  -# PrivateProtectedMethod(): void");
    }

    [Fact]
    public void GenerateDiagram_WithNestedClasses_GeneratesCorrectHierarchy()
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
                        "OuterClass",
                        "TestProject",
                        Visibility.Public,
                        false, // IsSealed
                        false, // IsRecord
                        false, // IsAbstract
                        false, // IsStatic
                        Array.Empty<string>(),
                        new[]
                        {
                            new PropertyInfo("OuterProp", "string", Visibility.Public, false, false, false, true, false, false)
                        },
                        Array.Empty<MethodInfo>(),
                        Array.Empty<TypeParameterInfo>(),
                        Array.Empty<RelationshipInfo>(),
                        new[]
                        {
                            new ClassInfo(
                                "NestedClass",
                                "TestProject",
                                Visibility.Public,
                                false, // IsSealed
                                false, // IsRecord
                                false, // IsAbstract
                                false, // IsStatic
                                Array.Empty<string>(),
                                new[]
                                {
                                    new PropertyInfo("NestedProp", "string", Visibility.Public, false, false, false, true, false, false)
                                },
                                Array.Empty<MethodInfo>(),
                                Array.Empty<TypeParameterInfo>(),
                                Array.Empty<RelationshipInfo>(),
                                new[]
                                {
                                    new ClassInfo(
                                        "DeeplyNestedClass",
                                        "TestProject",
                                        Visibility.Private,
                                        false, // IsSealed
                                        false, // IsRecord
                                        false, // IsAbstract
                                        false, // IsStatic
                                        Array.Empty<string>(),
                                        new[]
                                        {
                                            new PropertyInfo("DeeplyNestedProp", "string", Visibility.Public, false, false, false, true, false, false)
                                        },
                                        Array.Empty<MethodInfo>(),
                                        Array.Empty<TypeParameterInfo>(),
                                        Array.Empty<RelationshipInfo>(),
                                        Array.Empty<ClassInfo>())
                                })
                        })
                })
        };

        // Act
        var result = _generator.GenerateDiagram(dependencies);

        // Assert
        result.Should().Contain("class OuterClass  {");
        result.Should().Contain("  + OuterProp: string");
        result.Should().Contain("class NestedClass  {");
        result.Should().Contain("  + NestedProp: string");
        result.Should().Contain("class DeeplyNestedClass  {");
        result.Should().Contain("  + DeeplyNestedProp: string");
        result.Should().Contain("OuterClass +-- NestedClass");
        result.Should().Contain("NestedClass +-- DeeplyNestedClass");
    }
} 