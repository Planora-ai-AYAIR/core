using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NetTopologySuite.Geometries;
using Planora.Domain.Enums;
using Planora.Domain.Shared.Abstractions;
using Planora.Domain.Shared.Results;

namespace Planora.Domain.Parcels
{
    public sealed class Parcel : SoftDeletableEntity
    {
        public Guid UserId { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public Polygon Boundary { get; private set; } = null!;
        public decimal AreaHectares { get; private set; }
        public Point Centroid { get; private set; } = null!;
        public string? Country { get; private set; }
        public string? Governorate { get; private set; }
        public string? GeojsonKey { get; private set; }
        public ParcelStatus Status { get; private set; }

        private Parcel() { } 

        private Parcel(Guid id,
            Guid userId,
            string name,
            Polygon boundary,
            decimal areaHectares,
            Point centroid,
            string? geojsonKey)
        {
            Id = id;
            UserId = userId;
            Name = name;
            Boundary = boundary;
            AreaHectares = areaHectares;
            Centroid = centroid;
            GeojsonKey = geojsonKey;
            Status = ParcelStatus.Draft;
            CreatedAt = DateTime.UtcNow;
        }

        public static Result<Parcel> Create(Guid id, Guid userId, string name, Polygon boundary, decimal areaHectares, Point centroid, string? geojsonS3Key = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return ParcelErrors.InvalidName;
            if (boundary == null || !boundary.IsValid) return ParcelErrors.InvalidBoundary;
            if (areaHectares < 0.5m) return ParcelErrors.AreaTooSmall;

            return new Parcel(id, userId, name.Trim(), boundary, areaHectares, centroid, geojsonS3Key);
        }

        public void MarkAsProcessing()
        {
            Status = ParcelStatus.Processing;
        }

        public void MarkAsCompleted()
        {
            Status = ParcelStatus.Completed;
        }

        public void MarkAsFailed()
        {
            Status = ParcelStatus.Failed;
        }
    }
}
