import { Component, signal } from '@angular/core';

@Component({
  selector: 'app-features',
  standalone: true,
  templateUrl: './features.component.html',
  styleUrls: ['./features.component.css'],
})
export class FeaturesComponent {
  activeFeature = signal<string>('terrain');

  features = [
    {
      id: 'terrain',
      title: 'Terrain & Topography',
      description:
        'High-resolution DTM, contour lines every 0.5m, slope analysis, cut-fill estimates — no survey crew needed.',
      icon: 'pi-chart-line', // PrimeIcon
      color: 'bg-[#E3D5C0]', // desert
      textColor: 'text-planora-basalt-800',
      badgeColor: 'bg-[#A87F36]', // darker gold for badge
      mapBadge: 'Elevation ready',
      depth: 'Surface',
    },
    {
      id: 'soil',
      title: 'Soil Composition',
      description:
        'Sand, silt, clay ratios, organic content, pH, Atterberg limits — derived from satellite spectroscopy.',
      icon: 'pi-sort-amount-down',
      color: 'bg-[#F5DDCE]', // clay light
      textColor: 'text-planora-basalt-800',
      badgeColor: 'bg-[#B86E3D]',
      mapBadge: 'Soil layers mapped',
      depth: '0-5 m',
    },
    {
      id: 'bearing',
      title: 'Bearing Capacity',
      description:
        'Estimated safe bearing capacity in kPa, recommended foundation type & maximum building height.',
      icon: 'pi-arrow-down',
      color: 'bg-[#F2DDA7]', // gold light
      textColor: 'text-planora-basalt-800',
      badgeColor: 'bg-[#C7A14D]',
      mapBadge: '245 kPa (Medium)',
      depth: '5-10 m',
    },
    {
      id: 'risks',
      title: 'Flood & Seismic Risks',
      description:
        'Multi-hazard assessment: flood zones, groundwater depth, seismic acceleration, liquefaction potential.',
      icon: 'pi-exclamation-triangle',
      color: 'bg-[#C1D3B7]', // silt light
      textColor: 'text-planora-basalt-800',
      badgeColor: 'bg-[#6B7F5E]',
      mapBadge: 'Low risk',
      depth: 'All depths',
    },
    {
      id: 'boreholes',
      title: 'Optimal Borehole Plan',
      description:
        'AI suggests the minimum number of boreholes and their ideal locations — reducing cost by 40-60%.',
      icon: 'pi-circle',
      color: 'bg-[#E0BF6B]', // gold medium
      textColor: 'text-planora-basalt-800',
      badgeColor: 'bg-[#B86E3D]',
      mapBadge: '5 boreholes optimal',
      depth: '15-20 m',
    },
    {
      id: 'report',
      title: 'PDF Site Report',
      description:
        'Professional downloadable report with maps, tables, risk matrices, and data source references.',
      icon: 'pi-file-pdf',
      color: 'bg-[#2B2D31]', // basalt
      textColor: 'text-planora-basalt-800',
      badgeColor: 'bg-[#1A1C1E]',
      mapBadge: 'Ready to download',
      depth: 'Final',
    },
  ];

  getActiveFeature() {
    return this.features.find((f) => f.id === this.activeFeature());
  }
}
