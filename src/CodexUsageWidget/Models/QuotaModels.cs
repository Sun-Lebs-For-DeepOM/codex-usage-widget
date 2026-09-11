namespace CodexUsageWidget.Models;

public sealed record RateLimitWindowSnapshot(
    int UsedPercent,
    long? WindowDurationMinutes,
    DateTimeOffset? ResetsAt)
{
    public int RemainingPercent => Math.Clamp(100 - UsedPercent, 0, 100);
}

public sealed record CreditsSnapshot(
    bool HasCredits,
    bool Unlimited,
    string? Balance);

public sealed record IndividualLimitSnapshot(
    string Limit,
    string Used,
    int RemainingPercent,
    DateTimeOffset ResetsAt);

public sealed record CodexQuotaSnapshot(
    string? LimitId,
    string? LimitName,
    string? PlanType,
    RateLimitWindowSnapshot? Primary,
    RateLimitWindowSnapshot? Secondary,
    CreditsSnapshot? Credits,
    IndividualLimitSnapshot? IndividualLimit,
    int ResetCreditsAvailable,
    string Source,
    DateTimeOffset UpdatedAt,
    string? Warning = null)
{
    // API primary/secondary describe source slots, not fixed reset periods.
    public CodexQuotaSnapshot NormalizeWindowOrder()
    {
        if (Secondary?.WindowDurationMinutes == 300 && Primary?.WindowDurationMinutes != 300 ||
            Primary?.WindowDurationMinutes == 10080 && Secondary?.WindowDurationMinutes != 10080)
            return this with { Primary = Secondary, Secondary = Primary };
        return this;
    }
}
