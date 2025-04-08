using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Build.Evaluation;
using cs2plant.Models;
using cs2plant.Core.Services;

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

        logger.LogInformation("Analyzing solution: {SolutionPath}", solutionPath);
        var projectFiles = LoadProjectsFromSolution(solutionPath);
        
        var dependencies = new List<ProjectDependency>();
        foreach (var projectFile in projectFiles)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Array.Empty<ProjectDependency>();
            }

            var projectDependency = await AnalyzeSingleProjectAsync(projectFile, cancellationToken);
            if (projectDependency != null)
            {
                dependencies.Add(projectDependency);
            }
        }

        return dependencies;
    }

    private List<string> LoadProjectsFromSolution(string solutionPath)
    {
        var projectFiles = projectLoader.GetProjectsFromSolution(solutionPath);
        var projectList = projectFiles.ToList();
        logger.LogInformation("Found {Count} projects in solution", projectList.Count);
        return projectList;
    }

    private async Task<ProjectDependency?> AnalyzeSingleProjectAsync(string projectFile, CancellationToken cancellationToken)
    {
        logger.LogInformation("Loading project: {ProjectPath}", projectFile);
        var project = projectLoader.LoadProject(projectFile, cancellationToken);
        if (project == null)
        {
            logger.LogWarning("Failed to analyze project: {ProjectPath}", projectFile);
            return null;
        }

        var projectName = Path.GetFileNameWithoutExtension(projectFile);
        var projectReferences = ExtractProjectReferences(project);
        var classes = await AnalyzeProjectClassesAsync(project, projectName, cancellationToken);

        return CreateProjectDependency(projectName, projectFile, projectReferences, classes);
    }

    private List<string> ExtractProjectReferences(Microsoft.CodeAnalysis.Project project)
    {
        var references = project.ProjectReferences
            .Select(r => Path.GetFileNameWithoutExtension(r.ProjectId.ToString()))
            .ToList();

        logger.LogInformation("Found {Count} project references", references.Count);
        return references;
    }

    private async Task<IReadOnlyList<ClassInfo>> AnalyzeProjectClassesAsync(
        Microsoft.CodeAnalysis.Project project, 
        string projectName, 
        CancellationToken cancellationToken)
    {
        var classes = await classAnalyzer.AnalyzeClassesAsync(project, cancellationToken);
        logger.LogInformation("Found {Count} classes in project {ProjectName}", classes.Count, projectName);
        return classes;
    }

    private static ProjectDependency CreateProjectDependency(
        string projectName, 
        string projectFile, 
        List<string> projectReferences, 
        IReadOnlyList<ClassInfo> classes)
    {
        return new ProjectDependency(
            projectName,
            Path.GetFullPath(projectFile),
            "net8.0", // Hardcoded for now since we can't get it from MSBuildWorkspace
            Array.Empty<string>(), // No package references in MSBuildWorkspace
            projectReferences,
            classes
        );
    }
} 