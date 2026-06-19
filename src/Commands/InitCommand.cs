namespace DotnetUp.Commands;

public static class InitCommand
{
    private const string FileName = "dotnet-up.json";

    private static readonly string Template = """
        {
          "$schema": "https://raw.githubusercontent.com/skttl/dotnet-up/main/schema/dotnet-up.schema.json",
          "processes": [
            {
              "name": "example",
              "cmd": "echo",
              "args": ["Hello from dotnet-up!"],
              "workDir": "{root}"
            }
          ]
        }
        """;

    public static int Run(string directory)
    {
        var path = Path.Combine(directory, FileName);

        if (File.Exists(path))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"{FileName} already exists at {path}");
            Console.ResetColor();
            return 1;
        }

        File.WriteAllText(path, Template);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"Created {path}");
        Console.ResetColor();
        Console.WriteLine("Edit the file to define your processes, then run 'dotnet up'.");
        return 0;
    }
}
