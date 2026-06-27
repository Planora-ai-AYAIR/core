namespace Planora.Application.Features.Analysis.Dtos.Realtime;

/// <summary>
/// Real-time SignalR payload broadcast to the <c>parcel:{id}</c> group when a single
/// analysis type completes. Carries the full typed result so the UI can render
/// immediately without re-fetching via GET.
/// </summary>
public sealed record AnalysisResultEnvelope(
    string EventType,
    Guid ParcelId,
    Guid AnalysisJobId,
    string AnalysisType,
    object Result,
    DateTime Timestamp);
