using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planora.Application.Common.Dtos;

namespace Planora.Application.Interfaces.Jobs
{
    public interface IProcessTopographyJob
    {
        string Enqueue(ProccessTopographyJobAiRequest parcelId, CancellationToken ct);
    }
}
