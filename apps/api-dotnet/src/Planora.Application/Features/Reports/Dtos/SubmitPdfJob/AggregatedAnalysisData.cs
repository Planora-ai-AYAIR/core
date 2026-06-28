using Planora.Domain.Analysis;

namespace Planora.Application.Features.Reports.Dtos.SubmitPdfJob;

public sealed class AggregatedAnalysisData
{
    public Guid AnalysisJobId { get; set; }
    public PdfTopographyData? Topography { get; set; }
    public PdfSoilData? Soil { get; set; }
    public PdfBearingData? Bearing { get; set; }
    public PdfRiskData? Risk { get; set; }
    public PdfBoreholeData? Borehole { get; set; }
}