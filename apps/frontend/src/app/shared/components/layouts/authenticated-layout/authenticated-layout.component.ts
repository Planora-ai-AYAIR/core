import { Component, signal, inject, OnInit, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  RouterOutlet,
  Router,
  NavigationEnd,
  ActivatedRoute,
  RouterLink,
} from '@angular/router';
import { SidebarComponent } from '../../sidebar/sidebar.component';
import { BreakpointObserver } from '@angular/cdk/layout';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { filter, map } from 'rxjs/operators';
import { NotificationDropdownComponent } from '../../notification-dropdown/notification-dropdown.component';

interface Breadcrumb {
  label: string;
  route: string | null;
}

@Component({
  selector: 'app-authenticated-layout',
  standalone: true,
  imports: [CommonModule, SidebarComponent, RouterOutlet, RouterLink, NotificationDropdownComponent],
  templateUrl: './authenticated-layout.component.html',
  styleUrls: ['./authenticated-layout.component.css'],
})
export class AuthenticatedLayoutComponent implements OnInit {
  private breakpointObserver = inject(BreakpointObserver);
  private router = inject(Router);
  private activatedRoute = inject(ActivatedRoute);
  private destroyRef = inject(DestroyRef);

  user = { name: 'Ahmed Mahmoud', avatar: null };

  isSidebarCollapsed = signal(false);
  isSidebarMobileOpen = signal(false);
  breadcrumbs = signal<Breadcrumb[]>([{ label: 'Dashboard', route: null }]);

  ngOnInit() {
    this.breadcrumbs.set(this.buildBreadcrumbs());

    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        map(() => this.buildBreadcrumbs()),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((bc) => this.breadcrumbs.set(bc));

    // ── Breakpoints ──
    const largeScreen = '(min-width: 1280px)';
    const mediumScreen = '(min-width: 1024px) and (max-width: 1279px)';
    const smallScreen = '(max-width: 1023px)';

    this.breakpointObserver
      .observe([largeScreen, mediumScreen, smallScreen])
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((result) => {
        if (result.breakpoints[largeScreen]) {
          this.isSidebarCollapsed.set(false);
          this.isSidebarMobileOpen.set(false);
        } else if (result.breakpoints[mediumScreen]) {
          this.isSidebarCollapsed.set(true);
          this.isSidebarMobileOpen.set(false);
        } else {
          this.isSidebarCollapsed.set(false);
          this.isSidebarMobileOpen.set(false);
        }
      });

    // ── Dynamic breadcrumbs ──
    this.router.events
      .pipe(
        filter((event) => event instanceof NavigationEnd),
        map(() => this.buildBreadcrumbs()),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((bc) => this.breadcrumbs.set(bc));
  }

  private buildBreadcrumbs(): Breadcrumb[] {
    const crumbs: Breadcrumb[] = [];
    let currentRoute: ActivatedRoute | null = this.activatedRoute.root;
    let url = '';

    while (currentRoute) {
      // Get the path segments for the current level
      const segment = currentRoute.snapshot.url.map((s) => s.path).join('/');

      if (segment) {
        // Build the URL prefix incrementally
        url += `/${segment}`;

        // We skip adding 'app' to the visible breadcrumbs list,
        // but the 'url' variable now correctly includes it for subsequent links.
        if (segment !== 'app') {
          const label = currentRoute.snapshot.title || this.capitalize(segment);
          crumbs.push({
            label: label,
            route: url,
          });
        }
      }
      // Move to the next child in the route tree
      currentRoute = currentRoute.firstChild;
    }

    // Default fallback if no valid crumbs are found
    if (crumbs.length === 0) {
      return [{ label: 'Dashboard', route: null }];
    }

    // Ensure the current (last) page is not clickable
    crumbs[crumbs.length - 1].route = null;

    return crumbs;
  }

  private capitalize(s: string): string {
    return s.charAt(0).toUpperCase() + s.slice(1);
  }

  private pathToTitle(url: any[]): string | null {
    if (!url || url.length === 0) return null;
    const segment = url[url.length - 1].path;
    if (segment === 'app') return null;
    return segment.charAt(0).toUpperCase() + segment.slice(1);
  }

  onCollapsedChange(collapsed: boolean) {
    this.isSidebarCollapsed.set(collapsed);
  }

  onMobileOpenChange(open: boolean) {
    this.isSidebarMobileOpen.set(open);
  }

  toggleMobileSidebar() {
    this.isSidebarMobileOpen.set(!this.isSidebarMobileOpen());
  }
}
