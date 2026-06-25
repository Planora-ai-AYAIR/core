import { Injectable } from '@angular/core';
import { MapInitialiser } from '../../interfaces/map-initialiser';
import maplibregl from 'maplibre-gl';
import { BearingData } from '../../interfaces/bearing-data';

@Injectable({ providedIn: 'root' })
export class BearingMapInitialiser implements MapInitialiser<BearingData> {
  addLayers(map: maplibregl.Map, data: BearingData): void {
    this.addBearingPoints(map, data.bearingPoints);
    this.addWaterTable(map, data.waterTableLines);
  }

  private addBearingPoints(map: maplibregl.Map, points: BearingData['bearingPoints']) {
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
