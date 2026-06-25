import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import { CreateParcelResponse } from '../interfaces/parcel-new/create-parcel-response';
import { CreateParcelRequest } from '../interfaces/parcel-new/create-parcel-request';
import { ParcelListResponse } from '../interfaces/parcel-list/parcel-list-response';

@Injectable({ providedIn: 'root' })
export class ParcelApiService {
  private http = inject(HttpClient);
  private baseUrl = environment.apiUrl;

  createParcel(request: CreateParcelRequest): Observable<CreateParcelResponse> {
    return this.http
      .post<any>(`${this.baseUrl}${environment.Parcels.create}`, request)
      .pipe(map((envelope) => envelope.data));
  }

  getMyParcels(): Observable<ParcelListResponse[]> {
    return this.http
      .get<any>(`${this.baseUrl}${environment.Parcels.list}`)
      .pipe(map((envelope) => envelope.data));
  }
}
