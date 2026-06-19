import { Injectable } from '@angular/core';
import { MapInitialiser } from '../interfaces/map-initialiser';
import maplibregl from 'maplibre-gl';
import { BoreholeData } from '../interfaces/borehole-data';
import type { FeatureCollection, Polygon } from 'geojson';

@Injectable({ providedIn: 'root' })
@Injectable({ providedIn: 'root' })
export class BoreholeMapInitialiser implements MapInitialiser<BoreholeData> {
  addLayers(map: maplibregl.Map, data: BoreholeData): void {
    // Add borehole points source
    map.addSource('boreholes-src', {
      type: 'geojson',
      data: {
        type: 'FeatureCollection',
        features: data.placementPoints.map((p) => ({
          type: 'Feature',
          properties: { id: p.id, priority: p.priority, reason: p.reason, depth: p.estimatedDepth },
          geometry: { type: 'Point', coordinates: [p.lng, p.lat] },
        })),
      },
    });

    map.addLayer({
      id: 'borehole-points',
      type: 'circle',
      source: 'boreholes-src',
      paint: {
        'circle-radius': 8,
        'circle-color': [
          'match',
          ['get', 'priority'],
          'Critical',
          '#EF4444',
          'High',
          '#F97316',
          'Medium',
          '#EAB308',
          'Low',
          '#10B981',
          '#ccc',
        ],
        'circle-opacity': 0.9,
        'circle-stroke-width': 2,
        'circle-stroke-color': '#fff',
      },
    });

    // Popup on click
    map.on('click', 'borehole-points', (e) => {
      const props = e.features?.[0].properties;
      if (!props) return;
      const html = `<strong>${props['id']}</strong><br>
                    Priority: ${props['priority']}<br>
                    Reason: ${props['reason']}<br>
                    Est. depth: ${props['depth']} m`;
      new maplibregl.Popup().setLngLat(e.lngLat).setHTML(html).addTo(map);
    });

    map.on('mouseenter', 'borehole-points', () => (map.getCanvas().style.cursor = 'pointer'));
    map.on('mouseleave', 'borehole-points', () => (map.getCanvas().style.cursor = ''));
  }
}
