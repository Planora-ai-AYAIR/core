import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ToastContainerComponent } from './shared/components/toast-container/toast-container.component';
import { SignalRService } from './core/services/signalr.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ToastContainerComponent],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App implements OnInit {
  private signalR = inject(SignalRService);

  protected readonly title = signal('planora-portal');

  ngOnInit(): void {
    this.signalR.startConnection();
  }
}
