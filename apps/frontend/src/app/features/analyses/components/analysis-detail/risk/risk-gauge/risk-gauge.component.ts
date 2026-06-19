import { Component, Input, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-risk-gauge',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './risk-gauge.component.html',
  styleUrls: ['./risk-gauge.component.css'],
})
export class RiskGaugeComponent implements OnInit {
  @Input() score = 0;
  @Input() level = '';
  @Input() description = '';
  @Input() benchmark = '';

  animatedDashArray = '0 283';
  gaugeColor = '#10B981';

  ngOnInit(): void {
    const circumference = 2 * Math.PI * 45; // ≈ 282.74
    setTimeout(() => {
      this.animatedDashArray = `${(this.score / 100) * circumference} ${circumference - (this.score / 100) * circumference}`;
    }, 100);
    this.gaugeColor = this.getColor(this.score);
  }

  getColor(score: number): string {
    if (score <= 40) return '#10B981';
    if (score <= 60) return '#F59E0B';
    if (score <= 80) return '#F97316';
    return '#EF4444';
  }
}
