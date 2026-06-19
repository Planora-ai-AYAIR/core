import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { ButtonComponent } from '../../../../shared/components/button/button.component';

interface ParcelOption {
  id: string;
  name: string;
  area: number; // m²
  location: string;
}

@Component({
  selector: 'app-analysis-new',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, ButtonComponent],
  templateUrl: './analysis-new.component.html',
  styleUrls: ['./analysis-new.component.css'],
})
export class AnalysisNewComponent {
  parcels = signal<ParcelOption[]>([
    {
      id: 'parcel_550e8400',
      name: 'Talaat Moustafa Group',
      area: 50000,
      location: '30.044°N, 31.236°E',
    },
    {
      id: 'parcel_1a2b3c4d',
      name: 'Orascom Construction',
      area: 120000,
      location: '29.981°N, 31.112°E',
    },
    {
      id: 'parcel_9z8y7x6w',
      name: 'Egyptian Resorts Company',
      area: 30000,
      location: '31.198°N, 29.914°E',
    },
  ]);

  selectedParcelId = signal('');
  launching = signal(false);

  // The six geotechnical modules that will run
  readonly modules = [
    { name: 'Topography', icon: 'pi-chart-line', color: 'text-planora-clay-500' },
    { name: 'Soil Composition', icon: 'pi-database', color: 'text-planora-silt-500' },
    { name: 'Bearing Capacity', icon: 'pi-bolt', color: 'text-planora-gold-500' },
    { name: 'Risk Assessment', icon: 'pi-exclamation-triangle', color: 'text-planora-risk' },
    { name: 'Borehole Plan', icon: 'pi-map-marker', color: 'text-planora-basalt-600' },
    { name: 'PDF Report', icon: 'pi-file-pdf', color: 'text-planora-clay-600' },
  ];

  // The currently selected parcel object (if any)
  selectedParcel = signal<ParcelOption | undefined>(undefined);

  constructor(private router: Router) {
    // When the selection changes, update the selected parcel detail
    // (We'll do this via a reactive signal or simple method)
  }

  selectParcel(id: string) {
    this.selectedParcelId.set(id);
    this.selectedParcel.set(this.parcels().find((p) => p.id === id));
  }

  launchAnalysis() {
    if (!this.selectedParcelId()) return;
    this.launching.set(true);
    // Simulate API call
    setTimeout(() => {
      this.launching.set(false);
      this.router.navigate(['/app/analyses']);
    }, 2000);
  }
}
