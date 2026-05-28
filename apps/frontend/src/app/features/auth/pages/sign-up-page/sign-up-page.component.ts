import { Component, ChangeDetectionStrategy } from '@angular/core';
import { RegistrationComponent } from '../../components/registration/registration.component';
import { CommonModule } from '@angular/common';
import { AuthLayoutComponent } from '../../components/auth-layout/auth-layout.component';
import { ButtonComponent } from '../../../../shared/components/button/button.component';

@Component({
  selector: 'app-sign-up-page',
  imports: [AuthLayoutComponent, RegistrationComponent, CommonModule, ButtonComponent],
  templateUrl: './sign-up-page.component.html',
  styleUrls: ['./sign-up-page.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignUpPageComponent {}
