namespace Planora.Infrastructure.Options;

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string BaseUrl { get; init; } = string.Empty;
    public string SharedSecret { get; init; } = string.Empty;
}