using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Domain.Enums
{
    public enum NotificationType { 
        ReportQueued, 
        ReportProcessing, 
        ReportCompleted, 
        ReportFailed, 
        PaymentReceived, 
        PaymentRefunded, 
        AccountBanned, 
        AccountActivated,
        ModuleCompleted,
        ModuleFailed,
        AnalysisStarted
    }
}
