namespace ProjectToFileBasedAppConverter.Models;

/// <summary>
/// Represents a reference to another project in the solution.
/// </summary>
/// <param name="Path">The relative path to the referenced project file.</param>
public sealed record class ProjectReference(string Path);
