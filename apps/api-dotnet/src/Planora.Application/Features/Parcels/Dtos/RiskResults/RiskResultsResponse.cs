namespace Planora.Application.Features.Parcels.Dtos.RiskResults;

public sealed record RiskResultsResponse(
    Guid ParcelId,
    int OverallRiskScore,
    string OverallRiskLevel,
    RiskSubResultDto Flood,
    RiskSubResultDto Seismic,
    RiskSubResultDto ExpansiveSoil,
    RiskSubResultDto Liquefaction,
    DateTime GeneratedAt
);
