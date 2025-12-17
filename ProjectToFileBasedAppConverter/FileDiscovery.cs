namespace ProjectToFileBasedAppConverter;

public static class FileDiscovery
{
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
