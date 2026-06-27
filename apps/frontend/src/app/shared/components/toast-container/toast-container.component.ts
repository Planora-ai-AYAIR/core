import { Component, OnDestroy, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { ToastMessage } from '../../interfaces/toaster-message';
import { Subscription } from 'rxjs';
import { ToastService } from '../../services/toaster.service';

const ICONS: Record<ToastMessage['type'], string> = {
  success: `<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" stroke-width="2">
    <path stroke-linecap="round" stroke-linejoin="round" d="M4.5 12.75l6 6 9-13.5" />
  </svg>`,
  error: `<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" stroke-width="2">
    <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v3.75m0 3.75h.008v.008H12V16.5zm9-4.5a9 9 0 11-18 0 9 9 0 0118 0z" />
  </svg>`,
  info: `<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" stroke-width="2">
    <path stroke-linecap="round" stroke-linejoin="round" d="M11.25 11.25h.75v4.5h.75M12 7.5h.008v.008H12V7.5z" />
    <circle cx="12" cy="12" r="9" stroke-linecap="round" stroke-linejoin="round" />
  </svg>`,
  warning: `<svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" stroke-width="2">
    <path stroke-linecap="round" stroke-linejoin="round" d="M12 9v3.75m0 3.75h.008v.008H12V16.5zm-8.485 1.5h16.97c1.04 0 1.69-1.127 1.17-2.025L13.65 4.05c-.52-.9-1.78-.9-2.3 0L3.345 16.975c-.52.898.13 2.025 1.17 2.025z" />
  </svg>`,
};

const ACCENT_BORDER: Record<ToastMessage['type'], string> = {
  success: 'border-l-planora-silt-500',
  error: 'border-l-planora-risk-600',
  info: 'border-l-planora-clay-500',
  warning: 'border-l-planora-gold-500',
};

const ACCENT_TEXT: Record<ToastMessage['type'], string> = {
  success: 'text-planora-silt-600',
  error: 'text-planora-risk-600',
  info: 'text-planora-clay-600',
  warning: 'text-planora-gold-600',
};

const ACCENT_BG: Record<ToastMessage['type'], string> = {
  success: 'bg-planora-silt-500',
  error: 'bg-planora-risk-600',
  info: 'bg-planora-clay-500',
  warning: 'bg-planora-gold-500',
};

@Component({
  selector: 'app-toast-container',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './toast-container.component.html',
  styleUrls: ['./toast-container.component.css'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToastContainerComponent implements OnDestroy {
  toast: ToastMessage | null = null;
  private sub?: Subscription;
  private timeout?: any;
  private iconCache = new Map<ToastMessage['type'], SafeHtml>();

  constructor(
    private toastService: ToastService,
    private cdr: ChangeDetectorRef,
    private sanitizer: DomSanitizer,
  ) {
    this.sub = this.toastService.messages$.subscribe((msg) => {
      if (msg) {
        this.toast = msg;
        this.cdr.markForCheck();

        clearTimeout(this.timeout);
        this.timeout = setTimeout(() => {
          this.toast = null;
          this.cdr.markForCheck();
        }, msg.duration ?? 6000);
      }
    });
  }

  accentClass(type: ToastMessage['type']): string {
    return ACCENT_BORDER[type];
  }

  iconColorClass(type: ToastMessage['type']): string {
    return ACCENT_TEXT[type];
  }

  accentBgClass(type: ToastMessage['type']): string {
    return ACCENT_BG[type];
  }

  iconSvg(type: ToastMessage['type']): SafeHtml {
    let cached = this.iconCache.get(type);
    if (!cached) {
      cached = this.sanitizer.bypassSecurityTrustHtml(ICONS[type]);
      this.iconCache.set(type, cached);
    }
    return cached;
  }

  dismiss() {
    clearTimeout(this.timeout);
    this.toast = null;
    this.cdr.markForCheck();
  }

  ngOnDestroy() {
    this.sub?.unsubscribe();
    clearTimeout(this.timeout);
  }
}
