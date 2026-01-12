namespace ProjectToFileBasedAppConverter.Utilities;

/// <summary>
/// Provides functionality to discover and locate C# project files (.csproj) and C# source files (.cs) based on command-line arguments or directory scanning.
/// </summary>
public static class FileDiscovery
{
    /// <summary>
    /// Discovers the paths to a .csproj file and a .cs source file based on the provided command-line arguments.
    /// </summary>
    /// <param name="args">
    /// The command-line arguments array. Can be:
    /// <list type="bullet">
    /// <item><see langword="null"/> or empty: searches the current directory for files.</item>
    /// <item>One argument: can be a directory path, a .csproj file path, or a .cs file path.</item>
    /// <item>Two arguments: expects both a .csproj file path and a .cs file path (order-independent).</item>
    /// </list>
    /// </param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    /// <item><c>csprojPath</c>: The full path to the discovered .csproj file, or <see langword="null"/> if not found.</item>
    /// <item><c>sourcePath</c>: The full path to the discovered .cs file, or <see langword="null"/> if not found.</item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// When searching directories, the method only succeeds if exactly one .csproj file and exactly one .cs file are found in the specified directory.
    /// If multiple files of the same type are found, <see langword="null"/> is returned for that file type.
    /// </remarks>
    public static (string? csprojPath, string? sourcePath) DiscoverFiles(string[]? args)
    {
        string? csprojPath = null;
        string? sourcePath = null;

        if (args is null || args.Length == 0)
        {
            var currentDir = Directory.GetCurrentDirectory();
            (csprojPath, sourcePath) = FindFilesInDirectory(currentDir);
        }
        else if (args.Length == 1)
        {
            var arg = args[0];

            if (Directory.Exists(arg))
            {
                (csprojPath, sourcePath) = FindFilesInDirectory(arg);
            }
            else if (File.Exists(arg))
            {
                if (arg.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    csprojPath = Path.GetFullPath(arg);
                    var directory = Path.GetDirectoryName(csprojPath);
                    if (directory is not null)
                    {
                        sourcePath = FindCsFile(directory);
                    }
                }
                else if (arg.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    sourcePath = Path.GetFullPath(arg);
                    var directory = Path.GetDirectoryName(sourcePath);
                    if (directory is not null)
                    {
                        csprojPath = FindCsprojFile(directory);
                    }
                }
            }
        }
        else if (args.Length == 2)
        {
            foreach (var arg in args)
            {
                if (File.Exists(arg))
                {
                    if (arg.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase))
                    {
                        csprojPath = Path.GetFullPath(arg);
                    }
                    else if (arg.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    {
                        sourcePath = Path.GetFullPath(arg);
                    }
                }
            }
        }

        return (csprojPath, sourcePath);
    }

    private static (string? csprojPath, string? sourcePath) FindFilesInDirectory(string directory)
    {
        var csprojPath = FindCsprojFile(directory);
        var sourcePath = FindCsFile(directory);
        return (csprojPath, sourcePath);
    }

    private static string? FindCsprojFile(string directory)
    {
        var csprojFiles = Directory.GetFiles(directory, "*.csproj");
        return csprojFiles.Length == 1 ? Path.GetFullPath(csprojFiles[0]) : null;
    }

    private static string? FindCsFile(string directory)
    {
        var csFiles = Directory.GetFiles(directory, "*.cs");
        return csFiles.Length == 1 ? Path.GetFullPath(csFiles[0]) : null;
    }
}
