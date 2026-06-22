using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Application.Features.Analysis.Commands.AnalysisFailed;
using Planora.Application.Features.Analysis.Commands.BoreholeCompleted;
using Planora.Application.Features.Analysis.Commands.PdfCompleted;
using Planora.Application.Features.Analysis.Commands.RiskCompleted;
using Planora.Application.Features.Analysis.Commands.SoilCompleted;
using Planora.Application.Features.Analysis.Commands.TopographyCompleted;
using Planora.Application.Features.Analysis.Dtos;
using Planora.Application.Features.Parcels.Dtos.Webhook;
using Planora.Domain.AnalysisJob;
using System.Text.Json;

namespace Planora.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
[AllowAnonymous]
public sealed class AiWebhookController(ISender mediator) : BaseApiController
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [HttpPost("ai-events")]
    public async Task<IActionResult> Receive([FromBody] AiWebhookEnvelope envelope, CancellationToken ct)
    {
        var result = envelope.EventType switch
        {
            AiWebhookEventTypes.TopographyCompleted => await mediator.Send(
                new TopographyCompletedCommand
                {
                    PythonJobId = envelope.JobId,
                    Payload = envelope.Data.Deserialize<TopographyResultPayload>(JsonOptions)!
                },
                ct),

            AiWebhookEventTypes.AnalysisFailed => await mediator.Send(
                new AnalysisFailedCommand
                {
                    PythonJobId = envelope.JobId,
                    Reason = envelope.Data.Deserialize<AnalysisFailurePayload>(JsonOptions)!.Reason
                },
                ct),

            AiWebhookEventTypes.SoilCompleted => await mediator.Send(
                new SoilCompletedCommand
                {
                    PythonJobId = envelope.JobId,
                    Payload = envelope.Data.Deserialize<SoilResultPayload>(JsonOptions)!
                },
                ct),

            AiWebhookEventTypes.RiskCompleted => await mediator.Send(
                new RiskCompletedCommand
                {
                    PythonJobId = envelope.JobId,
                    Payload = envelope.Data.Deserialize<RiskResultPayload>(JsonOptions)!
                },
                ct),

            AiWebhookEventTypes.BoreholeCompleted => await mediator.Send(
                new BoreholeCompletedCommand
                {
                    PythonJobId = envelope.JobId,
                    Payload = envelope.Data.Deserialize<BoreholeResultPayload>(JsonOptions)!
                },
                ct),

            AiWebhookEventTypes.PdfCompleted => await mediator.Send(
                new PdfCompletedCommand
                {
                    PythonJobId = envelope.JobId,
                    Payload = envelope.Data.Deserialize<PdfResultPayload>(JsonOptions)!
                },
                ct),

            _ => AnalysisJobErrors.UnsupportedEventType
        };

        if (result.IsError) return Problem(result.Errors);

        return OkEnvelope(result.Value, "Event Handled");
    }
}
