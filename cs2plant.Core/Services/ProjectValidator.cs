namespace cs2plant.Core.Services;

/// <summary>
/// Provides validation methods for C# project files.
/// </summary>
public static class ProjectValidator
{
    /// <summary>
    /// Determines if the given path points to a valid C# project file.
    /// </summary>
    /// <param name="projectPath">The path to validate.</param>
    /// <returns>True if the path exists and points to a .csproj file, false otherwise.</returns>
    public static bool IsValidCSharpProject(string projectPath)
    {
        return File.Exists(projectPath) && 
               Path.GetExtension(projectPath).Equals(".csproj", StringComparison.OrdinalIgnoreCase);
    }
} 