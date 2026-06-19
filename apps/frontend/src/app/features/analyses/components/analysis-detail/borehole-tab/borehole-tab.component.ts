import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MapLayerService } from '../../../services/map-layer.service';
import { LayerControlComponent } from '../layer-control/layer-control.component';
import { AnalysisDetailFacadeService } from '../../../services/analysis-detail-facade.service';

@Component({
  selector: 'app-borehole-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, LayerControlComponent],
  templateUrl: './borehole-tab.component.html',
  styleUrls: ['./borehole-tab.component.css'],
})
export class BoreholeTabComponent {
  private _layerService = inject(MapLayerService);
  private _facade = inject(AnalysisDetailFacadeService);

  mapLayers = this._layerService.layersByGroup('borehole');

  boreholes = computed(() => this._facade.boreholeData()?.points ?? []);
  boreholeStats = computed(() => {
    const d = this._facade.boreholeData();
    return d
      ? {
          estimatedCost: d.estimatedCost,
          drillingCost: d.drillingCost,
          testingCost: d.testingCost,
        }
      : null;
  });

  legend = signal([
    { color: '#B86E3D', label: 'Proposed Borehole' },
    { color: '#10B981', label: 'Optimal Drilling Area' },
  ]);

  drillingCost(): number {
    return this.boreholes().reduce((sum, bh) => sum + bh.cost * 0.7, 0);
  }

  testingCost(): number {
    return this.boreholes().reduce((sum, bh) => sum + bh.cost * 0.3, 0);
  }

  onToggleLayer(layerId: string) {
    this._layerService.toggleLayerVisibility(layerId);
  }

  onLayerOpacityChange(layerId: string, opacity: number) {
    this._layerService.setLayerOpacity(layerId, opacity);
  }
}
