using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Notifications;
using MotelLease.Application.Notifications.Contracts;

namespace MotelLease.Api.Controllers;

/// <summary>
/// In-app notifications. Every action is scoped to the caller's own rows by the handler, so there
/// is no role policy beyond being signed in — a notification is addressed to one person
/// (docs/api-design.md). Realtime delivery of the same rows is the SignalR hub at
/// <c>/hubs/notifications</c>.
/// </summary>
[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResponse<NotificationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<NotificationResponse>>> List(
        [FromServices] ListNotificationsHandler handler,
        CancellationToken cancellationToken,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = Paged.DefaultPageSize) =>
        Ok(await handler.HandleAsync(unreadOnly, page, pageSize, cancellationToken));

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadNotificationCountResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UnreadNotificationCountResponse>> UnreadCount(
        [FromServices] CountUnreadNotificationsHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(cancellationToken));

    [HttpPut("{id:guid}/read")]
    [ProducesResponseType(typeof(NotificationResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationResponse>> MarkRead(
        Guid id,
        [FromServices] MarkNotificationReadHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpPut("read-all")]
    [ProducesResponseType(typeof(MarkNotificationsReadResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MarkNotificationsReadResponse>> MarkAllRead(
        [FromServices] MarkAllNotificationsReadHandler handler,
        CancellationToken cancellationToken) =>
        Ok(await handler.HandleAsync(cancellationToken));
}
