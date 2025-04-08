using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using cs2plant.Services;
using cs2plant.Core.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
});

builder.Services.AddTransient<MSBuildProjectLoader>();
builder.Services.AddTransient<ClassAnalyzer>();
builder.Services.AddTransient<IDependencyAnalyzer, MSBuildDependencyAnalyzer>();
builder.Services.AddTransient<IPlantUmlGenerator, PlantUmlGenerator>();

using var host = builder.Build();

if (args.Length != 2)
{
    Console.WriteLine("Usage: cs2plant.Console <solution-path> <output-path>");
    return 1;
}

var solutionPath = args[0];
var outputPath = args[1];

try
{
    var analyzer = host.Services.GetRequiredService<IDependencyAnalyzer>();
    var generator = host.Services.GetRequiredService<IPlantUmlGenerator>();

    var dependencies = await analyzer.AnalyzeProjectAsync(solutionPath);
    var plantUml = generator.GenerateDiagram(dependencies);

    await File.WriteAllTextAsync(outputPath, plantUml);
    Console.WriteLine($"PlantUML diagram generated successfully at {outputPath}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Error: {ex.Message}");
    return 1;
}
