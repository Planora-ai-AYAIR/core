import maplibregl from 'maplibre-gl';

export interface MapInitialiser<T = any> {
  addLayers(map: maplibregl.Map, data: T): void;
}
