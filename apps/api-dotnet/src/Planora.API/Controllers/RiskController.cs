using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Application.Features.Parcels.Commands.SubmitRiskJob;
using Planora.Application.Features.Parcels.Dtos.SubmitRiskJob;
using Planora.Application.Features.Parcels.Queries.GetRiskResults;

namespace Planora.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class RiskController : BaseApiController
{
    [HttpPost("jobs")]
    public async Task<ActionResult> SubmitJob(
        [FromBody] SubmitRiskJobRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new SubmitRiskJobCommand(request.ParcelId);

        var result = await sender.Send(command, ct);

        return result.Match<ActionResult>(
            response => AcceptedEnvelope(
                response,
                "Risk assessment job accepted for processing"),
            errors => Problem(errors));
    }

    [HttpGet("{parcelId:guid}")]
    public async Task<ActionResult> GetResults(
        Guid parcelId,
        ISender sender,
        CancellationToken ct)
    {
        var query = new GetRiskResultsQuery(parcelId);
        var result = await sender.Send(query, ct);

        return result.Match<ActionResult>(
            response => OkEnvelope(response, "Risk results retrieved successfully"),
            errors => Problem(errors));
    }
}
