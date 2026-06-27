using NetTopologySuite.Geometries;
using Planora.Application.Common.Dtos;

namespace Planora.Application.Common.Helpers;

public static class AiGeometryExtensions
{
    public static AiGeoJsonPolygon ToAiGeoJsonPolygon(this Polygon polygon)
    {
        var ring = new List<List<double>>(polygon.ExteriorRing.NumPoints);
        foreach (var coord in polygon.ExteriorRing.Coordinates)
        {
            ring.Add(new List<double> { coord.X, coord.Y });
        }

        return new AiGeoJsonPolygon(
            Type: "Polygon",
            Coordinates: new List<List<List<double>>> { ring });
    }

    public static AiBoundingBox ToAiBoundingBox(this Polygon polygon)
    {
        var env = polygon.EnvelopeInternal;
        return new AiBoundingBox(env.MinX, env.MinY, env.MaxX, env.MaxY);
    }
}
