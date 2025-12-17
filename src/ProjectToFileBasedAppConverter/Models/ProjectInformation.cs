namespace ProjectToFileBasedAppConverter.Models;

public sealed record class ProjectInformation(string SdkType, IList<ProjectProperty> Properties, IList<PackageReference> PackageReferences);
