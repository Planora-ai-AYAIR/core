import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { ROUTES } from '../../../../shared/config/constants';

@Component({
  selector: 'app-dashboard-home',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './dashboard-home.component.html',
  styleUrls: ['./dashboard-home.component.css'],
})
export class DashboardHomeComponent {
  constructor(private router: Router) {}

  // Global Metrics based on Module 5 & 6 [cite: 410, 416]
  metrics = [
    { label: 'Total Area Surveyed', value: '142 ha', icon: 'pi-map' },
    { label: 'Borehole Cost Savings', value: 'EGP 840k', icon: 'pi-percentage', trend: '+12%' },
    { label: 'Avg Risk Index', value: 'Low (24)', icon: 'pi-shield' },
  ];

  // The 6 Pillars of Planora AI [cite: 463, 467, 471, 475, 480, 484]
  modules = [
    {
      id: 1,
      name: 'Topographic Profile',
      status: 'Completed',
      icon: 'pi-chart-line',
      color: 'bg-planora-clay-500',
    },
    {
      id: 2,
      name: 'Soil Composition',
      status: 'Completed',
      icon: 'pi-database',
      color: 'bg-planora-silt-500',
    },
    {
      id: 3,
      name: 'Bearing Capacity',
      status: 'Processing',
      icon: 'pi-bolt',
      color: 'bg-planora-gold-500',
    },
    {
      id: 4,
      name: 'Risk Assessment',
      status: 'Pending',
      icon: 'pi-exclamation-triangle',
      color: 'bg-planora-basalt-400',
    },
    {
      id: 5,
      name: 'Borehole Plan',
      status: 'Pending',
      icon: 'pi-map-marker',
      color: 'bg-planora-basalt-400',
    },
    {
      id: 6,
      name: 'PDF Site Intelligence',
      status: 'Pending',
      icon: 'pi-file-pdf',
      color: 'bg-planora-basalt-400',
    },
  ];

  activeParcel = {
    name: 'New Giza Industrial Sector B',
    area: '12.4 Hectares',
    coordinates: '30.013°N, 31.208°E',
  };

  newParcel() {
    this.router.navigate([ROUTES.newParcel]);
  }
}
