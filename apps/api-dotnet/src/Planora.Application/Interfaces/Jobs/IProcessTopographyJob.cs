namespace Planora.Application.Interfaces.Jobs;

public interface IProcessTopographyJob
{
    string Enqueue(Guid parcelId, Guid analysisJobId);
}
