import { Component, ChangeDetectionStrategy } from '@angular/core';
import { AuthLayoutComponent } from '../../components/auth-layout/auth-layout.component';
import { ConfirmEmailComponent } from '../../components/confirm-email/confirm-email.component';

@Component({
  selector: 'app-confirm-email-page',
  standalone: true,
  imports: [AuthLayoutComponent, ConfirmEmailComponent],
  templateUrl: './confirm-email-page.component.html',
  styleUrls: ['./confirm-email-page.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmEmailPageComponent {}
