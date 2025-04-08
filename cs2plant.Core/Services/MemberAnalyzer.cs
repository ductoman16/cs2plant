using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using cs2plant.Models;
using cs2plant.Core.Services;

namespace cs2plant.Services;

/// <summary>
/// Analyzes class members (properties and methods).
/// </summary>
public class MemberAnalyzer(ClassDeclarationSyntax classDeclaration, SemanticModel semanticModel)
{
    private readonly TypeAnalyzer _typeAnalyzer = new TypeAnalyzer(semanticModel);

    public List<PropertyInfo> GetProperties()
    {
        return classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Select(CreatePropertyInfo)
            .ToList();
    }

    private PropertyInfo CreatePropertyInfo(PropertyDeclarationSyntax property)
    {
        var propertySymbol = semanticModel.GetDeclaredSymbol(property);
        var propertyType = propertySymbol?.Type;
        var typeString = propertyType != null
            ? TypeAnalyzer.GetTypeName(propertyType)
            : property.Type.ToString();

        var modifiers = new ModifierInfo(property.Modifiers);

        return new PropertyInfo(
            property.Identifier.Text,
            typeString,
            modifiers.Visibility,
            modifiers.IsStatic,
            modifiers.IsVirtual,
            modifiers.IsOverride,
            property.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) ?? false,
            property.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) ?? false,
            property.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.InitAccessorDeclaration)) ?? false);
    }

    public List<MethodInfo> GetMethods()
    {
        return classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Select(CreateMethodInfo)
            .ToList();
    }

    private MethodInfo CreateMethodInfo(MethodDeclarationSyntax method)
    {
        var methodSymbol = semanticModel.GetDeclaredSymbol(method);
        var returnType = methodSymbol?.ReturnType;
        var returnTypeString = returnType != null
            ? TypeAnalyzer.GetTypeName(returnType)
            : method.ReturnType.ToString();

        var modifiers = new ModifierInfo(method.Modifiers);

        return new MethodInfo(
            method.Identifier.Text,
            returnTypeString,
            modifiers.Visibility,
            method.Modifiers.Any(mod => mod.IsKind(SyntaxKind.AsyncKeyword)),
            modifiers.IsStatic,
            modifiers.IsVirtual,
            modifiers.IsOverride,
            modifiers.IsAbstract,
            GetParameters(method.ParameterList.Parameters),
            _typeAnalyzer.GetTypeParameters(method.TypeParameterList));
    }

    private List<ParameterInfo> GetParameters(SeparatedSyntaxList<ParameterSyntax> parameters)
    {
        return parameters
            .Select(p =>
            {
                var parameterSymbol = semanticModel.GetDeclaredSymbol(p);
                var parameterType = parameterSymbol?.Type;
                var typeString = parameterType != null
                    ? TypeAnalyzer.GetTypeName(parameterType)
                    : p.Type?.ToString() ?? string.Empty;
                return new ParameterInfo(p.Identifier.Text, typeString);
            })
            .ToList();
    }

    public static Visibility GetVisibility(SyntaxTokenList modifiers)
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