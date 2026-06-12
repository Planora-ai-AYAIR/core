import { Component, inject, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MapLayerService } from '../../../services/map-layer.service';
import { LayerControlComponent } from '../layer-control/layer-control.component';
import { AnalysisDetailFacadeService } from '../../../services/analysis-detail-facade.service';

@Component({
  selector: 'app-risk-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, LayerControlComponent],
  templateUrl: './risk-tab.component.html',
  styleUrls: ['./risk-tab.component.css'],
})
export class RiskTabComponent {
  private _layerService = inject(MapLayerService);
  private _facade = inject(AnalysisDetailFacadeService);

  mapLayers = this._layerService.layersByGroup('risk');

  riskStats = computed(() => {
    const d = this._facade.riskData();
    return d
      ? {
          floodRisk: d.floodRisk,
          seismicZone: d.seismicZone,
          liquefactionPotential: d.liquefactionPotential,
          landslideRisk: d.landslideRisk,
        }
      : null;
  });

  hazards = computed(() => this._facade.riskData()?.hazards ?? []);

  legend = signal([
    { color: '#A13E3A', label: 'High Flood Risk' },
    { color: '#D97706', label: 'Medium Flood Risk' },
    { color: '#F59E0B', label: 'Seismic Zone II' },
    { color: '#8B5CF6', label: 'Liquefaction Area' },
  ]);

  onToggleLayer(layerId: string) {
    this._layerService.toggleLayerVisibility(layerId);
  }

  onLayerOpacityChange(layerId: string, opacity: number) {
    this._layerService.setLayerOpacity(layerId, opacity);
  }
}
