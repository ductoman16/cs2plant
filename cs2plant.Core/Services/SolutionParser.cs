using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace cs2plant.Core.Services;

/// <summary>
/// Handles parsing of Visual Studio solution files to extract project information.
/// </summary>
public class SolutionParser
{
    private readonly ILogger<SolutionParser> _logger;
    private static readonly Regex _projectRegex = new(
        @"Project\(""\{[^}]+\}""\)\s*=\s*""([^""]+)""\s*,\s*""([^""]+)""\s*,\s*""\{[^}]+\}""",
        RegexOptions.Compiled | RegexOptions.Multiline);

    public SolutionParser(ILogger<SolutionParser> logger)
    {
        _logger = logger;
    }

    public IEnumerable<string> GetProjectPaths(string solutionPath)
    {
        try
        {
            var solutionInfo = GetSolutionInfo(solutionPath);
            var matches = _projectRegex.Matches(solutionInfo.Content);
            return ParseProjectsFromMatches(matches, solutionInfo.Directory);
        }
        catch (Exception ex)
        {
            LogSolutionLoadError(ex, solutionPath);
            return Enumerable.Empty<string>();
        }
    }

    private (string Directory, string Content) GetSolutionInfo(string solutionPath)
    {
        var fullSolutionPath = Path.GetFullPath(solutionPath);
        var solutionDirectory = Path.GetDirectoryName(fullSolutionPath) ?? string.Empty;
        var solutionContent = File.ReadAllText(fullSolutionPath);
        return (solutionDirectory, solutionContent);
    }

    private IEnumerable<string> ParseProjectsFromMatches(MatchCollection matches, string solutionDirectory)
    {
        var projects = new List<string>();

        foreach (Match match in matches)
        {
            if (match.Groups.Count < 3)
            {
                continue;
            }

            var projectInfo = GetProjectInfo(match, solutionDirectory);
            if (ProjectValidator.IsValidCSharpProject(projectInfo.FullPath))
            {
                _logger.LogInformation("Found project: {ProjectName} at {ProjectPath}", projectInfo.Name, projectInfo.FullPath);
                projects.Add(projectInfo.FullPath);
            }
            else
            {
                _logger.LogWarning("Project file not found or not a .csproj: {ProjectPath}", projectInfo.FullPath);
            }
        }

        return projects;
    }

    private (string Name, string FullPath) GetProjectInfo(Match match, string solutionDirectory)
    {
        var projectName = match.Groups[1].Value;
        var relativePath = match.Groups[2].Value.Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(solutionDirectory, relativePath));
        return (projectName, fullPath);
    }

    private void LogSolutionLoadError(Exception ex, string solutionPath)
    {
        _logger.LogError(ex, "Failed to get projects from solution: {SolutionPath}", solutionPath);
    }
} 