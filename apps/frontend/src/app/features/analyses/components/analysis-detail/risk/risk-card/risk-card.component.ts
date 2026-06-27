import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-risk-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './risk-card.component.html',
  styleUrls: ['./risk-card.component.css'],
})
export class RiskCardComponent {
  @Input() label = '';
  @Input() risk!: {
    score: number;
    level: string;
    icon: string;
    color: string;
    factors: any[];
    mitigation?: string;
  };
}
