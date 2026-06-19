import {
  Component,
  ChangeDetectionStrategy,
  ElementRef,
  QueryList,
  ViewChildren,
  AfterViewInit,
  forwardRef,
  signal,
} from '@angular/core';
import { ControlValueAccessor, NG_VALUE_ACCESSOR } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-otp-input',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './otp-input.component.html',
  styleUrls: ['./otp-input.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => OtpInputComponent),
      multi: true,
    },
  ],
})
export class OtpInputComponent implements ControlValueAccessor, AfterViewInit {
  @ViewChildren('otpInput') inputs!: QueryList<ElementRef<HTMLInputElement>>;

  digits = Array(6).fill(0);
  values = signal<string[]>(Array(6).fill(''));

  private onChange: (value: string) => void = () => {};
  private onTouched: () => void = () => {};

  // ── ControlValueAccessor ──
  writeValue(value: string): void {
    const chars = (value || '').split('').slice(0, 6);
    const arr = Array(6).fill('');
    chars.forEach((c, i) => (arr[i] = c));
    this.values.set(arr);
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    // If needed, store disabled state; we'll skip for simplicity.
  }

  // ── Focus first input after view init ──
  ngAfterViewInit(): void {
    setTimeout(() => {
      this.inputs?.first?.nativeElement?.focus();
    });
  }

  // ── Input handlers ──
  onInput(index: number, event: Event): void {
    const input = event.target as HTMLInputElement;
    const val = input.value.replace(/\D/g, ''); // only digits
    if (val.length === 0) {
      this.updateDigit(index, '');
      return;
    }

    // Take last character (in case of multi-char paste or weirdness)
    const digit = val[val.length - 1];
    this.updateDigit(index, digit);
    // Auto-advance to next input
    if (index < 5) {
      this.inputs.get(index + 1)?.nativeElement?.focus();
    }
  }

  onKeydown(index: number, event: KeyboardEvent): void {
    const input = event.target as HTMLInputElement;

    if (event.key === 'Backspace') {
      if (input.value === '' && index > 0) {
        // Move focus to previous and clear it
        this.updateDigit(index - 1, '');
        this.inputs.get(index - 1)?.nativeElement?.focus();
        event.preventDefault();
      }
    } else if (event.key === 'ArrowLeft' && index > 0) {
      this.inputs.get(index - 1)?.nativeElement?.focus();
    } else if (event.key === 'ArrowRight' && index < 5) {
      this.inputs.get(index + 1)?.nativeElement?.focus();
    }
  }

  onPaste(event: ClipboardEvent): void {
    event.preventDefault();
    const pasteData = event.clipboardData?.getData('text') || '';
    const digits = pasteData.replace(/\D/g, '').split('').slice(0, 6);
    const arr = Array(6).fill('');
    digits.forEach((d, i) => (arr[i] = d));
    this.values.set(arr);
    this.emitValue();
    // Focus last filled or first empty
    const lastIdx = digits.length - 1;
    const focusIdx = lastIdx < 5 ? lastIdx + 1 : 5;
    this.inputs.get(focusIdx)?.nativeElement?.focus();
  }

  onFocus(index: number): void {
    // Select existing content for easy overwrite
    const input = this.inputs.get(index)?.nativeElement;
    if (input) input.select();
    this.onTouched();
  }

  // ── Private helpers ──
  private updateDigit(index: number, digit: string): void {
    const newValues = [...this.values()];
    newValues[index] = digit;
    this.values.set(newValues);
    this.emitValue();
  }

  private emitValue(): void {
    const code = this.values().join('');
    this.onChange(code);
  }
}
