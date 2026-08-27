using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MotelLease.Application.Catalogue;
using MotelLease.Application.Catalogue.Contracts;

namespace MotelLease.Api.Controllers;

[ApiController]
[Route("api/v1/provinces")]
[AllowAnonymous]
public sealed class GeoController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProvinceResponse>>> ListProvinces(
        [FromServices] ListProvincesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{code}/districts")]
    public async Task<ActionResult<IReadOnlyList<DistrictResponse>>> ListDistricts(
        string code,
        [FromServices] ListDistrictsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(code, cancellationToken);
        return Ok(result);
    }
}
