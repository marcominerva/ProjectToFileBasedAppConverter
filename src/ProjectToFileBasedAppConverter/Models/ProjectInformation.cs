namespace ProjectToFileBasedAppConverter.Models;

/// <summary>
/// Contains information extracted from a C# project file, including SDK type, properties, package references, and project references.
/// </summary>
/// <param name="SdkType">The SDK type specified in the project file.</param>
/// <param name="Properties">The list of properties defined in the project.</param>
/// <param name="PackageReferences">The list of NuGet package references.</param>
/// <param name="ProjectReferences">The list of project-to-project references.</param>
public sealed record class ProjectInformation(string SdkType, IList<ProjectProperty> Properties, IList<PackageReference> PackageReferences, IList<ProjectReference> ProjectReferences);
