using System.Text.Json;

namespace Planora.Application.Features.Parcels.Dtos.Webhook;
public sealed class AiWebhookEnvelope
{
    public string EventType { get; init; } = string.Empty;
    public string JobId { get; init; } = string.Empty;
    public JsonElement Data { get; init; }
    public DateTime Timestamp { get; init; }
}