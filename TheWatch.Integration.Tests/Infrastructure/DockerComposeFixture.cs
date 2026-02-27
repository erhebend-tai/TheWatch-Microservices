using System.Diagnostics;

namespace TheWatch.Integration.Tests.Infrastructure;

/// <summary>
/// xUnit class fixture that spins up the docker-compose stack before tests and tears it down after.
/// Waits for all services to become healthy before yielding control.
/// </summary>
public class DockerComposeFixture : IAsyncLifetime
{
    private const string ComposeFile = "docker-compose.yml";
    private const int MaxWaitSeconds = 180;

    public async Task InitializeAsync()
    {
        // Check if services are already running (developer has compose up)
        if (await AreServicesHealthyAsync())
        {
            return;
        }

        // Start docker-compose stack
        await RunDockerComposeAsync("up -d --build --wait");

        // Wait for all services to respond
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < TimeSpan.FromSeconds(MaxWaitSeconds))
        {
            if (await AreServicesHealthyAsync())
                return;

            await Task.Delay(3000);
        }

        throw new TimeoutException(
            $"Docker compose services did not become healthy within {MaxWaitSeconds}s. " +
            "Run 'docker compose logs' to diagnose.");
    }

    public async Task DisposeAsync()
    {
        // Only tear down if we started it (check for CI environment)
        if (Environment.GetEnvironmentVariable("WATCH_INTEGRATION_TEARDOWN") == "true")
        {
            await RunDockerComposeAsync("down -v --remove-orphans");
        }
    }

    private static async Task<bool> AreServicesHealthyAsync()
    {
        var endpoints = new[]
        {
            $"{ServiceEndpoints.AuthSecurity}/health",
            $"{ServiceEndpoints.VoiceEmergency}/health",
            $"{ServiceEndpoints.FirstResponder}/health",
            $"{ServiceEndpoints.FamilyHealth}/health",
        };

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        foreach (var endpoint in endpoints)
        {
            try
            {
                var response = await client.GetAsync(endpoint);
                if (!response.IsSuccessStatusCode) return false;
            }
            catch
            {
                return false;
            }
        }

        return true;
    }

    private static async Task RunDockerComposeAsync(string args)
    {
        var workDir = FindComposeDirectory();
        var psi = new ProcessStartInfo("docker", $"compose -f {ComposeFile} {args}")
        {
            WorkingDirectory = workDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start docker compose");

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var stderr = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"docker compose {args} failed: {stderr}");
        }
    }

    private static string FindComposeDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, ComposeFile)))
                return dir.FullName;
            dir = dir.Parent;
        }

        throw new FileNotFoundException(
            $"Could not find {ComposeFile} in any parent directory of {AppContext.BaseDirectory}");
    }
}
