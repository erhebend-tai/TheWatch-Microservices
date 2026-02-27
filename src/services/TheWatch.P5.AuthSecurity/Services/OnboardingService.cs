using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TheWatch.P5.AuthSecurity.Data;
using TheWatch.P5.AuthSecurity.Models;

namespace TheWatch.P5.AuthSecurity.Services;

public class OnboardingService
{
    private readonly AuthIdentityDbContext _db;

    public OnboardingService(AuthIdentityDbContext db)
    {
        _db = db;
    }

    public async Task<object> GetProgressAsync(Guid userId)
    {
        var progress = await _db.OnboardingProgresses.FirstOrDefaultAsync(o => o.UserId == userId);
        if (progress is null)
        {
            return new { steps = Array.Empty<string>(), isComplete = false };
        }

        var steps = JsonSerializer.Deserialize<string[]>(progress.CompletedStepsJson) ?? [];
        return new
        {
            steps,
            isComplete = progress.IsComplete,
            startedAt = progress.StartedAt,
            completedAt = progress.CompletedAt
        };
    }

    public async Task CompleteStepAsync(Guid userId, string step)
    {
        var progress = await _db.OnboardingProgresses.FirstOrDefaultAsync(o => o.UserId == userId);
        if (progress is null)
        {
            progress = new OnboardingProgress { UserId = userId };
            _db.OnboardingProgresses.Add(progress);
        }

        var steps = JsonSerializer.Deserialize<List<string>>(progress.CompletedStepsJson) ?? [];
        if (!steps.Contains(step))
        {
            steps.Add(step);
            progress.CompletedStepsJson = JsonSerializer.Serialize(steps);
        }

        await _db.SaveChangesAsync();
    }

    public async Task ResetAsync(Guid userId)
    {
        var progress = await _db.OnboardingProgresses.FirstOrDefaultAsync(o => o.UserId == userId);
        if (progress is not null)
        {
            progress.CompletedStepsJson = "[]";
            progress.IsComplete = false;
            progress.CompletedAt = null;
            progress.StartedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }
}
