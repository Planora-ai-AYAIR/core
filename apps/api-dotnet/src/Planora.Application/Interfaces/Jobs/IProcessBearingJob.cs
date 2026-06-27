namespace Planora.Application.Interfaces.Jobs;

public interface IProcessBearingJob
{
    string Enqueue(Guid parcelId, Guid analysisJobId);
}
