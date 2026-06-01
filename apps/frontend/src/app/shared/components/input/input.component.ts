import {
  Component,
  ChangeDetectionStrategy,
  input,
  signal,
  computed,
  forwardRef,
  ViewChild,
  ElementRef,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-input',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './input.component.html',
  styleUrls: ['./input.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
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

  // ── Inputs (using new signal-based input) ──
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

  // ── Internal state ──
  value = signal<string>('');
  isFocused = signal<boolean>(false);
  isDisabled = signal<boolean>(false);
  isTouched = signal<boolean>(false);
  showPassword = signal<boolean>(false);

  // ── Computed properties ──
  isPasswordField = computed(() => this.type() === 'password');
  isDateField = computed(() => this.type() === 'date');
  currentInputType = computed(() =>
    this.isPasswordField() && this.showPassword() ? 'text' : this.type(),
  );

  shouldShowError = computed(() => this.showError());

  // ── Planora Earth styles ──
  inputClasses = computed(() => {
    const base =
      'w-full px-5 py-3.5 bg-white border rounded-md text-planora-basalt-800 placeholder:text-planora-basalt-400 focus:outline-none focus:ring-2 focus:ring-offset-1 transition duration-200';

    const borderColor = this.shouldShowError()
      ? 'border-planora-risk focus:ring-planora-risk'
      : 'border-planora-desert-300 focus:ring-planora-clay-400 focus:border-planora-clay-400';

    const extraPadding =
      this.suffixIcon() || this.isPasswordField() || this.isDateField() ? 'pr-12' : '';

    return `${base} ${borderColor} ${extraPadding}`;
  });

  // ── ControlValueAccessor ──
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

  // ── Event handlers ──
  onInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const newValue = input.value;
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

  togglePassword(): void {
    this.showPassword.update((v) => !v);
  }

  openDatePicker(): void {
    const el = this.inputEl?.nativeElement;
    if (!el || this.isDisabled()) return;
    if (typeof el.showPicker === 'function') {
      el.showPicker();
    } else {
      el.focus();
    }
  }

  // Suffix icon click handler (can be extended with output)
  onSuffixIconClick(): void {
    // Placeholder for future use
  }
}
