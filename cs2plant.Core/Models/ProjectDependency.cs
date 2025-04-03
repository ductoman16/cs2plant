namespace cs2plant.Models;

/// <summary>
/// Represents a project and its dependencies.
/// </summary>
/// <param name="ProjectName">The name of the project.</param>
/// <param name="ProjectPath">The absolute path to the project file.</param>
/// <param name="TargetFramework">The target framework of the project.</param>
/// <param name="PackageReferences">The list of NuGet package references.</param>
/// <param name="ProjectReferences">The list of project references.</param>
/// <param name="Classes">The list of classes in the project.</param>
public sealed record ProjectDependency(
    string ProjectName,
    string ProjectPath,
    string TargetFramework,
    IReadOnlyList<string> PackageReferences,
    IReadOnlyList<string> ProjectReferences,
    IReadOnlyList<ClassInfo> Classes); 