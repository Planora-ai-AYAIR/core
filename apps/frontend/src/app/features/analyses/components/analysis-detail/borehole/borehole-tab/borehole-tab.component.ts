import { Component, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MapLayerService } from '../../../../services/map-layer.service';
import { AnalysisDetailFacadeService } from '../../../../services/analysis-detail-facade.service';
import { BoreholeSummaryComponent } from '../borehole-summary/borehole-summary.component';
import { CostComparisonComponent } from '../cost-comparison/cost-comparison.component';
import { BoreholeParameters } from '../../../../interfaces/borehole-data';
import { LayerControlComponent } from '../../layer-control/layer-control.component';

@Component({
  selector: 'app-borehole-tab',
  standalone: true,
  imports: [
    CommonModule,
    BoreholeSummaryComponent,
    CostComparisonComponent,
    LayerControlComponent,
  ],
  templateUrl: './borehole-tab.component.html',
  styleUrls: ['./borehole-tab.component.css'],
})
export class BoreholeTabComponent {
  private _layerService = inject(MapLayerService);
  private _facade = inject(AnalysisDetailFacadeService);

  mapLayers = this._layerService.layersByGroup('borehole');
  boreholeData = computed(() => this._facade.boreholeData());

  onToggleLayer(layerId: string) {
    this._layerService.toggleLayerVisibility(layerId);
  }
  onLayerOpacityChange(layerId: string, opacity: number) {
    this._layerService.setLayerOpacity(layerId, opacity);
  }

  downloadGeoJSON() {
    const data = this.boreholeData();
    if (!data) return;
    const geoJSON = {
      type: 'FeatureCollection',
      features: data.placementPoints.map((p) => ({
        type: 'Feature',
        properties: { id: p.id, priority: p.priority, reason: p.reason, depth: p.estimatedDepth },
        geometry: { type: 'Point', coordinates: [p.lng, p.lat] },
      })),
    };
    const blob = new Blob([JSON.stringify(geoJSON, null, 2)], { type: 'application/geo+json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'borehole-plan.geojson';
    a.click();
    URL.revokeObjectURL(url);
  }
}
