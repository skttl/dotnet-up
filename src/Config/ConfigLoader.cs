using System.Text.Json;

namespace DotnetUp.Config;

public static class ConfigLoader
{
    private const string FileName = "dotnet-up.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>
    /// Finds dotnet-up.json starting from <paramref name="startDir"/>, walking up the tree.
    /// Returns (configPath, rootDir) or throws if not found.
    /// Prompts the user for confirmation if the file is not in <paramref name="startDir"/> itself.
    /// </summary>
    public static (string ConfigPath, string RootDir) FindConfig(string startDir)
    {
        var dir = new DirectoryInfo(startDir);

        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, FileName);
            if (File.Exists(candidate))
            {
                if (!string.Equals(dir.FullName, startDir, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine();
                    Console.Write($"Found {FileName} in: ");
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine(dir.FullName);
                    Console.ResetColor();
                    Console.Write("Start dev processes from there? [Y/n] ");
                    var answer = Console.ReadLine()?.Trim().ToLowerInvariant();
                    if (answer is "n" or "no")
                    {
                        Console.WriteLine("Aborted.");
                        Environment.Exit(0);
                    }
                }
                return (candidate, dir.FullName);
            }
            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            $"{FileName} not found in '{startDir}' or any parent directory. " +
            $"Run 'dotnet up init' to create one.");
    }

    public static UpConfig Load(string configPath)
    {
        var json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<UpConfig>(json, JsonOptions)
               ?? throw new InvalidOperationException($"Failed to parse {configPath}");
    }

    /// <summary>Resolves {root} token to the directory containing dotnet-up.json.</summary>
    public static string ResolveWorkDir(string workDir, string rootDir)
    {
        var resolved = workDir.Replace("{root}", rootDir);
        return Path.GetFullPath(resolved);
    }
}
