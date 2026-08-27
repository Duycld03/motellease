using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Admin;
using MotelLease.Application.Admin.Contracts;
using MotelLease.Application.Common.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/admin/accounts")]
[Authorize(Roles = "Admin")]
public sealed class AdminAccountsController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AdminAccountSummaryResponse>>> List(
        [FromQuery] UserRole? role = null,
        [FromQuery] bool? isLocked = null,
        [FromQuery] string? search = null,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] AdminListAccountsHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(role, isLocked, search, includeDeleted, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<AdminAccountSummaryResponse>> Create(
        [FromBody] AdminCreateAccountRequest request,
        [FromServices] AdminCreateAccountHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AdminAccountDetailResponse>> GetById(
        Guid id,
        [FromServices] AdminGetAccountHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminAccountSummaryResponse>> Update(
        Guid id,
        [FromBody] AdminUpdateAccountRequest request,
        [FromServices] AdminUpdateAccountHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] AdminDeleteAccountHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<IActionResult> Restore(
        Guid id,
        [FromServices] AdminRestoreAccountHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/lock")]
    public async Task<IActionResult> Lock(
        Guid id,
        [FromBody] AdminLockAccountRequest request,
        [FromServices] AdminLockAccountHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, request, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:guid}/unlock")]
    public async Task<IActionResult> Unlock(
        Guid id,
        [FromServices] AdminUnlockAccountHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return NoContent();
    }
}
