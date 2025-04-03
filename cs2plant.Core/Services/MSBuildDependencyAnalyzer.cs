using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Build.Evaluation;
using cs2plant.Models;

namespace cs2plant.Services;

/// <summary>
/// Analyzes project dependencies using MSBuild.
/// </summary>
public class MSBuildDependencyAnalyzer(
    MSBuildProjectLoader projectLoader,
    ILogger<MSBuildDependencyAnalyzer> logger,
    ClassAnalyzer classAnalyzer) : IDependencyAnalyzer
{
    public async Task<IReadOnlyList<ProjectDependency>> AnalyzeProjectAsync(string solutionPath, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Array.Empty<ProjectDependency>();
        }

        var dependencies = new List<ProjectDependency>();
        logger.LogInformation("Analyzing solution: {SolutionPath}", solutionPath);
        var projectFiles = projectLoader.GetProjectsFromSolution(solutionPath);
        var projectList = projectFiles.ToList();
        logger.LogInformation("Found {Count} projects in solution", projectList.Count);

        foreach (var projectFile in projectList)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Array.Empty<ProjectDependency>();
            }

            logger.LogInformation("Loading project: {ProjectPath}", projectFile);
            var project = projectLoader.LoadProject(projectFile, cancellationToken);
            if (project == null)
            {
                logger.LogWarning("Failed to analyze project: {ProjectPath}", projectFile);
                continue;
            }

            var projectName = Path.GetFileNameWithoutExtension(projectFile);
            var projectReferences = project.ProjectReferences
                .Select(r => Path.GetFileNameWithoutExtension(r.ProjectId.ToString()))
                .ToList();

            logger.LogInformation("Project {ProjectName} has {Count} project references", projectName, projectReferences.Count);

            // Analyze classes in the project
            var classes = await classAnalyzer.AnalyzeClassesAsync(project, cancellationToken);
            logger.LogInformation("Found {Count} classes in project {ProjectName}", classes.Count, projectName);

            dependencies.Add(new ProjectDependency(
                projectName,
                Path.GetFullPath(projectFile),
                "net8.0", // Hardcoded for now since we can't get it from MSBuildWorkspace
                Array.Empty<string>(), // No package references in MSBuildWorkspace
                projectReferences,
                classes
            ));
        }

        return dependencies;
    }
} 