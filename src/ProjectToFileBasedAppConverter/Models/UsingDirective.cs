namespace ProjectToFileBasedAppConverter.Models;

/// <summary>
/// Represents a global using directive from the project file.
/// </summary>
/// <param name="Namespace">The namespace to include.</param>
/// <param name="Alias">The optional alias for the namespace.</param>
/// <param name="IsStatic">Indicates whether this is a static using directive.</param>
public sealed record class UsingDirective(string Namespace, string? Alias = null, bool IsStatic = false);
