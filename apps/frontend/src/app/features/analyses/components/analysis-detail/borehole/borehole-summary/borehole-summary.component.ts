import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-borehole-summary',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './borehole-summary.component.html',
  styleUrls: ['./borehole-summary.component.css'],
})
export class BoreholeSummaryComponent {
  @Input() minRequired = 0;
  @Input() recommended = 0;
  @Input() coveragePercent = 0;
  @Input() gridSize = '';
  @Input() strategy = '';

  get coverageColor() {
    if (this.coveragePercent > 80) return '#10B981';
    if (this.coveragePercent > 60) return '#F59E0B';
    return '#EF4444';
  }

  get coverageDashArray() {
    const circumference = 100; // 2 * PI * 15.915
    return `${(this.coveragePercent / 100) * circumference} ${circumference - (this.coveragePercent / 100) * circumference}`;
  }
}
