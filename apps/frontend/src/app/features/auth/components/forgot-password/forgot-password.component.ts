import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { InputComponent } from '../../../../shared/components/input/input.component';
import { ROUTES, VALIDATION_ERROR_MESSAGES } from '../../../../shared/config/constants';
import { ForgotPasswordFacadeService } from '../../services/forgot-password/forgot-password-facade.service';

type ResetMethod = 'email' | 'phone';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, InputComponent, ButtonComponent, RouterLink],
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ForgotPasswordComponent implements OnInit {
  ROUTES = ROUTES;
  readonly VALIDATION_ERROR_MESSAGES = VALIDATION_ERROR_MESSAGES;

  forgotPasswordForm!: FormGroup;
  method: ResetMethod = 'email';

  private facade = inject(ForgotPasswordFacadeService);
  private fb = inject(FormBuilder);

  isLoading = this.facade.loading;

  ngOnInit(): void {
    this.forgotPasswordForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: [''],
    });
    this.applyMethodValidators();
  }

  setMethod(method: ResetMethod): void {
    if (this.method === method) return;
    this.method = method;
    this.applyMethodValidators();
  }

  private applyMethodValidators(): void {
    const email = this.forgotPasswordForm.get('email');
    const phoneNumber = this.forgotPasswordForm.get('phoneNumber');

    if (this.method === 'email') {
      email?.setValidators([Validators.required, Validators.email]);
      phoneNumber?.clearValidators();
      phoneNumber?.setValue('');
    } else {
      phoneNumber?.setValidators([Validators.required]);
      email?.clearValidators();
      email?.setValue('');
    }
    email?.updateValueAndValidity();
    phoneNumber?.updateValueAndValidity();
  }

  getErrorMessage(controlName: string): string {
    const control = this.forgotPasswordForm.get(controlName);
    if (!control || !control.errors || !control.touched) return '';

    if (control.errors['required']) return VALIDATION_ERROR_MESSAGES.REQUIRED;
    if (control.errors['email']) return VALIDATION_ERROR_MESSAGES.EMAIL;
    return '';
  }

  onSubmit(): void {
    if (this.forgotPasswordForm.invalid) {
      this.forgotPasswordForm.markAllAsTouched();
      return;
    }

    const value = this.forgotPasswordForm.value;
    this.facade.requestReset({
      email: this.method === 'email' ? value.email : null,
      phoneNumber: this.method === 'phone' ? value.phoneNumber : null,
    });
  }
}
