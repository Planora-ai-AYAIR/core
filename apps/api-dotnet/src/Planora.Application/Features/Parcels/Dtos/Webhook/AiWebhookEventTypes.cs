namespace Planora.Application.Features.Parcels.Dtos.Webhook;

public static class AiWebhookEventTypes
{
    public const string TopographyCompleted = "topography.completed";
    public const string TopographyFailed = "topography.failed";
    public const string SoilCompleted = "soil.completed";
    public const string SoilFailed = "soil.failed";
    public const string RiskCompleted = "risk.completed";
    public const string RiskFailed = "risk.failed";
    public const string BoreholeCompleted = "borehole.completed";
    public const string PdfCompleted = "pdf.completed";
    public const string AnalysisFailed = "analysis.failed";
}
