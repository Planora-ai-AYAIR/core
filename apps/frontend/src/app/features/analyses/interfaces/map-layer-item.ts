export interface MapLayerItem {
  id: string;
  label: string;
  visible: boolean;
  opacity: number;
  group: string;
  
  setOpacity?: (map: maplibregl.Map, opacity: number) => void;
  linkedLayers?: string[];
}