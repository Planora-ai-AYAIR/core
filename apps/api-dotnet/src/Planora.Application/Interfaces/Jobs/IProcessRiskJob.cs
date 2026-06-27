using Planora.Application.Common.Dtos;

namespace Planora.Application.Interfaces.Jobs
{
    public interface IProcessRiskJob
    {
        string Enqueue(ProccessRiskJobAiRequest request);
    }
}