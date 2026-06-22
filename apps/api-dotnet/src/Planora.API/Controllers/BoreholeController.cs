using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Application.Features.Parcels.Commands.SubmitBoreholeJob;
using Planora.Application.Features.Parcels.Dtos.SubmitBoreholeJob;
using Planora.Application.Features.Parcels.Queries.GetBoreholeResults;

namespace Planora.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BoreholeController : BaseApiController
{
    [HttpPost("jobs")]
    public async Task<ActionResult> SubmitJob(
        [FromBody] SubmitBoreholeJobRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new SubmitBoreholeJobCommand(request.ParcelId);

        var result = await sender.Send(command, ct);

        return result.Match<ActionResult>(
            response => AcceptedEnvelope(
                response,
                "Borehole optimization job accepted for processing"),
            errors => Problem(errors));
    }

    [HttpGet("{parcelId:guid}")]
    public async Task<ActionResult> GetResults(
        Guid parcelId,
        ISender sender,
        CancellationToken ct)
    {
        var query = new GetBoreholeResultsQuery(parcelId);
        var result = await sender.Send(query, ct);

        return result.Match<ActionResult>(
            response => OkEnvelope(response, "Borehole results retrieved successfully"),
            errors => Problem(errors));
    }
}
