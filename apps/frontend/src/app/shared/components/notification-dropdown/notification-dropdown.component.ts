import { Component, inject, ElementRef, HostListener } from '@angular/core';
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
  private hostElement = inject(ElementRef);
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

  @HostListener('document:click', ['$event.target'])
  onClickOutside(target: EventTarget | null) {
    if (!target) return;
    const clickedInside = this.hostElement.nativeElement.contains(target as Node);
    if (!clickedInside && this.open) {
      this.open = false;
    }
  }
}
