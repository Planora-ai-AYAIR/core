import { Injectable, inject, signal, computed, OnDestroy } from '@angular/core';
import { NotificationApiService } from './notification-api.service';
import { ToastService } from '../../../shared/services/toaster.service';
import { forkJoin, Subject, takeUntil } from 'rxjs';
import { SignalRService } from '../signalr.service';
import { NotificationDto } from '../../interfaces/notification/notification-dto';

@Injectable({ providedIn: 'root' })
export class NotificationFacadeService implements OnDestroy {
  private api = inject(NotificationApiService);
  private signalR = inject(SignalRService);
  private toast = inject(ToastService);

  notifications = signal<NotificationDto[]>([]);
  unreadCount = computed(() => this.notifications().filter((n) => !n.isRead).length);
  loading = signal(false);

  private destroy$ = new Subject<void>();

  constructor() {
    this.signalR.notification$.pipe(takeUntil(this.destroy$)).subscribe((notification) => {
      this.notifications.update((list) => [notification, ...list]);
      this.toast.info(notification.message);
    });
  }

  loadNotifications(unreadOnly = false, take = 20, skip = 0): void {
    this.loading.set(true);
    this.api.getNotifications(unreadOnly, take, skip).subscribe({
      next: (page) => {
        this.notifications.set(page.items);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.toast.error('Failed to load notifications');
      },
    });
  }

  markAsRead(id: string): void {
    this.api.markAsRead(id).subscribe(() => {
      this.notifications.update((list) =>
        list.map((n) => (n.id === id ? { ...n, isRead: true } : n)),
      );
    });
  }

  markAllAsRead(): void {
    const unreadIds = this.notifications()
      .filter((n) => !n.isRead)
      .map((n) => n.id);
    if (!unreadIds.length) return;
    forkJoin(unreadIds.map((id) => this.api.markAsRead(id))).subscribe(() => {
      this.notifications.update((list) => list.map((n) => ({ ...n, isRead: true })));
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
