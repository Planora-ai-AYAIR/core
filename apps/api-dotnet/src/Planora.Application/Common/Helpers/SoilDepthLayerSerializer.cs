using Planora.Application.Features.Analysis.Dtos;
using System.Text.Json;

namespace Planora.Application.Common.Helpers;

internal static class SoilDepthLayerSerializer
{
    public static string? Serialize(List<SoilDepthLayerPayload>? layers) =>
        layers is null
            ? null
            : JsonSerializer.Serialize(layers.Select(d => new
            {
                depth = d.Depth,
                sand = d.Sand,
                silt = d.Silt,
                clay = d.Clay,
                type = d.SoilType,
                bulkDensity = d.BulkDensity
            }));
}
