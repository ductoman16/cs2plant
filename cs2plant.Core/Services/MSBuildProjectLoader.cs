using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.Extensions.Logging;
using Project = Microsoft.CodeAnalysis.Project;

namespace cs2plant.Core.Services;

/// <summary>
/// Handles loading and initialization of MSBuild projects.
/// </summary>
public class MSBuildProjectLoader
{
    private readonly ILogger<MSBuildProjectLoader> _logger;
    private readonly SolutionParser _solutionParser;
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

    public MSBuildProjectLoader(ILogger<MSBuildProjectLoader> logger, SolutionParser solutionParser)
    {
        _logger = logger;
        _solutionParser = solutionParser;
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
            var workspace = CreateWorkspace();
            return LoadProjectInWorkspace(workspace, projectPath, cancellationToken);
        }
        catch (Exception ex)
        {
            LogProjectLoadError(ex, projectPath);
            return null;
        }
    }

    private MSBuildWorkspace CreateWorkspace()
    {
        var workspace = MSBuildWorkspace.Create(_globalProperties);
        workspace.WorkspaceFailed += HandleWorkspaceFailure;
        return workspace;
    }

    private void HandleWorkspaceFailure(object? sender, WorkspaceDiagnosticEventArgs args)
    {
        _logger.LogWarning("Workspace warning: {Message}", args.Diagnostic.Message);
    }

    private Project LoadProjectInWorkspace(MSBuildWorkspace workspace, string projectPath, CancellationToken cancellationToken)
    {
        return workspace.OpenProjectAsync(projectPath, cancellationToken: cancellationToken).Result;
    }

    private void LogProjectLoadError(Exception ex, string projectPath)
    {
        _logger.LogError(ex, "Failed to load project: {ProjectPath}", projectPath);
    }

    /// <summary>
    /// Gets all projects from a solution file.
    /// </summary>
    /// <param name="solutionPath">The path to the solution file.</param>
    /// <returns>A list of projects in the solution.</returns>
    public IEnumerable<string> GetProjectsFromSolution(string solutionPath)
    {
        return _solutionParser.GetProjectPaths(solutionPath);
    }
}