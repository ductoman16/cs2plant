using System.Collections.Generic;
using cs2plant.Models;

namespace cs2plant.Services;

/// <summary>
/// Generates PlantUML diagrams from project dependencies.
/// </summary>
public interface IPlantUmlGenerator
{
    /// <summary>
    /// Generates a PlantUML diagram from a list of project dependencies.
    /// </summary>
    /// <param name="dependencies">The list of project dependencies.</param>
    /// <returns>A string containing the PlantUML diagram.</returns>
    string GenerateDiagram(IEnumerable<ProjectDependency> dependencies);
} 