using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using cs2plant.Models;

namespace cs2plant.Core.Services;

/// <summary>
/// Handles analysis of C# types and type parameters.
/// </summary>
public class TypeAnalyzer(SemanticModel semanticModel)
{
    private readonly List<string> _constraints = [];

    public List<TypeParameterInfo> GetTypeParameters(TypeParameterListSyntax? typeParameterList)
    {
        var typeParameters = new List<TypeParameterInfo>();
        if (typeParameterList == null) return typeParameters;

        foreach (var tp in typeParameterList.Parameters)
        {
            var typeParamSymbol = semanticModel.GetDeclaredSymbol(tp) as ITypeParameterSymbol;
            if (typeParamSymbol == null) continue;

            _constraints.Clear();
            AddSpecialConstraints(typeParamSymbol);
            AddTypeConstraints(typeParamSymbol);

            typeParameters.Add(new TypeParameterInfo(tp.Identifier.Text, _constraints.AsReadOnly()));
        }

        return typeParameters;
    }

    private void AddSpecialConstraints(ITypeParameterSymbol typeParamSymbol)
    {
        if (typeParamSymbol.HasReferenceTypeConstraint) _constraints.Add("class");
        if (typeParamSymbol.HasValueTypeConstraint) _constraints.Add("struct");
        if (typeParamSymbol.HasConstructorConstraint) _constraints.Add("new()");
        if (typeParamSymbol.HasNotNullConstraint) _constraints.Add("notnull");
        if (typeParamSymbol.HasUnmanagedTypeConstraint) _constraints.Add("unmanaged");
    }

    private void AddTypeConstraints(ITypeParameterSymbol typeParamSymbol)
    {
        foreach (var constraintType in typeParamSymbol.ConstraintTypes)
        {
            _constraints.Add(constraintType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
        }
    }

    public static string GetTypeName(ITypeSymbol? type)
    {
        return type == null ? string.Empty : new TypeSymbolAnalyzer(type).GetTypeName();
    }
}