using System.CommandLine;
using ProjectToFileBasedApp;

var filesArgument = new Argument<string[]>("files")
{
    Description = "File paths (.csproj and/or C# file) or directory path. If not provided, files will be automatically searched in the current directory. If exactly one file of each type is found, they will be used.",
    Arity = ArgumentArity.ZeroOrMore
};

var outOption = new Option<string?>("--out", "-o")
{
    Description = "Optional output file path for the generated file-based app. If not provided, a file with the same name as the C# file ending with '_FileBased.cs' will be created."
};

var rootCommand = new RootCommand("Reads CSPROJ and CS files information")
{
    filesArgument,
    outOption
};

rootCommand.SetAction(result =>
{
    var files = result.GetValue(filesArgument) ?? [];
    var outputPath = result.GetValue(outOption);

    var (csprojPath, sourcePath) = FileDiscovery.DiscoverFiles(files);

    if (csprojPath is null)
    {
        Console.WriteLine("Error: No .csproj file found (or multiple available in the specified location).");
        return;
    }

    if (sourcePath is null)
    {
        Console.WriteLine("Error: No .cs file found (or multiple available in the specified location).");
        return;
    }

    // Handle output file path
    string? finalOutputPath = null;
    if (outputPath is not null)
    {
        // If -out was provided, check if it already exists
        if (File.Exists(outputPath))
        {
            Console.WriteLine($"Error: Output file already exists: {outputPath}");
            return;
        }

        finalOutputPath = outputPath;
    }
    else if (sourcePath is not null)
    {
        // If -out was not provided, automatically generate the name
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

        writer.WriteLine();

        foreach (var packageReference in projectInfo.PackageReferences)
        {
            writer.WriteLine($"#:package {packageReference.Name}@{packageReference.Version}");
        }

        writer.WriteLine();

        var sourceContent = File.ReadAllText(sourcePath!);
        writer.Write(sourceContent);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error writing output file: {ex.Message}");
    }
});

return rootCommand.Parse(args).Invoke();