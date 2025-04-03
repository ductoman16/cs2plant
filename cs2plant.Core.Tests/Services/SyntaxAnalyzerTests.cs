using FluentAssertions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using cs2plant.Services;
using Xunit;

namespace cs2plant.Tests.Services;

public class SyntaxAnalyzerTests
{
    [Fact]
    public void GetBaseTypes_WithNoBaseTypes_ReturnsEmptyList()
    {
        // Arrange
        var classDeclaration = ParseClass("public class TestClass { }");

        // Act
        var result = SyntaxAnalyzer.GetBaseTypes(classDeclaration);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetBaseTypes_WithBaseTypes_ReturnsBaseTypes()
    {
        // Arrange
        var classDeclaration = ParseClass("public class TestClass : IDisposable, BaseClass { }");

        // Act
        var result = SyntaxAnalyzer.GetBaseTypes(classDeclaration);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("IDisposable");
        result.Should().Contain("BaseClass");
    }

    [Fact]
    public void GetProperties_WithNoProperties_ReturnsEmptyList()
    {
        // Arrange
        var classDeclaration = ParseClass("public class TestClass { }");

        // Act
        var result = SyntaxAnalyzer.GetProperties(classDeclaration);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetProperties_WithProperties_ReturnsPropertyInfo()
    {
        // Arrange
        var classDeclaration = ParseClass(@"
            public class TestClass 
            { 
                public string Name { get; set; }
                public int Age { get; init; }
                private bool IsActive { get; }
            }");

        // Act
        var result = SyntaxAnalyzer.GetProperties(classDeclaration);

        // Assert
        result.Should().HaveCount(3);
        
        result[0].Name.Should().Be("Name");
        result[0].Type.Should().Be("string");
        result[0].HasGet.Should().BeTrue();
        result[0].HasSet.Should().BeTrue();
        result[0].HasInit.Should().BeFalse();

        result[1].Name.Should().Be("Age");
        result[1].Type.Should().Be("int");
        result[1].HasGet.Should().BeTrue();
        result[1].HasSet.Should().BeFalse();
        result[1].HasInit.Should().BeTrue();

        result[2].Name.Should().Be("IsActive");
        result[2].Type.Should().Be("bool");
        result[2].HasGet.Should().BeTrue();
        result[2].HasSet.Should().BeFalse();
        result[2].HasInit.Should().BeFalse();
    }

    [Fact]
    public void GetMethods_WithNoMethods_ReturnsEmptyList()
    {
        // Arrange
        var classDeclaration = ParseClass("public class TestClass { }");

        // Act
        var result = SyntaxAnalyzer.GetMethods(classDeclaration);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetMethods_WithMethods_ReturnsMethodInfo()
    {
        // Arrange
        var classDeclaration = ParseClass(@"
            public class TestClass 
            { 
                public async Task DoSomethingAsync(string input, int count) { }
                private static int Calculate(double value) { }
                protected override void OnDispose() { }
            }");

        // Act
        var result = SyntaxAnalyzer.GetMethods(classDeclaration);

        // Assert
        result.Should().HaveCount(3);
        
        result[0].Name.Should().Be("DoSomethingAsync");
        result[0].ReturnType.Should().Be("Task");
        result[0].IsAsync.Should().BeTrue();
        result[0].IsStatic.Should().BeFalse();
        result[0].IsOverride.Should().BeFalse();
        result[0].Parameters.Should().HaveCount(2);
        result[0].Parameters[0].Name.Should().Be("input");
        result[0].Parameters[0].Type.Should().Be("string");
        result[0].Parameters[1].Name.Should().Be("count");
        result[0].Parameters[1].Type.Should().Be("int");

        result[1].Name.Should().Be("Calculate");
        result[1].ReturnType.Should().Be("int");
        result[1].IsAsync.Should().BeFalse();
        result[1].IsStatic.Should().BeTrue();
        result[1].IsOverride.Should().BeFalse();
        result[1].Parameters.Should().HaveCount(1);
        result[1].Parameters[0].Name.Should().Be("value");
        result[1].Parameters[0].Type.Should().Be("double");

        result[2].Name.Should().Be("OnDispose");
        result[2].ReturnType.Should().Be("void");
        result[2].IsAsync.Should().BeFalse();
        result[2].IsStatic.Should().BeFalse();
        result[2].IsOverride.Should().BeTrue();
        result[2].Parameters.Should().BeEmpty();
    }

    private static ClassDeclarationSyntax ParseClass(string code)
    {
        var tree = CSharpSyntaxTree.ParseText(code);
        return tree.GetRoot()
            .DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .First();
    }
} 