import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  computed,
  inject,
  OnDestroy,
  OnInit,
  signal,
} from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { OtpInputComponent } from '../../../../shared/components/otp-input/otp-input.component';
import {
  REG_EXP,
  ROUTES,
  OTP_CONFIG,
  VALIDATION_ERROR_MESSAGES,
} from '../../../../shared/config/constants';

@Component({
  selector: 'app-verify-otp',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, OtpInputComponent, ButtonComponent, RouterLink],
  templateUrl: './verify-otp.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VerifyOtpComponent implements OnInit, OnDestroy {
  ROUTES = ROUTES;

  userId = signal<string>('');
  otpForm!: FormGroup;
  isResending = false;

  private fb = inject(FormBuilder);
  private router = inject(Router);

  // Countdown
  private countdownValue = signal<number>(OTP_CONFIG.RESEND_TIMER_SECONDS);
  readonly countdown = computed(() => this.countdownValue());
  readonly canResend = computed(() => this.countdownValue() === 0 && !this.isResending);
  private countdownInterval: ReturnType<typeof setInterval> | null = null;

  readonly VALIDATION_ERROR_MESSAGES = VALIDATION_ERROR_MESSAGES;

  ngOnInit(): void {
    this.userId.set(history.state.userId);
    if (!this.userId()) {
      this.router.navigate([ROUTES.forgotPassword]);
    }

    this.otpForm = this.fb.group({
      otp: ['', [Validators.required, Validators.pattern(REG_EXP.OTP)]],
    });

    this.startCountdown();
  }

  ngOnDestroy(): void {
    this.stopCountdown();
  }

  private startCountdown(): void {
    this.stopCountdown();
    this.countdownValue.set(OTP_CONFIG.RESEND_TIMER_SECONDS);
    this.countdownInterval = setInterval(() => {
      this.countdownValue.update((v) => v - 1);
      if (this.countdownValue() <= 0) {
        this.stopCountdown();
      }
    }, 1000);
  }

  private stopCountdown(): void {
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
      this.countdownInterval = null;
    }
  }

  getErrorMessage(controlName: string): string {
    const control = this.otpForm.get(controlName);
    if (!control || !control.errors || !control.touched) return '';
    if (control.errors['required']) return VALIDATION_ERROR_MESSAGES.REQUIRED;
    if (control.errors['pattern']) return 'Please enter a valid 6-digit code';
    return '';
  }

  onSubmit(): void {
    if (this.otpForm.invalid) {
      this.otpForm.markAllAsTouched();
      return;
    }

    const otp = this.otpForm.value.otp;
    // Navigate to reset password with userId and otp (via state)
    this.router.navigate([ROUTES.resetPassword], {
      state: { userId: this.userId(), otp },
    });
  }

  resendOtp(): void {
    if (!this.canResend()) return;

    this.isResending = true;
    // TODO: connect to actual resend API for password‑reset flow
    // For now, restart countdown after simulation
    setTimeout(() => {
      this.isResending = false;
      this.startCountdown();
    }, 1000);
  }
}
