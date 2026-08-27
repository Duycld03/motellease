using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using MotelLease.Application.Common;
using MotelLease.Application.Images.Contracts;
using MotelLease.Application.Notifications;
using MotelLease.Application.Notifications.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Tests.Integration;

[Collection(PostgresCollection.Name)]
public sealed class ImagesAndRealtimeNotificationsFlowTests : IAsyncLifetime
{
    private readonly PostgresFixture _postgres;
    private MotelLeaseAppFactory _app = null!;
    private HttpClient _client = null!;

    public ImagesAndRealtimeNotificationsFlowTests(PostgresFixture postgres) => _postgres = postgres;

    public async Task InitializeAsync()
    {
        _app = new MotelLeaseAppFactory(
            _postgres.ConnectionString,
            imageStorage: new RecordingImageStorage(),
            useRealSignalR: true);
        await _app.MigrateAsync();
        _client = _app.CreateClient();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return _app.DisposeAsync().AsTask();
    }

    [Fact]
    public async Task Images_upload_validation_and_delete_flow()
    {
        var token = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);

        // 1. Unauthenticated request rejected (401)
        using var unauthContent = new MultipartFormDataContent();
        var fileBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46 }; // Fake JPEG header
        var byteContent = new ByteArrayContent(fileBytes);
        byteContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        unauthContent.Add(byteContent, "file", "photo.jpg");

        var unauthResp = await _client.PostAsync("/api/v1/images", unauthContent);
        Assert.Equal(HttpStatusCode.Unauthorized, unauthResp.StatusCode);

        // 2. Upload valid image
        using var validContent = new MultipartFormDataContent();
        var validByteContent = new ByteArrayContent(fileBytes);
        validByteContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        validContent.Add(validByteContent, "file", "avatar.jpg");

        var uploadReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/images") { Content = validContent };
        uploadReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var uploadResp = await _client.SendAsync(uploadReq);
        Assert.Equal(HttpStatusCode.OK, uploadResp.StatusCode);

        var uploaded = await uploadResp.ReadAsync<UploadImageResponse>();
        Assert.False(string.IsNullOrWhiteSpace(uploaded.Url));
        Assert.False(string.IsNullOrWhiteSpace(uploaded.PublicId));

        // 3. Reject unsupported MIME type (e.g. application/pdf)
        using var invalidMimeContent = new MultipartFormDataContent();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46 };
        var pdfContent = new ByteArrayContent(pdfBytes);
        pdfContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        invalidMimeContent.Add(pdfContent, "file", "document.pdf");

        var invalidMimeReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/images") { Content = invalidMimeContent };
        invalidMimeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var invalidMimeResp = await _client.SendAsync(invalidMimeReq);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidMimeResp.StatusCode);

        // 4. Reject file exceeding max bytes
        using var tooLargeContent = new MultipartFormDataContent();
        var oversizedBytes = new byte[ImageUploadRules.MaxBytes + 1024];
        var largeContent = new ByteArrayContent(oversizedBytes);
        largeContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
        tooLargeContent.Add(largeContent, "file", "large.png");

        var tooLargeReq = new HttpRequestMessage(HttpMethod.Post, "/api/v1/images") { Content = tooLargeContent };
        tooLargeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var tooLargeResp = await _client.SendAsync(tooLargeReq);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooLargeResp.StatusCode);

        // 5. Delete image
        var deleteResp = await _client.SendAsync(
            HttpMethod.Delete,
            $"/api/v1/images/{uploaded.PublicId}",
            token);
        Assert.Equal(HttpStatusCode.NoContent, deleteResp.StatusCode);
    }

    [Fact]
    public async Task SignalR_notifications_hub_realtime_push_flow()
    {
        var tenantToken = await _client.RegisterAsync(_app.Emails, UserRole.Tenant);
        var tenantId = await _client.UserIdAsync(tenantToken);

        // Connect to SignalR Hub using in-memory test server handler
        var hubConnection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notifications", options =>
            {
                options.HttpMessageHandlerFactory = _ => _app.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(tenantToken);
            })
            .Build();

        var notificationTcs = new TaskCompletionSource<NotificationResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        hubConnection.On<NotificationResponse>("notification", n => notificationTcs.TrySetResult(n));

        await hubConnection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, hubConnection.State);

        // Dispatch a push notification to this tenant via INotificationRealtime
        using (var scope = _app.Services.CreateScope())
        {
            var realtime = scope.ServiceProvider.GetRequiredService<INotificationRealtime>();
            var sampleNotification = new NotificationResponse(
                Id: Guid.NewGuid(),
                Type: NotificationType.AppointmentHandled,
                Title: "Lịch hẹn xem phòng",
                Body: "Chủ trọ đã chấp nhận lịch hẹn",
                Payload: null,
                LinkUrl: "/appointments",
                IsRead: false,
                ReadAt: null,
                CreatedAt: DateTimeOffset.UtcNow);

            await realtime.PushAsync(tenantId, sampleNotification);
        }

        // Verify the client receives the realtime event
        var receivedNotificationTask = await Task.WhenAny(notificationTcs.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.True(receivedNotificationTask == notificationTcs.Task, "SignalR client did not receive push notification within 5 seconds.");

        var received = await notificationTcs.Task;
        Assert.Equal(NotificationType.AppointmentHandled, received.Type);
        Assert.Equal("Lịch hẹn xem phòng", received.Title);

        await hubConnection.StopAsync();
        await hubConnection.DisposeAsync();
    }
}
