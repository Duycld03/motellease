using System.ComponentModel.DataAnnotations;

namespace MotelLease.Infrastructure.Caching;

public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    [Range(1, 60)]
    public int LifetimeMinutes { get; set; } = 5;

    /// <summary>
    /// How long before the same address may ask for another code. Without a cooldown the send
    /// endpoint would double as a free mail cannon aimed at any address (docs/features.md §3.7).
    /// </summary>
    [Range(10, 600)]
    public int ResendCooldownSeconds { get; set; } = 60;

    /// <summary>
    /// Wrong guesses allowed before the code is discarded. Six digits is a million
    /// combinations, so a handful of tries keeps brute force out of reach.
    /// </summary>
    [Range(1, 10)]
    public int MaxAttempts { get; set; } = 5;

    /// <summary>How long a verified registration address stays usable.</summary>
    [Range(1, 240)]
    public int VerifiedEmailWindowMinutes { get; set; } = 30;
}
