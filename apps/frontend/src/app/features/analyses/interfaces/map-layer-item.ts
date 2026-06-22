export interface MapLayerItem {
  id: string;
  label: string;
  visible: boolean;
  opacity: number;
  group: string;
  hidden?: boolean;

  setOpacity?: (map: maplibregl.Map, opacity: number) => void;
  linkedLayers?: string[];
}
