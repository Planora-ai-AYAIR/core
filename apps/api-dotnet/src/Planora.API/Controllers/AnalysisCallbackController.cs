using MediatR;
using Microsoft.AspNetCore.Mvc;
using Planora.Api.Filters;
using Planora.Application.Features.Notifications.Commands.MarkModuleCompleted;
using Planora.Domain.AnalysisJob;

namespace Planora.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public sealed class AnalysisCallbackController : BaseApiController
{
    [HttpPost("analysis-completed")]
    [ServiceFilter(typeof(HmacSignatureFilter))]
    public async Task<ActionResult> AnalysisCompleted(
        [FromBody] AnalysisCompletedWebhookPayload payload,
        ISender sender,
        CancellationToken ct)
    {
        var command = new MarkModuleCompletedCommand(
            payload.PythonJobId,
            payload.ModuleType,
            payload.ResultSummary);

        var result = await sender.Send(command, ct);

        return result.Match<ActionResult>(
            onValue: _ => OkEnvelope<object?>(null, "Webhook processed"),
            onError: errors => Problem(errors));
    }
}

public sealed record AnalysisCompletedWebhookPayload(
    string PythonJobId,
    AnalysisType ModuleType,
    string? ResultSummary);
