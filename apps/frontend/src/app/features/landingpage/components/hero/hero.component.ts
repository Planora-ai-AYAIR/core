import {
  Component,
  ElementRef,
  HostListener,
  ViewChild,
  AfterViewInit,
  OnDestroy,
  signal,
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { ButtonComponent } from '../../../../shared/components/button/button.component';
import { ROUTES } from '../../../../shared/config/constants';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-hero',
  standalone: true,
  imports: [CommonModule, ButtonComponent, RouterLink],
  templateUrl: './hero.component.html',
  styleUrls: ['./hero.component.css'],
})
export class HeroComponent implements AfterViewInit, OnDestroy {
  public ROUTES = ROUTES;

  targetX = 50;
  targetY = -20;
  targetZ = -35;

  rotateX = 50;
  rotateY = -20;
  rotateZ = -35;

  activeMetric = signal<string>('bearing');

  private isMouseOverScene = false;
  private animationFrameId: number | null = null;

  @ViewChild('sceneContainer') sceneContainer!: ElementRef<HTMLDivElement>;

  metrics = [
    {
      id: 'bearing',
      label: 'Bearing Capacity',
      value: '245 kPa',
      iconClass: 'pi-chart-bar', // PrimeIcon class
      iconColor: '#B86E3D', // planora-clay-600 hex
      color: 'text-planora-clay-600',
      description: 'Safe soil load capacity',
    },
    {
      id: 'flood',
      label: 'Flood Risk',
      value: 'Low',
      iconClass: '',
      iconColor: '#2563EB', // blue-600
      color: 'text-blue-600',
      description: '0.5m elevation above max flood',
    },
    {
      id: 'earthquake',
      label: 'Seismic Risk',
      value: 'Moderate',
      iconClass: 'pi-bolt',
      iconColor: '#D97706', // amber-600
      color: 'text-amber-600',
      description: 'Zone II (6.8 magnitude)',
    },
    {
      id: 'floors',
      label: 'Suitable Floors',
      value: '8-10',
      iconClass: 'pi-building',
      iconColor: '#2B2D31', // planora-basalt-700
      color: 'text-planora-basalt-700',
      description: 'Based on soil analysis',
    },
  ];

  ngAfterViewInit(): void {
    this.startInertiaRenderLoop();
  }

  @HostListener('document:mousemove', ['$event'])
  onMouseMove(event: MouseEvent) {
    if (!this.sceneContainer || !this.isMouseOverScene) return;

    const rect = this.sceneContainer.nativeElement.getBoundingClientRect();
    const centerX = rect.left + rect.width / 2;
    const centerY = rect.top + rect.height / 2;

    const percentX = (event.clientX - centerX) / (rect.width / 2);
    const percentY = (event.clientY - centerY) / (rect.height / 2);

    this.targetX = 50 - percentY * 10;
    this.targetY = -20 + percentX * 15;
    this.targetZ = -35 + percentX * 5;
  }

  onMetricHover(metricId: string) {
    this.activeMetric.set(metricId);
  }

  @HostListener('mouseenter')
  onSceneMouseEnter() {
    this.isMouseOverScene = true;
  }

  @HostListener('mouseleave')
  onSceneMouseLeave() {
    this.isMouseOverScene = false;
  }

  private startInertiaRenderLoop() {
    const step = () => {
      if (this.isMouseOverScene) {
        this.rotateX += (this.targetX - this.rotateX) * 0.08;
        this.rotateY += (this.targetY - this.rotateY) * 0.08;
        this.rotateZ += (this.targetZ - this.rotateZ) * 0.08;
      }
      this.animationFrameId = requestAnimationFrame(step);
    };
    this.animationFrameId = requestAnimationFrame(step);
  }

  ngOnDestroy() {
    if (this.animationFrameId) cancelAnimationFrame(this.animationFrameId);
  }
}
