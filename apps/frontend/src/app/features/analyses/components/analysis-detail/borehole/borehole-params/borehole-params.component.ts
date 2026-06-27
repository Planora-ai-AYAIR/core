import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BoreholeParameters } from '../../../../interfaces/borehole-data';

@Component({
  selector: 'app-borehole-params',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './borehole-params.component.html',
  styleUrls: ['./borehole-params.component.css'],
})
export class BoreholeParamsComponent {
  @Input() params!: BoreholeParameters;
  @Output() paramsChange = new EventEmitter<Partial<BoreholeParameters>>();
  @Output() recalculate = new EventEmitter<void>();

  update(field: string, value: any) {
    this.paramsChange.emit({ [field]: value });
  }
}
