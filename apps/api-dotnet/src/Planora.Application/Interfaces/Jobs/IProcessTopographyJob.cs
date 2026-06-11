using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Application.Interfaces.Jobs
{
    public interface IProcessTopographyJob
    {
        string Enqueue(Guid parcelId, CancellationToken ct);
    }
}
