import { Injectable } from '@angular/core';
import { MapInitialiser } from '../interfaces/map-initialiser';
import maplibregl from 'maplibre-gl';
import { RiskData } from '../interfaces/risk-data';

@Injectable({ providedIn: 'root' })
export class RiskMapInitialiser implements MapInitialiser<RiskData> {
  addLayers(map: maplibregl.Map, data: RiskData): void {
    this.addFloodZones(map, data.floodFeatures);
    this.addSeismicZones(map, data.seismicPoint);
    this.addLiquefactionZones(map, data.liquefactionPolygon);
  }

  private addFloodZones(
    map: maplibregl.Map,
    features: { risk: string; coords: [number, number][] }[],
  ) {
    map.addSource('flood-src', {
      type: 'geojson',
      data: {
        type: 'FeatureCollection',
        features: features.map((f) => ({
          type: 'Feature',
          properties: { risk: f.risk },
          geometry: { type: 'Polygon', coordinates: [f.coords] },
        })),
      },
    });
    map.addLayer({
      id: 'flood-zone',
      type: 'fill',
      source: 'flood-src',
      paint: {
        'fill-color': ['match', ['get', 'risk'], 'High', '#A13E3A', 'Medium', '#D97706', '#ccc'],
        'fill-opacity': 0.5,
      },
    });
  }

  private addSeismicZones(map: maplibregl.Map, point: [number, number]) {
    map.addSource('seismic-src', {
      type: 'geojson',
      data: {
        type: 'Feature',
        properties: { zone: 'II' },
        geometry: { type: 'Point', coordinates: point },
      },
    });
    map.addLayer({
      id: 'seismic-zone',
      type: 'circle',
      source: 'seismic-src',
      paint: {
        'circle-radius': 80,
        'circle-color': '#F59E0B',
        'circle-opacity': 0.3,
        'circle-stroke-color': '#B45309',
        'circle-stroke-width': 2,
      },
    });
  }

  private addLiquefactionZones(map: maplibregl.Map, polygon: [number, number][]) {
    map.addSource('liquefaction-src', {
      type: 'geojson',
      data: {
        type: 'Feature',
        properties: {},
        geometry: { type: 'Polygon', coordinates: [polygon] },
      },
    });
    map.addLayer({
      id: 'liquefaction',
      type: 'fill',
      source: 'liquefaction-src',
      paint: { 'fill-color': '#8B5CF6', 'fill-opacity': 0.6, 'fill-outline-color': '#6D28D9' },
      layout: { visibility: 'none' },
    });
  }
}
