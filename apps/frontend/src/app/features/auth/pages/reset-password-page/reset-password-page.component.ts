import { ChangeDetectionStrategy, Component } from '@angular/core';
import { AuthLayoutComponent } from '../../components/auth-layout/auth-layout.component';
import { ResetPasswordComponent } from '../../components/reset-password/reset-password.component';

@Component({
  selector: 'app-reset-password-page',
  standalone: true,
  imports: [AuthLayoutComponent, ResetPasswordComponent],
  templateUrl: './reset-password-page.component.html',
  styleUrls: ['./reset-password-page.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ResetPasswordPageComponent {}
