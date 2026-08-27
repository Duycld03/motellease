using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Common.Contracts;
using MotelLease.Application.Expenses;
using MotelLease.Application.Expenses.Contracts;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/my/boarding-houses/{id:guid}/expenses")]
public sealed class ExpensesController : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Owner,Staff")]
    public async Task<ActionResult<PagedResponse<ExpenseResponse>>> List(
        Guid id,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromServices] ListExpensesHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(id, year, month, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ExpenseResponse>> Create(
        Guid id,
        [FromBody] CreateExpenseRequest request,
        [FromServices] CreateExpenseHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id, expenseId = result.Id }, result);
    }

    [HttpGet("{expenseId:guid}")]
    [Authorize(Roles = "Owner,Staff")]
    public async Task<ActionResult<ExpenseResponse>> GetById(
        Guid id,
        Guid expenseId,
        [FromServices] GetExpenseHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, expenseId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("{expenseId:guid}")]
    [Authorize(Roles = "Owner")]
    public async Task<ActionResult<ExpenseResponse>> Update(
        Guid id,
        Guid expenseId,
        [FromBody] UpdateExpenseRequest request,
        [FromServices] UpdateExpenseHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, expenseId, request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{expenseId:guid}")]
    [Authorize(Roles = "Owner")]
    public async Task<IActionResult> Delete(
        Guid id,
        Guid expenseId,
        [FromServices] DeleteExpenseHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, expenseId, cancellationToken);
        return NoContent();
    }
}
