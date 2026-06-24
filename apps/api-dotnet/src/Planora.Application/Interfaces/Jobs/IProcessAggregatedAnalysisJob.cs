namespace Planora.Application.Interfaces.Jobs;

public interface IProcessAggregatedAnalysisJob
{
    string Enqueue(Guid parcelId, Guid analysisJobId);
}
