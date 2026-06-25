using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Planora.Domain.Shared.Results;

namespace Planora.Domain.Reports
{
    public static class ReportErrors
    {
        public static readonly Error InvalidEstimatedDuration = 
            Error.Validation("Report.Duration.Invalid", "Estimated duration must be positive.");
        
        public static readonly Error ModuleNotFound = 
            Error.NotFound("Report.Module.NotFound", "The requested module was not found in this report.");

        public static readonly Error NotFound =
            Error.NotFound("Report.NotFound", "No completed report was found for this parcel.");

        public static readonly Error NotReady =
            Error.Failure("Report.NotReady", "The report is still processing or has failed. Please try again later.");

        public static readonly Error TopographyModuleMissing =
            Error.NotFound("Report.TopographyModuleMissing", "The topography module data is missing from this report.");

        public static readonly Error PdfModuleMissing =
            Error.NotFound("Report.PdfModuleMissing", "The PDF report module data is missing from this report.");

        public static readonly Error MetadataCorrupted =
            Error.Failure("Report.MetadataCorrupted", "The topography metadata could not be read. Please contact support.");
        public static readonly Error AlreadyRunning = 
            Error.Conflict("Report.AlreadyRunning","A report is already being generated for this parcel.");

    }
}
