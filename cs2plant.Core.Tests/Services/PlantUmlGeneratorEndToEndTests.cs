using System.Diagnostics;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Extensions.Logging;
using System.Text;
using cs2plant.Models;
using cs2plant.Services;
using Xunit;
using Xunit.Abstractions;

namespace cs2plant.Tests.Services;

public sealed class PlantUmlGeneratorEndToEndTests : IDisposable
{
    private readonly ILogger<PlantUmlGenerator> _logger;
    private readonly PlantUmlGenerator _generator;
    private readonly string _tempFilePath;
    private readonly ITestOutputHelper _output;
    private readonly string _plantUmlJarPath;

    public PlantUmlGeneratorEndToEndTests(ITestOutputHelper output)
    {
        _output = output;
        _logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<PlantUmlGenerator>();
        _generator = new PlantUmlGenerator(_logger);
        _tempFilePath = Path.Combine(Path.GetTempPath(), $"test-diagram-{Guid.NewGuid()}.puml");
        _plantUmlJarPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "cs2plant.Core", "Resources", "plantuml.jar");
    }

    [Fact]
    public void GenerateDiagram_WithComplexProject_GeneratesValidPlantUml()
    {
        // Verify PlantUML jar exists
        File.Exists(_plantUmlJarPath).Should().BeTrue("PlantUML jar file should exist at: " + _plantUmlJarPath);

        // Arrange
        var dependencies = new[]
        {
            new ProjectDependency(
                "TestProject.Core",
                "/path/to/core",
                "net8.0",
                new[] { "Microsoft.Extensions.Logging@8.0.0" },
                Array.Empty<string>(),
                new[]
                {
                    new ClassInfo(
                        "TestService",
                        "TestProject.Core.Services",
                        true, // IsSealed
                        false, // IsRecord
                        new[] { "ITestService" },
                        new[]
                        {
                            new PropertyInfo("Id", "int", true, false, true),
                            new PropertyInfo("Name", "string", true, true, false)
                        },
                        new[]
                        {
                            new MethodInfo("ProcessData", "Task<bool>", true, false, false,
                                new[] { new ParameterInfo("data", "string") })
                        })
                }),
            new ProjectDependency(
                "TestProject.Api",
                "/path/to/api",
                "net8.0",
                new[] { "Microsoft.AspNetCore.OpenApi@8.0.0" },
                new[] { "TestProject.Core" },
                Array.Empty<ClassInfo>())
        };

        try
        {
            // Act
            var plantUml = _generator.GenerateDiagram(dependencies);
            File.WriteAllText(_tempFilePath, plantUml);

            // Log the generated PlantUML
            _output.WriteLine("Generated PlantUML:");
            _output.WriteLine(plantUml);

            // Assert
            using (new AssertionScope())
            {
                // Verify diagram syntax using PlantUML jar
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-jar \"{_plantUmlJarPath}\" -checkonly \"{_tempFilePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                using var process = Process.Start(processStartInfo);
                process.Should().NotBeNull();
                process!.WaitForExit();

                // Log any output or errors
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (!string.IsNullOrEmpty(output))
                {
                    _output.WriteLine("PlantUML Output:");
                    _output.WriteLine(output);
                }

                if (!string.IsNullOrEmpty(error))
                {
                    _output.WriteLine("PlantUML Error:");
                    _output.WriteLine(error);
                }

                process.ExitCode.Should().Be(0, "PlantUML syntax should be valid");

                // Also verify key content is present
                plantUml.Should().StartWith("@startuml");
                plantUml.Should().EndWith("@enduml\n");
                plantUml.Should().Contain("[TestProject.Core]");
                plantUml.Should().Contain("[TestProject.Api]");
                plantUml.Should().Contain("TestProject.Core.Services");
                plantUml.Should().Contain("class TestService <<sealed>>");
                plantUml.Should().Contain("interface ITestService");
                plantUml.Should().Contain("+ Id: int { get init }");
                plantUml.Should().Contain("+ Name: string { get set }");
                plantUml.Should().Contain("+ async ProcessData(data: string): Task<bool>");
                plantUml.Should().Contain("[TestProject.Core] <-- [TestProject.Api]");
            }
        }
        finally
        {
            // Cleanup
            if (File.Exists(_tempFilePath))
            {
                File.Delete(_tempFilePath);
            }
        }
    }

    [Fact]
    public void GenerateDiagram_ShouldContainCompleteUmlStructure()
    {
        // Arrange
        var dependencies = new[]
        {
            new ProjectDependency(
                "cs2plant.Core",
                "/path/to/core",
                "net8.0",
                new[] { "Package1@1.0.0", "Package2@2.0.0" },
                Array.Empty<string>(),
                Array.Empty<ClassInfo>()),
            new ProjectDependency(
                "cs2plant.Web",
                "/path/to/web",
                "net8.0",
                new[] { "Package3@3.0.0" },
                new[] { "cs2plant.Core" },
                Array.Empty<ClassInfo>())
        };

        // Act
        var plantUml = _generator.GenerateDiagram(dependencies);

        // Write the PlantUML to a file
        File.WriteAllText(_tempFilePath, plantUml);

        // Run PlantUML jar to validate syntax
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar \"{_plantUmlJarPath}\" -checkonly \"{_tempFilePath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        process.WaitForExit();

        // Log any output or errors
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();

        if (!string.IsNullOrEmpty(output))
        {
            _output.WriteLine("PlantUML Output:");
            _output.WriteLine(output);
        }

        if (!string.IsNullOrEmpty(error))
        {
            _output.WriteLine("PlantUML Error:");
            _output.WriteLine(error);
        }

        // Assert
        process.ExitCode.Should().Be(0, "PlantUML syntax should be valid");

        // Also verify key content
        using (new AssertionScope())
        {
            plantUml.Should().StartWith("@startuml");
            plantUml.Should().EndWith("@enduml\n");
            plantUml.Should().Contain("[cs2plant.Core]");
            plantUml.Should().Contain("[cs2plant.Web]");
            plantUml.Should().Contain("Packages:\n  - Package1@1.0.0\n  - Package2@2.0.0");
            plantUml.Should().Contain("Packages:\n  - Package3@3.0.0");
            plantUml.Should().Contain("[cs2plant.Core] <-- [cs2plant.Web]");
        }
    }

    public void Dispose()
    {
        if (File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }
} 