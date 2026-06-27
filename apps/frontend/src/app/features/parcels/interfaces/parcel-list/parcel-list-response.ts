export interface ParcelListResponse {
  id: string;
  name: string;
  areaHectares: number; 
  status: string;
  createdAt: string;
  centroidLatitude?: number;
  centroidLongitude?: number;
}
