import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-how-it-works',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './how-it-works.component.html',
  styleUrls: ['./how-it-works.component.css'],
})
export class HowItWorksComponent implements OnInit {
  steps = [
    {
      step: '01',
      title: 'Define Your Site',
      description:
        'Draw a polygon on the interactive map or upload a GeoJSON file. The platform instantly captures the exact boundaries of your parcel.',
      visual: 'map',
    },
    {
      step: '02',
      title: 'AI Analysis',
      description:
        'Our machine learning models process satellite data, soil databases, and topographic information to generate a comprehensive site intelligence report.',
      visual: 'satellite',
    },
    {
      step: '03',
      title: 'Download Report',
      description:
        'Within hours, you receive a professional PDF with terrain analysis, bearing capacity, risk assessments, and an optimal borehole plan.',
      visual: 'report',
    },
  ];

  isVisible = false;

  ngOnInit() {
    setTimeout(() => {
      this.isVisible = true;
    }, 100);
  }
}
