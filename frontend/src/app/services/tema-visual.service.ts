import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../core/models/api-response.model';
import { TemaVisual } from '../core/models/tema-visual.model';

@Injectable({ providedIn: 'root' })
export class TemaVisualService {
  private readonly apiUrl = `${environment.apiUrl}/tema-visual`;

  constructor(private http: HttpClient) {}

  get(): Observable<ApiResponse<TemaVisual>> {
    return this.http.get<ApiResponse<TemaVisual>>(this.apiUrl);
  }

  update(tema: TemaVisual): Observable<ApiResponse<TemaVisual>> {
    return this.http.put<ApiResponse<TemaVisual>>(this.apiUrl, tema);
  }

  restaurar(): Observable<ApiResponse<TemaVisual>> {
    return this.http.post<ApiResponse<TemaVisual>>(`${this.apiUrl}/restaurar`, {});
  }
}
