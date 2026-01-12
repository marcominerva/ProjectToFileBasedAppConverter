using System.CommandLine;
using System.CommandLine.Help;
using ProjectToFileBasedAppConverter.Actions;

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
var helpOption = rootCommand.Options.OfType<HelpOption>().FirstOrDefault();
helpOption?.Action = new CustomHelpAction();

rootCommand.Action = new ConvertProjectAction(filesArgument, outOption);

return rootCommand.Parse(args).Invoke();