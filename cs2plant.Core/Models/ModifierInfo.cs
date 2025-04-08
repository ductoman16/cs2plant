using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace cs2plant.Models;

/// <summary>
/// Encapsulates common C# modifiers used across different member types.
/// </summary>
public sealed record ModifierInfo
{
    public Visibility Visibility { get; init; }
    public bool IsStatic { get; init; }
    public bool IsVirtual { get; init; }
    public bool IsOverride { get; init; }
    public bool IsAbstract { get; init; }

    public ModifierInfo(SyntaxTokenList modifiers)
    {
        Visibility = GetVisibility(modifiers);
        IsStatic = modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));
        IsVirtual = modifiers.Any(m => m.IsKind(SyntaxKind.VirtualKeyword));
        IsOverride = modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword));
        IsAbstract = modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword));
    }

    private static Visibility GetVisibility(SyntaxTokenList modifiers)
    {
        var hasPublic = modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword));
        var hasPrivate = modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword));
        var hasProtected = modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword));
        var hasInternal = modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword));

        if (hasPublic) return Visibility.Public;
        if (hasPrivate && hasProtected) return Visibility.PrivateProtected;
        if (hasProtected && hasInternal) return Visibility.ProtectedInternal;
        if (hasPrivate) return Visibility.Private;
        if (hasProtected) return Visibility.Protected;
        if (hasInternal) return Visibility.Internal;
        return Visibility.Private; // Default to private if no visibility modifier
    }
} 