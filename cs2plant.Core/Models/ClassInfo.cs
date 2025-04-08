using System.Collections.Generic;

namespace cs2plant.Models;

/// <summary>
/// Represents the visibility level of a type or member.
/// </summary>
public enum Visibility
{
    /// <summary>
    /// Public visibility - accessible from any code.
    /// </summary>
    Public,

    /// <summary>
    /// Private visibility - accessible only within the containing type.
    /// </summary>
    Private,

    /// <summary>
    /// Protected visibility - accessible within the containing type and derived types.
    /// </summary>
    Protected,

    /// <summary>
    /// Internal visibility - accessible within the same assembly.
    /// </summary>
    Internal,

    /// <summary>
    /// Protected Internal visibility - accessible within the same assembly and derived types.
    /// </summary>
    ProtectedInternal,

    /// <summary>
    /// Private Protected visibility - accessible within the containing type and derived types within the same assembly.
    /// </summary>
    PrivateProtected
}

/// <summary>
/// Represents a class in a C# project.
/// </summary>
/// <param name="Name">The name of the class.</param>
/// <param name="Namespace">The namespace containing the class.</param>
/// <param name="Visibility">The visibility level of the class.</param>
/// <param name="IsSealed">Whether the class is sealed.</param>
/// <param name="IsRecord">Whether the class is a record.</param>
/// <param name="IsAbstract">Whether the class is abstract.</param>
/// <param name="IsStatic">Whether the class is static.</param>
/// <param name="BaseTypes">The base types (classes and interfaces) that this class inherits from or implements.</param>
/// <param name="Properties">The properties defined in the class.</param>
/// <param name="Methods">The methods defined in the class.</param>
/// <param name="TypeParameters">The generic type parameters if this is a generic class.</param>
/// <param name="Relationships">The relationships this class has with other types.</param>
/// <param name="NestedClasses">The classes nested within this class.</param>
public sealed record ClassInfo(
    string Name,
    string Namespace,
    Visibility Visibility,
    bool IsSealed,
    bool IsRecord,
    bool IsAbstract,
    bool IsStatic,
    IReadOnlyList<string> BaseTypes,
    IReadOnlyList<PropertyInfo> Properties,
    IReadOnlyList<MethodInfo> Methods,
    IReadOnlyList<TypeParameterInfo> TypeParameters,
    IReadOnlyList<RelationshipInfo> Relationships,
    IReadOnlyList<ClassInfo> NestedClasses);

/// <summary>
/// Represents a property in a class.
/// </summary>
/// <param name="Name">The name of the property.</param>
/// <param name="Type">The type of the property.</param>
/// <param name="Visibility">The visibility level of the property.</param>
/// <param name="IsStatic">Whether the property is static.</param>
/// <param name="IsVirtual">Whether the property is virtual.</param>
/// <param name="IsOverride">Whether the property overrides a base class property.</param>
/// <param name="HasGet">Whether the property has a getter.</param>
/// <param name="HasSet">Whether the property has a setter.</param>
/// <param name="HasInit">Whether the property has an init accessor.</param>
public sealed record PropertyInfo(
    string Name,
    string Type,
    Visibility Visibility,
    bool IsStatic,
    bool IsVirtual,
    bool IsOverride,
    bool HasGet,
    bool HasSet,
    bool HasInit);

/// <summary>
/// Represents a method in a class.
/// </summary>
/// <param name="Name">The name of the method.</param>
/// <param name="ReturnType">The return type of the method.</param>
/// <param name="Visibility">The visibility level of the method.</param>
/// <param name="IsAsync">Whether the method is async.</param>
/// <param name="IsStatic">Whether the method is static.</param>
/// <param name="IsVirtual">Whether the method is virtual.</param>
/// <param name="IsOverride">Whether the method overrides a base class method.</param>
/// <param name="IsAbstract">Whether the method is abstract.</param>
/// <param name="Parameters">The parameters of the method.</param>
/// <param name="TypeParameters">The generic type parameters if this is a generic method.</param>
public sealed record MethodInfo(
    string Name,
    string ReturnType,
    Visibility Visibility,
    bool IsAsync,
    bool IsStatic,
    bool IsVirtual,
    bool IsOverride,
    bool IsAbstract,
    IReadOnlyList<ParameterInfo> Parameters,
    IReadOnlyList<TypeParameterInfo> TypeParameters);

/// <summary>
/// Represents a parameter in a method.
/// </summary>
/// <param name="Name">The name of the parameter.</param>
/// <param name="Type">The type of the parameter.</param>
public sealed record ParameterInfo(
    string Name,
    string Type);
