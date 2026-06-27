import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-stat-card',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './stat-card.component.html',
  styleUrls: ['./stat-card.component.css'],
})
export class StatCardComponent {
  @Input() icon = 'pi-chart-line';
  @Input() label = 'Label';
  @Input() value: string | number = 0;
  @Input() iconBgClass = 'bg-planora-limestone';
  @Input() iconTextClass = 'text-planora-clay-600';
}
