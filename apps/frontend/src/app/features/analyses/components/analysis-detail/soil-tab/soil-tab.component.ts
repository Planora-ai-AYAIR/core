import { Component, inject, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MapLayerService } from '../../../services/map-layer.service';
import { LayerControlComponent } from '../layer-control/layer-control.component';
import { AnalysisDetailFacadeService } from '../../../services/analysis-detail-facade.service';

@Component({
  selector: 'app-soil-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, LayerControlComponent],
  templateUrl: './soil-tab.component.html',
  styleUrls: ['./soil-tab.component.css'],
})
export class SoilTabComponent {
  private _layerService = inject(MapLayerService);
  private _facade = inject(AnalysisDetailFacadeService);

  mapLayers = this._layerService.layersByGroup('soil');

  soilStats = computed(() => {
    const d = this._facade.soilData();
    return d
      ? {
          bearingCapacity: d.bearingCapacity,
          plasticityIndex: d.plasticityIndex,
          organicContent: d.organicContent,
          cohesion: d.cohesion,
        }
      : null;
  });

  soilComposition = computed(() => this._facade.soilData()?.composition ?? []);

  legend = signal([
    { color: '#B86E3D', label: 'Clay' },
    { color: '#6B7F5E', label: 'Silt' },
    { color: '#E0BF6B', label: 'Sand' },
    { color: '#C7A14D', label: 'Bearing Point' },
    { color: '#2563EB', label: 'Water Table' },
  ]);

  bearingRating(): string {
    const capacity = this.soilStats()?.bearingCapacity ?? 0;
    if (capacity >= 300) return 'High';
    if (capacity >= 200) return 'Medium';
    return 'Low';
  }

  onToggleLayer(layerId: string) {
    this._layerService.toggleLayerVisibility(layerId);
  }

  onLayerOpacityChange(layerId: string, opacity: number) {
    this._layerService.setLayerOpacity(layerId, opacity);
  }
}
