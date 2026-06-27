using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Helpers;
using Planora.Application.Features.Analysis.Commands.StartAnalysis;
using Planora.Application.Features.Analysis.Dtos.StartAnalysis;
using Planora.Application.Features.Analysis.Queries.GetAnalysisJobsSummary;
using Planora.Domain.Shared.Results;

namespace Planora.Api.Controllers;

[Route("api/parcels/{parcelId:guid}/analysis")]
[ApiController]
[Authorize]
public sealed class AnalysisController(ISender sender) : BaseApiController
{
    [HttpGet("/api/analysis/jobs")]
    public async Task<ActionResult> GetAnalysisJobsSummary(
        CancellationToken ct)
    {
        if (!User.TryGetUserId(out var userId))
            return Problem([Error.Unauthorized("Unauthorized", "User ID not found in token.")]);

        var result = await sender.Send(new GetAnalysisJobsSummaryQuery(userId), ct);

        return result.Match<ActionResult>(
            onValue: response => OkEnvelope(response, "Analysis jobs summary retrieved"),
            onError: errors => Problem(errors));
    }

    [HttpPost]
    public async Task<ActionResult> StartAnalysis(
        Guid parcelId,
        [FromBody] StartAnalysisRequest request,
        CancellationToken ct)
    {
        var command = new StartAnalysisCommand(parcelId, request.AnalysisOptions);

        var result = await sender.Send(command, ct);

        return result.Match<ActionResult>(
            response => AcceptedEnvelope(response, "Analysis started successfully"),
            errors => Problem(errors));
    }
}
