using Planora.Application.Common.Dtos;

namespace Planora.Application.Interfaces.Jobs
{
    public interface IProcessTopographyJob
    {
        string Enqueue(ProccessTopographyJobAiRequest request);
    }
}
