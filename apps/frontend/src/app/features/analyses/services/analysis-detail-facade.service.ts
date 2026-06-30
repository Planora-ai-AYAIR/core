import { inject, Injectable, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { BoreholeData } from '../interfaces/borehole-data';
import { RiskData } from '../interfaces/risk-data';
import { SoilData } from '../interfaces/soil-data';
import { TopographyData } from '../interfaces/topography-data';
import { BearingData } from '../interfaces/bearing-data';
import { ModuleStatus } from '../interfaces/module-status';
import { NotificationDto } from '../../../core/interfaces/notification/notification-dto';
import { ParcelAnalysisStatusResponse } from '../interfaces/analysis/parcel-analysis-status-response';
import { SignalRService } from '../../../core/services/signalr.service';
import { AnalysisResultEnvelope } from '../interfaces/analysis/analysis-result-envelope';
import { ParcelAnalysisFullResponse } from '../interfaces/analysis/analysis-full-response';
import { AnalysisApiService } from './analysis-api.service';

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
  private analysisResultSub?: Subscription;

  /** Start real‑time progress tracking for the given parcel. */
  async startRealtimeProgress(parcelId: string) {
    this.loading.set(true);

    try {
      await this.signalR.startConnection();
    } catch (_) {
      this.error.set('Unable to establish real‑time connection');
      this.loading.set(false);
      return;
    }

    // 1. Try to get already-completed analysis via REST
    this.api.getFullAnalysis(parcelId).subscribe({
      next: (data) => {
        this.populateFromFullResponse(data);
        this.loading.set(false);
      },
      error: (err) => {
        if (err.status === 409) {
          this.subscribeToSignalR(parcelId);
          this.api
            .getParcelAnalysisStatus(parcelId)
            .subscribe((s) => this.updateProgressFromStatus(s));
        } else {
          this.error.set('Failed to load analysis');
        }
        this.loading.set(false);
      },
    });
  }

  private subscribeToSignalR(parcelId: string) {
    this.signalR.subscribeToParcel(parcelId);
    this.analysisResultSub = this.signalR.analysisResult$.subscribe((envelope) =>
      this.handleAnalysisResult(envelope, parcelId),
    );
  }

  private populateFromFullResponse(data: ParcelAnalysisFullResponse) {
    const result = data.result;
    if (result?.topography) {
      this.topographyData.set(this.mapTopographyFromFull(result.topography));
    }
    if (result?.soil) {
      this.soilData.set(this.mapSoilFromFull(result.soil));
      if (result.bearing) {
        this.bearingData.set(this.mapBearingFromFull(result.bearing));
      }
    }
    if (result?.risk) {
      this.riskData.set(this.mapRiskFromFull(result.risk));
    }
    if (result?.borehole) {
      this.boreholeData.set(this.mapBoreholeFromFull(result.borehole));
    }

    // Mark all returned modules as completed
    this.moduleProgress.update((prev) => ({
      ...prev,
      ...(result.topography
        ? { ['topography']: { status: 'Completed' as ModuleStatus, estimatedSeconds: 0 } }
        : {}),
      ...(result.soil ? { ['soil']: { status: 'Completed', estimatedSeconds: 0 } } : {}),
      ...(result.bearing ? { ['bearing']: { status: 'Completed', estimatedSeconds: 0 } } : {}),
      ...(result.risk ? { ['risk']: { status: 'Completed', estimatedSeconds: 0 } } : {}),
      ...(result.borehole ? { ['borehole']: { status: 'Completed', estimatedSeconds: 0 } } : {}),
    }));
  }

  stopRealtimeProgress(): void {
    this.notificationSub?.unsubscribe();
    this.analysisResultSub?.unsubscribe();
  }

  // ── SignalR event handler (per‑module & aggregated) ────────────────
  private handleAnalysisResult(raw: any, parcelId: string): void {
    // Normalise case – backend may send camelCase
    const eventType = raw.eventType || raw.EventType || '';
    const envelopeParcelId = raw.parcelId || raw.ParcelId || '';
    if (envelopeParcelId !== parcelId) return;

    // Unwrap the double‑result structure
    const outerResult = raw.result || raw.Result;
    if (!outerResult) return;
    // The actual module data is inside outerResult.result
    const innerResult = outerResult.result;
    if (!innerResult) return;

    // Normalise the event type to our internal constant
    const isAnalysisCompleted =
      eventType === 'analysis.completed' || eventType === 'AnalysisCompleted';

    if (eventType === 'TopographyCompleted' || isAnalysisCompleted) {
      const topo = innerResult.topography;
      if (topo) {
        this.topographyData.set(this.mapTopographyFromFull(topo)); // ✅ use FromFull
        this.moduleProgress.update((prev) => ({
          ...prev,
          topography: { status: 'Completed', estimatedSeconds: 0 },
        }));
      }
    }

    if (eventType === 'SoilCompleted' || isAnalysisCompleted) {
      const soil = innerResult.soil;
      if (soil) {
        this.soilData.set(this.mapSoilFromFull(soil)); // ✅ use FromFull
        this.moduleProgress.update((prev) => ({
          ...prev,
          soil: { status: 'Completed', estimatedSeconds: 0 },
        }));
      }
    }

    // ✅ NEW: Bearing is a top‑level module, not embedded in soil
    if (eventType === 'BearingCompleted' || isAnalysisCompleted) {
      const bearing = innerResult.bearing;
      if (bearing) {
        this.bearingData.set(this.mapBearingFromFull(bearing)); // ✅ use FromFull
        this.moduleProgress.update((prev) => ({
          ...prev,
          bearing: { status: 'Completed', estimatedSeconds: 0 },
        }));
      }
    }

    if (eventType === 'RiskCompleted' || isAnalysisCompleted) {
      const risk = innerResult.risk;
      if (risk) {
        this.riskData.set(this.mapRiskFromFull(risk)); // ✅ use FromFull
        this.moduleProgress.update((prev) => ({
          ...prev,
          risk: { status: 'Completed', estimatedSeconds: 0 },
        }));
      }
    }

    if (eventType === 'BoreholeCompleted' || isAnalysisCompleted) {
      const bore = innerResult.borehole;
      if (bore) {
        this.boreholeData.set(this.mapBoreholeFromFull(bore)); // ✅ use FromFull
        this.moduleProgress.update((prev) => ({
          ...prev,
          borehole: { status: 'Completed', estimatedSeconds: 0 },
        }));
      }
    }
  }

  // ── Notification handler ───────────────────────────────────────────
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
  }

  private updateProgressFromStatus(status: ParcelAnalysisStatusResponse): void {
    const progress = { ...this.moduleProgress() };
    status.modules.forEach((m) => {
      const mappedStatus: ModuleStatus =
        m.status === 'Completed' ? 'Completed' : m.status === 'Failed' ? 'Failed' : 'Processing';
      progress[m.type.toLowerCase()] = { status: mappedStatus, estimatedSeconds: 0 };
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

  // ═══════════════════════════════════════════════════════════════════
  // Mappers: Raw SignalR payload → frontend models
  // (Field names match the Python webhook output you confirmed)
  // ═══════════════════════════════════════════════════════════════════

  private mapTopographyFromSignalR(payload: any): TopographyData {
    const slopeDist = payload.slopeDistribution || [];
    const zonesCount = payload.pondingZonesCount ?? 0;
    const totalArea = payload.pondingTotalArea ?? 0;

    // Derive ponding risk level
    let riskLevel = 'Low';
    if (zonesCount > 0 && totalArea > 10000) {
      riskLevel = 'High';
    } else if (zonesCount > 0 && totalArea > 5000) {
      riskLevel = 'Medium';
    }

    return {
      minElevation: payload.elevationMin ?? 0,
      maxElevation: payload.elevationMax ?? 0,
      meanElevation: payload.elevationMean ?? 0,
      cutFill: payload.netVolume ?? 0,
      slopeDistribution: slopeDist.map((s: any) => ({
        name: s.range || s.category,
        value: s.percentage,
      })),
      // ── Added ponding risk summary ──
      pondingRisk: {
        riskLevel: riskLevel,
        zonesCount: zonesCount,
        affectedAreaM2: totalArea,
      },
      // ── Still kept for future visual assets ──
      pondingZones: [],
      engineeringFlags: [],
      elevationGrid: [],
      contourLines: [],
      slopePolygons: [],
      pondingPolygons: [],
    };
  }

  private mapSoilFromSignalR(payload: any): SoilData {
    const depthProfiles = payload.depthProfiles || [];
    return {
      bulkDensity: payload.bulkDensity ?? 0,
      organicCarbon: payload.organicCarbon ?? 0,
      pH: payload.ph ?? 0,
      classification: payload.primaryType ?? '',
      confidence: payload.aiConfidence ?? 0,
      composition: [
        { type: 'Clay', percent: payload.clayPercent ?? 0, color: '#C0392B' },
        { type: 'Silt', percent: payload.siltPercent ?? 0, color: '#A0522D' },
        { type: 'Sand', percent: payload.sandPercent ?? 0, color: '#F4D03F' },
      ],
      soilCompositionGeoJSON: null,
      depthProfiles: depthProfiles.map((p: any) => ({
        depthRange: p.depth,
        sandPercent: p.sand,
        siltPercent: 100 - p.sand - p.clay,
        clayPercent: p.clay,
        classification: p.type,
        color: this.soilColor(p.type),
      })),
      heatmapUrls: {},
      heatmapLegend: [
        { color: '#F4D03F', label: 'Sand' },
        { color: '#A0522D', label: 'Silt' },
        { color: '#C0392B', label: 'Clay' },
      ],
      spectralIndices: payload?.spectralIndices ?? undefined,
    };
  }

  private mapBearingFromSignalR(payload: any): BearingData {
    const bearing = payload.bearing || {};
    return {
      bearingCapacity: payload.bearingCapacityEstimate ?? 0,
      uncertaintyRangeKpa: {
        min: bearing.uncertaintyRange?.minimumKpa ?? 0,
        max: bearing.uncertaintyRange?.maximumKpa ?? 0,
      },
      capacityClass: payload.bearingCapacityCategory ?? 'Medium',
      isUnreliableEstimate: false,
      floorCountCategory: bearing.floorCountCategory ?? '1-2 floors',
      maxFloorsWithoutDeepFoundation: bearing.maxFloorsWithoutDeepFoundation ?? 0,
      foundationType: bearing.recommendedFoundation ?? 'Shallow',
      factors: {
        clayPercent: {
          value: payload.clayPercent,
          unit: '%',
          safeThreshold: 50,
          source: 'Soil module',
          tooltip: '',
        },
        sandPercent: {
          value: payload.sandPercent,
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
          value: payload.waterTableDepthMeters ?? 0,
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
      trafficLight: bearing.trafficLight ?? undefined,
      range: bearing.range ?? undefined,
      confidence: bearing.confidence ?? undefined,
      disclaimer: bearing.disclaimer ?? undefined,
      modelMetadata: bearing.modelMetadata
        ? {
            modelName: bearing.modelMetadata.modelName ?? '',
            framework: bearing.modelMetadata.framework ?? '',
            trainingR2: bearing.modelMetadata.trainingR2 ?? 0,
            shapEnabled: bearing.modelMetadata.shapEnabled ?? false,
          }
        : undefined,

      featureImportance:
        bearing?.featureImportance?.map((f: any) => ({
          feature: f.feature,
          weight: f.weight,
        })) ?? undefined,
    };
  }

  private mapRiskFromSignalR(payload: any): RiskData {
    const sub = (item: any, icon: string, color: string) => ({
      score: item?.score ?? 0,
      level: item?.level ?? '',
      icon,
      color,
      factors: (item?.factors || []).map((f: string) => ({ label: '', detail: f })),
      mitigation: item?.level === 'High' ? 'See recommendations' : '',
    });

    return {
      overallRiskScore: payload.overallRiskScore ?? 0,
      overallRiskLevel: payload.overallRiskLevel ?? '',
      benchmarkComparison: 'Lower than 65% of sites in Nile Delta region',
      floodRisk: sub(payload.flood, 'pi pi-cloud-download', '#3B82F6'),
      seismicRisk: sub(payload.seismic, 'pi pi-compass', '#F59E0B'),
      expansiveSoilRisk: sub(payload.expansiveSoil, 'pi pi-arrows-v', '#A0522D'),
      liquefactionRisk: sub(payload.liquefaction, 'pi pi-exclamation-triangle', '#8B5CF6'),
      mitigations:
        payload?.mitigationSuggestions?.map((m: any) => ({
          riskType: m.riskType ?? '',
          suggestion: m.suggestion ?? '',
          costImpact: m.costImpact ?? '',
          feasibility: m.feasibility ?? '',
        })) ?? [],
      floodFeatures: [],
      seismicZonesGeoJSON: null,
      expansiveSoilZonesGeoJSON: null,
      liquefactionZonesGeoJSON: null,
    };
  }

  private mapBoreholeFromSignalR(payload: any): BoreholeData {
    return {
      minRequired: payload.minimumRequired ?? 0,
      recommended: payload.optimalCount ?? 0,
      coveragePercent: payload.coveragePercentage ?? 0,
      gridSize: payload.gridSize ?? '',
      strategy: payload.placementStrategy ?? '',
      placementPoints: (payload.placementPoints || []).map((p: any) => ({
        id: p.id,
        lng: p.longitude,
        lat: p.latitude,
        priority: p.priority as any,
        reason: p.reason ?? '',
        estimatedDepth: p.estimatedDepth ?? 0,
      })),
      costAnalysis: {
        traditionalCount: payload.traditionalBoreholeCount ?? 0,
        traditionalCost: payload.traditionalEstimatedCost ?? 0,
        optimizedCount: payload.optimizedBoreholeCount ?? 0,
        optimizedCost: payload.optimizedEstimatedCost ?? 0,
        savingsAmount: payload.savingsAmount ?? 0,
        savingsPercent: payload.savingsPercentage ?? 0,
        ratePerMeter: 700,
      },
      parameters: {
        maxSpacing: 30,
        minBoreholes: payload.minimumRequired ?? 0,
        targetDepth: payload.placementPoints?.[0]?.estimatedDepth ?? 20,
        unit: 'm',
      },
    };
  }

  // ═══════════════════════════════════════════════════════════════════
  // Mappers: Full REST response → frontend models
  // (Used when loading an already‑completed analysis)
  // ═══════════════════════════════════════════════════════════════════

  private mapTopographyFromFull(dto: any): TopographyData {
    const elevation = dto?.elevation ?? {};
    const slope = dto?.slopeDistribution ?? [];
    const cutFill = dto?.cutFillAnalysis ?? {};
    const ponding = dto?.pondingRisk ?? {};
    return {
      minElevation: elevation.minimumMeters ?? 0,
      maxElevation: elevation.maximumMeters ?? 0,
      meanElevation: elevation.averageMeters ?? 0,
      cutFill: cutFill.netVolumeM3 ?? 0,
      slopeDistribution: slope.map((s: any) => ({
        name: s.range,
        value: s.percentage,
      })),
      pondingRisk: {
        riskLevel: ponding.riskLevel ?? 'Unknown',
        zonesCount: ponding.zonesCount ?? 0,
        affectedAreaM2: ponding.affectedAreaM2 ?? 0,
      },
      pondingZones: [],
      engineeringFlags: [],
      elevationGrid: [],
      contourLines: [],
      slopePolygons: [],
      pondingPolygons: [],
    };
  }

  private mapSoilFromFull(dto: any): SoilData {
    const classification = dto?.classification ?? {};
    const surface = dto?.surfaceComposition ?? {};
    const properties = dto?.properties ?? {};
    const depthLayers = dto?.depthLayers ?? [];
    return {
      bulkDensity: properties.bulkDensity ?? 0,
      organicCarbon: properties.organicCarbonPercentage ?? 0,
      pH: properties.ph ?? 0,
      classification: classification.primaryType ?? '',
      confidence: classification.aiConfidence ?? 0,
      composition: [
        { type: 'Clay', percent: surface.clayPercentage ?? 0, color: '#C0392B' },
        { type: 'Silt', percent: surface.siltPercentage ?? 0, color: '#A0522D' },
        { type: 'Sand', percent: surface.sandPercentage ?? 0, color: '#F4D03F' },
      ],
      soilCompositionGeoJSON: null,
      depthProfiles: depthLayers.map((l: any) => ({
        depthRange: l.depth,
        sandPercent: l.sand,
        siltPercent: l.silt,
        clayPercent: l.clay,
        classification: l.soilType,
        color: this.soilColor(l.soilType),
      })),
      heatmapUrls: {},
      heatmapLegend: [
        { color: '#F4D03F', label: 'Sand' },
        { color: '#A0522D', label: 'Silt' },
        { color: '#C0392B', label: 'Clay' },
      ],
      spectralIndices: dto?.spectralIndices ?? undefined,
    };
  }

  private mapBearingFromFull(dto: any): BearingData {
    return {
      bearingCapacity: dto?.bearingCapacityKpa ?? 0,
      uncertaintyRangeKpa: {
        min: dto?.uncertaintyRange?.minimumKpa ?? 0,
        max: dto?.uncertaintyRange?.maximumKpa ?? 0,
      },
      capacityClass: dto?.classification ?? 'Medium',
      isUnreliableEstimate: false,
      floorCountCategory: dto?.floorCountCategory ?? '1-2 floors',
      maxFloorsWithoutDeepFoundation: dto?.maxFloorsWithoutDeepFoundation ?? 0,
      foundationType: dto?.recommendedFoundation ?? 'Shallow',
      factors: {
        clayPercent: {
          value: dto?.soilFactors?.clayContent ?? 0,
          unit: '%',
          safeThreshold: 50,
          source: 'Soil module',
          tooltip: '',
        },
        sandPercent: {
          value: dto?.soilFactors?.sandContent ?? 0,
          unit: '%',
          safeThreshold: 60,
          source: 'Soil module',
          tooltip: '',
        },
        moistureIndex: {
          value: dto?.soilFactors?.moistureIndex ?? 0,
          unit: '',
          safeThreshold: 0.4,
          source: 'Sentinel-2 NDMI',
          tooltip: '',
        },
        waterTableDepth: {
          value: dto?.soilFactors?.depthToWaterTableMeters ?? 0,
          unit: 'm',
          safeThreshold: 5,
          source: 'SoilGrids',
          tooltip: '',
        },
        terrainSlope: {
          value: dto?.soilFactors?.terrainSlopePercent ?? 0,
          unit: '%',
          safeThreshold: 5,
          source: 'Topography module',
          tooltip: '',
        },
      },
      buildingLoadReferences: [],
      bearingPoints: [],
      waterTableLines: [],
      trafficLight: dto?.trafficLight ?? undefined,
      range: dto?.range ?? undefined,
      confidence: dto?.confidence ?? undefined,
      disclaimer: dto?.disclaimer ?? undefined,
      modelMetadata: dto?.modelMetadata
        ? {
            modelName: dto.modelMetadata.modelName ?? '',
            framework: dto.modelMetadata.framework ?? '',
            trainingR2: dto.modelMetadata.trainingR2 ?? 0,
            shapEnabled: dto.modelMetadata.shapEnabled ?? false,
          }
        : undefined,
      featureImportance:
        dto?.featureImportance?.map((f: any) => ({
          feature: f.feature,
          weight: f.weight,
        })) ?? undefined,
    };
  }

  private mapRiskFromFull(dto: any): RiskData {
    const sub = (item: any, icon: string, color: string) => ({
      score: item?.score ?? 0,
      level: item?.level ?? '',
      icon,
      color,
      factors: (item?.factors || []).map((f: string) => ({ label: '', detail: f })),
      mitigation: item?.level === 'High' ? 'See recommendations' : '',
    });

    return {
      overallRiskScore: dto?.overallScore ?? 0,
      overallRiskLevel: dto?.overallRiskLevel ?? '',
      benchmarkComparison: 'Lower than 65% of sites in Nile Delta region', // static for now
      floodRisk: sub(dto?.riskBreakdown?.flood, 'pi pi-cloud-download', '#3B82F6'),
      seismicRisk: sub(dto?.riskBreakdown?.seismic, 'pi pi-compass', '#F59E0B'),
      expansiveSoilRisk: sub(dto?.riskBreakdown?.expansiveSoil, 'pi pi-arrows-v', '#A0522D'),
      liquefactionRisk: sub(
        dto?.riskBreakdown?.liquefaction,
        'pi pi-exclamation-triangle',
        '#8B5CF6',
      ),
      // 👇 Mitigation suggestions from the API
      mitigations:
        dto?.mitigationSuggestions?.map((m: any) => ({
          riskType: m.riskType ?? '',
          suggestion: m.suggestion ?? '',
          costImpact: m.costImpact ?? '',
          feasibility: m.feasibility ?? '',
        })) ?? [],
      // Map visual layers (empty until GeoJSON available)
      floodFeatures: [],
      seismicZonesGeoJSON: null,
      expansiveSoilZonesGeoJSON: null,
      liquefactionZonesGeoJSON: null,
    };
  }

  private mapBoreholeFromFull(dto: any): BoreholeData {
    const rec = dto?.recommendation ?? {};
    const cost = dto?.costAnalysis ?? {};
    return {
      minRequired: rec.minimumRequired ?? 0,
      recommended: rec.optimalCount ?? 0,
      coveragePercent: rec.coveragePercentage ?? 0,
      gridSize: rec.gridSize ?? '',
      strategy: rec.strategy ?? '',
      placementPoints: (dto?.placementPoints || []).map((p: any) => ({
        id: p.id,
        lng: p.longitude,
        lat: p.latitude,
        priority: p.priority as any,
        reason: p.reason ?? '',
        estimatedDepth: p.estimatedDepthMeters ?? 0,
      })),
      costAnalysis: {
        traditionalCount: cost.traditionalApproach?.boreholes ?? 0,
        traditionalCost: cost.traditionalApproach?.estimatedCost ?? 0,
        optimizedCount: cost.optimizedApproach?.boreholes ?? 0,
        optimizedCost: cost.optimizedApproach?.estimatedCost ?? 0,
        savingsAmount: cost.savings?.amount ?? 0,
        savingsPercent: cost.savings?.percentage ?? 0,
        ratePerMeter: 700,
      },
      parameters: {
        maxSpacing: 30,
        minBoreholes: rec.minimumRequired ?? 0,
        targetDepth: dto?.placementPoints?.[0]?.estimatedDepthMeters ?? 20,
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
