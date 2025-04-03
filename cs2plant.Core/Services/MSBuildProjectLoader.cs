using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using Microsoft.Extensions.Logging;
using Project = Microsoft.CodeAnalysis.Project;
using System.Text.RegularExpressions;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace cs2plant.Services;

/// <summary>
/// Handles loading and initialization of MSBuild projects.
/// </summary>
public class MSBuildProjectLoader
{
    private readonly ILogger<MSBuildProjectLoader> _logger;
    private static readonly Dictionary<string, string> _globalProperties = new()
    {
        { "DesignTimeBuild", "true" },
        { "BuildingProject", "false" },
        { "SkipCompilerExecution", "true" },
        { "ProvideCommandLineArgs", "true" },
        { "MSBuildCopyMarkerName", "disabled" },
        { "SkipCustomBeforeMicrosoftCommonTargets", "true" },
        { "CustomBeforeMicrosoftCommonTargets", string.Empty },
        { "CustomAfterMicrosoftCommonTargets", string.Empty }
    };

    private static bool _initialized;
    private static readonly object _lock = new();

    private static readonly Regex _projectRegex = new(
        @"Project\(""\{[^}]+\}""\)\s*=\s*""([^""]+)""\s*,\s*""([^""]+)""\s*,\s*""\{[^}]+\}""",
        RegexOptions.Compiled | RegexOptions.Multiline);

    public MSBuildProjectLoader(ILogger<MSBuildProjectLoader> logger)
    {
        _logger = logger;
        InitializeMSBuild();
    }

    private static void InitializeMSBuild()
    {
        lock (_lock)
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                MSBuildLocator.RegisterDefaults();
                _initialized = true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to initialize MSBuild. Make sure Visual Studio or the .NET SDK is installed.", ex);
            }
        }
    }

    /// <summary>
    /// Loads a project from the specified path.
    /// </summary>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A tuple containing the loaded project and project collection.</returns>
    public Project? LoadProject(string projectPath, CancellationToken cancellationToken)
    {
        try
        {
            var workspace = MSBuildWorkspace.Create(_globalProperties);
            workspace.WorkspaceFailed += (sender, args) =>
            {
                _logger.LogWarning("Workspace warning: {Message}", args.Diagnostic.Message);
            };

            var project = workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken).Result;
            return project;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load project: {ProjectPath}", projectPath);
            return null;
        }
    }

    /// <summary>
    /// Gets all projects from a solution file.
    /// </summary>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <returns>A list of projects in the solution.</returns>
    public IEnumerable<string> GetProjectsFromSolution(string solutionPath)
    {
        List<string> projects = new();

        try
        {
            var fullSolutionPath = Path.GetFullPath(solutionPath);
            var solutionDirectory = Path.GetDirectoryName(fullSolutionPath) ?? string.Empty;
            var solutionContent = File.ReadAllText(fullSolutionPath);
            var matches = _projectRegex.Matches(solutionContent);

            foreach (Match match in matches)
            {
                if (match.Groups.Count < 3)
                {
                    continue;
                }

                var projectName = match.Groups[1].Value;
                var relativePath = match.Groups[2].Value.Replace('\\', Path.DirectorySeparatorChar);
                var fullPath = Path.GetFullPath(Path.Combine(solutionDirectory, relativePath));

                if (File.Exists(fullPath) && Path.GetExtension(fullPath).Equals(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("Found project: {ProjectName} at {ProjectPath}", projectName, fullPath);
                    projects.Add(fullPath);
                }
                else
                {
                    _logger.LogWarning("Project file not found or not a .csproj: {ProjectPath}", fullPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get projects from solution: {SolutionPath}", solutionPath);
            return Enumerable.Empty<string>();
        }

        return projects;
    }
} 