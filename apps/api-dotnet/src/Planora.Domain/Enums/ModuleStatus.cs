using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Domain.Enums
{
    public enum ModuleStatus { 
        Pending, 
        Processing,        
        Completed,        
        Failed,      
        Skipped 
    }
}
