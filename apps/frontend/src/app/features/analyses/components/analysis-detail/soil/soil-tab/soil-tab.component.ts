import { Component, inject, computed, signal, effect, untracked } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MapLayerService } from '../../../../services/map-layer.service';
import { LayerControlComponent } from '../../layer-control/layer-control.component';
import { AnalysisDetailFacadeService } from '../../../../services/analysis-detail-facade.service';
import { SoilPieChartComponent } from '../soil-pie-chart/soil-pie-chart.component';
import { SoilClassificationBadgeComponent } from '../soil-classification-badge/soil-classification-badge.component';
import { SoilPropertiesTableComponent } from '../soil-properties-table/soil-properties-table.component';
import { DepthSelectorComponent } from '../depth-selector/depth-selector.component';
import { SoilProfileComponent } from '../soil-profile/soil-profile.component';

@Component({
  selector: 'app-soil-tab',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LayerControlComponent,
    SoilPieChartComponent,
    SoilClassificationBadgeComponent,
    SoilPropertiesTableComponent,
    DepthSelectorComponent,
    SoilProfileComponent,
  ],
  templateUrl: './soil-tab.component.html',
  styleUrls: ['./soil-tab.component.css'],
})
export class SoilTabComponent {
  private _layerService = inject(MapLayerService);
  private _facade = inject(AnalysisDetailFacadeService);

  // Layers actually exposed to the user in the Map Overlays list.
  // Per-depth heatmap rasters are registered as `hidden` and are driven by
  // the depth selector together with the single 'soil-heatmap' master toggle.
  mapLayers = computed(() =>
    this._layerService
      .layersByGroup('soil')()
      .filter((l) => !l.hidden),
  );

  soilComposition = computed(() => this._facade.soilData()?.composition ?? []);

  selectedDepth = signal('0-20cm');
  profileViewMode = signal<'stacked' | 'side-by-side'>('stacked');

  constructor() {
    effect(() => {
      // These establish what SHOULD trigger the effect execution
      const isMapReady = this._layerService.isMapReady();
      const hasData = !!this._facade.soilData();
      const currentDepth = this.selectedDepth();

      if (isMapReady && hasData) {
        untracked(() => {
          this.syncMapLayersToState(currentDepth);
        });
      }
    });
  }

  /**
   * Shows the heatmap raster matching the active depth tab, but only when
   * the user has the master 'soil-heatmap' toggle switched on.
   */
  private syncMapLayersToState(activeDepth: string) {
    const soilData = this._facade.soilData();
    if (!soilData) return;

    const masterToggle = this.mapLayers().find((l) => l.id === 'soil-heatmap');
    const isMasterVisible = masterToggle ? masterToggle.visible : true;

    Object.keys(soilData.heatmapUrls).forEach((depth) => {
      const layerId = `soil-heatmap-${depth}`;
      // A sub-layer must only show if it matches the selected depth AND the master toggle is on
      const shouldBeVisible = isMasterVisible && depth === activeDepth;
      this._layerService.setLayerVisible(layerId, shouldBeVisible);
    });
  }

  onToggleLayer(layerId: string) {
    this._layerService.toggleLayerVisibility(layerId);
    // Explicit re-sync immediately after user handles master visibility toggle
    this.syncMapLayersToState(this.selectedDepth());
  }

  onLayerOpacityChange(layerId: string, opacity: number) {
    this._layerService.setLayerOpacity(layerId, opacity);

    // The master heatmap slider applies down to all depth rasters
    const soilData = this._facade.soilData();
    if (soilData && layerId === 'soil-heatmap') {
      Object.keys(soilData.heatmapUrls).forEach((depth) => {
        this._layerService.setLayerOpacity(`soil-heatmap-${depth}`, opacity);
      });
    }
  }

  onDepthChange(depth: string) {
    this.selectedDepth.set(depth);
    this.syncMapLayersToState(depth);
  }

  onToggleViewMode(mode: 'stacked' | 'side-by-side') {
    this.profileViewMode.set(mode);
  }

  soilProperties = computed(() => {
    const d = this._facade.soilData();
    return d ? { bulkDensity: d.bulkDensity, organicCarbon: d.organicCarbon, pH: d.pH } : null;
  });

  classificationData = computed(() => {
    const d = this._facade.soilData();
    return d ? { classification: d.classification, confidence: d.confidence } : null;
  });

  depthProfiles = computed(() => this._facade.soilData()?.depthProfiles ?? []);
  heatmapLegend = computed(() => this._facade.soilData()?.heatmapLegend ?? []);

  onExportCSV() {
    const profiles = this.depthProfiles();
    if (!profiles.length) return;
    const header = 'Depth,Sand%,Silt%,Clay%,Classification';
    const rows = profiles.map(
      (p) =>
        `${p.depthRange},${p.sandPercent},${p.siltPercent},${p.clayPercent},${p.classification}`,
    );
    const csv = [header, ...rows].join('\n');
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.setAttribute('href', url);
    link.setAttribute('download', `soil_profile_${new Date().toISOString().split('T')[0]}.csv`);
    link.click();
    URL.revokeObjectURL(url);
  }
}
