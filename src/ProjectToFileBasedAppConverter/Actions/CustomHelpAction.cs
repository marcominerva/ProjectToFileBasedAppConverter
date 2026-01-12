using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Invocation;

namespace ProjectToFileBasedAppConverter.Actions;

/// <summary>
/// Custom help action that displays help information without the "Usage:" section.
/// </summary>
internal sealed class CustomHelpAction : SynchronousCommandLineAction
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
            .Where(o => o is not HelpOption and not VersionOption)
            .ToList();

        if (regularOptions.Count > 0 || command.Options.Any(o => o is HelpOption))
        {
            Console.WriteLine("Options:");

            foreach (var options in regularOptions)
            {
                // Get all aliases (including the name)
                var aliases = string.Join(", ", new[] { options.Name }
                    .Concat(options.Aliases.Where(a => a != options.Name))
                    .OrderBy(a => a.Length));

                // Determine the help name (parameter name for the option)
                var helpName = string.Empty;
                if (!string.IsNullOrEmpty(options.HelpName))
                {
                    helpName = $" <{options.HelpName}>";
                }
                else if (options.ValueType != typeof(bool))
                {
                    // For non-boolean options, show a placeholder based on the option name
                    var placeholder = options.Name.TrimStart('-');
                    helpName = $" <{placeholder}>";
                }

                Console.WriteLine($"  {aliases}{helpName}  {options.Description}");
            }

            // Add help option
            var helpOption = command.Options.OfType<HelpOption>().FirstOrDefault();
            if (helpOption is not null)
            {
                var helpAliases = string.Join(", ", new[] { helpOption.Name }
                    .Concat(helpOption.Aliases.Where(a => a != helpOption.Name))
                    .OrderBy(a => a.Length));

                Console.WriteLine($"  {helpAliases}   Show help and usage information");
            }

            // Add version option
            var versionOption = command.Options.OfType<VersionOption>().FirstOrDefault();
            if (versionOption is not null)
            {
                Console.WriteLine($"  {versionOption.Name}        Show version information");
            }
        }

        return 0;
    }
}
