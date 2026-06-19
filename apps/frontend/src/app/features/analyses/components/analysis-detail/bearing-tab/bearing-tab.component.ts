import { Component, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AnalysisDetailFacadeService } from '../../../services/analysis-detail-facade.service';
import { MapLayerService } from '../../../services/map-layer.service';

@Component({
  selector: 'app-bearing-tab',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './bearing-tab.component.html',
  styleUrls: ['./bearing-tab.component.css'],
})
export class BearingTabComponent {
  private _facade = inject(AnalysisDetailFacadeService);

  bearingData = computed(() => {
    const d = this._facade.bearingData();
    if (!d) return null;
    return {
      capacity: d.bearingCapacity,
      clayPercent: d.clayPercent,
      sandPercent: d.sandPercent,
      moistureIndex: d.moistureIndex,
      depthToWaterTable: d.waterTableDepth,
      terrainSlope: d.terrainSlope,
      foundationRecommendation:
        d.bearingCapacity > 200 ? 'Shallow Foundations Suitable' : 'Deep Foundations Required',
      floorCount: this.getFloorCount(d.bearingCapacity),
      disclaimer:
        'AI estimate for preliminary planning only. Physical borehole verification mandatory.',
    };
  });

  private getFloorCount(capacity: number) {
    if (capacity < 75) return '1-2 floors';
    if (capacity < 200) return '3-5 floors';
    if (capacity < 300) return '6-10 floors';
    return '10+ floors';
  }

  trafficLightColor(capacity: number) {
    if (capacity < 75) return 'bg-red-500';
    if (capacity < 200) return 'bg-yellow-500';
    return 'bg-green-500';
  }
}
