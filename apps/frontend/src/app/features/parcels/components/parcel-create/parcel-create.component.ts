import { Component, signal, ViewChild, computed, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { MapComponent } from '../../../../shared/components/map/map.component';
import { ROUTES, STORAGE_KEYS } from '../../../../shared/config/constants';
import area from '@turf/area';
import length from '@turf/length';
import distance from '@turf/distance';
import kinks from '@turf/kinks';
import * as turf from '@turf/turf';

@Component({
  selector: 'app-parcel-create',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule, MapComponent],
  templateUrl: './parcel-create.component.html',
  styleUrls: ['./parcel-create.component.css'],
})
export class ParcelCreateComponent {
  @ViewChild(MapComponent) mapComponent!: MapComponent;

  isSatelliteActive = signal(false);
  mapOpacity = signal(100);

  parcelName = signal<string>('');
  polygonPoints = signal<[number, number][]>([]);
  drawnGeoJSON = signal<string>('');
  saving = signal(false);
  dmsMode = signal(false);
  highlightedIndex = signal<number | null>(null);

  geoJsonError = signal<string | null>(null);
  isGeoJsonLoading = signal(false);

  sidebarOpen = signal(false);

  initialCenter: [number, number] = [31.2357, 30.0444];
  initialZoom = 12;

  constructor(private router: Router) {
    const saved = localStorage.getItem(STORAGE_KEYS.PARCELS_POINTS);
    if (saved) {
      this.polygonPoints.set(JSON.parse(saved));
    }
    effect(() => {
      const points = this.polygonPoints();
      localStorage.setItem(STORAGE_KEYS.PARCELS_POINTS, JSON.stringify(points));
    });
  }

  dmsPoints = computed(() => {
    return this.polygonPoints().map((point) => ({
      lng: this.toDMSParts(point[0], 'lng'),
      lat: this.toDMSParts(point[1], 'lat'),
    }));
  });

  edgeLengths = computed(() => {
    const pts = this.polygonPoints();
    if (pts.length < 2) return [] as number[];
    return pts.map((p, i) => {
      const next = pts[(i + 1) % pts.length];
      return distance(p, next, { units: 'meters' });
    });
  });

  parcelStats = computed(() => {
    const points = this.polygonPoints();
    if (points.length < 3) return { sqMeters: '0', hectares: '0.000', perimeter: '0' };
    const feature: any = {
      type: 'Feature',
      geometry: { type: 'Polygon', coordinates: [[...points, points[0]]] },
    };
    const sqM = area(feature);
    const peri = length(feature, { units: 'meters' });
    return {
      sqMeters: Math.round(sqM).toLocaleString(),
      hectares: (sqM / 10000).toFixed(3),
      perimeter: peri.toFixed(1),
    };
  });

  onMapReady(map: maplibregl.Map) {
    const existingPoints = this.polygonPoints();
    if (existingPoints.length >= 3) {
      this.mapComponent?.setPoints(existingPoints);
      const bounds = this.calculateBounds(existingPoints);
      map.fitBounds(bounds, { padding: 50, duration: 0 });
    } else {
      this.mapComponent?.locateMe();
    }
  }

  onPointsChanged(points: [number, number][]) {
    this.polygonPoints.set(points);
    if (points.length >= 3) {
      const feature: any = {
        type: 'Feature',
        geometry: { type: 'Polygon', coordinates: [[...points, points[0]]] },
        properties: {},
      };
      this.drawnGeoJSON.set(JSON.stringify(feature));
    } else {
      this.drawnGeoJSON.set('');
    }
  }

  highlightPoint(index: number | null) {
    this.highlightedIndex.set(index);
  }

  focusPoint(index: number) {
    this.mapComponent?.flyToPoint(index);
  }

  addManualPoint() {
    this.mapComponent?.addManualPoint();
  }

  private calculateBounds(points: [number, number][]): maplibregl.LngLatBoundsLike {
    const lats = points.map((p) => p[1]);
    const lngs = points.map((p) => p[0]);
    return [
      [Math.min(...lngs), Math.min(...lats)],
      [Math.max(...lngs), Math.max(...lats)],
    ];
  }

  removePoint(index: number) {
    const current = this.polygonPoints();
    const updated = current.filter((_, i) => i !== index);
    if (updated.length < 3) {
      this.mapComponent?.clearDrawing();
    } else {
      this.mapComponent?.setPoints(updated);
    }
  }

  clearDrawing() {
    this.mapComponent?.clearDrawing();
    this.polygonPoints.set([]);
    this.drawnGeoJSON.set('');
    localStorage.removeItem(STORAGE_KEYS.PARCELS_POINTS);
  }

  saveParcel() {
    if (!this.parcelName() || this.polygonPoints().length < 3) return;
    this.saving.set(true);
    setTimeout(() => {
      this.saving.set(false);
      this.router.navigate([ROUTES.newParcel]);
    }, 1500);
  }

  locateMe() {
    this.mapComponent?.locateMe();
  }

  updateOpacity(event: Event) {
    const numericValue = parseInt((event.target as HTMLInputElement).value, 10);
    this.mapOpacity.set(numericValue);
    const map = this.mapComponent?.map;
    if (map && map.getLayer('satellite')) {
      map.setPaintProperty('satellite', 'raster-opacity', numericValue / 100);
    }
  }

  private toDMSParts(decimal: number, type: 'lat' | 'lng') {
    const absolute = Math.abs(decimal);
    const degrees = Math.floor(absolute);
    const minutesFloat = (absolute - degrees) * 60;
    const minutes = Math.floor(minutesFloat);
    const seconds = parseFloat(((minutesFloat - minutes) * 60).toFixed(2));
    const direction = type === 'lat' ? (decimal >= 0 ? 'N' : 'S') : decimal >= 0 ? 'E' : 'W';
    return { degrees, minutes, seconds, direction };
  }

  onDmsChange(
    pointIndex: number,
    coordType: 'lng' | 'lat',
    field: 'degrees' | 'minutes' | 'seconds',
    event: Event,
  ) {
    const input = event.target as HTMLInputElement;
    const value = parseFloat(input.value);
    if (isNaN(value)) return;

    const parts = this.dmsPoints()[pointIndex][coordType];
    const newParts = { ...parts, [field]: value };

    // Validate ranges
    if (field === 'degrees' && (value < 0 || value >= 180)) return;
    if (field === 'minutes' && (value < 0 || value >= 60)) return;
    if (field === 'seconds' && (value < 0 || value >= 60)) return;

    // Convert back to decimal
    const decimal = this.dmsToDecimal(
      newParts.degrees,
      newParts.minutes,
      newParts.seconds,
      newParts.direction,
    );

    // Update the point
    const points = [...this.polygonPoints()];
    const point = [...points[pointIndex]] as [number, number];
    if (coordType === 'lng') point[0] = decimal;
    else point[1] = decimal;
    points[pointIndex] = point;
    this.mapComponent?.setPoints(points);
  }

  private dmsToDecimal(
    degrees: number,
    minutes: number,
    seconds: number,
    direction: string,
  ): number {
    let decimal = degrees + minutes / 60 + seconds / 3600;
    if (direction === 'S' || direction === 'W') decimal = -decimal;
    return decimal;
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    this.geoJsonError.set(null);
    this.isGeoJsonLoading.set(true);

    // 1. Validate extension
    const name = file.name.toLowerCase();
    if (!name.endsWith('.geojson') && !name.endsWith('.json')) {
      this.geoJsonError.set('Invalid file type. Please upload a .geojson or .json file.');
      this.isGeoJsonLoading.set(false);
      input.value = ''; // reset
      return;
    }

    // 2. Validate size (5 MB)
    if (file.size > 5 * 1024 * 1024) {
      this.geoJsonError.set('File too large. Maximum size is 5 MB.');
      this.isGeoJsonLoading.set(false);
      input.value = '';
      return;
    }

    const reader = new FileReader();
    reader.onload = (e: ProgressEvent<FileReader>) => {
      try {
        const text = e.target?.result as string;
        const geojson = JSON.parse(text);

        // 3. Check GeoJSON structure: must be a FeatureCollection or a single Feature
        let geometry;
        if (geojson.type === 'FeatureCollection') {
          if (!geojson.features || geojson.features.length === 0) {
            throw new Error('GeoJSON file contains no features.');
          }
          geometry = geojson.features[0].geometry;
        } else if (geojson.type === 'Feature') {
          geometry = geojson.geometry;
        } else if (geojson.type === 'Polygon' || geojson.type === 'MultiPolygon') {
          geometry = geojson;
        } else {
          throw new Error('GeoJSON must be a FeatureCollection, Feature, or Polygon geometry.');
        }

        // 4. Extract ring from geometry
        const ring = geometry.coordinates[0] as any[];

        // Convert all coordinates (they may be DMS strings)
        let convertedCoords: [number, number][] = [];
        try {
          convertedCoords = ring.map((pair) => {
            if (!Array.isArray(pair) || pair.length < 2) {
              throw new Error('Invalid coordinate pair');
            }
            return [this.parseDMSCoordinate(pair[0]), this.parseDMSCoordinate(pair[1])] as [
              number,
              number,
            ];
          });
        } catch (err: any) {
          throw new Error(`Coordinate parsing failed: ${err.message}`);
        }

        // Handle ring closure (after conversion, so we can compare numbers)
        let coords: [number, number][] = [];
        if (convertedCoords.length >= 3) {
          const first = convertedCoords[0];
          const last = convertedCoords[convertedCoords.length - 1];
          if (first[0] === last[0] && first[1] === last[1]) {
            coords = convertedCoords.slice(0, -1); // closed ring - remove duplicate
          } else {
            coords = convertedCoords; // open ring - keep all
          }
        }

        if (coords.length < 3) {
          throw new Error('The polygon must have at least 3 vertices.');
        }

        // 5. Check for self-intersections using the converted coords
        const poly = turf.polygon([[...coords, coords[0]]]);
        const intersectionPoints = kinks(poly);
        if (intersectionPoints.features.length > 0) {
          throw new Error('The polygon is self-intersecting. Please fix the geometry.');
        }

        // Success: load the converted points
        this.geoJsonError.set(null);
        this.mapComponent?.setPoints(coords);
        const bounds = this.calculateBounds(coords);
        this.mapComponent?.map?.fitBounds(bounds, { padding: 50, duration: 1000 });

        // Optionally set parcel name from file name
        if (!this.parcelName()) {
          const baseName = file.name.replace(/\.[^/.]+$/, '');
          this.parcelName.set(baseName);
        }
      } catch (err: any) {
        this.geoJsonError.set(err.message || 'Invalid GeoJSON file.');
      } finally {
        this.isGeoJsonLoading.set(false);
        input.value = ''; // reset file input
      }
    };

    reader.onerror = () => {
      this.geoJsonError.set('Failed to read the file.');
      this.isGeoJsonLoading.set(false);
      input.value = '';
    };

    reader.readAsText(file);
  }

  private parseDMSCoordinate(value: any): number {
    if (typeof value === 'number') return value;

    if (typeof value !== 'string') {
      throw new Error('Coordinate must be a number or a DMS string.');
    }

    const trimmed = value.trim();

    // Check if it's a plain decimal number
    const plainNumber = parseFloat(trimmed);
    if (!isNaN(plainNumber) && /^[-+]?(\d+(\.\d*)?|\.\d+)$/.test(trimmed)) {
      return plainNumber;
    }

    // Standard DMS: 40°26'46"N
    const dmsRegex = /^([+-]?\d+)\s*°\s*(\d+)\s*'\s*(\d+(?:\.\d+)?)\s*"\s*([NSEW])$/i;
    const match = trimmed.match(dmsRegex);
    if (match) {
      return this.dmsToDecimal(
        parseInt(match[1], 10),
        parseInt(match[2], 10),
        parseFloat(match[3]),
        match[4].toUpperCase(),
      );
    }

    // Alternative spacing: 40 26 46 N
    const altRegex = /^(\d+)\s+(\d+)\s+(\d+(?:\.\d+)?)\s*([NSEW])$/i;
    const altMatch = trimmed.match(altRegex);
    if (altMatch) {
      return this.dmsToDecimal(
        parseInt(altMatch[1], 10),
        parseInt(altMatch[2], 10),
        parseFloat(altMatch[3]),
        altMatch[4].toUpperCase(),
      );
    }

    throw new Error(`Invalid DMS coordinate: "${value}"`);
  }
}
