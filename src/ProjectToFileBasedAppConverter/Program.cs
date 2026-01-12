using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using ProjectToFileBasedAppConverter;

var filesArgument = new Argument<string[]>("files")
{
    Description = "File paths (.csproj and/or C# file) or directory path. If not provided, files will be automatically searched in the current directory. If exactly one file of each type is found, they will be used.",
    Arity = ArgumentArity.ZeroOrMore
};

var outOption = new Option<string?>("--out", "-o")
{
    Description = "Optional output file path for the generated file-based app. If not provided, a file with the same name as the C# file ending with '_FileBased.cs' will be created."
};

var rootCommand = new RootCommand("""
    Converts traditional C# projects into File-Based Apps by combining project configuration and source code into a single executable file

    Examples:
      dnx FileBasedConverter ./MyProject
      dnx FileBasedConverter MyProject.csproj Program.cs
      dnx FileBasedConverter Program.cs --out MyApp.cs
    """)
{
    filesArgument,
    outOption
};

// Customize the help option to remove the "Usage:" section
var helpOption = rootCommand.Options.FirstOrDefault(o => o is HelpOption) as HelpOption;
if (helpOption is not null)
{
    helpOption.Action = new CustomHelpAction();
}

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

    // Handle output file path.
    string? finalOutputPath = null;
    if (outputPath is not null)
    {
        // If -out was provided, check if it already exists.
        if (File.Exists(outputPath))
        {
            Console.WriteLine($"Error: Output file already exists: {outputPath}");
            return;
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
    }
});

return rootCommand.Parse(args).Invoke();

static void WriteEmptyLineIf(bool condition, StreamWriter writer)
{
    if (condition)
    {
        writer.WriteLine();
    }
}

/// <summary>
/// Custom help action that displays help information without the "Usage:" section.
/// </summary>
file sealed class CustomHelpAction : SynchronousCommandLineAction
{
    public override int Invoke(ParseResult parseResult)
    {
        var command = parseResult.CommandResult.Command;

        // Print description (preserving formatting)
        if (!string.IsNullOrWhiteSpace(command.Description))
        {
            Console.WriteLine("Description:");
            var lines = command.Description.Split('\n');
            foreach (var line in lines)
            {
                Console.WriteLine($"  {line}");
            }

            Console.WriteLine();
        }

        // Print arguments
        if (command.Arguments.Count > 0)
        {
            Console.WriteLine("Arguments:");
            foreach (var arg in command.Arguments)
            {
                Console.WriteLine($"  <{arg.Name}>  {arg.Description}");
            }

            Console.WriteLine();
        }

        // Print options (excluding help and version options to manually control their display)
        var regularOptions = command.Options
            .Where(o => o is not HelpOption && o is not VersionOption)
            .ToList();

        if (regularOptions.Count > 0 || command.Options.Any(o => o is HelpOption))
        {
            Console.WriteLine("Options:");
            
            foreach (var opt in regularOptions)
            {
                // Get all aliases (including the name)
                var allAliases = new List<string> { opt.Name };
                allAliases.AddRange(opt.Aliases.Where(a => a != opt.Name));
                var aliases = string.Join(", ", allAliases.OrderBy(a => a.Length));

                // Determine the help name (parameter name for the option)
                var helpName = "";
                if (!string.IsNullOrEmpty(opt.HelpName))
                {
                    helpName = $" <{opt.HelpName}>";
                }
                else if (opt.ValueType != typeof(bool))
                {
                    // For non-boolean options, show a placeholder based on the option name
                    var placeholder = opt.Name.TrimStart('-');
                    helpName = $" <{placeholder}>";
                }

                Console.WriteLine($"  {aliases}{helpName}  {opt.Description}");
            }
            
            // Manually add help and version options at the end
            Console.WriteLine("  -?, -h, --help   Show help and usage information");
            Console.WriteLine("  --version        Show version information");
        }

        return 0;
    }
}