using System.Diagnostics;
using DotnetUp.Config;

namespace DotnetUp.Runner;

public sealed class ManagedProcess
{
    private Process? _process;
    private readonly ProcessConfig _config;
    private readonly string _workDir;
    private readonly string _pidFile;

    public string Name => _config.Name;
    public bool ExitHandled { get; set; }
    public bool IsMonitorable { get; private set; }
    public bool HasExited => _process?.HasExited ?? false;
    public int ExitCode => _process?.ExitCode ?? -1;

    public ManagedProcess(ProcessConfig config, string workDir)
    {
        _config = config;
        _workDir = workDir;
        _pidFile = Path.Combine(Path.GetTempPath(), $"dotnet-up-{config.Name}-{Guid.NewGuid():N}.pid");
    }

    public void Start()
    {
        var scriptFile = WriteLaunchScript();

        ProcessStartInfo psi;

        if (OperatingSystem.IsWindows())
        {
            if (IsCommandAvailable("wt"))
            {
                psi = new ProcessStartInfo
                {
                    FileName = "wt",
                    Arguments = $"--window 0 new-tab --title \"{_config.Name}\" --startingDirectory \"{_workDir}\" pwsh -NoExit -File \"{scriptFile}\"",
                    UseShellExecute = true,
                };
                IsMonitorable = false;
            }
            else
            {
                psi = new ProcessStartInfo
                {
                    FileName = "pwsh",
                    Arguments = $"-NoExit -File \"{scriptFile}\"",
                    UseShellExecute = true,
                    WorkingDirectory = _workDir,
                };
                IsMonitorable = false;
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            psi = new ProcessStartInfo
            {
                FileName = "osascript",
                Arguments = $"-e \"tell app \\\"Terminal\\\" to do script \\\"pwsh -NoExit -File '{scriptFile}'\\\"\"",
                UseShellExecute = true,
            };
            IsMonitorable = false;
        }
        else
        {
            var terminal = FindLinuxTerminal();
            if (terminal != null)
            {
                psi = new ProcessStartInfo
                {
                    FileName = terminal,
                    Arguments = $"-- pwsh -NoExit -File \"{scriptFile}\"",
                    UseShellExecute = true,
                };
                IsMonitorable = false;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[{Name}] No terminal emulator found — starting as background process.");
                Console.ResetColor();
                var args = string.Join(" ", _config.Args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a));
                psi = new ProcessStartInfo
                {
                    FileName = _config.Cmd,
                    Arguments = args,
                    WorkingDirectory = _workDir,
                    UseShellExecute = false,
                };
                IsMonitorable = true;
            }
        }

        foreach (var (key, value) in _config.Env)
            psi.Environment[key] = value;

        _process = Process.Start(psi);
    }

    private string WriteLaunchScript()
    {
        var scriptFile = Path.Combine(Path.GetTempPath(), $"dotnet-up-{_config.Name}-{Guid.NewGuid():N}.ps1");
        var args = string.Join(" ", _config.Args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a));
        var innerCmd = $"{_config.Cmd} {args}".Trim();

        var lines = new List<string>();

        foreach (var (key, value) in _config.Env)
            lines.Add($"$env:{key} = '{value.Replace("'", "''")}'");

        lines.Add($"$PID | Out-File -FilePath '{_pidFile}' -Encoding ascii");
        lines.Add($"Set-Location '{_workDir}'");
        lines.Add(innerCmd);

        File.WriteAllLines(scriptFile, lines);
        return scriptFile;
    }

    public void Kill()
    {
        // Try to kill via PID file (spawned terminal window)
        if (File.Exists(_pidFile))
        {
            try
            {
                var pidText = File.ReadAllText(_pidFile).Trim();
                if (int.TryParse(pidText, out var pid))
                {
                    var inner = Process.GetProcessById(pid);
                    inner.Kill(entireProcessTree: true);
                }
            }
            catch { /* process already gone */ }
            finally
            {
                try { File.Delete(_pidFile); } catch { }
            }
            return;
        }

        // Fallback: kill the launcher process directly (background process case)
        try { _process?.Kill(entireProcessTree: true); }
        catch { /* already exited */ }
    }

    private static bool IsCommandAvailable(string cmd)
    {
        try
        {
            using var p = Process.Start(new ProcessStartInfo("where.exe", cmd)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            });
            p?.WaitForExit();
            return p?.ExitCode == 0;
        }
        catch { return false; }
    }

    private static string? FindLinuxTerminal()
    {
        string[] candidates = ["gnome-terminal", "xterm", "konsole", "xfce4-terminal"];
        foreach (var t in candidates)
        {
            using var which = Process.Start(new ProcessStartInfo("which", t)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
            which?.WaitForExit();
            if (which?.ExitCode == 0) return t;
        }
        return null;
    }
}
