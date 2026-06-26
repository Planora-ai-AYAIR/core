import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationFacadeService } from '../../../core/services/notification/notification-facade.service';

@Component({
  selector: 'app-notification-dropdown',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './notification-dropdown.component.html',
  styleUrls: ['./notification-dropdown.component.css'],
})
export class NotificationDropdownComponent {
  facade = inject(NotificationFacadeService);
  open = false;

  unreadCount = this.facade.unreadCount;

  toggleDropdown(): void {
    this.open = !this.open;
    if (this.open) {
      this.facade.loadNotifications();
    }
  }

  closeDropdown(): void {
    this.open = false;
  }
}
