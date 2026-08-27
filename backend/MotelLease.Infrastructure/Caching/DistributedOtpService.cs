using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MotelLease.Application.Common.Abstractions;

namespace MotelLease.Infrastructure.Caching;

/// <summary>
/// OTP state in a distributed cache. Codes expire in minutes, so a restart losing them is
/// harmless and the schema needs no extra table; swapping the in-memory cache for Redis is a
/// registration change, not a code change.
/// </summary>
public sealed class DistributedOtpService(
    IDistributedCache cache,
    IOptions<OtpOptions> options,
    TimeProvider clock) : IOtpService
{
    private readonly OtpOptions _options = options.Value;

    public async Task<OtpIssueResult> IssueAsync(
        OtpPurpose purpose,
        string subject,
        CancellationToken cancellationToken = default)
    {
        var lifetime = TimeSpan.FromMinutes(_options.LifetimeMinutes);
        var cooldown = TimeSpan.FromSeconds(_options.ResendCooldownSeconds);
        var key = KeyFor(purpose, subject);
        var now = clock.GetUtcNow();

        var existing = await ReadAsync(key, cancellationToken);

        // Resending is allowed, but not immediately: the cooldown runs from the last send,
        // not from the code's expiry.
        if (existing is not null && now - existing.IssuedAt < cooldown)
        {
            return new OtpIssueResult(false, null, cooldown - (now - existing.IssuedAt), lifetime);
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var entry = new OtpEntry(code, now, 0);

        await cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(entry),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = lifetime },
            cancellationToken);

        return new OtpIssueResult(true, code, null, lifetime);
    }

    public async Task<OtpVerifyResult> VerifyAsync(
        OtpPurpose purpose,
        string subject,
        string code,
        CancellationToken cancellationToken = default)
    {
        var key = KeyFor(purpose, subject);
        var entry = await ReadAsync(key, cancellationToken);

        if (entry is null)
        {
            return OtpVerifyResult.NotFound;
        }

        // Fixed-time comparison: a six-digit code is small enough that a timing side channel
        // would meaningfully narrow the search.
        if (CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(entry.Code),
                System.Text.Encoding.UTF8.GetBytes(code)))
        {
            await cache.RemoveAsync(key, cancellationToken);

            return OtpVerifyResult.Valid;
        }

        var attempts = entry.Attempts + 1;

        if (attempts >= _options.MaxAttempts)
        {
            // Discarded, not just counted: the holder has to request a fresh code, which the
            // cooldown then rate-limits.
            await cache.RemoveAsync(key, cancellationToken);

            return OtpVerifyResult.TooManyAttempts;
        }

        var remaining = TimeSpan.FromMinutes(_options.LifetimeMinutes)
                        - (clock.GetUtcNow() - entry.IssuedAt);

        if (remaining <= TimeSpan.Zero)
        {
            await cache.RemoveAsync(key, cancellationToken);

            return OtpVerifyResult.NotFound;
        }

        // Rewritten with the original expiry preserved, so failed attempts cannot extend the
        // code's life.
        await cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(entry with { Attempts = attempts }),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = remaining },
            cancellationToken);

        return OtpVerifyResult.Mismatch;
    }

    private async Task<OtpEntry?> ReadAsync(string key, CancellationToken cancellationToken)
    {
        var payload = await cache.GetStringAsync(key, cancellationToken);

        return payload is null ? null : JsonSerializer.Deserialize<OtpEntry>(payload);
    }

    /// <summary>
    /// The purpose is part of the key, so a registration code can never be replayed against
    /// a password reset for the same address.
    /// </summary>
    private static string KeyFor(OtpPurpose purpose, string subject) =>
        $"otp:{purpose}:{subject}";

    private sealed record OtpEntry(string Code, DateTimeOffset IssuedAt, int Attempts);
}
