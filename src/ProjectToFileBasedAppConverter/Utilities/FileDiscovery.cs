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
    /// <item><c>CsprojPath</c>: The full path to the discovered .csproj file, or <see langword="null"/> if not found.</item>
    /// <item><c>CsSourcePath</c>: The full path to the discovered .cs file, or <see langword="null"/> if not found.</item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// When searching directories, the method only succeeds if exactly one .csproj file and exactly one .cs file are found in the specified directory.
    /// If multiple files of the same type are found, <see langword="null"/> is returned for that file type.
    /// </remarks>
    public static (string? CsprojPath, string? CsSourcePath) DiscoverFiles(string[]? args)
    {
        string? csprojPath = null;
        string? csSourcePath = null;

        if (args is null || args.Length == 0)
        {
            var currentDir = Directory.GetCurrentDirectory();
            (csprojPath, csSourcePath) = FindFilesInDirectory(currentDir);
        }
        else if (args is [var arg])
        {
            if (Directory.Exists(arg))
            {
                (csprojPath, csSourcePath) = FindFilesInDirectory(arg);
            }
            else if (File.Exists(arg))
            {
                if (IsCsproj(arg))
                {
                    // If it's a .csproj file, find the .cs file in the same directory (if there is only one).
                    csprojPath = Path.GetFullPath(arg);
                    var directory = Path.GetDirectoryName(csprojPath);
                    if (directory is not null)
                    {
                        csSourcePath = FindCsFile(directory);
                    }
                }
                else if (IsCsSource(arg))
                {
                    // If it's a .cs file, find the .csproj file in the same directory (if there is only one).
                    csSourcePath = Path.GetFullPath(arg);
                    var directory = Path.GetDirectoryName(csSourcePath);
                    if (directory is not null)
                    {
                        csprojPath = FindCsprojFile(directory);
                    }
                }
            }
        }
        else if (args is [var arg0, var arg1] && File.Exists(arg0) && File.Exists(arg1))
        {
            // If two file paths are provided, determine which is the .csproj and which is the .cs file (order-independent).
            (csprojPath, csSourcePath) = (arg0, arg1) switch
            {
                var (a, b) when IsCsproj(a) && IsCsSource(b) => (Path.GetFullPath(a), Path.GetFullPath(b)),
                var (a, b) when IsCsSource(a) && IsCsproj(b) => (Path.GetFullPath(b), Path.GetFullPath(a)),
                _ => (null, null)
            };
        }

        return (csprojPath, csSourcePath);
    }

    private static (string? CsprojPath, string? CsSourcePath) FindFilesInDirectory(string directory)
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

    private static bool IsCsproj(string path)
        => Path.GetExtension(path).Equals(".csproj", StringComparison.OrdinalIgnoreCase);

    private static bool IsCsSource(string path)
        => Path.GetExtension(path).Equals(".cs", StringComparison.OrdinalIgnoreCase);
}
