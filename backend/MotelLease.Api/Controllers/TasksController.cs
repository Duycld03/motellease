using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Tasks;
using MotelLease.Application.Tasks.Contracts;
using MotelLease.Domain.Enums;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/tasks")]
public sealed class TasksController : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Owner,Staff,Admin")]
    public async Task<ActionResult<PagedResponse<TaskResponse>>> List(
        [FromQuery] Guid? boardingHouseId = null,
        [FromQuery] Guid? assignedTo = null,
        [FromQuery] WorkTaskStatus? status = null,
        [FromQuery] TaskPriority? priority = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] ListTasksHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(boardingHouseId, assignedTo, status, priority, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Owner,Staff")]
    public async Task<ActionResult<TaskResponse>> Create(
        [FromBody] CreateTaskRequest request,
        [FromServices] CreateTaskHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<TaskResponse>> GetById(
        Guid id,
        [FromServices] GetTaskHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Owner,Staff")]
    public async Task<ActionResult<TaskResponse>> Update(
        Guid id,
        [FromBody] UpdateTaskRequest request,
        [FromServices] UpdateTaskHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Owner,Staff")]
    public async Task<ActionResult<TaskResponse>> UpdateStatus(
        Guid id,
        [FromBody] UpdateTaskStatusRequest request,
        [FromServices] UpdateTaskStatusHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return Ok(result);
    }
}
