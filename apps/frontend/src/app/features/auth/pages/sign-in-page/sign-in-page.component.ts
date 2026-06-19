import { Component, ChangeDetectionStrategy } from '@angular/core';
import { AuthLayoutComponent } from '../../components/auth-layout/auth-layout.component';
import { SignInComponent } from '../../components/sign-in/sign-in.component';
import { ROUTES } from '../../../../shared/config/constants';

@Component({
  selector: 'app-sign-in-page',
  templateUrl: './sign-in-page.component.html',
  styleUrls: ['./sign-in-page.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AuthLayoutComponent, SignInComponent],
})
export class SignInPageComponent {
  readonly ROUTES = ROUTES;
}
