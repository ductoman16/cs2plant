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
                Visibility: GetVisibility(p.Modifiers),
                IsStatic: p.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)),
                IsVirtual: p.Modifiers.Any(m => m.IsKind(SyntaxKind.VirtualKeyword)),
                IsOverride: p.Modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword)),
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
                Visibility: GetVisibility(m.Modifiers),
                IsAsync: m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.AsyncKeyword)),
                IsStatic: m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)),
                IsVirtual: m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.VirtualKeyword)),
                IsOverride: m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.OverrideKeyword)),
                IsAbstract: m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.AbstractKeyword)),
                Parameters: m.ParameterList.Parameters
                    .Select(p => new ParameterInfo(
                        Name: p.Identifier.Text,
                        Type: p.Type?.ToString() ?? "object"))
                    .ToList(),
                TypeParameters: GetTypeParameters(m.TypeParameterList)))
            .ToList();

    private static List<TypeParameterInfo> GetTypeParameters(TypeParameterListSyntax? typeParameterList)
    {
        if (typeParameterList == null)
        {
            return new List<TypeParameterInfo>();
        }

        return typeParameterList.Parameters
            .Select(tp => new TypeParameterInfo(
                Name: tp.Identifier.Text,
                Constraints: new List<string>()))
            .ToList();
    }

    private static Visibility GetVisibility(SyntaxTokenList modifiers)
    {
        var isPublic = modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword));
        var isPrivate = modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword));
        var isProtected = modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword));
        var isInternal = modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword));

        return (isPublic, isPrivate, isProtected, isInternal) switch
        {
            (true, _, _, _) => Visibility.Public,
            (_, _, true, true) => Visibility.ProtectedInternal,
            (_, true, true, _) => Visibility.PrivateProtected,
            (_, _, true, _) => Visibility.Protected,
            (_, _, _, true) => Visibility.Internal,
            (_, true, _, _) => Visibility.Private,
            _ => Visibility.Private // Default visibility is private
        };
    }
} 