using Planora.Application.Common.Dtos;

namespace Planora.Application.Interfaces.Jobs;

public interface IProcessBoreholeJob
{
    string Enqueue(ProccessBoreholeJobAiRequest request);
}
