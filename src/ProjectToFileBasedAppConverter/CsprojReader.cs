using System.Xml.Linq;
using ProjectToFileBasedAppConverter.Models;

namespace ProjectToFileBasedAppConverter;

/// <summary>
/// Provides functionality to read and parse C# project files (.csproj) and extract project information.
/// </summary>
public sealed class CsprojReader
{
    private readonly string csprojPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="CsprojReader"/> class for the specified project file.
    /// </summary>
    /// <param name="csprojPath">The full path to the .csproj file to read.</param>
    /// <exception cref="FileNotFoundException">Thrown when the specified project file does not exist.</exception>
    public CsprojReader(string csprojPath)
    {
        if (!File.Exists(csprojPath))
        {
            throw new FileNotFoundException($"The project file was not found at: {csprojPath}", csprojPath);
        }

        this.csprojPath = csprojPath;
    }

    /// <summary>
    /// Extracts and returns comprehensive information from the project file, including SDK type, properties, package references, project references, and global using directives.
    /// </summary>
    /// <returns>A <see cref="ProjectInformation"/> object containing all extracted project details.</returns>
    /// <remarks>
    /// This method ensures that the <c>PublishAot</c> property is set to <see langword="false"/> if it is not explicitly defined in the project file,
    /// to maintain compatibility with the original project behavior when converting to a file-based app.
    /// </remarks>
    public ProjectInformation GetProjectInformation()
    {
        var doc = XDocument.Load(csprojPath);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

        var sdkType = doc.Root?.Attribute("Sdk")?.Value ?? string.Empty;

        var properties = new List<ProjectProperty>();
        var propertyGroups = doc.Descendants(ns + "PropertyGroup");
        foreach (var propertyGroup in propertyGroups)
        {
            foreach (var property in propertyGroup.Elements())
            {
                properties.Add(new(property.Name.LocalName, property.Value));
            }
        }

        // In File Based Apps, PublishAot is true by default, so if the source project does not set this property, we set it to false to
        // ensure the original behavior.
        if (!properties.Any(p => p.Name == "PublishAot"))
        {
            properties.Add(new("PublishAot", "false"));
        }

        var packageReferences = new List<PackageReference>();
        var packageReferenceElements = doc.Descendants(ns + "PackageReference");
        foreach (var packageReference in packageReferenceElements)
        {
            var packageName = packageReference.Attribute("Include")?.Value;
            var version = packageReference.Attribute("Version")?.Value ?? packageReference.Element(ns + "Version")?.Value;

            if (packageName is not null)
            {
                packageReferences.Add(new(packageName, version));
            }
        }

        var projectReferences = new List<ProjectReference>();
        var projectReferenceElements = doc.Descendants(ns + "ProjectReference");
        foreach (var projectReference in projectReferenceElements)
        {
            var projectPath = projectReference.Attribute("Include")?.Value;

            if (projectPath is not null)
            {
                projectReferences.Add(new(projectPath));
            }
        }

        var usingDirectives = new List<UsingDirective>();
        var usingElements = doc.Descendants(ns + "Using");
        foreach (var usingElement in usingElements)
        {
            var includeNamespace = usingElement.Attribute("Include")?.Value;

            if (!string.IsNullOrWhiteSpace(includeNamespace))
            {
                var alias = usingElement.Attribute("Alias")?.Value;
                var isStatic = bool.TryParse(usingElement.Attribute("Static")?.Value, out var staticValue) && staticValue;
                usingDirectives.Add(new(includeNamespace, alias, isStatic));
            }
        }

        return new ProjectInformation(sdkType, properties, packageReferences, projectReferences, usingDirectives);
    }
}
