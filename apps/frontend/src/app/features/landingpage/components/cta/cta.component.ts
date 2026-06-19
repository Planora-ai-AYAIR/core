import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { ROUTES } from '../../../../shared/config/constants';

@Component({
  selector: 'app-cta',
  standalone: true,
  imports: [RouterLink, ButtonComponent],
  templateUrl: './cta.component.html',
  styleUrls: ['./cta.component.css'],
})
export class CtaComponent {
  readonly ROUTES = ROUTES;
}
