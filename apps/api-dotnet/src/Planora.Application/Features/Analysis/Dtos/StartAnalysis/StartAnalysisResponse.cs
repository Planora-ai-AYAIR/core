namespace Planora.Application.Features.Analysis.Dtos.StartAnalysis;

public sealed record StartAnalysisResponse(
    string AnalysisJobId,
    string ParcelId,
    string Status,
    DateTime SubmittedAt,
    string EstimatedDuration,
    string PollEndpoint);
