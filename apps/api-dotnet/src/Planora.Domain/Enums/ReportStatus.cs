using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Domain.Enums
{
    public enum ReportStatus { 
        PendingPayment, 
        Queued, 
        Processing, 
        Completed, 
        Failed, 
        Cancelled 
    }
}
