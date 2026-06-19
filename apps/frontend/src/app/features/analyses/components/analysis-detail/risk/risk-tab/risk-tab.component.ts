import { Component, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RiskGaugeComponent } from '../risk-gauge/risk-gauge.component';
import { RiskCardComponent } from '../risk-card/risk-card.component';
import { MitigationSuggestionsComponent } from '../mitigation-suggestions/mitigation-suggestions.component';
import { AnalysisDetailFacadeService } from '../../../../services/analysis-detail-facade.service';
import { MapLayerService } from '../../../../services/map-layer.service';
import { LayerControlComponent } from '../../layer-control/layer-control.component';

@Component({
  selector: 'app-risk-tab',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    LayerControlComponent,
    RiskGaugeComponent,
    RiskCardComponent,
    MitigationSuggestionsComponent,
  ],
  templateUrl: './risk-tab.component.html',
  styleUrls: ['./risk-tab.component.css'],
})
export class RiskTabComponent {
  private _layerService = inject(MapLayerService);
  private _facade = inject(AnalysisDetailFacadeService);

  mapLayers = this._layerService.layersByGroup('risk');

  // Data from facade
  riskData = computed(() => this._facade.riskData());

  // Derived for gauge description
  gaugeDescription = computed(() => {
    const level = this.riskData()?.overallRiskLevel;
    return level ? `${level} risk — engineering review recommended` : '';
  });

  // Legend derived from risk breakdown colors
  legend = computed(() => {
    const d = this.riskData();
    if (!d) return [];
    return [
      { color: d.floodRisk.color, label: 'Flood Risk' },
      { color: d.seismicRisk.color, label: 'Seismic Risk' },
      { color: d.expansiveSoilRisk.color, label: 'Expansive Soil' },
      { color: d.liquefactionRisk.color, label: 'Liquefaction' },
    ];
  });

  // Mitigations from data
  mitigations = computed(() => this.riskData()?.mitigations ?? []);

  relevantMitigations = computed(() => {
    const data = this.riskData();
    if (!data) return [];
    const highRiskTypes: string[] = [];
    if (data.floodRisk.score >= 40) highRiskTypes.push('High Flood');
    if (data.seismicRisk.score >= 40) highRiskTypes.push('High Seismic');
    if (data.expansiveSoilRisk.score >= 40) highRiskTypes.push('High Expansive Soil');
    if (data.liquefactionRisk.score >= 40) highRiskTypes.push('High Liquefaction');
    return data.mitigations.filter((m) => highRiskTypes.includes(m.risk));
  });

  onToggleLayer(layerId: string) {
    this._layerService.toggleLayerVisibility(layerId);
  }

  onLayerOpacityChange(layerId: string, opacity: number) {
    this._layerService.setLayerOpacity(layerId, opacity);
  }
}
