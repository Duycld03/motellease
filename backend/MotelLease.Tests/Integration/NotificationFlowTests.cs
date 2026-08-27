using System.Net;
using System.Net.Http.Headers;
using MotelLease.Application.Accounts.Contracts;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Notifications.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Tests.Integration;

/// <summary>
/// Drives the notification group of docs/api-design.md. What is worth asserting is that a stored
/// row becomes a sentence in the reader's language rather than the sender's, that reading is
/// per-person and idempotent, and that one user never sees or clears another user's rows.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class NotificationFlowTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public NotificationFlowTests(PostgresFixture postgres) => _postgres = postgres;

    public async Task InitializeAsync()
    {
        _app = new MotelLeaseAppFactory(_postgres.ConnectionString);
        await _app.MigrateAsync();
        _client = _app.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();

        return _app.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task A_stored_row_is_read_back_as_a_sentence_with_its_payload()
    {
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var tenantId = await UserIdAsync(tenant);

        await _app.NotifyAsync(tenantId, "101");

        var listed = Assert.Single((await ListAsync(tenant)).Items);

        Assert.Equal(NotificationType.AppointmentHandled, listed.Type);
        Assert.Contains("101", listed.Body);
        Assert.Contains("Nha tro Ben Thanh", listed.Body);
        Assert.DoesNotContain("{", listed.Body);
        Assert.Equal("/appointments/101", listed.LinkUrl);
        Assert.False(listed.IsRead);
        Assert.Null(listed.ReadAt);

        // The values stay machine-readable alongside the sentence, so a client can format them
        // its own way (docs/erd.md §6).
        Assert.Equal("101", listed.Payload?["roomNumber"]?.GetValue<string>());
    }

    [Fact]
    public async Task The_sentence_follows_the_reader_not_the_sender()
    {
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        await _app.NotifyAsync(await UserIdAsync(tenant), "101");

        var vietnamese = Assert.Single((await ListAsync(tenant)).Items);
        var english = Assert.Single((await ListAsync(tenant, language: "en")).Items);

        Assert.Equal(vietnamese.Id, english.Id);
        Assert.Contains("xem phòng", vietnamese.Body);
        Assert.Contains("viewing", english.Body);
    }

    [Fact]
    public async Task A_pushed_notification_carries_the_same_sentence_as_the_stored_one()
    {
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var tenantId = await UserIdAsync(tenant);

        await _app.NotifyAsync(tenantId, "101");

        var pushed = Assert.Single(_app.Realtime.PushedTo(tenantId));
        var listed = Assert.Single((await ListAsync(tenant)).Items);

        Assert.Equal(listed.Id, pushed.Id);
        Assert.Equal(listed.Body, pushed.Body);
    }

    [Fact]
    public async Task Reading_one_is_idempotent_and_the_unread_count_follows()
    {
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var tenantId = await UserIdAsync(tenant);

        var first = await _app.NotifyAsync(tenantId, "101");
        await _app.NotifyAsync(tenantId, "102");

        Assert.Equal(2, await UnreadCountAsync(tenant));

        var read = await MarkReadAsync(tenant, first);

        Assert.Equal(HttpStatusCode.OK, read.StatusCode);

        var opened = await read.ReadAsync<NotificationResponse>();

        Assert.True(opened.IsRead);
        Assert.NotNull(opened.ReadAt);
        Assert.Equal(1, await UnreadCountAsync(tenant));

        // Opening it again must not move the timestamp: it records when it was first seen. Both
        // values are read back from the database, which keeps microseconds where .NET keeps ticks.
        var firstSeen = await ReadAtAsync(tenant, first);

        await MarkReadAsync(tenant, first);

        Assert.Equal(firstSeen, await ReadAtAsync(tenant, first));
        Assert.Equal(1, await UnreadCountAsync(tenant));
    }

    [Fact]
    public async Task Unread_only_hides_what_has_already_been_opened()
    {
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var tenantId = await UserIdAsync(tenant);

        var first = await _app.NotifyAsync(tenantId, "101");
        await _app.NotifyAsync(tenantId, "102");

        await MarkReadAsync(tenant, first);

        var unread = await ListAsync(tenant, unreadOnly: true);

        Assert.Equal(1, unread.Total);
        Assert.NotEqual(first, Assert.Single(unread.Items).Id);
        Assert.Equal(2, (await ListAsync(tenant)).Total);
    }

    [Fact]
    public async Task Read_all_clears_the_callers_backlog_and_nobody_elses()
    {
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var other = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var tenantId = await UserIdAsync(tenant);
        var otherId = await UserIdAsync(other);

        await _app.NotifyAsync(tenantId, "101");
        await _app.NotifyAsync(tenantId, "102");
        await _app.NotifyAsync(otherId, "201");

        var response = await _client.SendAsync(
            HttpMethod.Put, "/api/v1/notifications/read-all", tenant);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, (await response.ReadAsync<MarkNotificationsReadResponse>()).Marked);

        Assert.Equal(0, await UnreadCountAsync(tenant));
        Assert.Equal(1, await UnreadCountAsync(other));

        // A second sweep has nothing left to do rather than failing.
        var repeat = await _client.SendAsync(
            HttpMethod.Put, "/api/v1/notifications/read-all", tenant);

        Assert.Equal(0, (await repeat.ReadAsync<MarkNotificationsReadResponse>()).Marked);
    }

    [Fact]
    public async Task Somebody_elses_notification_is_neither_listed_nor_openable()
    {
        var tenant = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var other = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        var theirs = await _app.NotifyAsync(await UserIdAsync(other), "201");

        Assert.Empty((await ListAsync(tenant)).Items);

        // Not found rather than forbidden: answering "not yours" would confirm the id exists.
        var attempt = await MarkReadAsync(tenant, theirs);

        Assert.Equal(HttpStatusCode.NotFound, attempt.StatusCode);
        Assert.Equal("error.notification.not_found", await attempt.ReadCodeAsync());
        Assert.Equal(1, await UnreadCountAsync(other));
    }

    [Fact]
    public async Task Signing_in_is_required()
    {
        var response = await _client.GetAsync("/api/v1/notifications");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private Task<HttpResponseMessage> MarkReadAsync(string accessToken, Guid notificationId) =>
        _client.SendAsync(
            HttpMethod.Put, $"/api/v1/notifications/{notificationId}/read", accessToken);

    private async Task<DateTimeOffset?> ReadAtAsync(string accessToken, Guid notificationId) =>
        (await ListAsync(accessToken)).Items.Single(n => n.Id == notificationId).ReadAt;

    private async Task<int> UnreadCountAsync(string accessToken)    {
        var response = await _client.SendAsync(
            HttpMethod.Get, "/api/v1/notifications/unread-count", accessToken);

        response.EnsureSuccessStatusCode();

        return (await response.ReadAsync<UnreadNotificationCountResponse>()).Unread;
    }

    private async Task<PagedResponse<NotificationResponse>> ListAsync(
        string accessToken,
        bool unreadOnly = false,
        string? language = null)
    {
        var request = new HttpRequestMessage(
            HttpMethod.Get, $"/api/v1/notifications?unreadOnly={unreadOnly}")
        {
            Headers = { Authorization = new AuthenticationHeaderValue("Bearer", accessToken) }
        };

        if (language is not null)
        {
            request.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue(language));
        }

        var response = await _client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        return await response.ReadAsync<PagedResponse<NotificationResponse>>();
    }

    private async Task<Guid> UserIdAsync(string accessToken)
    {
        var response = await _client.SendAsync(HttpMethod.Get, "/api/v1/me", accessToken);

        response.EnsureSuccessStatusCode();

        return (await response.ReadAsync<ProfileResponse>()).Id;
    }
}
