import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ROUTES } from '../../config/constants';

@Component({
  selector: 'app-terms-page',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './terms-page.component.html',
})
export class TermsPageComponent {
  readonly ROUTES = ROUTES;
  
  readonly currentDate = new Date().toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}
