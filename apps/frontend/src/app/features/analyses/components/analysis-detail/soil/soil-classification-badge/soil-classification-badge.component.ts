import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-soil-classification-badge',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './soil-classification-badge.component.html',
  styleUrls: ['./soil-classification-badge.component.css'],
})
export class SoilClassificationBadgeComponent {
  @Input() classification = '';
  @Input() confidence = 0;
}
