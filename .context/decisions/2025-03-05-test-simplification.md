---
type: decision
title: Maintain PlantUML Execution in Tests
created: 2025-03-05T13:07:19
updated: 2025-03-05T13:07:19
status: rejected
tags: [testing, validation, quality]
---

# Context
Considered removing PlantUML jar execution from tests in favor of string validation. However, this would have been a mistake as PlantUML syntax validation is critical to the tool's functionality.

# Decision
Keep PlantUML jar execution in tests because:
1. It validates that generated PlantUML is actually valid and can be rendered
2. It catches syntax errors that string validation alone would miss
3. It serves as an integration test to verify our tool works with PlantUML
4. The tool's primary purpose is to generate valid PlantUML diagrams

# Implementation
Maintain existing test implementation in:
- cs2plant.Core.Tests/Services/PlantUmlGeneratorEndToEndTests.cs
- cs2plant.Core.Tests/Services/PlantUmlGeneratorTests.cs

The tests will:
1. Generate PlantUML diagram
2. Write to temporary file
3. Execute PlantUML jar with -checkonly flag
4. Verify execution succeeds
5. Additionally verify content with string assertions

Example:
```csharp
// Verify PlantUML jar exists
File.Exists(_plantUmlJarPath).Should().BeTrue();

// Generate and validate
var plantUml = _generator.GenerateDiagram(dependencies);
File.WriteAllText(_tempFilePath, plantUml);

// Verify syntax using PlantUML jar
var process = Process.Start(new ProcessStartInfo
{
    FileName = "java",
    Arguments = $"-jar \"{_plantUmlJarPath}\" -checkonly \"{_tempFilePath}\"",
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    UseShellExecute = false
});

process.WaitForExit();
process.ExitCode.Should().Be(0, "PlantUML syntax should be valid");
```

# Consequences

## Positive
- Ensures generated PlantUML is actually valid
- Catches syntax errors early
- Provides true end-to-end testing
- Maintains high quality standards

## Negative
- Tests require Java runtime
- Tests are slightly slower
- More complex test setup

## Mitigations
1. Document Java requirement in README
2. Consider adding CI check for Java presence
3. Keep tests well-organized and documented

# References
- [PlantUML Command Line](https://plantuml.com/command-line)
- [XUnit Best Practices](https://xunit.net/docs/best-practices) 