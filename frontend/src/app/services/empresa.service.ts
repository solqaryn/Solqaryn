import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';

export interface EmpresaResumen {
  id: number;
  nombre: string;
  activa: boolean;
  fechaCreacion: string;
  fechaActualizacion: string;
}

@Injectable({ providedIn: 'root' })
export class EmpresaService {
  private readonly apiUrl = `${environment.apiUrl}/empresas`;

  constructor(private http: HttpClient) {}

  getAll(activa?: boolean): Observable<ApiResponse<EmpresaResumen[]>> {
    const params: Record<string, string | number | boolean> = {};
    if (activa !== undefined) {
      params['activa'] = activa;
    }

    return this.http.get<ApiResponse<EmpresaResumen[]>>(this.apiUrl, { params });
  }
}
