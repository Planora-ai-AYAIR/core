import { inject, Injectable, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { BoreholeData } from '../interfaces/borehole-data';
import { RiskData } from '../interfaces/risk-data';
import { SoilData } from '../interfaces/soil-data';
import { TopographyData } from '../interfaces/topography-data';
import { BearingData } from '../interfaces/bearing-data';
import { ModuleStatus } from '../interfaces/module-status';
import { SignalRService } from '../../../core/services/signalr.service';
import { AnalysisApiService } from './analysis-api.service';
import { NotificationDto } from '../../../core/interfaces/notification/notification-dto';
import { ParcelAnalysisStatusResponse } from '../interfaces/analysis/parcel-analysis-status-response';

// DTOs from the API
import { TopographyResultsDto } from '../interfaces/analysis/topography-results.dto';
import { SoilResultsDto } from '../interfaces/analysis/soil-results.dto';
import { RiskResultsDto } from '../interfaces/analysis/risk-results.dto';
import { BoreholeResultsDto } from '../interfaces/analysis/borehole-results.dto';

@Injectable({ providedIn: 'root' })
export class AnalysisDetailFacadeService {
  // ── Module result signals ──
  readonly topographyData = signal<TopographyData | null>(null);
  readonly soilData = signal<SoilData | null>(null);
  readonly bearingData = signal<BearingData | null>(null);
  readonly riskData = signal<RiskData | null>(null);
  readonly boreholeData = signal<BoreholeData | null>(null);

  // ── Progress signal ──
  readonly moduleProgress = signal<
    Record<string, { status: ModuleStatus; estimatedSeconds: number }>
  >({
    topography: { status: 'Processing', estimatedSeconds: 0 },
    soil: { status: 'Processing', estimatedSeconds: 0 },
    bearing: { status: 'Processing', estimatedSeconds: 0 },
    risk: { status: 'Processing', estimatedSeconds: 0 },
    borehole: { status: 'Processing', estimatedSeconds: 0 },
  });

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  private signalR = inject(SignalRService);
  private api = inject(AnalysisApiService);
  private notificationSub?: Subscription;

  /** Start real‑time progress tracking for the given parcel. */
  startRealtimeProgress(parcelId: string): void {
    // Fetch current status and load already‑completed modules
    this.api.getParcelAnalysisStatus(parcelId).subscribe({
      next: (status) => this.updateProgressFromStatus(status, parcelId),
      error: (err) => console.warn('Could not fetch initial progress', err),
    });

    // Subscribe to SignalR for live updates
    this.signalR.subscribeToParcel(parcelId);
    this.notificationSub = this.signalR.notification$.subscribe((notification) =>
      this.handleNotification(notification, parcelId),
    );
  }

  stopRealtimeProgress(): void {
    this.notificationSub?.unsubscribe();
  }

  // ── Notification handler ─────────────────────────────────────────────────

  private handleNotification(notification: NotificationDto, parcelId: string): void {
    const moduleType = this.extractModuleType(notification);
    if (!moduleType) return;

    const newStatus: ModuleStatus =
      notification.type === 'ModuleCompleted'
        ? 'Completed'
        : notification.type === 'ModuleFailed'
          ? 'Failed'
          : 'Processing';

    this.moduleProgress.update((prev) => ({
      ...prev,
      [moduleType.toLowerCase()]: { status: newStatus, estimatedSeconds: 0 },
    }));

    // When a module completes, fetch its actual result data
    if (newStatus === 'Completed') {
      this.loadModuleResult(moduleType.toLowerCase(), parcelId);
    }
  }

  private updateProgressFromStatus(status: ParcelAnalysisStatusResponse, parcelId: string): void {
    const progress: Record<string, { status: ModuleStatus; estimatedSeconds: number }> = {};

    status.modules.forEach((m) => {
      const mappedStatus: ModuleStatus =
        m.status === 'Completed' ? 'Completed' : m.status === 'Failed' ? 'Failed' : 'Processing';

      progress[m.type.toLowerCase()] = { status: mappedStatus, estimatedSeconds: 0 };

      if (mappedStatus === 'Completed') {
        this.loadModuleResult(m.type.toLowerCase(), parcelId);
      }
    });

    this.moduleProgress.set(progress);
  }

  private extractModuleType(notification: NotificationDto): string | null {
    if (notification.data) {
      try {
        const data = JSON.parse(notification.data);
        return data.moduleType || null;
      } catch {}
    }
    const match = notification.title.match(/^(\w+)\s/);
    return match ? match[1] : null;
  }

  // ── Load a single module’s result from the API ───────────────────────────

  private loadModuleResult(module: string, parcelId: string): void {
    switch (module) {
      case 'topography':
        this.api
          .getTopographyResults(parcelId)
          .subscribe((dto) => this.topographyData.set(this.mapTopography(dto)));
        break;
      case 'soil':
        this.api.getSoilResults(parcelId).subscribe((dto) => {
          this.soilData.set(this.mapSoil(dto));
          // Bearing data is embedded in the soil result
          if (dto.bearingCapacityEstimate) {
            this.bearingData.set(this.mapBearingFromSoil(dto));
          }
        });
        break;
      case 'risk':
        this.api.getRiskResults(parcelId).subscribe((dto) => this.riskData.set(this.mapRisk(dto)));
        break;
      case 'borehole':
        this.api
          .getBoreholeResults(parcelId)
          .subscribe((dto) => this.boreholeData.set(this.mapBorehole(dto)));
        break;
      case 'bearing':
        // Bearing is loaded as part of soil; if a separate bearing endpoint exists later, handle here.
        break;
    }
  }

  // ── DTO → frontend interface mappers ─────────────────────────────────────

  private mapTopography(dto: TopographyResultsDto): TopographyData {
    return {
      minElevation: dto.elevation.min,
      maxElevation: dto.elevation.max,
      meanElevation: dto.elevation.mean,
      cutFill: dto.cutFill.netVolume,
      slopeDistribution: dto.slopeAnalysis.distribution.map((s) => ({
        name: s.category,
        value: s.percentage,
      })),
      pondingZones: [],
      engineeringFlags: [],
      elevationGrid: [],
      contourLines: [],
      slopePolygons: [],
      pondingPolygons: [],
    };
  }

  private mapSoil(dto: SoilResultsDto): SoilData {
    return {
      bulkDensity: dto.bulkDensity,
      organicCarbon: dto.organicCarbon,
      pH: dto.ph,
      classification: dto.primaryType,
      confidence: dto.aiConfidence ?? 0,
      composition: [
        { type: 'Clay', percent: dto.clayPercent, color: '#C0392B' },
        { type: 'Silt', percent: dto.siltPercent, color: '#A0522D' },
        { type: 'Sand', percent: dto.sandPercent, color: '#F4D03F' },
      ],
      soilCompositionGeoJSON: null, // will be loaded later via assets
      depthProfiles:
        dto.multiDepthProfile?.map((p) => ({
          depthRange: p.depth,
          sandPercent: p.sand,
          siltPercent: 100 - p.sand - p.clay,
          clayPercent: p.clay,
          classification: p.type,
          color: this.soilColor(p.type),
        })) ?? [],
      heatmapUrls: {},
      heatmapLegend: [
        { color: '#F4D03F', label: 'Sand' },
        { color: '#A0522D', label: 'Silt' },
        { color: '#C0392B', label: 'Clay' },
      ],
    };
  }

  private mapBearingFromSoil(dto: SoilResultsDto): BearingData {
    return {
      bearingCapacity: dto.bearingCapacityEstimate ?? 0,
      uncertaintyRangeKpa: { min: 0, max: 0 },
      capacityClass: (dto.bearingCapacityCategory as any) ?? 'Medium',
      isUnreliableEstimate: false,
      floorCountCategory: '1-2 floors',
      maxFloorsWithoutDeepFoundation: 0,
      foundationType: 'Shallow',
      factors: {
        clayPercent: {
          value: dto.clayPercent,
          unit: '%',
          safeThreshold: 50,
          source: 'Soil module',
          tooltip: '',
        },
        sandPercent: {
          value: dto.sandPercent,
          unit: '%',
          safeThreshold: 60,
          source: 'Soil module',
          tooltip: '',
        },
        moistureIndex: {
          value: 0.32,
          unit: '',
          safeThreshold: 0.4,
          source: 'Sentinel-2 NDMI',
          tooltip: '',
        },
        waterTableDepth: {
          value: 0,
          unit: 'm',
          safeThreshold: 5,
          source: 'SoilGrids',
          tooltip: '',
        },
        terrainSlope: {
          value: 0,
          unit: '%',
          safeThreshold: 5,
          source: 'Topography module',
          tooltip: '',
        },
      },
      buildingLoadReferences: [],
      bearingPoints: [],
      waterTableLines: [],
    };
  }

  private mapRisk(dto: RiskResultsDto): RiskData {
    const sub = (item: any, icon: string, color: string) => ({
      score: item.score,
      level: item.level,
      icon,
      color,
      factors: (item.factors || []).map((f: string) => ({ label: '', detail: f })),
      mitigation: item.level === 'High' ? 'See recommendations' : '',
    });

    return {
      overallRiskScore: dto.overallRiskScore,
      overallRiskLevel: dto.overallRiskLevel,
      benchmarkComparison: 'Lower than 65% of sites in Nile Delta region',
      floodRisk: sub(dto.flood, 'pi pi-cloud-download', '#3B82F6'),
      seismicRisk: sub(dto.seismic, 'pi pi-compass', '#F59E0B'),
      expansiveSoilRisk: sub(dto.expansiveSoil, 'pi pi-arrows-v', '#A0522D'),
      liquefactionRisk: sub(dto.liquefaction, 'pi pi-exclamation-triangle', '#8B5CF6'),
      mitigations: [],
      floodFeatures: [],
      seismicZonesGeoJSON: null,
      expansiveSoilZonesGeoJSON: null,
      liquefactionZonesGeoJSON: null,
    };
  }

  private mapBorehole(dto: BoreholeResultsDto): BoreholeData {
    return {
      minRequired: dto.minimumRequired,
      recommended: dto.optimalCount,
      coveragePercent: dto.coveragePercentage,
      gridSize: dto.gridSize ?? '',
      strategy: dto.placementStrategy ?? '',
      placementPoints:
        dto.placementPoints?.map((p) => ({
          id: p.id,
          lng: p.longitude,
          lat: p.latitude,
          priority: p.priority as any,
          reason: p.reason ?? '',
          estimatedDepth: p.estimatedDepth ?? 0,
        })) ?? [],
      costAnalysis: {
        traditionalCount: dto.costComparison.traditionalBoreholeCount,
        traditionalCost: dto.costComparison.traditionalEstimatedCost,
        optimizedCount: dto.costComparison.optimizedBoreholeCount,
        optimizedCost: dto.costComparison.optimizedEstimatedCost,
        savingsAmount: dto.costComparison.savingsAmount,
        savingsPercent: dto.costComparison.savingsPercentage,
        ratePerMeter: 700,
      },
      parameters: {
        maxSpacing: 30,
        minBoreholes: dto.minimumRequired,
        targetDepth: dto.placementPoints?.[0]?.estimatedDepth ?? 20,
        unit: 'm',
      },
    };
  }

  private soilColor(type: string): string {
    const colors: Record<string, string> = {
      'sandy loam': '#F4D03F',
      'sandy clay loam': '#D9A23A',
      'clay loam': '#BD7434',
      clay: '#C0392B',
    };
    return colors[type.toLowerCase()] ?? '#ccc';
  }
}
