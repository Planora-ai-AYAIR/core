import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { InputComponent } from '../../../../shared/components/input/input.component';
import { REG_EXP, ROUTES, VALIDATION_ERROR_MESSAGES } from '../../../../shared/config/constants';
import { ResetPasswordFacadeService } from '../../services/reset-password/reset-password-facade.service';

@Component({
  selector: 'app-reset-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, InputComponent, ButtonComponent, RouterLink],
  templateUrl: './reset-password.component.html',
  styleUrls: ['./reset-password.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResetPasswordComponent implements OnInit {
  ROUTES = ROUTES;
  readonly VALIDATION_ERROR_MESSAGES = VALIDATION_ERROR_MESSAGES;

  userId = signal<string>('');
  resetPasswordForm!: FormGroup;

  private facade = inject(ResetPasswordFacadeService);
  private fb = inject(FormBuilder);
  private router = inject(Router);

  isLoading = this.facade.loading;

  ngOnInit(): void {
    this.userId.set(history.state.userId ?? '');
    if (!this.userId()) {
      this.router.navigate([ROUTES.forgotPassword]);
      return;
    }

    this.resetPasswordForm = this.fb.group(
      {
        otp: ['', [Validators.required, Validators.pattern(REG_EXP.OTP)]],
        newPassword: ['', [Validators.required, Validators.pattern(REG_EXP.PASSWORD)]],
        confirmPassword: ['', Validators.required],
      },
      { validators: this.passwordsMatchValidator },
    );
  }

  passwordsMatchValidator(form: FormGroup) {
    const newPassword = form.get('newPassword')?.value;
    const confirm = form.get('confirmPassword')?.value;
    return newPassword === confirm ? null : { mismatch: true };
  }

  get formHasMismatch(): boolean {
    return (
      this.resetPasswordForm.hasError('mismatch') &&
      !!this.resetPasswordForm.get('confirmPassword')?.touched
    );
  }

  getErrorMessage(controlName: string): string {
    const control = this.resetPasswordForm.get(controlName);
    if (!control || !control.errors || !control.touched) return '';

    if (control.errors['required']) return VALIDATION_ERROR_MESSAGES.REQUIRED;
    if (control.errors['pattern']) {
      if (controlName === 'newPassword') {
        return 'Must be 8+ characters, with upper, lower, number & special character.';
      }
      if (controlName === 'otp') {
        return 'Enter the 6-digit code.';
      }
      return 'Invalid format';
    }
    return '';
  }

  onSubmit(): void {
    if (this.resetPasswordForm.invalid) {
      this.resetPasswordForm.markAllAsTouched();
      return;
    }

    const value = this.resetPasswordForm.value;
    this.facade.resetPassword({
      userId: this.userId(),
      otp: value.otp,
      newPassword: value.newPassword,
      confirmPassword: value.confirmPassword,
    });
  }
}
