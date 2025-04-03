# cs2plant.Console

A command-line tool for generating PlantUML diagrams from C# solutions.

## Features

- Analyzes C# solutions and projects
- Extracts class information and dependencies
- Generates PlantUML diagrams showing:
  - Project dependencies
  - Class hierarchies
  - Properties and methods
  - Package references

## Requirements

- .NET 8.0 SDK
- Java Runtime Environment (JRE) for PlantUML

## Installation

1. Clone the repository
2. Build the solution:
   ```bash
   dotnet build
   ```

## Usage

```bash
dotnet run --project cs2plant.Console <solution-path> <output-path>
```

### Parameters

- `solution-path`: Path to the .sln file to analyze
- `output-path`: Path where the PlantUML diagram will be saved

### Example

```bash
dotnet run --project cs2plant.Console ./MyProject.sln ./dependency-diagram.puml
```

## Output

The tool generates a PlantUML diagram that includes:

- Project components and their dependencies
- Class diagrams for each project
- Package references as notes
- Inheritance relationships
- Property and method details

## Dependencies

### NuGet Packages

- Microsoft.Build
- Microsoft.Build.Framework
- Microsoft.Build.Locator
- Microsoft.CodeAnalysis.CSharp
- Microsoft.CodeAnalysis.CSharp.Workspaces
- Microsoft.Extensions.Logging

### Project References

- cs2plant.Core (project reference)

## License

MIT
