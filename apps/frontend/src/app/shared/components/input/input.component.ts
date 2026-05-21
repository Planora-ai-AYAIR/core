import {
  Component,
  ChangeDetectionStrategy,
  input,
  signal,
  computed,
  forwardRef,
  inject,
  ElementRef,
  ViewChild,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-input',
  templateUrl: './input.component.html',
  styleUrls: ['./input.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => InputComponent),
      multi: true,
    },
  ],
})
export class InputComponent implements ControlValueAccessor {
  @ViewChild('inputEl') private inputEl?: ElementRef<HTMLInputElement>;
  // Input properties using new input() function
  type = input<string>('text');
  placeholder = input<string>('');
  label = input<string>('');
  id = input<string>('');
  name = input<string>('');
  errorMessage = input<string>('');
  helperText = input<string>('');
  showError = input<boolean>(false);
  suffixIcon = input<string>('');
  suffixIconAriaLabel = input<string>('');

  // Internal state
  value = signal<string>('');
  isFocused = signal<boolean>(false);
  isDisabled = signal<boolean>(false);
  isTouched = signal<boolean>(false);
  showPassword = signal<boolean>(false);

  // Computed properties
  isPasswordField = computed(() => this.type() === 'password');
  isDateField = computed(() => this.type() === 'date');
  currentInputType = computed(() =>
    this.isPasswordField() && this.showPassword() ? 'text' : this.type(),
  );

  // Computed state for styling
  shouldShowError = computed(() => this.showError());
  inputClasses = computed(() => {
    const baseClasses =
      'w-full px-6 py-4 bg-tv-surface/40 backdrop-blur-md border rounded-xl text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:border-transparent transition';
    const borderColor = this.shouldShowError()
      ? 'border-red-500 focus:ring-red-500'
      : 'border-white/10 focus:ring-tv-cyan';
    const padding =
      this.suffixIcon() || this.isPasswordField() || this.isDateField() ? 'pr-12' : '';
    return `${baseClasses} ${borderColor} ${padding}`;
  });

  // ControlValueAccessor implementation
  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  writeValue(value: string): void {
    this.value.set(value || '');
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    this.isDisabled.set(isDisabled);
  }

  // Event handlers
  onInput(event: Event): void {
    const inputElement = event.target as HTMLInputElement;
    const newValue = inputElement.value;
    this.value.set(newValue);
    this.onChange(newValue);
  }

  onBlur(): void {
    this.isFocused.set(false);
    this.isTouched.set(true);
    this.onTouched();
  }

  onFocus(): void {
    this.isFocused.set(true);
  }

  onSuffixIconClick(): void {
    // Emit event or handle suffix icon click
    // This can be extended with output() for custom behavior
  }

  togglePassword(): void {
    this.showPassword.update((v) => !v);
  }

  openDatePicker(): void {
    const input = this.inputEl?.nativeElement;
    if (!input || this.isDisabled()) return;

    if (typeof input.showPicker === 'function') {
      input.showPicker();
      return;
    }

    input.focus();
  }
}
