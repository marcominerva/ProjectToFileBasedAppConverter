using System.CommandLine;
using System.CommandLine.Invocation;
using ProjectToFileBasedAppConverter.Utilities;

namespace ProjectToFileBasedAppConverter.Actions;

/// <summary>
/// Action that converts a traditional C# project into a file-based app.
/// </summary>
sealed class ConvertProjectAction(Argument<string[]> filesArgument, Option<string?> outOption) : SynchronousCommandLineAction
{
    public override int Invoke(ParseResult parseResult)
    {
        var files = parseResult.GetValue(filesArgument) ?? [];
        var outputPath = parseResult.GetValue(outOption);

        var (csprojPath, sourcePath) = FileDiscovery.DiscoverFiles(files);

        if (csprojPath is null)
        {
            Console.WriteLine("Error: No .csproj file found (or multiple available in the specified location).");
            return 1;
        }

        if (sourcePath is null)
        {
            Console.WriteLine("Error: No .cs file found (or multiple available in the specified location).");
            return 1;
        }

        // Handle output file path.
        string? finalOutputPath = null;
        if (outputPath is not null)
        {
            // If -out was provided, check if it already exists.
            if (File.Exists(outputPath))
            {
                Console.WriteLine($"Error: Output file already exists: {outputPath}");
                return 1;
            }

            finalOutputPath = outputPath;
        }
        else if (sourcePath is not null)
        {
            // If -out was not provided, automatically generate the name.
            var directory = Path.GetDirectoryName(sourcePath) ?? ".";
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourcePath);
            finalOutputPath = Path.Combine(directory, $"{fileNameWithoutExtension}_FileBased.cs");
        }

        var reader = new CsprojReader(csprojPath);
        var projectInfo = reader.GetProjectInformation();

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
                writer.WriteLine($"#:package {packageReference.Name}@{packageReference.Version}");
            }

            WriteEmptyLineIf(projectInfo.PackageReferences.Count > 0, writer);

            foreach (var projectReference in projectInfo.ProjectReferences)
            {
                writer.WriteLine($"#:project {projectReference.Path}");
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

            var sourceContent = File.ReadAllText(sourcePath!);
            writer.Write(sourceContent);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error writing output file: {ex.Message}");
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
}
