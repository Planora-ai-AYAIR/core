import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { InputComponent } from '../../../../shared/components/input/input.component';
import {
  AUTH_MESSAGES,
  ROUTES,
  VALIDATION_ERROR_MESSAGES,
  REG_EXP,
} from '../../../../shared/config/constants';
import { SignUpFacadeService } from '../../services/sign-up/sign-up-facade.service';

@Component({
  selector: 'app-sign-up',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, InputComponent, ButtonComponent, RouterLink],
  templateUrl: './sign-up.component.html',
  styleUrls: ['./sign-up.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignUpComponent implements OnInit {
  ROUTES = ROUTES;

  signUpForm!: FormGroup;
  readonly AUTH_MESSAGES = AUTH_MESSAGES;
  readonly VALIDATION_ERROR_MESSAGES = VALIDATION_ERROR_MESSAGES;

  private fb = inject(FormBuilder);
  private signUpFacade = inject(SignUpFacadeService);

  // Expose facade's loading signal to template
  isLoading = this.signUpFacade.loading;

  ngOnInit(): void {
    this.signUpForm = this.fb.group(
      {
        firstName: ['', Validators.required],
        lastName: ['', Validators.required],
        email: ['', [Validators.required, Validators.email]],
        password: ['', [Validators.required, Validators.pattern(REG_EXP.PASSWORD)]],
        confirmPassword: ['', Validators.required],
        acceptTerms: [false, Validators.requiredTrue],
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
    const control = this.signUpForm.get(controlName);
    if (!control || !control.errors || !control.touched) return '';

    if (control.errors['required']) return VALIDATION_ERROR_MESSAGES.REQUIRED;
    if (control.errors['email']) return VALIDATION_ERROR_MESSAGES.EMAIL;
    if (control.errors['pattern']) {
      if (controlName === 'password') {
        return 'Must be 8+ characters, with upper, lower, number & special character.';
      }
      return 'Invalid format';
    }
    if (control.errors['requiredTrue']) return 'You must accept the terms';
    return '';
  }

  get formHasMismatch(): boolean {
    return (
      this.signUpForm.hasError('mismatch') && !!this.signUpForm.get('confirmPassword')?.touched
    );
  }

  onSubmit(): void {
    if (this.signUpForm.invalid) {
      this.signUpForm.markAllAsTouched();
      return;
    }

    // Build request matching API contract
    const formValue = this.signUpForm.value;
    const request = {
      firstName: formValue.firstName,
      lastName: formValue.lastName,
      email: formValue.email,
      password: formValue.password,
      phoneNumber: null,
    };

    this.signUpFacade.register(request);
  }
}
