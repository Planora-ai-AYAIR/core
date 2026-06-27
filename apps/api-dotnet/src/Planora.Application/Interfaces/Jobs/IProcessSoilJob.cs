using Planora.Application.Common.Dtos;

namespace Planora.Application.Interfaces.Jobs
{
    public interface IProcessSoilJob
    {
        string Enqueue(ProccessSoilJobAiRequest request);
    }
}