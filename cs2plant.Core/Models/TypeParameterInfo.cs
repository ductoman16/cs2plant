namespace cs2plant.Models;

/// <summary>
/// Represents a generic type parameter with its constraints.
/// </summary>
/// <param name="Name">The name of the type parameter.</param>
/// <param name="Constraints">The constraints applied to the type parameter.</param>
public sealed record TypeParameterInfo(string Name, IReadOnlyList<string> Constraints); 