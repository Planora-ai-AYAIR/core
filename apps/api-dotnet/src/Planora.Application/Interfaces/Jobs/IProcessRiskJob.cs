namespace Planora.Application.Interfaces.Jobs;

public interface IProcessRiskJob
{
    string Enqueue(Guid parcelId, Guid analysisJobId);
}
