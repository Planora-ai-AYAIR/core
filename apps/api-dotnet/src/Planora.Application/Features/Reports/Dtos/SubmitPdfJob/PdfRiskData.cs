using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Application.Features.Reports.Dtos.SubmitPdfJob
{
    public sealed class PdfRiskData
    {
        public int FloodRiskScore { get; set; }
        public int SeismicRiskScore { get; set; }
        public int ExpansiveSoilRisk { get; set; }
        public int LiquefactionRisk { get; set; }
        public int OverallRiskScore { get; set; }
        public string? OverallRiskLevel { get; set; }
        public string? FloodGeoJsonUrl { get; set; }  // ← Presigned URL
        public string? SeismicLevel { get; set; }
        public string? SeismicSource { get; set; }
        public string? ExpansiveSoilLevel { get; set; }
        public double? ReplacementDepth { get; set; }
        public string? LiquefactionLevel { get; set; }
    }
}
