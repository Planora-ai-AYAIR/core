export interface BoundaryPoint {
  longitude: number;
  latitude: number;
}

export interface ParcelDetailResponse {
  id: string;
  name: string;
  areaHectares: number;
  country: string | null;
  governorate: string | null;
  status: string;
  geojsonKey: string | null;
  createdAt: string;
  centroidLatitude: number;
  centroidLongitude: number;
  boundaryCoordinates: BoundaryPoint[];
}
