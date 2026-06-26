import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AnalysisApiService } from '../../services/start-analysis/start-analysis-api.service';
import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { ParcelListResponse } from '../../../parcels/interfaces/parcel-list/parcel-list-response';
import { ParcelApiService } from '../../../parcels/services/parcel-api.service';
import { ROUTES } from '../../../../shared/config/constants';
import { AnalysisOptionsDto } from '../../interfaces/start-analysis/analysis-options-dto';

@Component({
  selector: 'app-analysis-new',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, ButtonComponent],
  templateUrl: './analysis-new.component.html',
  styleUrls: ['./analysis-new.component.css'],
})
export class AnalysisNewComponent implements OnInit {
  private parcelApi = inject(ParcelApiService);
  private analysisApi = inject(AnalysisApiService);
  private router = inject(Router);

  parcels = signal<ParcelListResponse[]>([]);
  selectedParcelId = signal('');
  launching = signal(false);

  readonly modules = [
    { name: 'Topography', icon: 'pi-chart-line', color: 'text-planora-clay-500' },
    { name: 'Soil Composition', icon: 'pi-database', color: 'text-planora-silt-500' },
    { name: 'Bearing Capacity', icon: 'pi-bolt', color: 'text-planora-gold-500' },
    { name: 'Risk Assessment', icon: 'pi-exclamation-triangle', color: 'text-planora-risk' },
    { name: 'Borehole Plan', icon: 'pi-map-marker', color: 'text-planora-basalt-600' },
    { name: 'PDF Report', icon: 'pi-file-pdf', color: 'text-planora-clay-600' },
  ];

  selectedParcel = computed(() => {
    const p = this.parcels().find((p) => p.id === this.selectedParcelId());
    if (!p) return null;
    return {
      ...p,
      areaM2: p.areaHectares * 10000, // m²
      location: this.formatLocation(p.centroidLatitude, p.centroidLongitude),
    };
  });

  private formatLocation(lat?: number, lng?: number): string {
    if (lat == null || lng == null) return 'Location not provided';
    const latDir = lat >= 0 ? 'N' : 'S';
    const lngDir = lng >= 0 ? 'E' : 'W';
    return `${Math.abs(lat).toFixed(3)}°${latDir}, ${Math.abs(lng).toFixed(3)}°${lngDir}`;
  }

  ngOnInit(): void {
    this.parcelApi.getMyParcels().subscribe((parcels) => this.parcels.set(parcels));
  }

  selectParcel(id: string) {
    this.selectedParcelId.set(id);
  }

  launchAnalysis() {
    if (!this.selectedParcelId()) return;
    this.launching.set(true);

    const options: AnalysisOptionsDto = {
      includeTopography: true,
      includeSoil: true,
      includeBearing: true,
      includeRisk: true,
      includeBorehole: true,
    };

    this.analysisApi.startAnalysis(this.selectedParcelId(), options).subscribe({
      next: (response) => {
        this.launching.set(false);
        this.router.navigate([ROUTES.analysis]);
      },
      error: () => {
        this.launching.set(false);
      },
    });
  }
}
