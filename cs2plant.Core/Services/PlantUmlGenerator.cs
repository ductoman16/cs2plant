using System.Text;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using cs2plant.Models;

namespace cs2plant.Services;

/// <summary>
/// Generates PlantUML diagrams from project dependencies.
/// </summary>
public sealed class PlantUmlGenerator(ILogger<PlantUmlGenerator> logger) : IPlantUmlGenerator
{
    private readonly ILogger<PlantUmlGenerator> _logger = Guard.Against.Null(logger);

    /// <inheritdoc />
    public string GenerateDiagram(IEnumerable<ProjectDependency> dependencies)
    {
        Guard.Against.Null(dependencies);
        var projectList = dependencies.ToList();
        _logger.LogInformation("Generating PlantUML diagram for {Count} projects", projectList.Count);

        var sb = new StringBuilder();
        sb.AppendLine("@startuml");
        sb.AppendLine("skinparam componentStyle rectangle");
        sb.AppendLine("skinparam packageStyle rectangle");
        sb.AppendLine("skinparam classAttributeIconSize 0");
        sb.AppendLine("skinparam component {");
        sb.AppendLine("  BackgroundColor White");
        sb.AppendLine("  ArrowColor Black");
        sb.AppendLine("  BorderColor Black");
        sb.AppendLine("}");
        sb.AppendLine("skinparam class {");
        sb.AppendLine("  BackgroundColor White");
        sb.AppendLine("  ArrowColor Black");
        sb.AppendLine("  BorderColor Black");
        sb.AppendLine("}");
        sb.AppendLine("skinparam package {");
        sb.AppendLine("  BackgroundColor LightGray");
        sb.AppendLine("  BorderColor Black");
        sb.AppendLine("}");
        sb.AppendLine();

        // Generate project components
        foreach (var project in projectList)
        {
            sb.AppendLine($"[{project.ProjectName}]");
            if (project.PackageReferences.Any())
            {
                sb.AppendLine($"note bottom of [{project.ProjectName}]");
                sb.AppendLine("  Packages:");
                foreach (var package in project.PackageReferences)
                {
                    sb.AppendLine($"  - {package}");
                }
                sb.AppendLine("end note");
            }
            sb.AppendLine();

            // Generate class diagrams for each project
            if (project.Classes.Any())
            {
                sb.AppendLine($"package {project.ProjectName} {{");
                
                // Group classes by namespace
                var classesByNamespace = project.Classes
                    .GroupBy(c => c.Namespace ?? "")
                    .OrderBy(g => g.Key);

                foreach (var namespaceGroup in classesByNamespace)
                {
                    if (!string.IsNullOrEmpty(namespaceGroup.Key))
                    {
                        sb.AppendLine($"  namespace {namespaceGroup.Key} {{");
                    }

                    // First, declare all interfaces
                    foreach (var classInfo in namespaceGroup.Where(c => c.BaseTypes.Any()))
                    {
                        foreach (var iface in classInfo.BaseTypes)
                        {
                            sb.AppendLine($"    interface {iface} {{");
                            sb.AppendLine("    }");
                        }
                    }

                    // Then declare all classes
                    foreach (var classInfo in namespaceGroup)
                    {
                        var classModifiers = new List<string>();
                        if (classInfo.IsSealed)
                        {
                            classModifiers.Add("<<sealed>>");
                        }
                        if (classInfo.IsRecord)
                        {
                            classModifiers.Add("<<record>>");
                        }
                        var classModifiersStr = classModifiers.Any() ? $" {string.Join(" ", classModifiers)}" : "";
                        
                        sb.AppendLine($"    class {classInfo.Name}{classModifiersStr} {{");
                        
                        // Properties
                        foreach (var property in classInfo.Properties)
                        {
                            var propertyAccessors = new List<string>();
                            if (property.HasGet)
                            {
                                propertyAccessors.Add("get");
                            }
                            if (property.HasSet)
                            {
                                propertyAccessors.Add("set");
                            }
                            if (property.HasInit)
                            {
                                propertyAccessors.Add("init");
                            }
                            var propertyAccessorsStr = propertyAccessors.Any() ? $" {{ {string.Join(" ", propertyAccessors)} }}" : string.Empty;
                            sb.AppendLine($"      + {property.Name}: {property.Type}{propertyAccessorsStr}");
                        }

                        // Methods
                        foreach (var method in classInfo.Methods)
                        {
                            var asyncPrefix = method.IsAsync ? "async " : string.Empty;
                            var parameters = string.Join(", ", method.Parameters.Select(p => $"{p.Name}: {p.Type}"));
                            sb.AppendLine($"      + {asyncPrefix}{method.Name}({parameters}): {method.ReturnType}");
                        }

                        sb.AppendLine("    }");

                        // Generate inheritance relationships
                        foreach (var baseType in classInfo.BaseTypes)
                        {
                            sb.AppendLine($"    {baseType} <|.. {classInfo.Name}");
                        }
                    }

                    if (!string.IsNullOrEmpty(namespaceGroup.Key))
                    {
                        sb.AppendLine("  }");
                    }
                }
                sb.AppendLine("}");
            }
        }

        // Generate project dependencies
        AppendProjectDependencies(sb, projectList);

        sb.AppendLine();
        sb.AppendLine("@enduml");

        return sb.ToString();
    }

    private static string SanitizeProjectName(string projectName) =>
        projectName.Replace(".", "_").Replace(" ", "_").Replace("-", "_");

    private void AppendProjectDependencies(StringBuilder sb, IReadOnlyList<ProjectDependency> dependencies)
    {
        foreach (var dependency in dependencies)
        {
            foreach (var reference in dependency.ProjectReferences)
            {
                sb.AppendLine($"[{reference}] <-- [{dependency.ProjectName}]");
            }
        }
    }
} 