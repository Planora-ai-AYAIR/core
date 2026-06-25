namespace Planora.Infrastructure.Options;

public sealed class AiOptions
{
    public const string SectionName = "AiOptions";

    public string BaseUrl { get; init; } = string.Empty;
    public string SharedSecret { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
}