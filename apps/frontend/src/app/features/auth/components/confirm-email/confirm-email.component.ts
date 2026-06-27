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
import { REG_EXP, ROUTES, OTP_CONFIG } from '../../../../shared/config/constants';
import { ConfirmEmailFacadeService } from '../../services/confirm-email/confirm-email-facade.service';

@Component({
  selector: 'app-confirm-email',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, OtpInputComponent, ButtonComponent, RouterLink],
  templateUrl: './confirm-email.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmEmailComponent implements OnInit, OnDestroy {
  ROUTES = ROUTES;
  userId = signal<string>('');
  otpForm!: FormGroup;
  isResending = false;

  private facade = inject(ConfirmEmailFacadeService);
  private fb = inject(FormBuilder);
  private router = inject(Router);

  // Expose facade's loading signal
  isLoading = this.facade.loading;

  // Countdown timer — use explicit generic type to avoid literal type narrowing
  private countdownValue = signal<number>(OTP_CONFIG.RESEND_TIMER_SECONDS);
  readonly countdown = computed(() => this.countdownValue());
  readonly canResend = computed(() => this.countdownValue() === 0 && !this.isResending);
  private countdownInterval: ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void {
    this.userId.set(history.state.userId);
    if (!this.userId()) {
      this.router.navigate([ROUTES.signIn]);
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
      this.countdownValue.update((v) => v - 1); // v is number now
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

  onSubmit(): void {
    if (this.otpForm.invalid) {
      this.otpForm.markAllAsTouched();
      return;
    }
    const otp = this.otpForm.value.otp;
    this.facade.verifyOtp(this.userId(), otp);
  }

  resendOtp(): void {
    if (!this.canResend()) return;
    this.isResending = true;
    this.facade.resendOtp(this.userId()).subscribe({
      complete: () => {
        this.isResending = false;
        this.startCountdown();
      },
      error: () => {
        this.isResending = false;
      },
    });
  }
}
