using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Application.Features.Reports.Dtos.SubmitPdfJob
{
    public sealed class PdfSoilData
    {
        public double SandPercent { get; set; }
        public double SiltPercent { get; set; }
        public double ClayPercent { get; set; }
        public double BulkDensity { get; set; }
        public double OrganicCarbon { get; set; }
        public double Ph { get; set; }
        public double BearingCapacityEstimate { get; set; }
        public string BearingCapacityCategory { get; set; } = string.Empty;
        public string? PrimaryType { get; set; }
        public string? UsdaClass { get; set; }
        public double? AiConfidence { get; set; }
        public string? MultiDepthProfileJson { get; set; }
        public string? HeatmapTileUrl { get; set; }  // ← Presigned URL
    }
}
