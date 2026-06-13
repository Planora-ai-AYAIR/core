using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Planora.Application.Common.Helpers;
public static class GeometryExtensions
{
    private static readonly GeoJsonWriter Writer = new();

    public static string ToGeoJson(this Geometry geometry) => Writer.Write(geometry);
}