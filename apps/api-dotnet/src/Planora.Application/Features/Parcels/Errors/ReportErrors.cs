using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Parcels.Errors;

public static class ReportErrors
{
    public static readonly Error NotFound =
        Error.NotFound("Report.NotFound", "No completed report was found for this parcel.");

    public static readonly Error NotReady =
        Error.Failure("Report.NotReady", "The report is still processing or has failed. Please try again later.");

    public static readonly Error TopographyModuleMissing =
        Error.NotFound("Report.TopographyModuleMissing", "The topography module data is missing from this report.");

    public static readonly Error MetadataCorrupted =
        Error.Failure("Report.MetadataCorrupted", "The topography metadata could not be read. Please contact support.");
}
