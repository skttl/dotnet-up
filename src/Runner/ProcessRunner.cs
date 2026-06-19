using DotnetUp.Config;

namespace DotnetUp.Runner;

public sealed class ProcessRunner
{
    private readonly List<ManagedProcess> _processes = [];
    private readonly CancellationTokenSource _cts = new();

    public async Task RunAsync(UpConfig config, string rootDir)
    {
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            _cts.Cancel();
        };

        foreach (var proc in config.Processes)
        {
            var workDir = ConfigLoader.ResolveWorkDir(proc.WorkDir, rootDir);
            if (!RunBefore(proc, workDir)) continue;

            var managed = new ManagedProcess(proc, workDir);
            managed.Start();
            _processes.Add(managed);
        }

        if (_processes.Count == 0)
        {
            Console.WriteLine("No processes started.");
            return;
        }

        PrintStarted();
        await MonitorAsync();
        await StopAllAsync();
    }

    private static bool RunBefore(ProcessConfig proc, string workDir)
    {
        foreach (var cmd in proc.RunBefore)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"[{proc.Name}] > {cmd}");
            Console.ResetColor();

            var (shell, shellArgs) = GetShell(cmd);
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = shell,
                Arguments = shellArgs,
                WorkingDirectory = workDir,
                UseShellExecute = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false,
            };

            using var p = System.Diagnostics.Process.Start(psi);
            p?.WaitForExit();

            if (p?.ExitCode != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[{proc.Name}] runBefore failed (exit {p?.ExitCode}): {cmd}");
                Console.WriteLine($"[{proc.Name}] Skipping this process.");
                Console.ResetColor();
                return false;
            }
        }
        return true;
    }

    private void PrintStarted()
    {
        Console.WriteLine();
        foreach (var p in _processes)
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"  [{p.Name}] ");
            Console.ResetColor();
            Console.WriteLine($"started in new terminal window");
        }
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Press Ctrl+C to stop all.");
        Console.ResetColor();
        Console.WriteLine();
    }

    private async Task MonitorAsync()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            foreach (var p in _processes.Where(p => p.IsMonitorable && p.HasExited && !p.ExitHandled))
            {
                p.ExitHandled = true;
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[{p.Name}] Process exited with code {p.ExitCode}.");
                Console.ResetColor();
                Console.Write("Keep other processes running? [Y/n] ");
                var answer = Console.ReadLine()?.Trim().ToLowerInvariant();
                if (answer is "n" or "no")
                {
                    _cts.Cancel();
                    return;
                }
            }

            var monitorable = _processes.Where(p => p.IsMonitorable).ToList();
            if (monitorable.Count > 0 && monitorable.All(p => p.HasExited))
            {
                Console.WriteLine("All processes have exited.");
                _cts.Cancel();
                return;
            }

            try { await Task.Delay(500, _cts.Token); }
            catch (TaskCanceledException) { return; }
        }
    }

    private async Task StopAllAsync()
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Shutting down...");
        Console.ResetColor();

        var tasks = _processes.Select(p => Task.Run(() => p.Kill())).ToArray();
        await Task.WhenAll(tasks);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("Done.");
        Console.ResetColor();
    }

    private static (string shell, string args) GetShell(string cmd)
    {
        if (OperatingSystem.IsWindows())
            return ("cmd.exe", $"/C {cmd}");
        return ("/bin/sh", $"-c \"{cmd.Replace("\"", "\\\"")}\"");
    }
}
