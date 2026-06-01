import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ROUTES } from '../../config/constants';
import { ButtonComponent } from '../button/button.component';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink, ButtonComponent],
  templateUrl: './not-found.component.html',
  styleUrl: './not-found.component.css',
})
export class NotFoundComponent {
  readonly ROUTES = ROUTES;
}
