import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { SignInFacadeService } from '../../services/sign-in/sign-in-facade.service';
import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { InputComponent } from '../../../../shared/components/input/input.component';
import {
  AUTH_MESSAGES,
  ROUTES,
  VALIDATION_ERROR_MESSAGES,
} from '../../../../shared/config/constants';
import { LoginRequest } from '../../interfaces/sign-in/login-request';

@Component({
  selector: 'app-sign-in',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, InputComponent, ButtonComponent, RouterLink],
  templateUrl: './sign-in.component.html',
  styleUrls: ['./sign-in.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignInComponent implements OnInit {
  ROUTES = ROUTES;

  signInForm!: FormGroup;

  private signInFacade = inject(SignInFacadeService);
  private fb = inject(FormBuilder);
  private router = inject(Router);

  isLoading = this.signInFacade.loading;

  // Expose constants for template
  readonly AUTH_MESSAGES = AUTH_MESSAGES;
  readonly VALIDATION_ERROR_MESSAGES = VALIDATION_ERROR_MESSAGES;

  ngOnInit(): void {
    this.signInForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
    });
  }

  getErrorMessage(controlName: string): string {
    const control = this.signInForm.get(controlName);
    if (!control || !control.errors || !control.touched) return '';

    if (control.errors['required']) return VALIDATION_ERROR_MESSAGES.REQUIRED;
    if (control.errors['email']) return VALIDATION_ERROR_MESSAGES.EMAIL;
    return '';
  }

  onSubmit(): void {
    if (this.signInForm.invalid) {
      this.signInForm.markAllAsTouched();
      return;
    }

    const credentials: LoginRequest = this.signInForm.value;
    this.signInFacade.login(credentials);
  }
}
