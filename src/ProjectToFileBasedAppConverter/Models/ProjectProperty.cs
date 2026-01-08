namespace ProjectToFileBasedAppConverter.Models;

/// <summary>
/// Represents a property defined in a C# project file.
/// </summary>
/// <param name="Name">The name of the property (e.g., "TargetFramework", "OutputType").</param>
/// <param name="Value">The value assigned to the property.</param>
public sealed record class ProjectProperty(string Name, string Value);
