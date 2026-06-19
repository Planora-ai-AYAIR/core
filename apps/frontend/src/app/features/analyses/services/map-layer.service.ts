import { Injectable, signal } from '@angular/core';
import { computed } from '@angular/core';
import { MapLayerItem } from '../interfaces/map-layer-item';
import maplibregl from 'maplibre-gl';

@Injectable({ providedIn: 'root' })
export class MapLayerService {
  private map = signal<maplibregl.Map | undefined>(undefined);
  private layers = signal<MapLayerItem[]>([]);
  private activeGroup = signal<string>('topography');

  readonly isMapReady = computed(() => !!this.map());

  layersByGroup = (group: string) => computed(() => this.layers().filter((l) => l.group === group));

  init(map: maplibregl.Map) {
    this.map.set(map);
  }

  registerLayers(newLayers: MapLayerItem[]) {
    this.layers.update((current) => {
      const existingIds = new Set(current.map((l) => l.id));
      const unique = newLayers.filter((l) => !existingIds.has(l.id));
      return [...current, ...unique];
    });

    const m = this.map();
    if (m && m.loaded()) {
      this.applyVisibility(m);
    } else if (m) {
      m.once('load', () => this.applyVisibility(m));
    }
  }

  setActiveGroup(group: string) {
    this.activeGroup.set(group);
    const m = this.map();
    if (m) this.applyVisibility(m);
  }

  toggleLayerVisibility(layerId: string) {
    this.layers.update((list) =>
      list.map((l) => (l.id === layerId ? { ...l, visible: !l.visible } : l)),
    );
    const m = this.map();
    if (m) this.applyVisibility(m);
  }

  setLayerOpacity(layerId: string, opacity: number) {
    this.layers.update((list) => list.map((l) => (l.id === layerId ? { ...l, opacity } : l)));
    const m = this.map();
    if (m && m.getLayer(layerId)) {
      this.applyOpacity(m, layerId, opacity);
    }
  }

  private applyVisibility(map: maplibregl.Map) {
    const active = this.activeGroup();
    this.layers().forEach((l) => {
      if (!map.getLayer(l.id)) return;
      const vis = l.group === active && l.visible ? 'visible' : 'none';
      map.setLayoutProperty(l.id, 'visibility', vis);
      if (l.linkedLayers) {
        l.linkedLayers.forEach((linkedId) => {
          if (map.getLayer(linkedId)) {
            map.setLayoutProperty(linkedId, 'visibility', vis);
          }
        });
      }
    });
  }

  private applyOpacity(map: maplibregl.Map, layerId: string, opacity: number) {
    const layer = this.layers().find((l) => l.id === layerId);
    if (!layer || !layer.setOpacity) {
      console.warn(`No setOpacity for layer ${layerId}`);
      return;
    }
    layer.setOpacity(map, opacity);
  }

  refreshVisibility() {
    const m = this.map();
    if (m) this.applyVisibility(m);
  }

  setLayerVisible(layerId: string, visible: boolean): void {
    const m = this.map();
    if (!m || !m.getLayer(layerId)) return;

    this.layers.update((list) => list.map((l) => (l.id === layerId ? { ...l, visible } : l)));

    const layoutVal = visible ? 'visible' : 'none';
    m.setLayoutProperty(layerId, 'visibility', layoutVal);
  }
}
