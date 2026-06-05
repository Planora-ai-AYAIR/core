using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Domain.Shared.Results
{
    public enum ErrorKind
    {
        Failure,
        Unexpected,
        Validation,
        Conflict,
        NotFound,
        Unauthorized,
        Forbidden,
        TooManyRequests
    }
}
