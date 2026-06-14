import { Component, Input, Output, EventEmitter } from '@angular/core';
import { MapLayerItem } from '../../../interfaces/map-layer-item';

@Component({
  selector: 'app-layer-control',
  standalone: true,
  templateUrl: './layer-control.component.html',
  styleUrls: ['./layer-control.component.css'],
})
export class LayerControlComponent {
  @Input() layer!: MapLayerItem;

  @Output() toggle = new EventEmitter<string>();
  @Output() opacityChange = new EventEmitter<number>();

  onOpacityInput(event: Event) {
    const value = parseFloat((event.target as HTMLInputElement).value);
    this.opacityChange.emit(value / 100);
  }
}
