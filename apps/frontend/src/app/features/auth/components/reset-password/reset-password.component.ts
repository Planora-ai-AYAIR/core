import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { InputComponent } from '../../../../shared/components/input/input.component';
import { ROUTES, VALIDATION_ERROR_MESSAGES, REG_EXP } from '../../../../shared/config/constants';

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

  resetForm!: FormGroup;
  isLoading = false;
  email = '';
  otp = '';

  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);
  private router = inject(Router);

  readonly VALIDATION_ERROR_MESSAGES = VALIDATION_ERROR_MESSAGES;

  ngOnInit(): void {
    // Retrieve email & OTP from query params (sent by verify-otp page)
    this.route.queryParams.subscribe((params) => {
      this.email = params['email'] || '';
      this.otp = params['otp'] || '';

      // If either parameter is missing, redirect back to forgot password
      if (!this.email || !this.otp) {
        this.router.navigate([ROUTES.forgotPassword]);
        return;
      }
    });

    this.resetForm = this.fb.group(
      {
        password: ['', [Validators.required, Validators.pattern(REG_EXP.PASSWORD)]],
        confirmPassword: ['', Validators.required],
      },
      { validators: this.passwordsMatchValidator },
    );
  }

  passwordsMatchValidator(form: FormGroup) {
    const password = form.get('password')?.value;
    const confirm = form.get('confirmPassword')?.value;
    return password === confirm ? null : { mismatch: true };
  }

  getErrorMessage(controlName: string): string {
    const control = this.resetForm.get(controlName);
    if (!control || !control.errors || !control.touched) return '';

    if (control.errors['required']) return VALIDATION_ERROR_MESSAGES.REQUIRED;
    if (control.errors['pattern']) {
      if (controlName === 'password') {
        return 'Must be 8+ characters, including uppercase, lowercase, number & special character.';
      }
      return 'Invalid format';
    }
    return '';
  }

  get formHasMismatch(): boolean {
    return this.resetForm.hasError('mismatch') && !!this.resetForm.get('confirmPassword')?.touched;
  }

  onSubmit(): void {
    if (this.resetForm.invalid) {
      this.resetForm.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    // Simulate password reset API call – replace with facade later
    setTimeout(() => {
      this.isLoading = false;
      // Redirect to sign-in on success
      this.router.navigate([ROUTES.signIn]);
    }, 1500);
  }
}
