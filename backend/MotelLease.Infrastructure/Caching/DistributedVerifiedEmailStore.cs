using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using MotelLease.Application.Common.Abstractions;

namespace MotelLease.Infrastructure.Caching;

/// <summary>
/// Remembers that an address passed OTP verification, so registration can be its own request.
/// Same cache as the codes: the fact is short-lived and worthless once used.
/// </summary>
public sealed class DistributedVerifiedEmailStore(
    IDistributedCache cache,
    IOptions<OtpOptions> options) : IVerifiedEmailStore
{
    private readonly OtpOptions _options = options.Value;

    public Task MarkVerifiedAsync(string email, CancellationToken cancellationToken = default) =>
        cache.SetStringAsync(
            KeyFor(email),
            "1",
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow =
                    TimeSpan.FromMinutes(_options.VerifiedEmailWindowMinutes)
            },
            cancellationToken);

    public async Task<bool> IsVerifiedAsync(
        string email,
        CancellationToken cancellationToken = default) =>
        await cache.GetStringAsync(KeyFor(email), cancellationToken) is not null;

    public Task ClearAsync(string email, CancellationToken cancellationToken = default) =>
        cache.RemoveAsync(KeyFor(email), cancellationToken);

    private static string KeyFor(string email) => $"verified-email:{email}";
}
