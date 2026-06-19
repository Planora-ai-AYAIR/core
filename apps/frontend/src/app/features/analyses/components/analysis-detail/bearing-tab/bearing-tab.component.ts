import { Component, inject, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AnalysisDetailFacadeService } from '../../../services/analysis-detail-facade.service';

@Component({
  selector: 'app-bearing-tab',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './bearing-tab.component.html',
  styleUrls: ['./bearing-tab.component.css'],
})
export class BearingTabComponent {
  private _facade = inject(AnalysisDetailFacadeService);

  bearingData = computed(() => this._facade.bearingData());

  trafficLightColor = computed(() => {
    const capacity = this.bearingData()?.bearingCapacity;
    if (capacity == null) return '';
    if (capacity < 75) return 'bg-red-500';
    if (capacity < 200) return 'bg-yellow-500';
    return 'bg-green-500';
  });

  capacityLabel = computed(() => this.bearingData()?.capacityClass ?? '');

  floorBadgeColor = computed(() => {
    switch (this.bearingData()?.floorCountCategory) {
      case '1-2 floors':
        return 'bg-red-50 border-red-400 text-red-700';
      case '3-5 floors':
        return 'bg-yellow-50 border-yellow-400 text-yellow-700';
      case '6-10 floors':
        return 'bg-green-50 border-green-400 text-green-700';
      case '10+ floors':
        return 'bg-emerald-100 border-emerald-700 text-emerald-800';
      default:
        return 'bg-planora-desert-50 border-planora-desert-300 text-planora-basalt-600';
    }
  });

  floorBadgeNumber = computed(() => {
    const category = this.bearingData()?.floorCountCategory ?? '';
    const match = category.match(/\d+/);
    return match ? match[0] : '-';
  });

  capacityRange = computed(() => {
    const capacity = this.bearingData()?.bearingCapacity;
    if (capacity == null) return '';
    if (capacity < 75) return '< 75 kPa';
    if (capacity < 200) return '75 - 200 kPa';
    return '> 200 kPa';
  });
}
