namespace Planora.Infrastructure.Options;

public sealed class WebhookOptions
{
    public const string SectionName = "Webhook";

    public string HmacSecret { get; init; } = string.Empty;
    public string SignatureHeader { get; init; } = "X-Signature";
}
