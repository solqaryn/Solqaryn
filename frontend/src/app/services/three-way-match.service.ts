import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { ThreeWayMatchResultDto } from '../core/models/three-way-match.model';

@Injectable({
  providedIn: 'root'
})
export class ThreeWayMatchService {
  constructor(private http: HttpClient) {}

  getThreeWayMatchResult(ordenCompraId: number): Observable<ApiResponse<ThreeWayMatchResultDto>> {
    return this.http.get<ApiResponse<ThreeWayMatchResultDto>>(`${environment.apiUrl}/conciliacion/ordenes-compra/${ordenCompraId}/three-way-match`);
  }
}
