using System.Text;
using cs2plant.Models;

namespace cs2plant.Core.Services;

/// <summary>
/// Encapsulates the context needed for generating PlantUML diagrams.
/// </summary>
/// <param name="StringBuilder">The StringBuilder instance to append PlantUML content to.</param>
/// <param name="Indent">The current indentation level for formatting.</param>
public sealed record class PlantUmlGenerationContext(
    StringBuilder StringBuilder,
    string Indent)
{
    /// <summary>
    /// Creates a new context with additional indentation.
    /// </summary>
    public PlantUmlGenerationContext WithIndent(string additionalIndent) =>
        this with { Indent = Indent + additionalIndent };

    /// <summary>
    /// Appends a line to the StringBuilder with the current indentation.
    /// </summary>
    public void AppendLine(string line) =>
        StringBuilder.AppendLine($"{Indent}{line}");

    /// <summary>
    /// Appends a line to the StringBuilder with the current indentation and additional indentation.
    /// </summary>
    public void AppendLine(string line, string additionalIndent) =>
        StringBuilder.AppendLine($"{Indent}{additionalIndent}{line}");

    /// <summary>
    /// Generates the PlantUML definition for a class.
    /// </summary>
    public void GenerateClassDefinition(ClassInfo classInfo)
    {
        if (classInfo == null)
            throw new ArgumentNullException(nameof(classInfo));

        var stereotype = GetClassStereotype(classInfo);
        var typeParams = GetTypeParameters(classInfo);
        
        AppendLine($"class {classInfo.Name}{stereotype}{typeParams}  {{");
        
        foreach (var property in classInfo.Properties)
        {
            GeneratePropertyDefinition(property);
        }

        foreach (var method in classInfo.Methods)
        {
            GenerateMethodDefinition(method);
        }

        foreach (var nestedClass in classInfo.NestedClasses)
        {
            var nestedContext = WithIndent("  ");
            nestedContext.GenerateClassDefinition(nestedClass);
        }

        AppendLine("}");

        GenerateClassRelationships(classInfo);
    }

    /// <summary>
    /// Generates the PlantUML definition for a method.
    /// </summary>
    public void GenerateMethodDefinition(MethodInfo method)
    {
        var visibility = GetVisibilitySymbol(method.Visibility);
        var modifiers = new List<string>();
        
        if (method.IsStatic) modifiers.Add("static");
        if (method.IsVirtual) modifiers.Add("virtual");
        if (method.IsOverride) modifiers.Add("override");
        if (method.IsAbstract) modifiers.Add("abstract");
        if (method.IsAsync) modifiers.Add("async");
        
        var modifierString = modifiers.Any() ? string.Join(" ", modifiers) + " " : "";
        var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Name}: {p.Type}"));
        var typeParameters = method.TypeParameters.Any() 
            ? $"<{string.Join(", ", method.TypeParameters)}>"
            : "";

        AppendLine($"{visibility}{modifierString}{method.Name}{typeParameters}({parameters}): {method.ReturnType}", "  ");
    }

    /// <summary>
    /// Generates the PlantUML definition for a property.
    /// </summary>
    public void GeneratePropertyDefinition(PropertyInfo property)
    {
        var visibility = GetVisibilitySymbol(property.Visibility);
        var modifiers = new List<string>();
        
        if (property.IsStatic) modifiers.Add("static");
        if (property.IsVirtual) modifiers.Add("virtual");
        if (property.IsOverride) modifiers.Add("override");
        
        var modifierString = modifiers.Any() ? string.Join(" ", modifiers) + " " : "";
        var accessors = GetPropertyAccessors(property);

        AppendLine($"  {visibility}{modifierString}{property.Name} : {property.Type} {accessors}");
    }

    /// <summary>
    /// Generates the PlantUML relationships for a class.
    /// </summary>
    public void GenerateClassRelationships(ClassInfo classInfo)
    {
        foreach (var relationship in classInfo.Relationships)
        {
            var arrow = GetRelationshipArrow(relationship.Type);
            if (relationship.Type == RelationshipType.Implementation)
            {
                AppendLine($"{classInfo.Name} --|> {relationship.TargetType}");
            }
            else
            {
                AppendLine($"{relationship.TargetType} {arrow} {classInfo.Name}");
            }
        }

        foreach (var nestedClass in classInfo.NestedClasses)
        {
            AppendLine($"{classInfo.Name} +-- {nestedClass.Name}");
        }
    }

    private static string GetTypeParameters(ClassInfo classInfo)
    {
        if (!classInfo.TypeParameters.Any())
            return string.Empty;

        var formattedParams = classInfo.TypeParameters
            .Select(tp => !tp.Constraints.Any() 
                ? tp.Name 
                : $"{tp.Name}: {string.Join(" & ", tp.Constraints)}");

        return $"<{string.Join(", ", formattedParams)}>";
    }

    private static string GetPropertyAccessors(PropertyInfo property)
    {
        var accessors = new List<string>();
        if (property.HasGet) accessors.Add("get");
        if (property.HasSet) accessors.Add("set");
        if (property.HasInit) accessors.Add("init");
        
        return accessors.Any() ? $"{{ {string.Join(" ", accessors)} }}" : string.Empty;
    }

    private static string GetVisibilitySymbol(Visibility visibility) => visibility switch
    {
        Visibility.Public => "+ ",
        Visibility.Private => "- ",
        Visibility.Protected => "# ",
        Visibility.Internal => "~ ",
        Visibility.ProtectedInternal => "# ",
        Visibility.PrivateProtected => "-# ",
        _ => "+ "
    };

    private static string GetClassStereotype(ClassInfo classInfo)
    {
        var stereotypes = new List<string>();
        if (classInfo.IsSealed) stereotypes.Add("sealed");
        if (classInfo.IsRecord) stereotypes.Add("record");
        if (classInfo.IsAbstract) stereotypes.Add("abstract");
        if (classInfo.IsStatic) stereotypes.Add("static");
        return stereotypes.Any() ? $" <<{string.Join(">> <<", stereotypes)}>>" : string.Empty;
    }

    private static string GetRelationshipArrow(RelationshipType type) => type switch
    {
        RelationshipType.Implementation => "|>",
        RelationshipType.Inheritance => "<|--",
        RelationshipType.Dependency => "-->",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };
} 