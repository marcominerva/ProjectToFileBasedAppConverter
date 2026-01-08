# Project To File-Based App Converter

A command-line tool that converts traditional C# projects into [File-Based Apps](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/tutorials/file-based-programs), combining project configuration and source code into a single executable file.

## Installation and Usage

### Run without installation (recommended)

You can run the tool directly without installing it using `dotnet tool execute` (or the shorthand `dnx`):

```bash
dnx FileBasedConverter [files] [options]
```

This is the recommended approach as it doesn't require permanent installation and always uses the latest version available.

### Install as a global tool

Alternatively, you can install the tool as a global .NET tool:

```bash
dotnet tool install -g FileBasedConverter
```

After installation, you can use the tool with the `filebased-convert` command.

## Purpose

This tool simplifies the distribution and execution of C# applications by creating self-contained file-based apps. It reads a `.csproj` file and a C# source file, then generates a single `.cs` file that includes:

- The project SDK information
- Project properties
- Package references
- Project references (references to other projects in the solution)
- The original source code

The resulting file can be executed directly using the `dotnet` command without requiring a separate project file.

## Command Reference

### When using dnx (no installation required)

```bash
dnx FileBasedConverter [files] [options]
```

### When installed as a global tool

```bash
filebased-convert [files] [options]
```

### Arguments

- `files` (optional): File paths (CSPROJ and/or C# files) or directory path
  - If not provided, files will be automatically searched in the current directory
  - If exactly one file of each type is found, they will be used
  - You can specify:
    - A single `.csproj` file (the tool will search for a single `.cs` file in the same directory)
    - A single `.cs` file (the tool will search for a single `.csproj` file in the same directory)
    - Both `.csproj` and `.cs` files
    - A directory path (the tool will search for both files in that directory)

### Options

- `--out`, `-o` (optional): Output file path for the generated file-based app
  - If not provided, a file with the same name as the C# file ending with `_FileBased.cs` will be created in the same directory

## Examples

### Using dnx (without installation)

Specify a directory:
```bash
dnx FileBasedConverter ./MyProject
```

Specify both files:
```bash
dnx FileBasedConverter MyProject.csproj Program.cs
```

Specify only the C# file:
```bash
dnx FileBasedConverter Program.cs
```

Specify custom output file:
```bash
dnx FileBasedConverter Program.cs --out MyApp.cs
```

### Using installed global tool

Specify a directory:
```bash
filebased-convert ./MyProject
```

Specify both files:
```bash
filebased-convert MyProject.csproj Program.cs
```

Specify only the C# file:
```bash
filebased-convert Program.cs
```

Specify custom output file:
```bash
filebased-convert Program.cs --out MyApp.cs
```

## Output Format

The generated file-based app includes:

```csharp
#!/usr/bin/env dotnet

#:sdk Microsoft.NET.Sdk

#:property PropertyName=PropertyValue
#:property AnotherProperty=AnotherValue

#:package PackageName@Version
#:package AnotherPackage@AnotherVersion

#:project ../ReferencedProject/ReferencedProject.csproj
#:project ../AnotherProject/AnotherProject.csproj

global using System.Text;
global using System.Collections.Generic;

// Original C# source code follows...
```

## Notes

- The tool automatically adds `PublishAot=false` if not already present in the project properties. This is necessary because File-Based Apps have `PublishAot=true` by default, and adding this property explicitly ensures the original project behavior is maintained
- Project references (`<ProjectReference>` elements in the `.csproj`) are converted to `#:project` directives with their relative paths preserved
- Global using directives (`<Using Include="...">` elements in the `.csproj`) are converted to `global using` statements in the generated file:
  - Regular using: `<Using Include="System" />` → `global using System;`
  - Using with alias: `<Using Include="System.Text" Alias="Text" />` → `global using Text = System.Text;`
  - Static using: `<Using Include="System.Math" Static="true" />` → `global using static System.Math;`
- If the output file already exists, the tool will display an error and exit without overwriting
- The tool requires exactly one `.csproj` file and one `.cs` file to be found or specified

## Requirements

- .NET 10 or later