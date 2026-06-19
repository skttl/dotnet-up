using System.Net.Http;
using System.Text.Json;
using System.Reflection;

namespace DotnetUp.Update;

public static class UpdateChecker
{
    private const string PackageId = "dotnet-up";
    private const string NuGetUrl = $"https://api.nuget.org/v3-flatcontainer/{PackageId}/index.json";

    public static async Task CheckAsync()
    {
        try
        {
            var current = Assembly.GetExecutingAssembly().GetName().Version;
            if (current is null) return;

            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var json = await http.GetStringAsync(NuGetUrl);
            using var doc = JsonDocument.Parse(json);

            var versions = doc.RootElement
                .GetProperty("versions")
                .EnumerateArray()
                .Select(v => v.GetString())
                .Where(v => v is not null && !v.Contains('-'))
                .Select(v => Version.TryParse(v, out var parsed) ? parsed : null)
                .Where(v => v is not null)
                .Cast<Version>()
                .ToList();

            var latest = versions.Max();
            if (latest is not null && latest > current)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"  A new version of dotnet-up is available: {latest} (current: {current.ToString(3)})");
                Console.WriteLine($"  Run: dotnet tool update -g dotnet-up");
                Console.ResetColor();
                Console.WriteLine();
            }
        }
        catch
        {
            // silently ignore — no network, rate limit, etc.
        }
    }
}
