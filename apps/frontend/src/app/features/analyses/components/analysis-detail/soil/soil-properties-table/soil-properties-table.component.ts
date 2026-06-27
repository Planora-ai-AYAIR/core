import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-soil-properties-table',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './soil-properties-table.component.html',
  styleUrls: ['./soil-properties-table.component.css'],
})
export class SoilPropertiesTableComponent {
  @Input() bulkDensity = 0;
  @Input() organicCarbon = 0;
  @Input() pH = 0;
}
