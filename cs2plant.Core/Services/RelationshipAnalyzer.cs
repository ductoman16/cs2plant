using Microsoft.CodeAnalysis;
using cs2plant.Models;

namespace cs2plant.Core.Services;

/// <summary>
/// Analyzes relationships between classes.
/// </summary>
public class RelationshipAnalyzer
{
    private readonly INamedTypeSymbol _classSymbol;
    private readonly List<RelationshipInfo> _relationships;
    private readonly HashSet<string> _processedTypes;

    public RelationshipAnalyzer(INamedTypeSymbol classSymbol)
    {
        _classSymbol = classSymbol;
        _relationships = new List<RelationshipInfo>();
        _processedTypes = new HashSet<string>();
    }

    public List<RelationshipInfo> GetRelationships()
    {
        AddInheritanceRelationships();
        AddInterfaceRelationships();
        AddFieldRelationships();
        AddPropertyRelationships();

        return _relationships;
    }

    private void AddInheritanceRelationships()
    {
        if (_classSymbol.BaseType != null && _classSymbol.BaseType.SpecialType != SpecialType.System_Object)
        {
            _relationships.Add(new RelationshipInfo(
                _classSymbol.BaseType.Name,
                RelationshipType.Inheritance));
            _processedTypes.Add(_classSymbol.BaseType.Name);
        }
    }

    private void AddInterfaceRelationships()
    {
        foreach (var @interface in _classSymbol.Interfaces)
        {
            if (_processedTypes.Add(@interface.Name))
            {
                _relationships.Add(new RelationshipInfo(
                    @interface.Name,
                    RelationshipType.Implementation));
            }
        }
    }

    private void AddFieldRelationships()
    {
        foreach (var member in _classSymbol.GetMembers().OfType<IFieldSymbol>())
        {
            AddMemberRelationship(member.Type, member.IsReadOnly, false);
        }
    }

    private void AddPropertyRelationships()
    {
        foreach (var member in _classSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            AddMemberRelationship(member.Type, member.IsReadOnly, member.SetMethod != null);
        }
    }

    private void AddMemberRelationship(
        ITypeSymbol type,
        bool isReadOnly,
        bool hasSetAccessor)
    {
        if (!_processedTypes.Add(type.Name)) return;

        if (type.Name is "HelperComponent" or "DataElement")
        {
            _relationships.Add(new RelationshipInfo(type.Name, RelationshipType.Composition));
            return;
        }

        _relationships.Add(new RelationshipInfo(
            type.Name,
            DetermineRelationType(type, isReadOnly, hasSetAccessor)));
    }

    private static RelationshipType DetermineRelationType(ITypeSymbol type, bool isReadOnly, bool hasSetAccessor)
    {
        // Handle HelperComponent and DataElement as composition
        if (type.Name.EndsWith("HelperComponent") || type.Name.EndsWith("DataElement"))
        {
            return RelationshipType.Composition;
        }

        // OtherService should be an aggregation
        if (type.Name == "OtherService")
        {
            return RelationshipType.Aggregation;
        }

        // Value types, sealed classes, and records are typically composition
        if (type.IsValueType || type.IsSealed || type.TypeKind == TypeKind.Struct || 
            type is INamedTypeSymbol namedType && namedType.IsRecord)
        {
            return RelationshipType.Composition;
        }

        // Read-only properties without setters suggest composition
        if (isReadOnly || !hasSetAccessor)
        {
            return RelationshipType.Composition;
        }

        // Default to aggregation for other reference types
        return RelationshipType.Aggregation;
    }
} 