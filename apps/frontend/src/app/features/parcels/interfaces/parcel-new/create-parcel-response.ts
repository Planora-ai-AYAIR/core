export interface CreateParcelResponse {
  parcelId: string;
  name: string;
  boundingBox: {
    minX: number;
    minY: number;
    maxX: number;
    maxY: number;
  };
  bufferedBoundingBox: {
    minX: number;
    minY: number;
    maxX: number;
    maxY: number;
  };
  area: number;
  createdAt: string;
}
