import { Component, signal, inject, computed, OnDestroy, OnInit, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
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
import maplibregl from 'maplibre-gl';

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
  private _analysisFacade = inject(AnalysisDetailFacadeService);
  private _topographyInit = inject(TopographyMapInitialiser);
  private _soilInit = inject(SoilMapInitialiser);
  private _riskInit = inject(RiskMapInitialiser);
  private _boreholeInit = inject(BoreholeMapInitialiser);
  private _bearingInit = inject(BearingMapInitialiser);

  map = signal<maplibregl.Map | undefined>(undefined);
  activeModule = signal('topography');

  private route = inject(ActivatedRoute);
  private parcelId!: string;

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

  constructor() {
    // ── Reactive layer adding ──
    // When data arrives for a completed module, add its map layers immediately.
    effect(() => {
      const map = this.map();
      const data = this._analysisFacade.topographyData();
      const progress = this.moduleProgress()['topography']?.status;
      if (map && data && progress === 'Completed') {
        this._topographyInit.addLayers(map, data);
      }
    });

    effect(() => {
      const map = this.map();
      const data = this._analysisFacade.soilData();
      const progress = this.moduleProgress()['soil']?.status;
      if (map && data && progress === 'Completed') {
        this._soilInit.addLayers(map, data);
      }
    });

    effect(() => {
      const map = this.map();
      const data = this._analysisFacade.bearingData();
      const progress = this.moduleProgress()['bearing']?.status;
      if (map && data && progress === 'Completed') {
        this._bearingInit.addLayers(map, data);
      }
    });

    effect(() => {
      const map = this.map();
      const data = this._analysisFacade.riskData();
      const progress = this.moduleProgress()['risk']?.status;
      if (map && data && progress === 'Completed') {
        this._riskInit.addLayers(map, data);
      }
    });

    effect(() => {
      const map = this.map();
      const data = this._analysisFacade.boreholeData();
      const progress = this.moduleProgress()['borehole']?.status;
      if (map && data && progress === 'Completed') {
        this._boreholeInit.addLayers(map, data);
      }
    });
  }

  ngOnInit(): void {
    this.parcelId = this.route.snapshot.paramMap.get('parcelId')!;
    if (this.parcelId) {
      this._analysisFacade.startRealtimeProgress(this.parcelId);
    }
  }

  ngOnDestroy(): void {
    this._analysisFacade.stopRealtimeProgress();
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
    const coords: [number, number][] = [
      [31.941933631896973, 30.633079403593342],
      [31.942059695720673, 30.633150948509027],
      [31.942183077335358, 30.632954776839696],
      [31.942048966884613, 30.63287861596676],
      [31.941933631896973, 30.633079403593342],
    ];

    const init = () => {
      if (!map.getSource('parcel-outline')) {
        map.addSource('parcel-outline', {
          type: 'geojson',
          data: {
            type: 'Feature',
            properties: {},
            geometry: { type: 'LineString', coordinates: coords },
          },
        });
        map.addLayer({
          id: 'parcel-outline-layer',
          type: 'line',
          source: 'parcel-outline',
          paint: { 'line-color': '#B86E3D', 'line-width': 2, 'line-dasharray': [4, 2] },
        });
      }

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
}
