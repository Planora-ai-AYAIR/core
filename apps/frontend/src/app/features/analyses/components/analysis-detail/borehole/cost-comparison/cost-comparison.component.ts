import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-cost-comparison',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './cost-comparison.component.html',
  styleUrls: ['./cost-comparison.component.css'],
})
export class CostComparisonComponent {
  @Input() traditionalCount = 0;
  @Input() traditionalCost = 0;
  @Input() optimizedCount = 0;
  @Input() optimizedCost = 0;
  @Input() savingsAmount = 0;
  @Input() savingsPercent = 0;
  @Input() rate = 700;
  @Output() rateChange = new EventEmitter<number>();
}
