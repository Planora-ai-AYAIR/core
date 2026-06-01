import { Component, ViewChild } from '@angular/core';
import { SidebarComponent } from '../../sidebar/sidebar.component';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-authenticated-layout',
  standalone: true,
  imports: [SidebarComponent, RouterOutlet],
  templateUrl: './authenticated-layout.component.html',
  styleUrls: ['./authenticated-layout.component.css'],
})
export class AuthenticatedLayoutComponent {
  @ViewChild(SidebarComponent) sidebarComponent?: SidebarComponent;

  // Mock user – replace with real auth data later
  user = {
    name: 'Ahmed Mahmoud',
    avatar: null,
  };

  toggleMobileSidebar(): void {
    this.sidebarComponent?.toggleMobile();
  }
}
