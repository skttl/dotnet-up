using DotnetUp.Commands;
using DotnetUp.Config;
using DotnetUp.Runner;
using DotnetUp.Update;

var command = args.FirstOrDefault()?.ToLowerInvariant();

switch (command)
{
    case "init":
        return InitCommand.Run(Directory.GetCurrentDirectory());

    case "--help":
    case "-h":
    case "-?":
        PrintHelp();
        return 0;

    case "--version":
        Console.WriteLine(typeof(ProcessRunner).Assembly.GetName().Version?.ToString(3) ?? "1.0.0");
        return 0;

    case null:
    case "":
        try
        {
            await UpdateChecker.CheckAsync();
            var cwd = Directory.GetCurrentDirectory();
            var (configPath, rootDir) = ConfigLoader.FindConfig(cwd);
            var config = ConfigLoader.Load(configPath);
            var runner = new ProcessRunner();
            await runner.RunAsync(config, rootDir);
            return 0;
        }
        catch (FileNotFoundException ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(ex.Message);
            Console.ResetColor();
            return 1;
        }

    default:
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"Unknown command: {command}");
        Console.ResetColor();
        PrintHelp();
        return 1;
}

static void PrintHelp()
{
    Console.WriteLine("""
        dotnet-up — launch all local development processes in parallel

        Usage:
          dotnet up           Start all processes defined in dotnet-up.json
          dotnet up init      Create a dotnet-up.json in the current directory
          dotnet up --help    Show this help
          dotnet up --version Show version

        dotnet-up.json is found by searching the current directory and parent
        directories. You will be asked to confirm if found in a parent.
        """);
}
