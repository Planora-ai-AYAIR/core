import { Component, ChangeDetectionStrategy } from '@angular/core';
import { AuthLayoutComponent } from '../../components/auth-layout/auth-layout.component';
import { SignUpComponent } from '../../components/sign-up/sign-up.component';

@Component({
  selector: 'app-sign-up-page',
  standalone: true,
  imports: [AuthLayoutComponent, SignUpComponent],
  templateUrl: './sign-up-page.component.html',
  styleUrls: ['./sign-up-page.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignUpPageComponent {}
  