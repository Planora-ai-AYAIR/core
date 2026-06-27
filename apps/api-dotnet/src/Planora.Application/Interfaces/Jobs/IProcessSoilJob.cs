namespace Planora.Application.Interfaces.Jobs;

public interface IProcessSoilJob
{
    string Enqueue(Guid parcelId, Guid analysisJobId);
}
