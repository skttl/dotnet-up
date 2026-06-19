namespace DotnetUp.Config;

public sealed class ProcessConfig
{
    public string Name { get; set; } = "";
    public string Cmd { get; set; } = "";
    public string[] Args { get; set; } = [];
    public string WorkDir { get; set; } = "{root}";
    public string[] RunBefore { get; set; } = [];
    public Dictionary<string, string> Env { get; set; } = [];
}
