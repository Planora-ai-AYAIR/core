import { Injectable } from '@angular/core';
import { MapInitialiser } from '../interfaces/map-initialiser';
import maplibregl from 'maplibre-gl';
import { SoilData } from '../interfaces/soil-data';

@Injectable({ providedIn: 'root' })
export class SoilMapInitialiser implements MapInitialiser<SoilData> {
  private _currentPopup: maplibregl.Popup | null = null;

  addLayers(map: maplibregl.Map, data: SoilData): void {
    const enrichedGeoJSON = this.enrichGeoJSONWithConfidence(data);

    // 1. Add background heatmaps
    this.addSoilHeatmaps(map, data.heatmapUrls);

    // 2. Add clickable composition layers on top
    this.addSoilComposition(map, enrichedGeoJSON);

    // 3. Set up event listeners
    this.setupMapInteractions(map);

    // Center the camera on the parcel's soil composition geometry
    if (data.soilCompositionGeoJSON?.features?.length > 0) {
      const firstFeature = data.soilCompositionGeoJSON.features[0];
      const firstCoordinate = firstFeature.geometry.coordinates[0][0]; // Grabs [lng, lat]

      map.flyTo({
        center: [firstCoordinate[0], firstCoordinate[1]],
        zoom: 14, // Close enough to view the engineering parcels clearly
        pitch: 0, // Flatten out the map view for better 2D polygon parsing
        essential: true, // Animation ignores user prefers-reduced-motion settings
      });
    }
  }

  /**
   * Updates tile URLs dynamically when S3 signed URLs refresh
   */
  updateHeatmapTiles(map: maplibregl.Map, heatmapUrls: Record<string, string>): void {
    Object.entries(heatmapUrls).forEach(([depth, url]) => {
      const sourceId = `soil-heatmap-${depth}`;
      const source = map.getSource(sourceId) as maplibregl.RasterTileSource;
      if (source && typeof source.setTiles === 'function') {
        source.setTiles([url]);
      }
    });
  }

  private enrichGeoJSONWithConfidence(data: SoilData): any {
    if (!data.soilCompositionGeoJSON?.features) return data.soilCompositionGeoJSON;

    // Ensure every geometry feature has access to the macro model confidence score
    return {
      ...data.soilCompositionGeoJSON,
      features: data.soilCompositionGeoJSON.features.map((feature: any) => ({
        ...feature,
        properties: {
          ...feature.properties,
          confidence: data.confidence, // Fallback macro confidence or layer specific
        },
      })),
    };
  }

  private addSoilComposition(map: maplibregl.Map, geoJSON: any) {
    if (map.getSource('soil-comp-src')) return;

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
          '#C0392B',
          'Silt',
          '#A0522D',
          'Sand',
          '#F4D03F',
          '#ccc',
        ],
        'fill-opacity': 0.6,
      },
    });
  }

  private addSoilHeatmaps(map: maplibregl.Map, heatmapUrls: Record<string, string>) {
    Object.entries(heatmapUrls).forEach(([depth, url]) => {
      const sourceId = `soil-heatmap-${depth}`;
      const layerId = `soil-heatmap-${depth}`;

      if (!map.getSource(sourceId)) {
        map.addSource(sourceId, {
          type: 'raster',
          tiles: [url],
          tileSize: 256,
          minzoom: 12,
          maxzoom: 22,
        });

        map.addLayer({
          id: layerId,
          type: 'raster',
          source: sourceId,
          paint: { 'raster-opacity': 0.7 },
          layout: { visibility: 'none' },
        });
      }
    });
  }

  private setupMapInteractions(map: maplibregl.Map) {
    const interactiveLayer = 'soil-composition';

    // 1. Click Interaction & Popup Generation (User Story 22)
    map.on('click', interactiveLayer, (e) => {
      if (!e.features || e.features.length === 0) return;

      // Clear preexisting active popups
      if (this._currentPopup) {
        this._currentPopup.remove();
      }

      const feature = e.features[0];
      const type = feature.properties['type'] || 'Unknown';
      const confidence = feature.properties['confidence']
        ? `${(feature.properties['confidence'] * 100).toFixed(0)}%`
        : 'N/A';

      const popupHtml = `
        <div style="padding: 4px; font-family: sans-serif;">
          <h4 style="margin: 0 0 4px 0; font-size: 13px; color: #1e293b;">Soil Analysis</h4>
          <p style="margin: 2px 0; font-size: 11px; color: #475569;">
            <strong>Type:</strong> ${type}
          </p>
          <p style="margin: 2px 0; font-size: 11px; color: #475569;">
            <strong>AI Confidence:</strong> ${confidence}
          </p>
        </div>
      `;

      this._currentPopup = new maplibregl.Popup({ closeButton: true, className: 'soil-map-popup' })
        .setLngLat(e.lngLat)
        .setHTML(popupHtml)
        .addTo(map);
    });

    // 2. Visual indicators showing layer is actionable
    map.on('mouseenter', interactiveLayer, () => {
      map.getCanvas().style.cursor = 'pointer';
    });

    map.on('mouseleave', interactiveLayer, () => {
      map.getCanvas().style.cursor = '';
    });
  }
}
