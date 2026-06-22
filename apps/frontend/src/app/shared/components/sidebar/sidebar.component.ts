import { Component, Input, Output, EventEmitter, HostBinding, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { ROUTES } from '../../config/constants';
import { SignInFacadeService } from '../../../features/auth/services/sign-in/sign-in-facade.service';
import { AuthService } from '../../../core/services/auth.service';

interface NavItem {
  id: string;
  label: string;
  icon: string;
  route: string;
}

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css'],
})
export class SidebarComponent {
  private signInFacade = inject(SignInFacadeService);
  protected authService = inject(AuthService);

  readonly ROUTES = ROUTES;

  @Input() collapsed = false;
  @Input() mobileOpen = false;
  @Output() collapsedChange = new EventEmitter<boolean>();
  @Output() mobileOpenChange = new EventEmitter<boolean>();

  // These apply the CSS classes that trigger the visual states
  @HostBinding('class.collapsed') get collapsedClass() {
    return this.collapsed;
  }
  @HostBinding('class.mobile-open') get mobileOpenClass() {
    return this.mobileOpen;
  }

  navItems: NavItem[] = [
    { id: 'dashboard', label: 'Dashboard', icon: 'pi-chart-bar', route: ROUTES.dashboard },
    { id: 'parcels', label: 'Parcels', icon: 'pi-map', route: ROUTES.newParcel },
    { id: 'analyses', label: 'Analyses', icon: 'pi-sitemap', route: ROUTES.newAnalysis },
    { id: 'monitoring', label: 'Monitoring', icon: 'pi-desktop', route: '/app/monitoring' },
    { id: 'reports', label: 'Reports', icon: 'pi-file-pdf', route: '/app/reports' },
    { id: 'settings', label: 'Settings', icon: 'pi-cog', route: '/app/settings' },
  ];

  toggleCollapse() {
    this.collapsedChange.emit(!this.collapsed);
  }

  closeMobileDrawer() {
    this.mobileOpenChange.emit(false);
  }

  getInitials(name: string): string {
    if (!name) return 'U';
    return name
      .split(' ')
      .map((n) => n[0])
      .join('')
      .toUpperCase()
      .substring(0, 2);
  }

  onLogout(): void {
    this.signInFacade.logout();
  }
}
