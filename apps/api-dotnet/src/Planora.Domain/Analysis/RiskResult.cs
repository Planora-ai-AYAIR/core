using Planora.Domain.Shared.Abstractions;

namespace Planora.Domain.Analysis;

public sealed class RiskResult : AuditableEntity
{
    public Guid AnalysisJobId { get; private set; }

    public int OverallRiskScore { get; private set; }
    public string? OverallRiskLevel { get; private set; }

    // Risk Breakdown
    public int FloodRiskScore { get; private set; }
    public string? FloodLevel { get; private set; }
    public string? FloodFactorsJson { get; private set; }


    public int SeismicRiskScore { get; private set; }
    public int ExpansiveSoilRisk { get; private set; }
    public int LiquefactionRisk { get; private set; }
    public string? FloodGeoJsonUrl { get; private set; }
    public string? SeismicLevel { get; private set; }
    public string? SeismicFactorsJson { get; private set; }
    public string? SeismicSource { get; private set; }
    public string? SeismicZone { get; private set; }
    public string? ExpansiveSoilLevel { get; private set; }
    public string? ExpansiveSoilFactorsJson { get; private set; }
    public double? ReplacementDepth { get; private set; }
    public string? LiquefactionLevel { get; private set; }
    public string? LiquefactionFactorsJson { get; private set; }
    public string? LiquefactionSusceptibility { get; private set; }
    public string? LiquefactionMethodology { get; private set; }
    public string? RiskHeatmapTileUrl { get; private set; }
    public string? MitigationSuggestionsJson { get; private set; }

    private RiskResult() { }

    public RiskResult(
        Guid analysisJobId,
        int floodRiskScore,
        int seismicRiskScore,
        int expansiveSoilRisk,
        int liquefactionRisk,
        int overallRiskScore,
        string? overallRiskLevel = null,
        string? floodLevel = null,
        string? floodFactorsJson = null,
        string? floodGeoJsonUrl = null,
        string? seismicLevel = null,
        string? seismicFactorsJson = null,
        string? seismicSource = null,
        string? seismicZone = null,
        string? expansiveSoilLevel = null,
        string? expansiveSoilFactorsJson = null,
        double? replacementDepth = null,
        string? liquefactionLevel = null,
        string? liquefactionFactorsJson = null,
        string? liquefactionSusceptibility = null,
        string? liquefactionMethodology = null,
        string? riskHeatmapTileUrl = null,
        string? mitigationSuggestionsJson = null)
    {
        Id = Guid.NewGuid();
        AnalysisJobId = analysisJobId;
        FloodRiskScore = floodRiskScore;
        SeismicRiskScore = seismicRiskScore;
        ExpansiveSoilRisk = expansiveSoilRisk;
        LiquefactionRisk = liquefactionRisk;
        OverallRiskScore = overallRiskScore;
        OverallRiskLevel = overallRiskLevel;
        FloodLevel = floodLevel;
        FloodFactorsJson = floodFactorsJson;
        FloodGeoJsonUrl = floodGeoJsonUrl;
        SeismicLevel = seismicLevel;
        SeismicFactorsJson = seismicFactorsJson;
        SeismicSource = seismicSource;
        SeismicZone = seismicZone;
        ExpansiveSoilLevel = expansiveSoilLevel;
        ExpansiveSoilFactorsJson = expansiveSoilFactorsJson;
        ReplacementDepth = replacementDepth;
        LiquefactionLevel = liquefactionLevel;
        LiquefactionFactorsJson = liquefactionFactorsJson;
        LiquefactionSusceptibility = liquefactionSusceptibility;
        LiquefactionMethodology = liquefactionMethodology;
        RiskHeatmapTileUrl = riskHeatmapTileUrl;
        MitigationSuggestionsJson = mitigationSuggestionsJson;
    }
}
