namespace cs2plant.Models;

/// <summary>
/// Represents the type of relationship between classes.
/// </summary>
public enum RelationshipType
{
    /// <summary>
    /// Inheritance relationship (extends).
    /// </summary>
    Inheritance,

    /// <summary>
    /// Interface implementation relationship (implements).
    /// </summary>
    Implementation,

    /// <summary>
    /// Composition relationship (strong ownership).
    /// </summary>
    Composition,

    /// <summary>
    /// Aggregation relationship (weak ownership).
    /// </summary>
    Aggregation,

    /// <summary>
    /// Dependency relationship (uses).
    /// </summary>
    Dependency
}

/// <summary>
/// Represents a relationship between classes.
/// </summary>
/// <param name="TargetType">The fully qualified name of the target type.</param>
/// <param name="Type">The type of relationship.</param>
public sealed record RelationshipInfo(string TargetType, RelationshipType Type); 