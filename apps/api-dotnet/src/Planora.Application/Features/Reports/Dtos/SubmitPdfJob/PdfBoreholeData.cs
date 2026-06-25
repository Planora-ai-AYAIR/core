using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Application.Features.Reports.Dtos.SubmitPdfJob
{
    public sealed class PdfBoreholeData
    {
        public int MinimumRequired { get; set; }
        public int OptimalCount { get; set; }
        public double CoveragePercentage { get; set; }
        public string? GridSize { get; set; }
        public string? PlacementStrategy { get; set; }
        public string? PlacementPointsJson { get; set; }
        public string? PlacementGeoJsonUrl { get; set; }  // ← Presigned URL
        public int TraditionalBoreholeCount { get; set; }
        public decimal TraditionalEstimatedCost { get; set; }
        public int OptimizedBoreholeCount { get; set; }
        public decimal OptimizedEstimatedCost { get; set; }
        public decimal SavingsAmount { get; set; }
        public double SavingsPercentage { get; set; }
    }
}
