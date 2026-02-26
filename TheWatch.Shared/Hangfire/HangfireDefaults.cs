using Hangfire;

namespace TheWatch.Shared.Hangfire;

public static class HangfireDefaults
{
    public static void ConfigureFilters(IGlobalConfiguration config)
    {
        config
            .UseFilter(new AutomaticRetryAttribute { Attempts = 3, DelaysInSeconds = new[] { 10, 60, 300 } });
    }
}
