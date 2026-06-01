import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { ROUTES } from '../../config/constants';

@Component({
  selector: 'app-footer',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './footer.component.html',
  styleUrls: ['./footer.component.css'],
})
export class FooterComponent {
  ROUTES = ROUTES;

  currentYear = new Date().getFullYear();

  footerColumns = [
    {
      title: 'Platform',
      links: [
        { label: 'Features', route: '/features' },
        { label: 'Pricing', route: '/pricing' },
        { label: 'API', route: '/api' },
      ],
    },
    {
      title: 'Company',
      links: [
        { label: 'About', route: '/about' },
        { label: 'Careers', route: '/careers' },
        { label: 'Contact', route: '/contact' },
      ],
    },
    {
      title: 'Resources',
      links: [
        { label: 'Documentation', route: '/docs' },
        { label: 'Blog', route: '/blog' },
        { label: 'Support', route: '/support' },
      ],
    },
  ];

  socialLinks = [
    { label: 'LinkedIn', icon: 'pi pi-linkedin', href: '#' },
    { label: 'Twitter', icon: 'pi pi-twitter', href: '#' },
    { label: 'YouTube', icon: 'pi pi-youtube', href: '#' },
  ];
}
