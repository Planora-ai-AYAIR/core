import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-button',
  imports: [CommonModule],
  templateUrl: './button.component.html',
  styleUrls: ['./button.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ButtonComponent {
  text = input<string>('');
  type = input<'button' | 'submit' | 'reset'>('button');
  disabled = input<boolean>(false);
  isLoading = input<boolean>(false);
  loadingText = input<string>('Loading...');
  fullWidth = input<boolean>(false);
  variant = input<'primary' | 'outline'>('primary');
  size = input<'sm' | 'md' | 'lg'>('md');
  ariaLabel = input<string | null>(null);
  className = input<string>('');

  buttonClass(): string {
    const baseClasses =
      'inline-flex items-center justify-center font-semibold rounded-sm transition-all focus:outline-none disabled:opacity-50 disabled:cursor-not-allowed touch-manipulation';

    // Size
    const sizeClasses = {
      sm: 'px-4 py-2 text-xs',
      md: 'px-6 py-2.5 text-sm',
      lg: 'px-8 py-4 text-base',
    }[this.size()];

    // Variant
    const variantClasses =
      this.variant() === 'outline'
        ? 'border border-planora-basalt-700 text-planora-basalt-800 hover:bg-planora-desert-100 focus-visible:ring-2 focus-visible:ring-planora-basalt-300'
        : 'bg-planora-clay-500 text-planora-limestone shadow-md shadow-planora-clay-200/60 hover:bg-planora-clay-600 hover:shadow-lg hover:shadow-planora-clay-300/40 focus-visible:ring-2 focus-visible:ring-planora-clay-400 active:scale-95';

    const widthClass = this.fullWidth() ? 'w-full' : '';

    return [baseClasses, sizeClasses, variantClasses, widthClass, this.className()]
      .filter(Boolean)
      .join(' ');
  }
}
