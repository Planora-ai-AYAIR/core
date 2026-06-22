import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-depth-selector',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './depth-selector.component.html',
  styleUrls: ['./depth-selector.component.css'],
})
export class DepthSelectorComponent {
  @Input() depths: string[] = ['0-20cm', '20-50cm', '50-100cm', '100-200cm'];
  @Input() selected = '0-20cm';
  @Output() select = new EventEmitter<string>();
}
