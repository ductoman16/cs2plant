using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using cs2plant.Models;
using cs2plant.Services;

namespace cs2plant.Core.Services;

/// <summary>
/// Encapsulates the context needed for analyzing a C# class.
/// </summary>
/// <param name="ClassDeclaration">The syntax tree representation of the class being analyzed.</param>
/// <param name="SemanticModel">The semantic model providing symbol information.</param>
/// <param name="TypeAnalyzer">The type analyzer for handling type-related operations.</param>
/// <param name="Symbol">The symbol representing the class being analyzed.</param>
public record class ClassAnalysisContext(
    ClassDeclarationSyntax ClassDeclaration,
    SemanticModel SemanticModel,
    TypeAnalyzer TypeAnalyzer,
    INamedTypeSymbol Symbol)
{
    /// <summary>
    /// Analyzes the class and returns detailed class information.
    /// </summary>
    public async Task<ClassInfo> AnalyzeAsync(CancellationToken cancellationToken)
    {
        var classMetadata = ExtractClassMetadata();
        var memberAnalyzer = new MemberAnalyzer(ClassDeclaration, SemanticModel);
        var relationshipAnalyzer = new RelationshipAnalyzer(Symbol);

        var typeParameters = TypeAnalyzer.GetTypeParameters(ClassDeclaration.TypeParameterList);
        var properties = memberAnalyzer.GetProperties();
        var methods = memberAnalyzer.GetMethods();
        var relationships = relationshipAnalyzer.GetRelationships();
        var nestedClasses = await GetNestedClassesAsync(cancellationToken);

        return new ClassInfo(
            classMetadata.Name,
            Symbol.ContainingNamespace.ToString() ?? string.Empty,
            classMetadata.Visibility,
            classMetadata.IsSealed,
            classMetadata.IsRecord,
            classMetadata.IsAbstract,
            classMetadata.IsStatic,
            classMetadata.BaseTypes,
            properties,
            methods,
            typeParameters,
            relationships,
            nestedClasses);
    }

    /// <summary>
    /// Gets the nested classes of the current class.
    /// </summary>
    public async Task<List<ClassInfo>> GetNestedClassesAsync(CancellationToken cancellationToken)
    {
        var nestedClasses = new List<ClassInfo>();
        foreach (var nestedClass in ClassDeclaration.Members.OfType<ClassDeclarationSyntax>())
        {
            var nestedSymbol = SemanticModel.GetDeclaredSymbol(nestedClass, cancellationToken: cancellationToken);
            if (nestedSymbol != null)
            {
                var nestedContext = new ClassAnalysisContext(nestedClass, SemanticModel, TypeAnalyzer, nestedSymbol);
                nestedClasses.Add(await nestedContext.AnalyzeAsync(cancellationToken));
            }
        }
        return nestedClasses;
    }

    private record ClassMetadata(
        string Name,
        Visibility Visibility,
        bool IsSealed,
        bool IsRecord,
        bool IsAbstract,
        bool IsStatic,
        List<string> BaseTypes);

    private ClassMetadata ExtractClassMetadata()
    {
        return new ClassMetadata(
            Name: ClassDeclaration.Identifier.Text,
            Visibility: MemberAnalyzer.GetVisibility(ClassDeclaration.Modifiers),
            IsSealed: ClassDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.SealedKeyword)),
            IsRecord: ClassDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.RecordKeyword)),
            IsAbstract: ClassDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword)),
            IsStatic: ClassDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)),
            BaseTypes: ClassDeclaration.BaseList?.Types.Select(t => t.ToString()).ToList() ?? new List<string>());
    }
} 