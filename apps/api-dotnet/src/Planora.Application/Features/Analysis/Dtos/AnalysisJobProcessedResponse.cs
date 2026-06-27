namespace Planora.Application.Features.Analysis.Dtos;

public sealed record AnalysisJobProcessedResponse
{
    public Guid AnalysisJobId { get; init; }
    public string PythonJobId { get; init; } = string.Empty;

    public AnalysisJobProcessedResponse(Guid analysisJobId, string pythonJobId)
    {
        AnalysisJobId = analysisJobId;
        PythonJobId = pythonJobId;
    }
}