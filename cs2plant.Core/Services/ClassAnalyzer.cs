using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using cs2plant.Models;

namespace cs2plant.Services;

/// <summary>
/// Analyzes C# classes to extract class information and dependencies.
/// </summary>
public class ClassAnalyzer(ILogger<ClassAnalyzer> logger)
{
    private readonly ILogger<ClassAnalyzer> _logger = logger;

    public async Task<IReadOnlyList<ClassInfo>> AnalyzeClassesAsync(Project project, CancellationToken cancellationToken)
    {
        if (project == null)
        {
            _logger.LogWarning("Project is null");
            return Array.Empty<ClassInfo>();
        }

        var compilation = await project.GetCompilationAsync(cancellationToken);
        if (compilation == null)
        {
            _logger.LogWarning("Failed to get compilation for project: {ProjectName}", project.Name);
            return Array.Empty<ClassInfo>();
        }

        var classes = new List<ClassInfo>();

        foreach (var document in project.Documents)
        {
            try
            {
                var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
                if (syntaxTree == null)
                {
                    continue;
                }

                var root = await syntaxTree.GetRootAsync(cancellationToken);
                var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var classDeclaration in classDeclarations)
                {
                    var semanticModel = compilation.GetSemanticModel(syntaxTree);
                    var symbol = semanticModel.GetDeclaredSymbol(classDeclaration);
                    if (symbol == null)
                    {
                        continue;
                    }

                    var properties = classDeclaration.Members
                        .OfType<PropertyDeclarationSyntax>()
                        .Select(p => new PropertyInfo(
                            p.Identifier.Text,
                            p.Type.ToString(),
                            p.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) ?? false,
                            p.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) ?? false,
                            p.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.InitAccessorDeclaration)) ?? false))
                        .ToList();

                    var methods = classDeclaration.Members
                        .OfType<MethodDeclarationSyntax>()
                        .Select(m => new MethodInfo(
                            m.Identifier.Text,
                            m.ReturnType.ToString(),
                            m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.AsyncKeyword)),
                            m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)),
                            m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.OverrideKeyword)),
                            m.ParameterList.Parameters
                                .Select(p => new ParameterInfo(p.Identifier.Text, p.Type?.ToString() ?? string.Empty))
                                .ToList()))
                        .ToList();

                    classes.Add(new ClassInfo(
                        classDeclaration.Identifier.Text,
                        symbol.ContainingNamespace.ToString() ?? string.Empty,
                        classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.SealedKeyword)),
                        classDeclaration.Modifiers.Any(m => m.IsKind(SyntaxKind.RecordKeyword)),
                        classDeclaration.BaseList?.Types.Select(t => t.ToString()).ToList() ?? new List<string>(),
                        properties,
                        methods));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to analyze class in file: {FilePath}", document.FilePath);
            }
        }

        return classes;
    }
} 