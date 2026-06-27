import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { ROUTES } from '../../../../shared/config/constants';

@Component({
  selector: 'app-auth-layout',
  standalone: true,
  imports: [RouterOutlet, RouterLink],
  templateUrl: './auth-layout.component.html',
  styleUrls: ['./auth-layout.component.css'],
})
export class AuthLayoutComponent {
  ROUTES = ROUTES;
}
