namespace ProjectToFileBasedAppConverter.Models;

/// <summary>
/// Represents a global using directive from the project file.
/// </summary>
/// <param name="Namespace">The namespace to include or remove.</param>
/// <param name="IsRemove">Indicates whether this is a Remove directive (true) or Include directive (false).</param>
public sealed record class UsingDirective(string Namespace, bool IsRemove = false);
