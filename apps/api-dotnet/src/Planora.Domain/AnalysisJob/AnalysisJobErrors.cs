using Planora.Domain.Shared.Results;

namespace Planora.Domain.AnalysisJob;
public static class AnalysisJobErrors
{
    public static readonly Error NotFound = Error.NotFound("AnalysisJob.NotFound", "Analysis job was not found.");
    public static readonly Error AlreadyRunning = Error.Conflict("AnalysisJob.AlreadyRunning", "An analysis job of this type is already running for this parcel.");
    public static readonly Error PythonJobNotFound = Error.NotFound("AnalysisJob.PythonJobNotFound", "Python job ID was not found on the AI service.");
    public static readonly Error Failed = Error.Failure("AnalysisJob.Failed", "Analysis job failed on the AI service.");
    public static readonly Error InvalidStatus = Error.Failure("AnalysisJob.InvalidStatus", "Analysis job is in an invalid status for this operation.");
    public static readonly Error InvalidParcelId = Error.Validation("AnalysisJob.InvalidParcelId", "Parcel ID is invalid.");
    public static readonly Error InvalidPythonJobId = Error.Validation("AnalysisJob.InvalidPythonJobId", "Python job ID cannot be empty.");
    public static readonly Error InvalidErrorMessage = Error.Validation("AnalysisJob.InvalidErrorMessage", "Error message cannot be empty.");
    public static readonly Error UnsupportedEventType = Error.Validation("AnalysisJob.UnsupportedEventType", "Unsupported webhook event type.");
}