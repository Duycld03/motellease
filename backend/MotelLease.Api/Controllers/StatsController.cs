using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Statistics;
using MotelLease.Application.Statistics.Contracts;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/my/stats")]
[Authorize(Roles = "Owner")]
public sealed class StatsController : ControllerBase
{
    [HttpGet("revenue")]
    public async Task<ActionResult<RevenueStatsResponse>> GetRevenue(
        [FromQuery] int? year = null,
        [FromQuery] Guid? boardingHouseId = null,
        [FromServices] GetRevenueStatsHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(year, boardingHouseId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("revenue/years")]
    public async Task<ActionResult<RevenueYearsResponse>> GetRevenueYears(
        [FromServices] GetRevenueYearsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("occupancy")]
    public async Task<ActionResult<OccupancyStatsResponse>> GetOccupancy(
        [FromServices] GetOccupancyStatsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("profit")]
    public async Task<ActionResult<ProfitStatsResponse>> GetProfit(
        [FromQuery] int? year = null,
        [FromQuery] Guid? boardingHouseId = null,
        [FromServices] GetProfitStatsHandler handler = null!,
        CancellationToken cancellationToken = default)
    {
        var result = await handler.HandleAsync(year, boardingHouseId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryResponse>> GetSummary(
        [FromServices] GetDashboardSummaryHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Ok(result);
    }
}
