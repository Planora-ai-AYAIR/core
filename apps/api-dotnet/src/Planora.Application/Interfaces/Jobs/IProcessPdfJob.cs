using Planora.Application.Common.Dtos;

namespace Planora.Application.Interfaces.Jobs;

public interface IProcessPdfJob
{
    string Enqueue(ProccessPdfJobAiRequest request);
}
