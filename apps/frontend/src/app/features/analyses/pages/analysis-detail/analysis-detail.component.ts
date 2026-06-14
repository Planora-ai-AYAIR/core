import { Component, signal, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MapComponent } from '../../../../shared/components/map/map.component';
import { MapLayerService } from '../../services/map-layer.service';
import { BoreholeTabComponent } from '../../components/analysis-detail/borehole-tab/borehole-tab.component';
import { RiskTabComponent } from '../../components/analysis-detail/risk-tab/risk-tab.component';
import { SoilTabComponent } from '../../components/analysis-detail/soil-tab/soil-tab.component';
import { TopographyTabComponent } from '../../components/analysis-detail/topography-tab/topography-tab.component';
import { MapLayerItem } from '../../interfaces/map-layer-item';
import { TopographyMapInitialiser } from '../../services/topography-map-initialiser.service';
import { SoilMapInitialiser } from '../../services/soil-map-initialiser.service';
import { RiskMapInitialiser } from '../../services/risk-map-initialiser.service';
import { BoreholeMapInitialiser } from '../../services/borehole-map-initialiser.service';
import { AnalysisDetailFacadeService } from '../../services/analysis-detail-facade.service';
import { ModuleProgressComponent } from '../../components/analysis-detail/module-progress/module-progress.component';
import maplibregl from 'maplibre-gl';
import { ModuleStatus } from '../../interfaces/module-status';

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
    RiskTabComponent,
    BoreholeTabComponent,
    ModuleProgressComponent,
  ],
  templateUrl: './analysis-detail.component.html',
  styleUrls: ['./analysis-detail.component.css'],
})
export class AnalysisDetailComponent {
  private _layerService = inject(MapLayerService);
  private _analysisFacade = inject(AnalysisDetailFacadeService);
  private _topographyInit = inject(TopographyMapInitialiser);
  private _soilInit = inject(SoilMapInitialiser);
  private _riskInit = inject(RiskMapInitialiser);
  private _boreholeInit = inject(BoreholeMapInitialiser);

  map = signal<maplibregl.Map | undefined>(undefined);
  activeModule = signal('topography');

  readonly today = new Date();
  modules = [
    { id: 'topography', label: 'Topography', status: 'ready' },
    { id: 'soil', label: 'Soil & Bearing', status: 'ready' },
    { id: 'risk', label: 'Construction Risk', status: 'loading' },
    { id: 'borehole', label: 'Drilling Plan', status: 'pending' },
  ];

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

      this._analysisFacade.loadAllData();

      const topoData = this._analysisFacade.topographyData();
      const soilData = this._analysisFacade.soilData();
      const riskData = this._analysisFacade.riskData();
      const boreholeData = this._analysisFacade.boreholeData();

      const progress = this.moduleProgress();

      if (progress['topography']?.status === 'Completed' && topoData)
        this._topographyInit.addLayers(map, topoData);
      if (progress['soil']?.status === 'Completed' && soilData)
        this._soilInit.addLayers(map, soilData);
      if (progress['risk']?.status === 'Completed' && riskData)
        this._riskInit.addLayers(map, riskData);
      if (progress['borehole']?.status === 'Completed' && boreholeData)
        this._boreholeInit.addLayers(map, boreholeData);

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
        {
          id: 'bearing-points',
          label: 'Bearing Capacity',
          visible: true,
          opacity: 1.0,
          group: 'soil',
          setOpacity: (m, o) => m.setPaintProperty('bearing-points', 'circle-opacity', o),
        },
        {
          id: 'water-table',
          label: 'Water Table Depth',
          visible: false,
          opacity: 0.5,
          group: 'soil',
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
          label: 'Seismic Hazard',
          visible: true,
          opacity: 0.4,
          group: 'risk',
          setOpacity: (m, o) => m.setPaintProperty('seismic-zone', 'circle-opacity', o),
        },
        {
          id: 'liquefaction',
          label: 'Liquefaction Areas',
          visible: false,
          opacity: 0.6,
          group: 'risk',
          setOpacity: (m, o) => m.setPaintProperty('liquefaction', 'fill-opacity', o),
        },
        // borehole
        {
          id: 'borehole-points',
          label: 'Proposed Boreholes',
          visible: true,
          opacity: 1.0,
          group: 'borehole',
          setOpacity: (m, o) => m.setPaintProperty('borehole-points', 'circle-opacity', o),
        },
        {
          id: 'depth-rings',
          label: 'Drilling Depth',
          visible: true,
          opacity: 0.5,
          group: 'borehole',
          setOpacity: (m, o) => m.setPaintProperty('depth-rings', 'fill-opacity', o),
        },
        {
          id: 'optimal-area',
          label: 'Optimal Drilling Area',
          visible: false,
          opacity: 0.3,
          group: 'borehole',
          setOpacity: (m, o) => m.setPaintProperty('optimal-area', 'fill-opacity', o),
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

  moduleProgress = signal<Record<string, { status: ModuleStatus; estimatedSeconds: number }>>({
    topography: { status: 'Completed', estimatedSeconds: 0 },
    soil: { status: 'Processing', estimatedSeconds: 15 },
    risk: { status: 'Queued', estimatedSeconds: 40 },
    borehole: { status: 'Waiting', estimatedSeconds: 90 },
  });

  // Make the computed return a Record that accepts any string key
  moduleStatus = computed(() => {
    const progress = this.moduleProgress();
    return {
      topography: progress['topography']?.status ?? 'pending',
      soil: progress['soil']?.status ?? 'pending',
      risk: progress['risk']?.status ?? 'pending',
      borehole: progress['borehole']?.status ?? 'pending',
    } as Record<string, string>; // allow indexing with any string
  });

  // cancelModule also uses the type
  cancelModule(moduleId: string) {
    this.moduleProgress.update((prev) => ({
      ...prev,
      [moduleId]: { ...prev[moduleId], status: 'Failed' as ModuleStatus },
    }));
  }

  isActiveModuleCompleted = computed(
    () => this.moduleProgress()[this.activeModule()]?.status === 'Completed',
  );
}
