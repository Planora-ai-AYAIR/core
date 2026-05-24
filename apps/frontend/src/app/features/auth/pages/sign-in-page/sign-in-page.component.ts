import { Component, ChangeDetectionStrategy } from '@angular/core';
import { AuthLayoutComponent } from '../../components/auth-layout/auth-layout.component';
import { SignInComponent } from '../../components/sign-in/sign-in.component';
import { RouterLink } from "@angular/router";
import { ROUTES } from '../../../../shared/config/constants';

@Component({
  selector: 'app-sign-in-page',
  templateUrl: './sign-in-page.component.html',
  styleUrls: ['./sign-in-page.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AuthLayoutComponent, SignInComponent, RouterLink],
})
export class SignInPageComponent {
  readonly ROUTES = ROUTES;
}
