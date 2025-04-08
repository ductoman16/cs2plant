using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Logging;
using cs2plant.Models;

namespace cs2plant.Core.Services;

/// <summary>
/// Analyzes C# classes to extract class information and dependencies.
/// </summary>
public class ClassAnalyzer(ILogger<ClassAnalyzer> logger)
{
    public async Task<IReadOnlyList<ClassInfo>> AnalyzeClassesAsync(Project project, CancellationToken cancellationToken)
    {
        if (!ValidateProject(project, out var compilation) || compilation is null)
        {
            return Array.Empty<ClassInfo>();
        }

        var classes = new List<ClassInfo>();
        foreach (var document in project.Documents)
        {
            var documentClasses = await AnalyzeDocumentClassesAsync(document, compilation, cancellationToken);
            classes.AddRange(documentClasses);
        }

        return classes;
    }

    private bool ValidateProject(Project? project, out Compilation? compilation)
    {
        compilation = null;

        if (project == null)
        {
            logger.LogWarning("Project is null");
            return false;
        }

        compilation = project.GetCompilationAsync().Result;
        if (compilation == null)
        {
            logger.LogWarning("Failed to get compilation for project: {ProjectName}", project.Name);
            return false;
        }

        return true;
    }

    private async Task<IEnumerable<ClassInfo>> AnalyzeDocumentClassesAsync(
        Document document,
        Compilation compilation,
        CancellationToken cancellationToken)
    {
        var classes = new List<ClassInfo>();

        try
        {
            var syntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
            if (syntaxTree == null) return classes;

            var root = await syntaxTree.GetRootAsync(cancellationToken);
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var typeAnalyzer = new TypeAnalyzer(semanticModel);

            var classDeclarations = GetTopLevelClassDeclarations(root);
            foreach (var classDeclaration in classDeclarations)
            {
                var symbol = semanticModel.GetDeclaredSymbol(classDeclaration);
                if (symbol != null)
                {
                    var context = new ClassAnalysisContext(classDeclaration, semanticModel, typeAnalyzer, symbol);
                    classes.Add(await context.AnalyzeAsync(cancellationToken));
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to analyze class in file: {FilePath}", document.FilePath);
        }

        return classes;
    }

    private static IEnumerable<ClassDeclarationSyntax> GetTopLevelClassDeclarations(SyntaxNode root)
    {
        return root.DescendantNodes()
            .OfType<ClassDeclarationSyntax>()
            .Where(c => c.Parent is not ClassDeclarationSyntax);
    }

    private record ClassMetadata(
        string Name,
        Visibility Visibility,
        bool IsSealed,
        bool IsRecord,
        bool IsAbstract,
        bool IsStatic,
        List<string> BaseTypes);
}