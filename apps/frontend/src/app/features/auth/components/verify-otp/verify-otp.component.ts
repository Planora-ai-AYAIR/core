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
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { ROUTES, VALIDATION_ERROR_MESSAGES, REG_EXP } from '../../../../shared/config/constants';
import { OtpInputComponent } from '../../../../shared/components/otp-input/otp-input.component';

@Component({
  selector: 'app-verify-otp',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, OtpInputComponent, ButtonComponent, RouterLink],
  templateUrl: './verify-otp.component.html',
  styleUrls: ['./verify-otp.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VerifyOtpComponent implements OnInit, OnDestroy {
  ROUTES = ROUTES;

  email = signal<string>('');
  otpForm!: FormGroup;
  isLoading = false;
  isResending = false;

  // ── Countdown timer (60 seconds) ──
  private countdownValue = signal(60);
  readonly countdown = computed(() => this.countdownValue());
  readonly canResend = computed(() => this.countdownValue() === 0 && !this.isResending);

  private countdownInterval: ReturnType<typeof setInterval> | null = null;

  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  readonly VALIDATION_ERROR_MESSAGES = VALIDATION_ERROR_MESSAGES;

  ngOnInit(): void {
    // Get email from query params
    this.route.queryParams.subscribe((params) => {
      if (params['email']) {
        this.email.set(params['email']);
      } else {
        this.router.navigate([ROUTES.forgotPassword]);
      }
    });

    this.otpForm = this.fb.group({
      otp: ['', [Validators.required, Validators.pattern(REG_EXP.OTP)]],
    });

    // Start the 60-second countdown (OTP was just sent)
    this.startCountdown();
  }

  ngOnDestroy(): void {
    this.stopCountdown();
  }

  private startCountdown(): void {
    this.stopCountdown();
    this.countdownValue.set(60);
    this.countdownInterval = setInterval(() => {
      this.countdownValue.update((v) => {
        if (v <= 1) {
          this.stopCountdown();
          return 0;
        }
        return v - 1;
      });
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

    this.isLoading = true;
    setTimeout(() => {
      this.isLoading = false;
      // Navigate to reset password with email and OTP token
      this.router.navigate([ROUTES.resetPassword], {
        queryParams: {
          email: this.email(),
          otp: this.otpForm.value.otp,
        },
      });
    }, 1500);
  }

  resendOtp(): void {
    if (!this.canResend()) return;

    this.isResending = true;
    // Simulate API call
    setTimeout(() => {
      this.isResending = false;
      // Restart the countdown
      this.startCountdown();
    }, 1000);
  }
}
