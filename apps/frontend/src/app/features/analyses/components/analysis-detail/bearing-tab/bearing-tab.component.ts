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
    const light = this.bearingData()?.trafficLight;
    if (light === 'green') return 'bg-green-500';
    if (light === 'yellow') return 'bg-yellow-500';
    if (light === 'red') return 'bg-red-500';
    return 'bg-gray-400'; // unknown
  });

  capacityRange = computed(() => this.bearingData()?.range ?? '');

  floorBadgeColor = computed(() => {
    const category = this.bearingData()?.floorCountCategory;
    if (category === '1-2 floors') return 'bg-red-50 border-red-400 text-red-700';
    if (category === '3-5 floors') return 'bg-yellow-50 border-yellow-400 text-yellow-700';
    if (category === '6-10 floors') return 'bg-green-50 border-green-400 text-green-700';
    if (category === '10+ floors') return 'bg-emerald-100 border-emerald-700 text-emerald-800';
    return 'bg-gray-50 border-gray-400 text-gray-700';
  });

  floorBadgeNumber = computed(() => {
    const category = this.bearingData()?.floorCountCategory ?? '';
    const match = category.match(/\d+/);
    return match ? match[0] : '-';
  });
}
