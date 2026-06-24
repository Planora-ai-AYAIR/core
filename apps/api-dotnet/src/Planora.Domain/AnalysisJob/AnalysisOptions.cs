namespace Planora.Domain.AnalysisJob;

public sealed class AnalysisOptions
{
    public bool IncludeTopography { get; private set; }
    public bool IncludeSoil { get; private set; }
    public bool IncludeBearing { get; private set; }
    public bool IncludeRisk { get; private set; }
    public bool IncludeBorehole { get; private set; }
    public decimal? ContourInterval { get; private set; }
    public string? SlopeCategories { get; private set; }
    public string? ReferencePlane { get; private set; }
    public string? SoilDepths { get; private set; }

    private AnalysisOptions() { }

    public AnalysisOptions(
        bool includeTopography,
        bool includeSoil,
        bool includeBearing,
        bool includeRisk,
        bool includeBorehole,
        decimal? contourInterval = null,
        string? slopeCategories = null,
        string? referencePlane = null,
        string? soilDepths = null)
    {
        IncludeTopography = includeTopography;
        IncludeSoil = includeSoil;
        IncludeBearing = includeBearing;
        IncludeRisk = includeRisk;
        IncludeBorehole = includeBorehole;
        ContourInterval = contourInterval;
        SlopeCategories = slopeCategories;
        ReferencePlane = referencePlane;
        SoilDepths = soilDepths;
    }
}
