using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using cs2plant.Models;
using Xunit;
using cs2plant.Core.Services;
using cs2plant.Services;
using NSubstitute;

namespace cs2plant.Core.Tests.Services;

public sealed class ClassAnalyzerTests : IDisposable
{
    private readonly ILogger<ClassAnalyzer> _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<ClassAnalyzer>();
    private readonly MSBuildProjectLoader _projectLoader;
    private readonly ClassAnalyzer _analyzer;
    private readonly SolutionParser _solutionParser;
    private readonly string _projectPath;

    public ClassAnalyzerTests()
    {
        _solutionParser = new SolutionParser(LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SolutionParser>());
        _projectLoader = new MSBuildProjectLoader(
            LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<MSBuildProjectLoader>(),
            _solutionParser
        );
        _analyzer = new ClassAnalyzer(_logger);
        (_projectPath, _) = CreateTestProject();
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
        classInfo.Visibility.Should().Be(Visibility.Public);
        classInfo.IsSealed.Should().BeTrue();
        classInfo.IsRecord.Should().BeFalse();
        classInfo.IsAbstract.Should().BeFalse();
        classInfo.IsStatic.Should().BeFalse();
        classInfo.NestedClasses.Should().BeEmpty();

        classInfo.Properties.Should().HaveCount(3);
        classInfo.Methods.Should().HaveCount(3);

        // Property assertions with visibility
        classInfo.Properties.Should().Contain(p => 
            p.Name == "Id" && 
            p.Type == "int" && 
            p.Visibility == Visibility.Public &&
            !p.IsStatic &&
            !p.IsVirtual &&
            !p.IsOverride &&
            p.HasGet && 
            p.HasInit);

        classInfo.Properties.Should().Contain(p => 
            p.Name == "Name" && 
            p.Type == "string" && 
            p.Visibility == Visibility.Public &&
            !p.IsStatic &&
            !p.IsVirtual &&
            !p.IsOverride &&
            p.HasGet && 
            p.HasSet);

        classInfo.Properties.Should().Contain(p => 
            p.Name == "Description" && 
            p.Type == "string" && 
            p.Visibility == Visibility.Public &&
            !p.IsStatic &&
            !p.IsVirtual &&
            !p.IsOverride &&
            p.HasGet);

        // Method assertions with visibility
        classInfo.Methods.Should().Contain(m => 
            m.Name == "DoSomething" && 
            m.ReturnType == "void" && 
            m.Visibility == Visibility.Public &&
            !m.IsAsync && 
            !m.IsStatic && 
            !m.IsVirtual &&
            !m.IsOverride &&
            !m.IsAbstract);

        classInfo.Methods.Should().Contain(m => 
            m.Name == "GetDataAsync" && 
            m.ReturnType == "Task<string>" && 
            m.Visibility == Visibility.Public &&
            m.IsAsync && 
            !m.IsStatic && 
            !m.IsVirtual &&
            !m.IsOverride &&
            !m.IsAbstract);

        classInfo.Methods.Should().Contain(m => 
            m.Name == "Create" && 
            m.ReturnType == "TestClass" && 
            m.Visibility == Visibility.Public &&
            !m.IsAsync && 
            m.IsStatic && 
            !m.IsVirtual &&
            !m.IsOverride &&
            !m.IsAbstract);
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

    [Fact]
    public async Task AnalyzeClassesAsync_WithGenericClass_ExtractsTypeParameters()
    {
        // Arrange
        var code = @"
            namespace TestProject;
            public class GenericService<T> where T : class, new()
            {
                public T Data { get; set; }
                public async Task<T> GetDataAsync<U>(U input) where U : struct
                {
                    return default;
                }
            }";

        var (projectPath, codePath) = CreateTestProject(code);
        var project = _projectLoader.LoadProject(projectPath, CancellationToken.None);
        project.Should().NotBeNull();

        // Act
        var result = await _analyzer.AnalyzeClassesAsync(project!, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCount(1);

        var classInfo = result[0];
        classInfo.Name.Should().Be("GenericService");
        classInfo.TypeParameters.Should().HaveCount(1);

        var typeParam = classInfo.TypeParameters[0];
        typeParam.Name.Should().Be("T");
        typeParam.Constraints.Should().Contain("class");
        typeParam.Constraints.Should().Contain("new()");

        var method = classInfo.Methods.Should().ContainSingle().Subject;
        method.Name.Should().Be("GetDataAsync");
        method.TypeParameters.Should().HaveCount(1);

        var methodTypeParam = method.TypeParameters[0];
        methodTypeParam.Name.Should().Be("U");
        methodTypeParam.Constraints.Should().Contain("struct");
    }

    [Fact]
    public async Task AnalyzeClassesAsync_WithInheritance_DetectsRelationships()
    {
        // Arrange
        var code = @"
            namespace TestProject;
            public interface IService { }
            public abstract class ServiceBase { }
            public class ConcreteService : ServiceBase, IService
            {
                private readonly HelperComponent _helper;
                public DataElement Data { get; set; }
                public OtherService Reference { get; set; }
            }
            public class HelperComponent { }
            public class DataElement { }
            public class OtherService { }";

        var (projectPath, codePath) = CreateTestProject(code);
        var project = _projectLoader.LoadProject(projectPath, CancellationToken.None);
        project.Should().NotBeNull();

        // Act
        var result = await _analyzer.AnalyzeClassesAsync(project!, CancellationToken.None);

        // Assert
        result.Should().NotBeEmpty();
        var service = result.Should().Contain(c => c.Name == "ConcreteService").Subject;

        service.Relationships.Should().HaveCount(5);
        service.Relationships.Should().Contain(r => r.TargetType == "ServiceBase" && r.Type == RelationshipType.Inheritance);
        service.Relationships.Should().Contain(r => r.TargetType == "IService" && r.Type == RelationshipType.Implementation);
        service.Relationships.Should().Contain(r => r.TargetType == "HelperComponent" && r.Type == RelationshipType.Composition);
        service.Relationships.Should().Contain(r => r.TargetType == "DataElement" && r.Type == RelationshipType.Composition);
        service.Relationships.Should().Contain(r => r.TargetType == "OtherService" && r.Type == RelationshipType.Aggregation);
    }

    [Fact]
    public async Task AnalyzeClassesAsync_WithVisibilityModifiers_DetectsCorrectVisibility()
    {
        // Arrange
        var code = @"
            namespace TestProject;
            public class PublicClass
            {
                public string PublicProperty { get; set; }
                private string PrivateProperty { get; set; }
                protected string ProtectedProperty { get; set; }
                internal string InternalProperty { get; set; }
                protected internal string ProtectedInternalProperty { get; set; }
                private protected string PrivateProtectedProperty { get; set; }

                public void PublicMethod() { }
                private void PrivateMethod() { }
                protected void ProtectedMethod() { }
                internal void InternalMethod() { }
                protected internal void ProtectedInternalMethod() { }
                private protected void PrivateProtectedMethod() { }
            }

            internal class InternalClass { }
            
            file class FileClass { }";

        var (projectPath, codePath) = CreateTestProject(code);
        var project = _projectLoader.LoadProject(projectPath, CancellationToken.None);
        project.Should().NotBeNull();

        // Act
        var result = await _analyzer.AnalyzeClassesAsync(project!, CancellationToken.None);

        // Assert
        result.Should().HaveCount(3);

        var publicClass = result.Should().Contain(c => c.Name == "PublicClass").Subject;
        var internalClass = result.Should().Contain(c => c.Name == "InternalClass").Subject;
        var fileClass = result.Should().Contain(c => c.Name == "FileClass").Subject;

        // Class visibility
        publicClass.Visibility.Should().Be(Visibility.Public);
        internalClass.Visibility.Should().Be(Visibility.Internal);
        fileClass.Visibility.Should().Be(Visibility.Private);

        // Property visibility
        publicClass.Properties.Should().Contain(p => p.Name == "PublicProperty" && p.Visibility == Visibility.Public);
        publicClass.Properties.Should().Contain(p => p.Name == "PrivateProperty" && p.Visibility == Visibility.Private);
        publicClass.Properties.Should().Contain(p => p.Name == "ProtectedProperty" && p.Visibility == Visibility.Protected);
        publicClass.Properties.Should().Contain(p => p.Name == "InternalProperty" && p.Visibility == Visibility.Internal);
        publicClass.Properties.Should().Contain(p => p.Name == "ProtectedInternalProperty" && p.Visibility == Visibility.ProtectedInternal);
        publicClass.Properties.Should().Contain(p => p.Name == "PrivateProtectedProperty" && p.Visibility == Visibility.PrivateProtected);

        // Method visibility
        publicClass.Methods.Should().Contain(m => m.Name == "PublicMethod" && m.Visibility == Visibility.Public);
        publicClass.Methods.Should().Contain(m => m.Name == "PrivateMethod" && m.Visibility == Visibility.Private);
        publicClass.Methods.Should().Contain(m => m.Name == "ProtectedMethod" && m.Visibility == Visibility.Protected);
        publicClass.Methods.Should().Contain(m => m.Name == "InternalMethod" && m.Visibility == Visibility.Internal);
        publicClass.Methods.Should().Contain(m => m.Name == "ProtectedInternalMethod" && m.Visibility == Visibility.ProtectedInternal);
        publicClass.Methods.Should().Contain(m => m.Name == "PrivateProtectedMethod" && m.Visibility == Visibility.PrivateProtected);
    }

    [Fact]
    public async Task AnalyzeClassesAsync_WithNestedClasses_DetectsNestedClassesCorrectly()
    {
        // Arrange
        var code = @"
            namespace TestProject;
            public class OuterClass
            {
                private class PrivateNestedClass
                {
                    public string Property { get; set; }
                    public void Method() { }
                }

                protected class ProtectedNestedClass
                {
                    private class DeeplyNestedClass
                    {
                        public int Value { get; set; }
                    }

                    public DeeplyNestedClass Data { get; set; }
                }

                public string OuterProperty { get; set; }
            }";

        var (projectPath, codePath) = CreateTestProject(code);
        var project = _projectLoader.LoadProject(projectPath, CancellationToken.None);
        project.Should().NotBeNull();

        // Act
        var result = await _analyzer.AnalyzeClassesAsync(project!, CancellationToken.None);

        // Assert
        result.Should().HaveCount(1);

        var outerClass = result[0];
        outerClass.Name.Should().Be("OuterClass");
        outerClass.Visibility.Should().Be(Visibility.Public);
        outerClass.Properties.Should().ContainSingle(p => p.Name == "OuterProperty");

        // Nested classes
        outerClass.NestedClasses.Should().HaveCount(2);

        var privateNestedClass = outerClass.NestedClasses.Should().Contain(c => c.Name == "PrivateNestedClass").Subject;
        privateNestedClass.Visibility.Should().Be(Visibility.Private);
        privateNestedClass.Properties.Should().ContainSingle(p => p.Name == "Property");
        privateNestedClass.Methods.Should().ContainSingle(m => m.Name == "Method");

        var protectedNestedClass = outerClass.NestedClasses.Should().Contain(c => c.Name == "ProtectedNestedClass").Subject;
        protectedNestedClass.Visibility.Should().Be(Visibility.Protected);
        protectedNestedClass.Properties.Should().ContainSingle(p => p.Name == "Data");

        // Deeply nested class
        protectedNestedClass.NestedClasses.Should().HaveCount(1);
        var deeplyNestedClass = protectedNestedClass.NestedClasses[0];
        deeplyNestedClass.Name.Should().Be("DeeplyNestedClass");
        deeplyNestedClass.Visibility.Should().Be(Visibility.Private);
        deeplyNestedClass.Properties.Should().ContainSingle(p => p.Name == "Value");
    }

    private (string ProjectPath, string CodePath) CreateTestProject(string code = "")
    {
        var projectDir = Path.Combine(Path.GetTempPath(), $"test-project-{Guid.NewGuid()}");
        Directory.CreateDirectory(projectDir);

        var projectPath = Path.Combine(projectDir, "TestProject.csproj");
        var codePath = Path.Combine(projectDir, "TestClass.cs");

        File.WriteAllText(projectPath, @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>");

        if (!string.IsNullOrEmpty(code))
        {
            File.WriteAllText(codePath, code);
        }
        else
        {
            File.WriteAllText(codePath, @"
namespace TestProject;
public sealed class TestClass
{
    public int Id { get; init; }
    public string Name { get; set; }
    public string Description { get; }

    public void DoSomething() { }
    public async Task<string> GetDataAsync(string input) { return input; }
    public static TestClass Create() { return new TestClass(); }
}");
        }

        return (projectPath, codePath);
    }

    public void Dispose()
    {
        var projectDir = Path.GetDirectoryName(_projectPath);
        if (projectDir != null && Directory.Exists(projectDir))
        {
            Directory.Delete(projectDir, true);
        }
    }
} 