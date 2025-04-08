using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace cs2plant.Models;

/// <summary>
/// Encapsulates metadata about a C# class.
/// </summary>
public sealed record ClassMetadataInfo
{
    public string Name { get; init; }
    public string Namespace { get; init; }
    public ModifierInfo Modifiers { get; init; }
    public bool IsRecord { get; init; }
    public IReadOnlyList<string> BaseTypes { get; init; }

    public ClassMetadataInfo(
        ClassDeclarationSyntax classDeclaration,
        string @namespace)
    {
        Name = classDeclaration.Identifier.Text;
        Namespace = @namespace;
        Modifiers = new ModifierInfo(classDeclaration.Modifiers);
        IsRecord = classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.RecordKeyword));
        BaseTypes = classDeclaration.BaseList?.Types
            .Select(t => t.ToString())
            .ToList() ?? new List<string>();
    }
} 