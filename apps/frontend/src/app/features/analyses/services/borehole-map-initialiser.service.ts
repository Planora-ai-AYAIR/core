import { Injectable } from '@angular/core';
import { MapInitialiser } from '../interfaces/map-initialiser';
import maplibregl from 'maplibre-gl';
import { BoreholeData } from '../interfaces/borehole-data';
import type { FeatureCollection, Polygon } from 'geojson';

@Injectable({ providedIn: 'root' })
export class BoreholeMapInitialiser implements MapInitialiser<BoreholeData> {
  addLayers(map: maplibregl.Map, data: BoreholeData): void {
    this.addBoreholePoints(map, data.points);
    this.addDepthRings(map, data.points);
    this.addOptimalArea(map, data.optimalArea);
  }

  private addBoreholePoints(map: maplibregl.Map, points: BoreholeData['points']) {
    map.addSource('bh-src', {
      type: 'geojson',
      data: {
        type: 'FeatureCollection',
        features: points.map((p) => ({
          type: 'Feature',
          properties: { name: p.id, depth: p.depth },
          geometry: { type: 'Point', coordinates: [p.lng, p.lat] },
        })),
      },
    });
    map.addLayer({
      id: 'borehole-points',
      type: 'circle',
      source: 'bh-src',
      paint: {
        'circle-radius': 10,
        'circle-color': '#B86E3D',
        'circle-stroke-color': '#fff',
        'circle-stroke-width': 2,
      },
    });
  }

  private addDepthRings(map: maplibregl.Map, points: BoreholeData['points']) {
    const ringFeatures: FeatureCollection<Polygon>['features'] = points.map((p) => {
      const radius = p.depth * 0.0001;
      const coords: [number, number][] = [];
      for (let a = 0; a <= 360; a += 30) {
        const rad = (a * Math.PI) / 180;
        coords.push([p.lng + radius * Math.cos(rad), p.lat + radius * Math.sin(rad)]);
      }
      return {
        type: 'Feature',
        properties: { name: p.id },
        geometry: { type: 'Polygon', coordinates: [coords] },
      };
    });
    map.addSource('depth-src', {
      type: 'geojson',
      data: { type: 'FeatureCollection', features: ringFeatures },
    });
    map.addLayer({
      id: 'depth-rings',
      type: 'fill',
      source: 'depth-src',
      paint: { 'fill-color': '#B86E3D', 'fill-opacity': 0.2, 'fill-outline-color': '#B86E3D' },
    });
  }

  private addOptimalArea(map: maplibregl.Map, coords: [number, number][]) {
    map.addSource('optimal-src', {
      type: 'geojson',
      data: {
        type: 'Feature',
        properties: {},
        geometry: { type: 'Polygon', coordinates: [coords] },
      },
    });
    map.addLayer({
      id: 'optimal-area',
      type: 'fill',
      source: 'optimal-src',
      paint: { 'fill-color': '#10B981', 'fill-opacity': 0.3, 'fill-outline-color': '#047857' },
      layout: { visibility: 'none' },
    });
  }
}
