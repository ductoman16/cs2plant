using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using cs2plant.Models;

namespace cs2plant.Services;

/// <summary>
/// Analyzes C# syntax trees to extract information about properties and methods.
/// </summary>
public static class SyntaxAnalyzer
{
    /// <summary>
    /// Gets the base types of a class declaration.
    /// </summary>
    public static List<string> GetBaseTypes(ClassDeclarationSyntax classDeclaration) =>
        classDeclaration.BaseList?.Types
            .Select(t => t.Type.ToString())
            .ToList() ?? new List<string>();

    /// <summary>
    /// Gets the properties of a class declaration.
    /// </summary>
    public static List<PropertyInfo> GetProperties(ClassDeclarationSyntax classDeclaration) =>
        classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Select(p => new PropertyInfo(
                Name: p.Identifier.Text,
                Type: p.Type.ToString(),
                HasGet: p.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) ?? false,
                HasSet: p.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) ?? false,
                HasInit: p.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.InitAccessorDeclaration)) ?? false))
            .ToList();

    /// <summary>
    /// Gets the methods of a class declaration.
    /// </summary>
    public static List<MethodInfo> GetMethods(ClassDeclarationSyntax classDeclaration) =>
        classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Select(m => new MethodInfo(
                Name: m.Identifier.Text,
                ReturnType: m.ReturnType.ToString(),
                IsAsync: m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.AsyncKeyword)),
                IsStatic: m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)),
                IsOverride: m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.OverrideKeyword)),
                Parameters: m.ParameterList.Parameters
                    .Select(p => new ParameterInfo(
                        Name: p.Identifier.Text,
                        Type: p.Type?.ToString() ?? "object"))
                    .ToList()))
            .ToList();
} 