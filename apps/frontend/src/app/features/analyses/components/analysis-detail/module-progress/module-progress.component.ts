import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ModuleStatus } from '../../../interfaces/module-status';

@Component({
  selector: 'app-module-progress',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './module-progress.component.html',
  styleUrls: ['./module-progress.component.css'],
})
export class ModuleProgressComponent {
  @Input() moduleName = '';
  @Input() status: ModuleStatus = 'Waiting';
  @Input() estimatedSeconds = 0;
  @Input() steps: string[] = ['Waiting', 'Queued', 'Processing', 'Completed'];
  @Output() cancel = new EventEmitter<void>();

  get currentStep(): number {
    const idx = this.steps.indexOf(this.status);
    return idx >= 0 ? idx : 0;
  }

  statusColor() {
    switch (this.status) {
      case 'Completed':
        return 'bg-green-100 text-green-700';
      case 'Processing':
        return 'bg-amber-100 text-amber-700';
      case 'Failed':
        return 'bg-red-100 text-red-700';
      default:
        return 'bg-planora-desert-100 text-planora-basalt-700';
    }
  }

  stepClass(i: number) {
    if (i < this.currentStep) return 'border-green-500 bg-green-500 text-white';
    if (i === this.currentStep)
      return this.status === 'Processing'
        ? 'border-amber-500 bg-white text-amber-600'
        : 'border-planora-desert-300 bg-white text-planora-basalt-500';
    return 'border-planora-desert-200 bg-white text-planora-basalt-400';
  }

  connectorClass(i: number) {
    return i < this.currentStep ? 'bg-green-400' : 'bg-planora-desert-200';
  }
}
