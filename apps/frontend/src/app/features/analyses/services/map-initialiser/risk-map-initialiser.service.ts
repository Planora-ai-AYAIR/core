import { Injectable } from '@angular/core';
import { MapInitialiser } from '../../interfaces/map-initialiser';
import maplibregl from 'maplibre-gl';
import { RiskData } from '../../interfaces/risk-data';

@Injectable({ providedIn: 'root' })
export class RiskMapInitialiser implements MapInitialiser<RiskData> {
  private _riskData: RiskData | null = null;

  addLayers(map: maplibregl.Map, data: RiskData): void {
    this._riskData = data;
    this.addFloodZones(map, data.floodFeatures);
    this.addSeismicZones(map, data.seismicZonesGeoJSON);
    this.addExpansiveSoilZones(map, data.expansiveSoilZonesGeoJSON);
    this.addLiquefactionZones(map, data.liquefactionZonesGeoJSON);
    this.attachPopups(map);
  }

  private addFloodZones(
    map: maplibregl.Map,
    features: { risk: string; coords: [number, number][] }[],
  ) {
    if (map.getSource('flood-src') || map.getLayer('flood-zone')) {
      return;
    }
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
        'fill-color': ['match', ['get', 'risk'], 'High', '#1E40AF', 'Medium', '#3B82F6', '#ccc'],
        'fill-opacity': 0.5,
      },
    });
  }

  private addSeismicZones(map: maplibregl.Map, geoJSON: any) {
    if (map.getSource('seismic-src') || map.getLayer('seismic-zone')) {
      return;
    }
    map.addSource('seismic-src', { type: 'geojson', data: geoJSON });
    map.addLayer({
      id: 'seismic-zone',
      type: 'circle',
      source: 'seismic-src',
      paint: {
        'circle-radius': 80,
        'circle-color': [
          'match',
          ['get', 'classification'],
          'Zone I',
          '#10B981',
          'Zone II',
          '#F59E0B',
          'Zone III',
          '#F97316',
          'Zone IV',
          '#EF4444',
          '#ccc',
        ],
        'circle-opacity': 0.35,
        'circle-stroke-width': 2,
        'circle-stroke-color': [
          'match',
          ['get', 'classification'],
          'Zone I',
          '#047857',
          'Zone II',
          '#B45309',
          'Zone III',
          '#C2410C',
          'Zone IV',
          '#B91C1C',
          '#999',
        ],
      },
      layout: {},
    });
    map.on('mouseenter', 'seismic-zone', () => {
      map.getCanvas().style.cursor = 'pointer';
      map.setPaintProperty('seismic-zone', 'circle-opacity', 0.7);
    });
    map.on('mouseleave', 'seismic-zone', () => {
      map.getCanvas().style.cursor = '';
      map.setPaintProperty('seismic-zone', 'circle-opacity', 0.35);
    });
  }

  private addExpansiveSoilZones(map: maplibregl.Map, geoJSON: any) {
    if (map.getSource('expansive-src') || map.getLayer('expansive-soil')) {
      return;
    }
    map.addSource('expansive-src', { type: 'geojson', data: geoJSON });
    map.addLayer({
      id: 'expansive-soil',
      type: 'fill',
      source: 'expansive-src',
      paint: {
        'fill-color': '#A0522D',
        'fill-opacity': 0.45,
        'fill-outline-color': '#7B3F1A',
      },
      // Ensure it's above flood zones
      ...(map.getLayer('flood-zone') ? { beforeId: 'flood-zone' } : {}),
    });

    map.on('mouseenter', 'expansive-soil', () => {
      map.getCanvas().style.cursor = 'pointer';
      map.setPaintProperty('expansive-soil', 'fill-opacity', 0.65);
    });
    map.on('mouseleave', 'expansive-soil', () => {
      map.getCanvas().style.cursor = '';
      map.setPaintProperty('expansive-soil', 'fill-opacity', 0.45);
    });
  }

  private addLiquefactionZones(map: maplibregl.Map, geoJSON: any) {
    if (map.getSource('liquefaction-src') || map.getLayer('liquefaction')) {
      return;
    }
    map.addSource('liquefaction-src', { type: 'geojson', data: geoJSON });
    map.addLayer({
      id: 'liquefaction',
      type: 'fill',
      source: 'liquefaction-src',
      paint: {
        'fill-color': '#EAB308',
        'fill-opacity': 0.45,
        'fill-outline-color': '#6D28D9',
      },
    });

    map.on('mouseenter', 'liquefaction', () => {
      map.getCanvas().style.cursor = 'pointer';
      map.setPaintProperty('liquefaction', 'fill-opacity', 0.65);
    });
    map.on('mouseleave', 'liquefaction', () => {
      map.getCanvas().style.cursor = '';
      map.setPaintProperty('liquefaction', 'fill-opacity', 0.45);
    });
  }

  private attachPopups(map: maplibregl.Map) {
    const data = this._riskData;
    if (!data) return;

    const popupLayers = ['flood-zone', 'seismic-zone', 'expansive-soil', 'liquefaction'];
    popupLayers.forEach((layerId) => {
      map.on('click', layerId, (e) => {
        const feature = e.features?.[0];
        if (!feature) return;

        let html = '';
        if (layerId === 'flood-zone') {
          html = `<strong>Flood Risk</strong><br>
                Score: ${data.floodRisk.score}/100 — ${data.floodRisk.level}<br>
                Factors: ${data.floodRisk.factors.map((f) => f.detail).join('<br>')}<br>
                Mitigation: ${data.floodRisk.mitigation}`;
        } else if (layerId === 'seismic-zone') {
          html = `<strong>Seismic Risk</strong><br>
                Score: ${data.seismicRisk.score}/100 — ${data.seismicRisk.level}<br>
                Factors: ${data.seismicRisk.factors.map((f) => f.detail).join('<br>')}<br>
                Mitigation: ${data.seismicRisk.mitigation}`;
        } else if (layerId === 'expansive-soil') {
          html = `<strong>Expansive Soil Risk</strong><br>
                Score: ${data.expansiveSoilRisk.score}/100 — ${data.expansiveSoilRisk.level}<br>
                Factors: ${data.expansiveSoilRisk.factors.map((f) => f.detail).join('<br>')}<br>
                Mitigation: ${data.expansiveSoilRisk.mitigation}`;
        } else if (layerId === 'liquefaction') {
          html = `<strong>Liquefaction Risk</strong><br>
                Score: ${data.liquefactionRisk.score}/100 — ${data.liquefactionRisk.level}<br>
                Factors: ${data.liquefactionRisk.factors.map((f) => f.detail).join('<br>')}<br>
                Mitigation: ${data.liquefactionRisk.mitigation}`;
        }
        new maplibregl.Popup().setLngLat(e.lngLat).setHTML(html).addTo(map);
      });
    });
  }
}
