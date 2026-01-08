namespace ProjectToFileBasedAppConverter.Models;

/// <summary>
/// Represents a NuGet package reference from the project file.
/// </summary>
/// <param name="Name">The name of the NuGet package.</param>
/// <param name="Version">The version of the package, or <see langword="null"/> if not specified.</param>
public sealed record class PackageReference(string Name, string? Version);
