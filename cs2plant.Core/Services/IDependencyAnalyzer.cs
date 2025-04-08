using cs2plant.Models;

namespace cs2plant.Core.Services;

/// <summary>
/// Analyzes project dependencies in a solution.
/// </summary>
public interface IDependencyAnalyzer
{
    /// <summary>
    /// Analyzes the dependencies of all projects in a solution.
    /// </summary>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A list of project dependencies.</returns>
    Task<IReadOnlyList<ProjectDependency>> AnalyzeProjectAsync(string solutionPath, CancellationToken cancellationToken = default);
}