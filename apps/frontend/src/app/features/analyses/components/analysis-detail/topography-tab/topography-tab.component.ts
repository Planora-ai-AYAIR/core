import { Component, computed, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MapLayerService } from '../../../services/map-layer.service';
import { LayerControlComponent } from '../layer-control/layer-control.component';
import { AnalysisDetailFacadeService } from '../../../services/analysis-detail-facade.service';

@Component({
  selector: 'app-topography-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, LayerControlComponent],
  templateUrl: './topography-tab.component.html',
  styleUrls: ['./topography-tab.component.css'],
})
export class TopographyTabComponent {
  private _layerService = inject(MapLayerService);
  private _facade = inject(AnalysisDetailFacadeService);

  mapLayers = this._layerService.layersByGroup('topography');

  topoStats = computed(() => {
    const d = this._facade.topographyData();
    return d
      ? {
          minElevation: d.minElevation,
          maxElevation: d.maxElevation,
          meanElevation: d.meanElevation,
          cutFill: d.cutFill,
        }
      : null;
  });

  slopeData = computed(() => this._facade.topographyData()?.slopeDistribution ?? []);

  pondingRisk = computed(() => {
    const d = this._facade.topographyData();
    if (!d) return null;
    return d.pondingRisk;
  });

  // ── Helpers ──
  elevDelta() {
    const s = this.topoStats();
    return s ? +(s.maxElevation - s.minElevation).toFixed(1) : 0;
  }

  meanPercent() {
    const s = this.topoStats();
    if (!s || s.maxElevation === s.minElevation) return 50;
    return +(
      ((s.meanElevation - s.minElevation) / (s.maxElevation - s.minElevation)) *
      100
    ).toFixed(1);
  }

  getSlopeColor(name: string): string {
    if (name.includes('0-2%')) return '#9BB88C';
    if (name.includes('2-5%')) return '#C7A14D';
    if (name.includes('5-15%')) return '#B86E3D';
    return '#5A2714';
  }

  onToggleLayer(layerId: string) {
    this._layerService.toggleLayerVisibility(layerId);
  }

  onLayerOpacityChange(layerId: string, opacity: number) {
    this._layerService.setLayerOpacity(layerId, opacity);
  }
}
