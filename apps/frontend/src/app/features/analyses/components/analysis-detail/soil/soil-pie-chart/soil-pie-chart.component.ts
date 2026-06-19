import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-soil-pie-chart',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './soil-pie-chart.component.html',
  styleUrls: ['./soil-pie-chart.component.css'],
})
export class SoilPieChartComponent {
  @Input() set composition(data: { type: string; percent: number; color: string }[]) {
    this.segments = this.buildSegments(data);
    this.primaryType = data.length
      ? data.reduce((a, b) => (a.percent > b.percent ? a : b)).type
      : '';
  }

  segments: any[] = [];
  primaryType = '';

  private buildSegments(data: { type: string; percent: number; color: string }[]) {
    const circumference = 100;
    let cumulativePercent = 0;
    const sorted = [...data].sort((a, b) => b.percent - a.percent);
    return sorted.map((d) => {
      const dashArray = `${(d.percent / 100) * circumference} ${circumference - (d.percent / 100) * circumference}`;
      const dashOffset = -(cumulativePercent * circumference) / 100;
      const result = {
        type: d.type,
        color: d.color,
        percent: d.percent,
        dashArray,
        dashOffset,
      };
      cumulativePercent += d.percent;
      return result;
    });
  }
}
