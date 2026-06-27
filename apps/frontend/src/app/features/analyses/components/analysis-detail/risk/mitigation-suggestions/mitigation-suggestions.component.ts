import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MitigationItem } from '../../../../interfaces/risk-data';

@Component({
  selector: 'app-mitigation-suggestions',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './mitigation-suggestions.component.html',
  styleUrls: ['./mitigation-suggestions.component.css'],
})
export class MitigationSuggestionsComponent {
  @Input() mitigations: MitigationItem[] = [];
}
