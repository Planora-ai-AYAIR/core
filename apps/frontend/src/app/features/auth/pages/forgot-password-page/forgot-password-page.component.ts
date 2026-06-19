import { ChangeDetectionStrategy, Component } from '@angular/core';
import { AuthLayoutComponent } from '../../components/auth-layout/auth-layout.component';
import { ForgotPasswordComponent } from '../../components/forgot-password/forgot-password.component';

@Component({
  selector: 'app-forgot-password-page',
  standalone: true,
  imports: [AuthLayoutComponent, ForgotPasswordComponent],
  templateUrl: './forgot-password-page.component.html',
  styleUrls: ['./forgot-password-page.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ForgotPasswordPageComponent {}
