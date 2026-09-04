using System.CommandLine;
using System.CommandLine.Invocation;
using ProjectToFileBasedAppConverter.Utilities;

namespace ProjectToFileBasedAppConverter.Actions;

/// <summary>
/// Action that converts a traditional C# project into a file-based app.
/// </summary>
internal sealed class ConvertProjectAction(Argument<string[]> filesArgument, Option<string?> outOption) : AsynchronousCommandLineAction
{
    public override async Task<int> InvokeAsync(ParseResult parseResult, CancellationToken cancellationToken = default)
    {
        var files = parseResult.GetValue(filesArgument) ?? [];
        var outputPath = parseResult.GetValue(outOption);

        var (csprojPath, csSourcePath) = FileDiscovery.DiscoverFiles(files);

        if (csprojPath is null)
        {
            Console.Error.WriteLine("Error: No .csproj file found (or multiple available in the specified location).");
            return 1;
        }

        if (csSourcePath is null)
        {
            Console.Error.WriteLine("Error: No .cs file found (or multiple available in the specified location).");
            return 1;
        }

        string? finalOutputPath = null;
        if (outputPath is not null)
        {
            if (File.Exists(outputPath))
            {
                Console.Error.WriteLine($"Error: Output file already exists: {outputPath}");
                return 1;
            }

            finalOutputPath = outputPath;
        }
        else if (csSourcePath is not null)
        {
            var directory = Path.GetDirectoryName(csSourcePath) ?? ".";
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(csSourcePath);
            finalOutputPath = Path.Combine(directory, $"{fileNameWithoutExtension}_FileBasedApp.cs");

            if (File.Exists(finalOutputPath))
            {
                Console.Error.WriteLine($"Error: Output file already exists: {finalOutputPath}");
                return 1;
            }
        }

        var reader = new CsprojReader(csprojPath);
        var projectInfo = reader.GetProjectInformation();
        var csprojDirectory = Path.GetDirectoryName(Path.GetFullPath(csprojPath)) ?? Directory.GetCurrentDirectory();
        var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(finalOutputPath!)) ?? Directory.GetCurrentDirectory();

        // Write the file based app file.
        try
        {
            using var writer = new StreamWriter(finalOutputPath!);

            writer.WriteLine("#!/usr/bin/env dotnet");
            writer.WriteLine();
            writer.WriteLine($"#:sdk {projectInfo.SdkType}");
            writer.WriteLine();

            foreach (var property in projectInfo.Properties)
            {
                writer.WriteLine($"#:property {property.Name}={property.Value}");
            }

            WriteEmptyLineIf(projectInfo.Properties.Count > 0, writer);

            foreach (var packageReference in projectInfo.PackageReferences)
            {
                var packageDirective = string.IsNullOrWhiteSpace(packageReference.Version)
                    ? packageReference.Name
                    : $"{packageReference.Name}@{packageReference.Version}";

                writer.WriteLine($"#:package {packageDirective}");
            }

            WriteEmptyLineIf(projectInfo.PackageReferences.Count > 0, writer);

            foreach (var projectReference in projectInfo.ProjectReferences)
            {
                var projectReferencePath = GetProjectReferencePath(projectReference.Path, csprojDirectory, outputDirectory);
                writer.WriteLine($"#:project {projectReferencePath}");
            }

            WriteEmptyLineIf(projectInfo.ProjectReferences.Count > 0, writer);

            foreach (var usingDirective in projectInfo.UsingDirectives)
            {
                if (!string.IsNullOrWhiteSpace(usingDirective.Alias))
                {
                    writer.WriteLine($"global using {usingDirective.Alias} = {usingDirective.Namespace};");
                }
                else if (usingDirective.IsStatic)
                {
                    writer.WriteLine($"global using static {usingDirective.Namespace};");
                }
                else
                {
                    writer.WriteLine($"global using {usingDirective.Namespace};");
                }
            }

            WriteEmptyLineIf(projectInfo.UsingDirectives.Count > 0, writer);

            var sourceContent = await File.ReadAllTextAsync(csSourcePath!, cancellationToken);
            writer.Write(sourceContent);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error writing output file: {ex.Message}");
            return 1;
        }

        return 0;
    }

    private static void WriteEmptyLineIf(bool condition, StreamWriter writer)
    {
        if (condition)
        {
            writer.WriteLine();
        }
    }

    private static string GetProjectReferencePath(string projectReferencePath, string csprojDirectory, string outputDirectory)
    {
        var fullProjectReferencePath = Path.GetFullPath(projectReferencePath, csprojDirectory);
        return Path.GetRelativePath(outputDirectory, fullProjectReferencePath);
    }
}
