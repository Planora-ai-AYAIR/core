using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Planora.Application.Features.Reports.Dtos.SubmitPdfJob
{
    public sealed class PdfTopographyData
    {
        public double ElevationMin { get; set; }
        public double ElevationMax { get; set; }
        public double ElevationMean { get; set; }
        public string SlopeDistributionJson { get; set; } = string.Empty;
        public double CutVolume { get; set; }
        public double FillVolume { get; set; }
        public double NetVolume { get; set; }
        public double ContourInterval { get; set; }
        public string? ContourGeoJsonUrl { get; set; }  // ← Public setter, can be presigned
        public string? PondingGeoJsonUrl { get; set; }
        public int? PondingZonesCount { get; set; }
        public double? PondingTotalArea { get; set; }
        public string? ElevationTileUrl { get; set; }  // ← Will hold presigned URL
        public string? SlopeTileUrl { get; set; }       // ← Will hold presigned URL
    }
}
