import { Component } from '@angular/core';
import { AuthLayoutComponent } from '../../components/auth-layout/auth-layout.component';
import { VerifyOtpComponent } from '../../components/verify-otp/verify-otp.component';

@Component({
  selector: 'app-verify-otp-page',
  imports: [AuthLayoutComponent, VerifyOtpComponent],
  templateUrl: './verify-otp-page.component.html',
  styleUrls: ['./verify-otp-page.component.css'],
})
export class VerifyOtpPageComponent {}
