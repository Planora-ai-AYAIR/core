import {
  Component,
  Input,
  Output,
  EventEmitter,
  AfterViewInit,
  OnDestroy,
  ViewChild,
  ElementRef,
} from '@angular/core';
import maplibregl from 'maplibre-gl';
import MapLibreGlDraw from 'maplibre-gl-draw';
import MapLibreGlGeocoder from '@maplibre/maplibre-gl-geocoder';
import distance from '@turf/distance';

@Component({
  selector: 'app-map',
  standalone: true,
  templateUrl: './map.component.html',
  styleUrls: ['./map.component.css'],
  host: {
    style: 'display: block; width: 100%; height: 100%; position: relative;',
  },
})
export class MapComponent implements AfterViewInit, OnDestroy {
  @ViewChild('mapContainer') mapContainer!: ElementRef<HTMLDivElement>;

  // ── Map configuration inputs ──
  @Input() center: [number, number] = [31.2357, 30.0444];
  @Input() zoom = 12;
  @Input() pitch = 0;
  @Input() showSatelliteToggle = false;
  @Input() style?: maplibregl.StyleSpecification | string;

  // ── Drawing inputs ──
  @Input() enableDrawing = false;
  @Input() drawControls: { polygon?: boolean; trash?: boolean } = { polygon: true, trash: true };

  // ── Geocoder input ──
  @Input() enableGeocoder = false;

  // ── Points / labels inputs ──
  @Input() points: [number, number][] = [];
  @Input() showLabels = false;
  @Input() set highlightedIndex(value: number | null) {
    this._highlightedIndex = value;
    this.updateHighlightLayer();
  }

  // ── Outputs ──
  @Output() mapReady = new EventEmitter<maplibregl.Map>();
  @Output() satelliteToggled = new EventEmitter<boolean>();
  @Output() pointsChange = new EventEmitter<[number, number][]>();
  @Output() pointClicked = new EventEmitter<number>();

  map?: maplibregl.Map;
  isSatelliteView = false;

  private resizeObserver?: ResizeObserver;
  private draw?: MapLibreGlDraw;
  private geocoder?: any;
  private labelMarkers: maplibregl.Marker[] = [];
  private _highlightedIndex: number | null = null;

  private defaultStyle: maplibregl.StyleSpecification = {
    version: 8,
    sources: {
      osm: {
        type: 'raster',
        tiles: ['https://a.tile.openstreetmap.org/{z}/{x}/{y}.png'],
        tileSize: 256,
        attribution: '© OpenStreetMap',
        maxzoom: 19,
      },
      satellite: {
        type: 'raster',
        tiles: ['https://ecn.t3.tiles.virtualearth.net/tiles/a{quadkey}.jpeg?g=1'],
        tileSize: 256,
        attribution: '© Microsoft Corporation',
        maxzoom: 19,
      },
      globalTerrain: {
        type: 'raster-dem',
        tiles: ['https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{z}/{x}/{y}.png'],
        encoding: 'terrarium',
        tileSize: 256,
        maxzoom: 14,
      },
      globalHillshade: {
        type: 'raster-dem',
        tiles: ['https://s3.amazonaws.com/elevation-tiles-prod/terrarium/{z}/{x}/{y}.png'],
        encoding: 'terrarium',
        tileSize: 256,
        maxzoom: 14,
      },
    },
    layers: [
      { id: 'osm', type: 'raster', source: 'osm' },
      { id: 'satellite', type: 'raster', source: 'satellite', layout: { visibility: 'none' } },

      // Use the AWS data to paint the mountain shadows
      {
        id: 'hills',
        type: 'hillshade',
        source: 'globalHillshade', // <--- Changed from globalTerrain
        layout: { visibility: 'visible' },
        paint: {
          'hillshade-shadow-color': '#473B24',
          'hillshade-exaggeration': 0.5,
        },
      },
    ],
    sky: {},
  };

  ngAfterViewInit() {
    setTimeout(() => this.initMap(), 100);
  }

  ngOnDestroy() {
    this.resizeObserver?.disconnect();
    this.map?.remove();
  }

  get highlightedIndex(): number | null {
    return this._highlightedIndex;
  }

  // ── Map initialisation ──
  private initMap() {
    const container = this.mapContainer.nativeElement;
    const style = this.style ?? this.defaultStyle;

    this.map = new maplibregl.Map({
      container,
      style,
      center: this.center,
      zoom: this.zoom,
      pitch: 70,
      maxPitch: 85,
      attributionControl: false,
      trackResize: true,
    });

    this.resizeObserver = new ResizeObserver(() => this.map?.resize());
    this.resizeObserver.observe(container);

    // Navigation control
    this.map.addControl(
      new maplibregl.NavigationControl({ visualizePitch: true, showZoom: true, showCompass: true }),
      'bottom-right',
    );

    // Terrain control
    const terrainControl = new maplibregl.TerrainControl({
      source: 'globalTerrain',
      exaggeration: 1.2,
    });
    (terrainControl as any).maplibregl = maplibregl;
    this.map.addControl(terrainControl, 'bottom-right');

    // Scale control
    this.map.addControl(
      new maplibregl.ScaleControl({ maxWidth: 100, unit: 'metric' }),
      'bottom-left',
    );

    this.map.on('load', () => {
      this.map?.resize();

      // Highlight source & layer (always added, hidden when not used)
      this.map!.addSource('point-highlight', {
        type: 'geojson',
        data: { type: 'FeatureCollection', features: [] },
      });
      this.map!.addLayer({
        id: 'point-highlight-layer',
        type: 'circle',
        source: 'point-highlight',
        paint: {
          'circle-radius': 8,
          'circle-color': '#facc15',
          'circle-stroke-width': 3,
          'circle-stroke-color': '#ffffff',
        },
      });

      // Initialise drawing & geocoder if enabled
      if (this.enableDrawing) this.setupDraw();
      if (this.enableGeocoder) this.setupGeocoder();

      this.updateHighlightLayer();
      this.updateMapLabels();
      this.mapReady.emit(this.map!);
    });
  }

  // ── Drawing ──
  private setupDraw() {
    this.draw = new MapLibreGlDraw({
      displayControlsDefault: false,
      controls: {
        polygon: this.drawControls.polygon ?? true,
        trash: this.drawControls.trash ?? true,
      },
      defaultMode: 'draw_polygon',
    });
    this.map!.addControl(this.draw as any, 'top-right');

    this.map!.on('draw.create', (e) => this.handleDrawEvent(e));
    this.map!.on('draw.update', (e) => this.handleDrawEvent(e));
    this.map!.on('draw.delete', (e) => this.handleDrawEvent(e));

    this.updateTrashButtonState();
  }

  private handleDrawEvent(e: any) {
    const allFeatures = this.draw?.getAll();
    if (!allFeatures) return;

    // Prevent multiple polygons
    if (e.type === 'draw.create' && allFeatures.features.length > 1) {
      const newId = e.features[0].id;
      allFeatures.features.forEach((f) => {
        if (f.id !== newId) this.draw?.delete(f.id as string);
      });
    }

    const data = this.draw?.getAll();
    if (data && data.features.length > 0) {
      const feature = data.features[0];
      let coords: [number, number][] = [];

      if (feature.geometry.type === 'Polygon') {
        coords = (feature.geometry.coordinates[0] as any[]).slice(0, -1);
      } else if (feature.geometry.type === 'LineString' || feature.geometry.type === 'Point') {
        coords = feature.geometry.coordinates as any;
      }

      this.points = coords.length >= 3 ? coords : [];
    } else {
      this.points = [];
    }
    this.pointsChange.emit([...this.points]);
    this.updateMapLabels();
    this.updateHighlightLayer();
    this.updateTrashButtonState();
  }

  clearDrawing() {
    this.draw?.deleteAll();
    this.points = [];
    this.pointsChange.emit([]);
    this.labelMarkers.forEach((m) => m.remove());
    this.labelMarkers = [];
    this.draw?.changeMode('draw_polygon');
    this.updateTrashButtonState();

    const source = this.map?.getSource('point-highlight') as maplibregl.GeoJSONSource;
    source?.setData({ type: 'FeatureCollection', features: [] });
  }

  addManualPoint() {
    const center = this.map?.getCenter() || { lng: 31.2357, lat: 30.0444 };
    const newPoint: [number, number] = [
      Number(center.lng.toFixed(6)),
      Number(center.lat.toFixed(6)),
    ];
    this.setPoints([...this.points, newPoint]);
  }

  setPoints(points: [number, number][]) {
    if (!this.draw) return;
    this.draw.deleteAll();
    this.points = [...points];
    this.pointsChange.emit([...points]);

    if (points.length === 0) return;

    let feature: any;
    if (points.length === 1) {
      feature = {
        type: 'Feature',
        geometry: { type: 'Point', coordinates: points[0] },
        properties: {},
      };
    } else if (points.length === 2) {
      feature = {
        type: 'Feature',
        geometry: { type: 'LineString', coordinates: points },
        properties: {},
      };
    } else {
      feature = {
        type: 'Feature',
        geometry: { type: 'Polygon', coordinates: [[...points, points[0]]] },
        properties: {},
      };
    }

    const ids = this.draw.add(feature);
    if (ids.length > 0) {
      this.draw.changeMode('simple_select', { featureIds: ids });
    }
    this.updateMapLabels();
    this.updateHighlightLayer();
    this.updateTrashButtonState();
  }

  // ── Geocoder ──
  private setupGeocoder() {
    const geocoderApi = {
      forwardGeocode: async (config: any) => {
        try {
          const query = config.query;
          const url = `https://photon.komoot.io/api/?q=${encodeURIComponent(query)}&limit=5`;
          const response = await fetch(url);
          if (!response.ok) throw new Error('Network response was not ok');
          const data = await response.json();
          const mappedFeatures = (data.features || []).map((f: any) => {
            const props = f.properties;
            const placeName = [props.name, props.city, props.state, props.country]
              .filter(Boolean)
              .join(', ');
            return {
              ...f,
              place_name: placeName,
              text: props.name,
              center: f.geometry.coordinates,
            };
          });
          return { type: 'FeatureCollection', features: mappedFeatures } as any;
        } catch (error) {
          console.error('Geocoding error:', error);
          return { type: 'FeatureCollection', features: [] } as any;
        }
      },
    };

    this.geocoder = new MapLibreGlGeocoder(geocoderApi, {
      maplibregl: maplibregl as any,
      marker: true,
      showResultsWhileTyping: true,
      minLength: 3,
    });
    this.map!.addControl(this.geocoder, 'top-left');
    this.geocoder.on('result', (e: any) => {
      const coords = e.result.geometry.coordinates;
      this.map?.flyTo({ center: coords, zoom: 16, essential: true });
    });
  }

  // ── Labels (point numbers + edge distances) ──
  private updateMapLabels() {
    if (!this.map || !this.showLabels) {
      this.labelMarkers.forEach((m) => m.remove());
      this.labelMarkers = [];
      return;
    }
    this.labelMarkers.forEach((m) => m.remove());
    this.labelMarkers = [];

    const pts = this.points;
    if (pts.length === 0) return;

    pts.forEach((coord, i) => {
      const el = document.createElement('div');
      el.className = 'parcel-point-marker';
      el.textContent = `P${i + 1}`;
      const marker = new maplibregl.Marker({ element: el, anchor: 'bottom' })
        .setLngLat(coord)
        .addTo(this.map!);
      this.labelMarkers.push(marker);
    });

    for (let i = 0; i < pts.length; i++) {
      const next = pts[(i + 1) % pts.length];
      const midLng = (pts[i][0] + next[0]) / 2;
      const midLat = (pts[i][1] + next[1]) / 2;
      const dist = distance(pts[i], next, { units: 'meters' });
      const el = document.createElement('div');
      el.className = 'edge-distance-marker';
      el.textContent = `${dist.toFixed(1)} m`;
      const marker = new maplibregl.Marker({ element: el, anchor: 'center' })
        .setLngLat([midLng, midLat])
        .addTo(this.map!);
      this.labelMarkers.push(marker);
    }
  }

  private updateHighlightLayer() {
    if (!this.map) return;
    const source = this.map.getSource('point-highlight') as maplibregl.GeoJSONSource;
    if (!source) return;

    const idx = this._highlightedIndex;
    const pts = this.points;
    if (idx !== null && pts[idx]) {
      source.setData({
        type: 'Feature',
        properties: {},
        geometry: { type: 'Point', coordinates: pts[idx] },
      });
    } else {
      source.setData({ type: 'FeatureCollection', features: [] });
    }
  }

  private updateTrashButtonState() {
    if (!this.map || !this.draw) return;
    // Try both class names (underscore for maplibre, same as mapbox)
    const trashBtn =
      document.querySelector('.maplibregl-draw_trash') ??
      document.querySelector('.mapbox-gl-draw_trash');

    if (!trashBtn) return;

    const n = this.points.length;
    if (n <= 3 && n > 0) {
      trashBtn.classList.add('opacity-20', 'pointer-events-none');
      (trashBtn as HTMLButtonElement).disabled = true;
    } else {
      trashBtn.classList.remove('opacity-20', 'pointer-events-none');
      (trashBtn as HTMLButtonElement).disabled = false;
    }
  }

  // ── Satellite toggle ──
  toggleSatellite() {
    if (!this.map) return;
    const visibility = this.isSatelliteView ? 'none' : 'visible';
    this.map.setLayoutProperty('satellite', 'visibility', visibility);
    this.isSatelliteView = !this.isSatelliteView;
    this.satelliteToggled.emit(this.isSatelliteView);
  }

  // ── Locate me ──
  locateMe() {
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (pos) => this.map?.flyTo({ center: [pos.coords.longitude, pos.coords.latitude], zoom: 16 }),
        (err) => console.error('Geolocation denied', err),
      );
    }
  }

  // ── Fly to point (called from parent) ──
  flyToPoint(index: number) {
    const point = this.points[index];
    if (point && this.map) {
      this.map.flyTo({ center: point, zoom: 21, essential: true, speed: 1.5 });
    }
  }
}
