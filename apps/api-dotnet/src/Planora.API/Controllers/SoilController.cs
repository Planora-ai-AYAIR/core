using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Application.Features.Parcels.Commands.SubmitSoilJob;
using Planora.Application.Features.Parcels.Dtos.SubmitSoilJob;
using Planora.Application.Features.Parcels.Queries.GetSoilResults;

namespace Planora.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class SoilController : BaseApiController
{
    [HttpPost("jobs")]
    public async Task<ActionResult> SubmitJob(
        [FromBody] SubmitSoilJobRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new SubmitSoilJobCommand(request.ParcelId);

        var result = await sender.Send(command, ct);

        return result.Match<ActionResult>(
            response => AcceptedEnvelope(
                response,
                "Soil analysis job accepted for processing"),
            errors => Problem(errors));
    }

    [HttpGet("{parcelId:guid}")]
    public async Task<ActionResult> GetResults(
        Guid parcelId,
        [FromQuery] string? depth,
        ISender sender,
        CancellationToken ct)
    {
        var query = new GetSoilResultsQuery(parcelId, depth);
        var result = await sender.Send(query, ct);

        return result.Match<ActionResult>(
            response => OkEnvelope(response, "Soil results retrieved successfully"),
            errors => Problem(errors));
    }
}
