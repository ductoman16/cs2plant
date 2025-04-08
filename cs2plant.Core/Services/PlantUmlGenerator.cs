using System.Text;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using cs2plant.Models;
using cs2plant.Services;
using cs2plant.Core.Services;

namespace cs2plant.Services;

/// <summary>
/// Generates PlantUML diagrams from class information.
/// </summary>
public class PlantUmlGenerator(ILogger<PlantUmlGenerator> logger) : IPlantUmlGenerator
{
    public string GenerateDiagram(IEnumerable<ProjectDependency> dependencies)
    {
        Guard.Against.Null(dependencies);
        var dependencyList = dependencies.ToList();
        
        logger.LogInformation("Generating PlantUML diagram for {Count} projects", dependencyList.Count);
        
        var sb = new StringBuilder();
        var context = new PlantUmlGenerationContext(sb, "");

        // Generate component diagram
        GenerateHeader(context);
        GenerateProjectDependencies(context, dependencyList);
        context.AppendLine("@enduml");

        // Generate class diagram if there are any classes
        if (dependencyList.Any(d => d.Classes.Any()))
        {
            GenerateHeader(context);
            foreach (var namespaceGroup in dependencyList
                .SelectMany(d => d.Classes)
                .GroupBy(c => c.Namespace))
            {
                GenerateNamespace(context, namespaceGroup);
            }
            context.AppendLine("@enduml");
        }

        return sb.ToString();
    }

    internal void GenerateHeader(PlantUmlGenerationContext context)
    {
        context.AppendLine("@startuml");
        context.AppendLine("skinparam monochrome true");
        context.AppendLine("skinparam shadowing false");
        context.AppendLine("skinparam linetype ortho");
        context.AppendLine("skinparam packageStyle rectangle");
        context.AppendLine("skinparam classAttributeIconSize 0");
        context.AppendLine("");
    }

    internal void GenerateProjectDependencies(PlantUmlGenerationContext context, IEnumerable<ProjectDependency> projects)
    {
        context.AppendLine("package \"Project Dependencies\" {");
        foreach (var project in projects)
        {
            context.AppendLine($"  component \"{project.ProjectName}\"");
            if (project.PackageReferences.Any())
            {
                context.AppendLine("  note right");
                context.AppendLine("  Packages:");
                foreach (var package in project.PackageReferences)
                {
                    context.AppendLine($"    - {package}");
                }
                context.AppendLine("  end note");
            }
        }

        foreach (var project in projects)
        {
            foreach (var reference in project.ProjectReferences)
            {
                context.AppendLine($"  \"{project.ProjectName}\" --> \"{reference}\"");
            }
        }
        context.AppendLine("}");
    }

    internal void GenerateNamespace(PlantUmlGenerationContext context, IGrouping<string, ClassInfo> namespaceGroup)
    {
        if (!string.IsNullOrEmpty(namespaceGroup.Key))
        {
            context.AppendLine($"namespace {namespaceGroup.Key} {{");
        }

        var namespaceContext = !string.IsNullOrEmpty(namespaceGroup.Key) 
            ? context.WithIndent("  ")
            : context;

        // First declare all interfaces
        var allInterfaces = namespaceGroup
            .SelectMany(c => c.BaseTypes)
            .Distinct()
            .ToList();

        foreach (var iface in allInterfaces)
        {
            namespaceContext.AppendLine($"interface {iface} {{");
            namespaceContext.AppendLine("}");
        }

        // Then declare all classes
        var allNestedClasses = namespaceGroup.SelectMany(c => c.NestedClasses).ToHashSet();
        foreach (var classInfo in namespaceGroup.Where(c => !allNestedClasses.Contains(c)))
        {
            namespaceContext.GenerateClassDefinition(classInfo);
        }

        if (!string.IsNullOrEmpty(namespaceGroup.Key))
        {
            context.AppendLine("}");
        }
    }
} 