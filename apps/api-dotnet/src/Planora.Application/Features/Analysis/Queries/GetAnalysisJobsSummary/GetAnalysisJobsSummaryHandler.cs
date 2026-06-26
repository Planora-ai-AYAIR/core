using MediatR;
using Microsoft.Extensions.Logging;
using Planora.Application.Features.Analysis.Dtos.AnalysisJobsSummary;
using Planora.Application.Interfaces.Repositories;
using Planora.Domain.AnalysisJob;
using Planora.Domain.Shared.Results;

namespace Planora.Application.Features.Analysis.Queries.GetAnalysisJobsSummary;

public sealed class GetAnalysisJobsSummaryHandler(
    IAnalysisJobRepository analysisJobRepository,
    IParcelRepository parcelRepository,
    ILogger<GetAnalysisJobsSummaryHandler> logger)
    : IRequestHandler<GetAnalysisJobsSummaryQuery, Result<AnalysisJobsSummaryResponse>>
{
    public async Task<Result<AnalysisJobsSummaryResponse>> Handle(
        GetAnalysisJobsSummaryQuery request, CancellationToken ct)
    {
        logger.LogInformation("Fetching the analysis jobs for User {Id} ", request.UserId);
        var jobs = await analysisJobRepository.GetByUserIdAsync(request.UserId, ct);
        var parcels = await parcelRepository.GetByUserIdAsync(request.UserId, ct);
        var parcelNames = parcels.ToLookup(p => p.Id, p => p.Name);

        var items = jobs.Select(j => new AnalysisJobSummaryItem(
            Id: j.Id,
            Name: parcelNames[j.ParcelId].FirstOrDefault() ?? "",
            Status: j.Status.ToString(),
            Modules: BuildModuleList(j),
            Date: j.CreatedAt)).ToList();

        logger.LogInformation("Fetched the analysis jobs for User {Id} ", request.UserId);
        return new AnalysisJobsSummaryResponse(
            Total: items.Count,
            Completed: items.Count(i => i.Status == nameof(AnalysisJobStatus.Completed)),
            Running: items.Count(i => i.Status is nameof(AnalysisJobStatus.Running) or nameof(AnalysisJobStatus.Pending)),
            Failed: items.Count(i => i.Status == nameof(AnalysisJobStatus.Failed)),
            Analysis: items);
    }

    private static IReadOnlyList<string> BuildModuleList(AnalysisJob job)
    {
        if (job.Options is null) return [];

        var modules = new List<string>();
        if (job.Options.IncludeTopography) modules.Add("Topography");
        if (job.Options.IncludeSoil) modules.Add("Soil");
        if (job.Options.IncludeBearing) modules.Add("Bearing");
        if (job.Options.IncludeRisk) modules.Add("Risk");
        if (job.Options.IncludeBorehole) modules.Add("Borehole");
        return modules;
    }
}
