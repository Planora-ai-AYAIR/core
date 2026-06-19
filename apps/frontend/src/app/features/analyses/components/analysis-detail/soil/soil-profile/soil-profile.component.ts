import { Component, EventEmitter, Input, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { SoilDepthData } from '../../../../interfaces/soil-data';

@Component({
  selector: 'app-soil-profile',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './soil-profile.component.html',
  styleUrls: ['./soil-profile.component.css'],
})
export class SoilProfileComponent {
  @Input() profiles: SoilDepthData[] = [];
  @Input() viewMode: 'stacked' | 'side-by-side' = 'stacked';
  @Output() toggleViewMode = new EventEmitter<'stacked' | 'side-by-side'>();
  @Output() exportCSV = new EventEmitter<void>();
}
