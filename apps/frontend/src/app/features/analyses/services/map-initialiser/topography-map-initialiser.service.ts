import { Injectable } from '@angular/core';
import { MapInitialiser } from '../../interfaces/map-initialiser';
import maplibregl from 'maplibre-gl';
import { TopographyData } from '../../interfaces/topography-data';

@Injectable({ providedIn: 'root' })
export class TopographyMapInitialiser implements MapInitialiser<TopographyData> {
  addLayers(map: maplibregl.Map, data: TopographyData): void {
    // 1. Elevation Heatmap
    if (data.elevationGrid?.length) {
      if (map.getSource('elevation-source')) {
        (map.getSource('elevation-source') as maplibregl.GeoJSONSource).setData({
          type: 'FeatureCollection',
          features: data.elevationGrid.map((p) => ({
            type: 'Feature',
            properties: { elev: p.elev },
            geometry: { type: 'Point', coordinates: [p.lng, p.lat] },
          })),
        });
      } else {
        this.addHeatmap(map, data.elevationGrid);
      }
    }

    // 2. Contour Lines
    if (data.contourLines?.length) {
      if (map.getSource('contours')) {
        (map.getSource('contours') as maplibregl.GeoJSONSource).setData({
          type: 'FeatureCollection',
          features: data.contourLines,
        });
      } else {
        this.addContours(map, data.contourLines);
      }
    }

    // 3. Slope Categories
    if (data.slopePolygons?.length) {
      if (map.getSource('slope-categories')) {
        (map.getSource('slope-categories') as maplibregl.GeoJSONSource).setData({
          type: 'FeatureCollection',
          features: data.slopePolygons,
        });
      } else {
        this.addSlope(map, data.slopePolygons);
      }
    }

    // 4. Ponding Zones
    if (data.pondingPolygons?.length) {
      if (map.getSource('ponding')) {
        (map.getSource('ponding') as maplibregl.GeoJSONSource).setData({
          type: 'FeatureCollection',
          features: data.pondingPolygons,
        });
      } else {
        this.addPonding(map, data.pondingPolygons);
      }
    }
  }

  private addHeatmap(map: maplibregl.Map, grid: any[]) {
    map.addSource('elevation-source', {
      type: 'geojson',
      data: {
        type: 'FeatureCollection',
        features: grid.map((p) => ({
          type: 'Feature',
          properties: { elev: p.elev },
          geometry: { type: 'Point', coordinates: [p.lng, p.lat] },
        })),
      },
    });
    map.addLayer({
      id: 'elevation-heat',
      type: 'heatmap',
      source: 'elevation-source',
      paint: {
        'heatmap-weight': ['interpolate', ['linear'], ['get', 'elev'], 22, 0, 46, 1],
        'heatmap-intensity': ['interpolate', ['linear'], ['zoom'], 14, 1, 20, 5],
        'heatmap-radius': ['interpolate', ['linear'], ['zoom'], 14, 14, 20, 50],
        'heatmap-opacity': 0.75,
        'heatmap-color': [
          'interpolate',
          ['linear'],
          ['heatmap-density'],
          0,
          'rgba(155,184,140,0)',
          0.2,
          '#9BB88C',
          0.4,
          '#C7A14D',
          0.6,
          '#B86E3D',
          0.8,
          '#9A4E29',
          1,
          '#5A2714',
        ],
      },
    });
  }

  private addContours(map: maplibregl.Map, lines: any[]) {
    map.addSource('contours', {
      type: 'geojson',
      data: { type: 'FeatureCollection', features: lines },
    });
    map.addLayer({
      id: 'contour-lines',
      type: 'line',
      source: 'contours',
      paint: {
        'line-color': [
          'interpolate',
          ['linear'],
          ['get', 'elevation'],
          24,
          '#5D7A52',
          33,
          '#8B6A3E',
          42,
          '#4A1E0A',
        ],
        'line-width': ['interpolate', ['linear'], ['get', 'elevation'], 24, 0.7, 33, 1.1, 42, 1.4],
        'line-opacity': 0.85,
      },
    });
  }

  private addSlope(map: maplibregl.Map, polygons: any[]) {
    map.addSource('slope-categories', {
      type: 'geojson',
      data: { type: 'FeatureCollection', features: polygons },
    });
    map.addLayer({
      id: 'slope-fill',
      type: 'fill',
      source: 'slope-categories',
      paint: {
        'fill-color': [
          'match',
          ['get', 'slope'],
          'flat',
          '#9BB88C',
          'gentle',
          '#C7A14D',
          'moderate',
          '#B86E3D',
          'steep',
          '#5A2714',
          '#ccc',
        ],
        'fill-opacity': 0.45,
        'fill-outline-color': 'rgba(0,0,0,0.08)',
      },
    });
  }

  private addPonding(map: maplibregl.Map, polygons: any[]) {
    map.addSource('ponding', {
      type: 'geojson',
      data: { type: 'FeatureCollection', features: polygons },
    });
    map.addLayer({
      id: 'ponding-zones',
      type: 'fill',
      source: 'ponding',
      paint: { 'fill-color': '#2563EB', 'fill-opacity': 0.5 },
    });
    map.addLayer({
      id: 'ponding-zones-outline',
      type: 'line',
      source: 'ponding',
      paint: { 'line-color': '#1D4ED8', 'line-width': 1.5, 'line-opacity': 0.9 },
    });
  }
}
