import { Component, signal, inject, computed, OnDestroy, OnInit, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MapComponent } from '../../../../shared/components/map/map.component';
import { MapLayerService } from '../../services/map-layer.service';
import { BoreholeTabComponent } from '../../components/analysis-detail/borehole/borehole-tab/borehole-tab.component';
import { SoilTabComponent } from '../../components/analysis-detail/soil/soil-tab/soil-tab.component';
import { TopographyTabComponent } from '../../components/analysis-detail/topography-tab/topography-tab.component';
import { MapLayerItem } from '../../interfaces/map-layer-item';
import { TopographyMapInitialiser } from '../../services/map-initialiser/topography-map-initialiser.service';
import { SoilMapInitialiser } from '../../services/map-initialiser/soil-map-initialiser.service';
import { RiskMapInitialiser } from '../../services/map-initialiser/risk-map-initialiser.service';
import { BoreholeMapInitialiser } from '../../services/map-initialiser/borehole-map-initialiser.service';
import { AnalysisDetailFacadeService } from '../../services/analysis-detail-facade.service';
import { ModuleProgressComponent } from '../../components/analysis-detail/module-progress/module-progress.component';
import { ModuleStatus } from '../../interfaces/module-status';
import { BearingMapInitialiser } from '../../services/map-initialiser/bearing-map-initialiser.service';
import { BearingTabComponent } from '../../components/analysis-detail/bearing-tab/bearing-tab.component';
import { RiskTabComponent } from '../../components/analysis-detail/risk/risk-tab/risk-tab.component';
import { ParcelFacadeService } from '../../../parcels/services/parcel-facade.service';
import maplibregl from 'maplibre-gl';
import { ReportFacadeService } from '../../services/report/report-facade.service';
import { SignalRService } from '../../../../core/services/signalr.service';

@Component({
  selector: 'app-analysis-detail',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    MapComponent,
    TopographyTabComponent,
    SoilTabComponent,
    BearingTabComponent,
    RiskTabComponent,
    BoreholeTabComponent,
    ModuleProgressComponent,
  ],
  templateUrl: './analysis-detail.component.html',
  styleUrls: ['./analysis-detail.component.css'],
})
export class AnalysisDetailComponent implements OnInit, OnDestroy {
  private _layerService = inject(MapLayerService);
  _analysisFacade = inject(AnalysisDetailFacadeService);
  private _parcelFacade = inject(ParcelFacadeService);
  reportFacade = inject(ReportFacadeService);
  private signalR = inject(SignalRService);
  private route = inject(ActivatedRoute);

  private _topographyInit = inject(TopographyMapInitialiser);
  private _soilInit = inject(SoilMapInitialiser);
  private _riskInit = inject(RiskMapInitialiser);
  private _boreholeInit = inject(BoreholeMapInitialiser);
  private _bearingInit = inject(BearingMapInitialiser);
  private router = inject(Router);

  map = signal<maplibregl.Map | undefined>(undefined);
  activeModule = signal('topography');

  parcelName = signal('');
  parcelCoordinates = signal('');
  mapCenter = signal<[number, number]>([31.942, 30.633]);

  private pendingFlyTo: { center: [number, number]; zoom: number } | null = null;

  parcelId!: string;

  private parcelBoundary: [number, number][] | null = null;

  readonly today = new Date();
  modules = [
    { id: 'topography', label: 'Topography', status: 'ready' },
    { id: 'soil', label: 'Soil Composition', status: 'ready' },
    { id: 'bearing', label: 'Bearing Capacity', status: 'ready' },
    { id: 'risk', label: 'Construction Risk', status: 'ready' },
    { id: 'borehole', label: 'Drilling Plan', status: 'ready' },
  ];

  // ── Progress from facade ──
  moduleProgress = this._analysisFacade.moduleProgress;

  // ── Derived status (for dot colors) ──
  moduleStatus = computed(() => {
    const progress = this.moduleProgress();
    return {
      topography: progress['topography']?.status ?? 'pending',
      soil: progress['soil']?.status ?? 'pending',
      bearing: progress['bearing']?.status ?? 'pending',
      risk: progress['risk']?.status ?? 'pending',
      borehole: progress['borehole']?.status ?? 'pending',
    } as Record<string, string>;
  });

  isActiveModuleCompleted = computed(
    () => this.moduleProgress()[this.activeModule()]?.status === 'Completed',
  );

  allModulesCompleted = computed(() => {
    const progress = this.moduleProgress();
    return Object.values(progress).every((m) => m.status === 'Completed');
  });

  constructor() {
    // ── Reactive layer adding ──
    // When data arrives for a completed module, add its map layers immediately.
    effect(() => {
      const map = this.map();
      const data = this._analysisFacade.topographyData();
      const progress = this.moduleProgress()['topography']?.status;
      if (map && data && progress === 'Completed') {
        this._topographyInit.addLayers(map, data);
        this._layerService.refreshVisibility();
      }
    });

    effect(() => {
      const map = this.map();
      const data = this._analysisFacade.soilData();
      const progress = this.moduleProgress()['soil']?.status;
      if (map && data && progress === 'Completed') {
        this._soilInit.addLayers(map, data);
        this._layerService.refreshVisibility();
      }
    });

    effect(() => {
      const map = this.map();
      const data = this._analysisFacade.bearingData();
      const progress = this.moduleProgress()['bearing']?.status;
      if (map && data && progress === 'Completed') {
        this._bearingInit.addLayers(map, data);
        this._layerService.refreshVisibility();
      }
    });

    effect(() => {
      const map = this.map();
      const data = this._analysisFacade.riskData();
      const progress = this.moduleProgress()['risk']?.status;
      if (map && data && progress === 'Completed') {
        this._riskInit.addLayers(map, data);
        this._layerService.refreshVisibility();
      }
    });

    effect(() => {
      const map = this.map();
      const data = this._analysisFacade.boreholeData();
      const progress = this.moduleProgress()['borehole']?.status;
      if (map && data && progress === 'Completed') {
        this._boreholeInit.addLayers(map, data);
        this._layerService.refreshVisibility();
      }
    });

    effect(() => {
      if (this.allModulesCompleted()) {
        this.reportFacade.checkExistingReport(this.parcelId);
      }
    });
  }

  ngOnInit(): void {
    const analysisId = this.route.snapshot.paramMap.get('analysisId');
    const parcelId = this.route.snapshot.paramMap.get('parcelId'); // from path

    if (parcelId) {
      this.parcelId = parcelId;

      // Start analysis real‑time
      this._analysisFacade.startRealtimeProgress(this.parcelId);

      // Load parcel metadata
      this._parcelFacade.getParcelById(this.parcelId).subscribe((parcel) => {
        if (parcel) {
          this.parcelName.set(parcel.name);
          this.parcelCoordinates.set(
            `${parcel.centroidLatitude.toFixed(3)}°N, ${parcel.centroidLongitude.toFixed(3)}°E`,
          );
          this.mapCenter.set([parcel.centroidLongitude, parcel.centroidLatitude]);
          this._addParcelBoundary(parcel.boundaryCoordinates);

          // Fly the map to the parcel’s centroid if map is ready, otherwise defer
          const target: [number, number] = [parcel.centroidLongitude, parcel.centroidLatitude];
          if (this.map()) {
            this.map()!.flyTo({ center: target, zoom: 17, essential: true });
          } else {
            this.pendingFlyTo = { center: target, zoom: 17 };
          }
        }
      });

      this.signalR.reportGenerated$.subscribe((event: any) => {
        if (event.ParcelId === this.parcelId) {
          this.reportFacade.onReportGenerated(this.parcelId, event.ReportJobId);
        }
      });

      this.signalR.reportFailed$.subscribe((event: any) => {
        if (event.ParcelId === this.parcelId) {
          this.reportFacade.onReportFailed(event.Message);
        }
      });
    } else {
      console.warn('No parcelId in URL - redirecting to list');
      this.router.navigate(['/app/analyses']);
    }
  }

  ngOnDestroy(): void {
    this._analysisFacade.stopRealtimeProgress();
  }

  private _addParcelBoundary(coords: { longitude: number; latitude: number }[]) {
    this.parcelBoundary = coords.map((p) => [p.longitude, p.latitude] as [number, number]);
    // If map already exists, draw now
    if (this.map()) {
      this._drawBoundary();
    }
  }

  private _drawBoundary() {
    const map = this.map()!;
    if (!this.parcelBoundary) return;
    if (map.getSource('parcel-outline')) {
      (map.getSource('parcel-outline') as any).setData({
        type: 'Feature',
        properties: {},
        geometry: { type: 'LineString', coordinates: this.parcelBoundary },
      });
    } else {
      map.addSource('parcel-outline', {
        type: 'geojson',
        data: {
          type: 'Feature',
          properties: {},
          geometry: { type: 'LineString', coordinates: this.parcelBoundary },
        },
      });
      map.addLayer({
        id: 'parcel-outline-layer',
        type: 'line',
        source: 'parcel-outline',
        paint: { 'line-color': '#B86E3D', 'line-width': 2, 'line-dasharray': [4, 2] },
      });
    }
  }

  activeModuleIndex() {
    return this.modules.findIndex((m) => m.id === this.activeModule());
  }
  activeModuleLabel() {
    return this.modules.find((m) => m.id === this.activeModule())?.label;
  }
  activeModuleDescription() {
    const descriptions: Record<string, string> = {
      topography: 'AI-generated elevation models, slope analysis, and ponding risk assessment.',
      soil: 'Soil composition classification, bearing capacity estimation, and water table mapping.',
      risk: 'Multi-hazard evaluation: flood, seismic, liquefaction, and landslide risks.',
      borehole: 'Optimized drilling plan with depth and spacing recommendations.',
    };
    return descriptions[this.activeModule()] || '';
  }

  onMapReady(map: maplibregl.Map) {
    const init = () => {
      // The layer metadata is registered immediately; actual data layers will be added by the effects above.
      const allLayers: MapLayerItem[] = [
        // topography
        {
          id: 'elevation-heat',
          label: 'Elevation Heatmap',
          visible: true,
          opacity: 0.75,
          group: 'topography',
          setOpacity: (m, o) => m.setPaintProperty('elevation-heat', 'heatmap-opacity', o),
        },
        {
          id: 'contour-lines',
          label: 'Contour Lines',
          visible: true,
          opacity: 0.85,
          group: 'topography',
          setOpacity: (m, o) => m.setPaintProperty('contour-lines', 'line-opacity', o),
        },
        {
          id: 'slope-fill',
          label: 'Slope Categories',
          visible: true,
          opacity: 0.45,
          group: 'topography',
          setOpacity: (m, o) => m.setPaintProperty('slope-fill', 'fill-opacity', o),
        },
        {
          id: 'ponding-zones',
          label: 'Ponding Risk',
          visible: true,
          opacity: 0.65,
          group: 'topography',
          linkedLayers: ['ponding-zones-outline'],
          setOpacity: (m, o) => {
            m.setPaintProperty('ponding-zones', 'fill-opacity', o * 0.75);
            if (m.getLayer('ponding-zones-outline'))
              m.setPaintProperty('ponding-zones-outline', 'line-opacity', o);
          },
        },
        // soil
        {
          id: 'soil-composition',
          label: 'Soil Composition',
          visible: true,
          opacity: 0.6,
          group: 'soil',
          setOpacity: (m, o) => m.setPaintProperty('soil-composition', 'fill-opacity', o),
        },
        // soil heatmap layers will be added later once soilData is available
        // they are already registered dynamically (the keys generation depends on data)
        // For now we just register the static ones; the dynamic ones can be registered later if needed.
        // bearing
        {
          id: 'bearing-points',
          label: 'Bearing Capacity',
          visible: true,
          opacity: 1.0,
          group: 'bearing',
          setOpacity: (m, o) => m.setPaintProperty('bearing-points', 'circle-opacity', o),
        },
        {
          id: 'water-table',
          label: 'Water Table Depth',
          visible: false,
          opacity: 0.5,
          group: 'bearing',
          setOpacity: (m, o) => m.setPaintProperty('water-table', 'line-opacity', o),
        },
        // risk
        {
          id: 'flood-zone',
          label: 'Flood Zones',
          visible: true,
          opacity: 0.5,
          group: 'risk',
          setOpacity: (m, o) => m.setPaintProperty('flood-zone', 'fill-opacity', o),
        },
        {
          id: 'seismic-zone',
          label: 'Seismic Zones',
          visible: true,
          opacity: 0.4,
          group: 'risk',
          setOpacity: (m, o) => m.setPaintProperty('seismic-zone', 'circle-opacity', o),
        },
        {
          id: 'expansive-soil',
          label: 'Expansive Soil Zones',
          visible: true,
          opacity: 0.5,
          group: 'risk',
          setOpacity: (m, o) => m.setPaintProperty('expansive-soil', 'fill-opacity', o),
        },
        {
          id: 'liquefaction',
          label: 'Liquefaction Zones',
          visible: true,
          opacity: 0.6,
          group: 'risk',
          setOpacity: (m, o) => m.setPaintProperty('liquefaction', 'fill-opacity', o),
        },
        // borehole
        {
          id: 'borehole-points',
          label: 'Borehole Points',
          visible: true,
          opacity: 1.0,
          group: 'borehole',
          setOpacity: (m, o) => m.setPaintProperty('borehole-points', 'circle-opacity', o),
        },
      ];

      allLayers.forEach((layer) => this._layerService.registerLayers([layer]));

      this._layerService.init(map);
      this._layerService.refreshVisibility();
      this.map.set(map);
      if (this.parcelBoundary) {
        this._drawBoundary();
      }

      if (this.pendingFlyTo) {
        map.flyTo({
          center: this.pendingFlyTo.center,
          zoom: this.pendingFlyTo.zoom,
          essential: true,
        });
        this.pendingFlyTo = null;
      }
    };

    map.loaded() ? init() : map.once('load', init);
  }

  setActiveModule(id: string) {
    this.activeModule.set(id);
    this._layerService.setActiveGroup(id);
  }

  cancelModule(moduleId: string) {
    this._analysisFacade.moduleProgress.update((prev) => ({
      ...prev,
      [moduleId]: { ...prev[moduleId], status: 'Failed' as ModuleStatus },
    }));
  }

  onGenerateReport() {
    const options = {
      language: 'en',
      companyName: 'Talaat Moustafa Group',
      projectName: 'New Alamein Phase 3',
      includeMaps: true,
      includeTables: true,
      includeRiskMatrix: true,
      includeBoreholePlan: true,
      disclaimerLevel: 'full',
    };
    this.reportFacade.generateReport(this.parcelId, options);
  }
}
