using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Application.Features.Analysis.Commands.StartAnalysis;
using Planora.Application.Features.Analysis.Dtos.StartAnalysis;

namespace Planora.Api.Controllers;

[Route("api/parcels/{parcelId:guid}/analysis")]
[ApiController]
[Authorize]
public sealed class AnalysisController : BaseApiController
{
    [HttpPost]
    public async Task<ActionResult> StartAnalysis(
        Guid parcelId,
        [FromBody] StartAnalysisRequest request,
        ISender sender,
        CancellationToken ct)
    {
        var command = new StartAnalysisCommand(parcelId, request.AnalysisOptions);

        var result = await sender.Send(command, ct);

        return result.Match<ActionResult>(
            response => AcceptedEnvelope(response, "Analysis started successfully"),
            errors => Problem(errors));
    }
}
