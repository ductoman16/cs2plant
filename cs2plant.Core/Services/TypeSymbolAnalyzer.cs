using Microsoft.CodeAnalysis;

namespace cs2plant.Core.Services;

/// <summary>
/// Handles analysis and naming of ITypeSymbol instances.
/// </summary>
public class TypeSymbolAnalyzer(ITypeSymbol typeSymbol)
{
    public string GetTypeName()
    {
        if (TryGetPrimitiveTypeName(out var primitiveTypeName))
        {
            return primitiveTypeName;
        }

        if (typeSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            return GetGenericTypeName(namedType);
        }

        return GetNormalizedTypeName();
    }

    private bool TryGetPrimitiveTypeName(out string typeName)
    {
        typeName = typeSymbol.SpecialType switch
        {
            SpecialType.System_Void => "void",
            SpecialType.System_Boolean => "bool",
            SpecialType.System_Int32 => "int",
            SpecialType.System_String => "string",
            SpecialType.System_Double => "double",
            SpecialType.System_Decimal => "decimal",
            SpecialType.System_Single => "float",
            SpecialType.System_Int64 => "long",
            SpecialType.System_Byte => "byte",
            SpecialType.System_Char => "char",
            SpecialType.System_Object => "object",
            _ => string.Empty
        };

        return !string.IsNullOrEmpty(typeName);
    }

    private string GetGenericTypeName(INamedTypeSymbol namedType)
    {
        var typeArgs = string.Join(", ", namedType.TypeArguments.Select(t => new TypeSymbolAnalyzer(t).GetTypeName()));
        return $"{namedType.Name}<{typeArgs}>";
    }

    private string GetNormalizedTypeName()
    {
        var typeName = typeSymbol.Name;
        if (!typeName.StartsWith("System.")) return typeName;

        var simpleName = typeName.Split('.').Last().ToLowerInvariant();
        return simpleName switch
        {
            "void" => "void",
            "string" => "string",
            "int32" => "int",
            "int64" => "long",
            "single" => "float",
            "boolean" => "bool",
            "double" => "double",
            "decimal" => "decimal",
            "datetime" => "DateTime",
            "datetimeoffset" => "DateTimeOffset",
            "timespan" => "TimeSpan",
            "guid" => "Guid",
            "object" => "object",
            "byte" => "byte",
            "char" => "char",
            _ => typeName.Split('.').Last() // Keep original case for other types
        };
    }
} 