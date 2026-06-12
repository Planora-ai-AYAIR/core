import { Injectable } from '@angular/core';
import { MapInitialiser } from '../interfaces/map-initialiser';
import maplibregl from 'maplibre-gl';
import { SoilData } from '../interfaces/soil-data';

@Injectable({ providedIn: 'root' })
export class SoilMapInitialiser implements MapInitialiser<SoilData> {
  addLayers(map: maplibregl.Map, data: SoilData): void {
    this.addSoilComposition(map, data.soilCompositionGeoJSON);
    this.addBearingPoints(map, data.bearingPoints);
    this.addWaterTable(map, data.waterTableLines);
  }

  private addSoilComposition(map: maplibregl.Map, geoJSON: any) {
    map.addSource('soil-comp-src', { type: 'geojson', data: geoJSON });
    map.addLayer({
      id: 'soil-composition',
      type: 'fill',
      source: 'soil-comp-src',
      paint: {
        'fill-color': [
          'match',
          ['get', 'type'],
          'Clay',
          '#B86E3D',
          'Silt',
          '#6B7F5E',
          'Sand',
          '#E0BF6B',
          '#ccc',
        ],
        'fill-opacity': 0.6,
      },
    });
  }

  private addBearingPoints(
    map: maplibregl.Map,
    points: { lng: number; lat: number; capacity: number; depth: number }[],
  ) {
    map.addSource('bearing-src', {
      type: 'geojson',
      data: {
        type: 'FeatureCollection',
        features: points.map((p) => ({
          type: 'Feature',
          properties: { capacity: p.capacity, depth: p.depth },
          geometry: { type: 'Point', coordinates: [p.lng, p.lat] },
        })),
      },
    });
    map.addLayer({
      id: 'bearing-points',
      type: 'circle',
      source: 'bearing-src',
      paint: {
        'circle-radius': ['interpolate', ['linear'], ['get', 'capacity'], 200, 6, 300, 14],
        'circle-color': '#C7A14D',
        'circle-opacity': 0.9,
        'circle-stroke-color': '#fff',
        'circle-stroke-width': 2,
      },
    });
  }

  private addWaterTable(map: maplibregl.Map, lines: any[]) {
    map.addSource('water-src', {
      type: 'geojson',
      data: { type: 'FeatureCollection', features: lines },
    });
    map.addLayer({
      id: 'water-table',
      type: 'line',
      source: 'water-src',
      paint: { 'line-color': '#2563EB', 'line-width': 1, 'line-opacity': 0.5 },
      layout: { visibility: 'none' },
    });
  }
}
