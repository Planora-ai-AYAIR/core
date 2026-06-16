using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Planora.Application.Features.Parcels.Dtos.Webhook;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Shared.Results;

namespace Planora.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
[AllowAnonymous]
public sealed class AiWebhookController(ISender sender) : BaseApiController
{
    [HttpPost("ai-events")]
    public async Task<IActionResult> Receive([FromBody] AiWebhookEnvelope envelope, CancellationToken ct)
    {
        Result<Success> result = envelope.EventType switch
        {
            // TODO : handle the events with meditr
            _ => AnalysisJobErrors.UnsupportedEventType
        };

        if (result.IsError) return Problem(result.Errors);

        return OkEnvelope(result.Value, "Event Handled");
    }

}