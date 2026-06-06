using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Domain.Enums
{
    public enum PaymentStatus { 
        Pending, 
        Succeeded, 
        Failed, 
        Refunded, 
        PartiallyRefunded 
    }
}
