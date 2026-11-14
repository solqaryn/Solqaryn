import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedRequest, PagedResult } from '../core/models/api-response.model';
import { CreateUsuarioValue, UpdateUsuarioValue, Usuario, UsuarioDetalle } from '../core/models/usuario.model';

export interface UsuarioEmpresa {
  id: number;
  usuarioId: number;
  empresaId: number;
  rolId: number;
  activa: boolean;
}

@Injectable({ providedIn: 'root' })
export class UsuarioService {
  private readonly apiUrl = `${environment.apiUrl}/usuarios`;

  constructor(private http: HttpClient) {}

  getAll(): Observable<ApiResponse<Usuario[]>> {
    return this.http.get<ApiResponse<Usuario[]>>(this.apiUrl);
  }

  getPaged(request: Partial<PagedRequest>): Observable<ApiResponse<PagedResult<Usuario>>> {
    return this.http.get<ApiResponse<PagedResult<Usuario>>>(`${this.apiUrl}/paginado`, { params: request as any });
  }

  getById(id: number): Observable<ApiResponse<UsuarioDetalle>> {
    return this.http.get<ApiResponse<UsuarioDetalle>>(`${this.apiUrl}/${id}`);
  }

  create(value: CreateUsuarioValue): Observable<ApiResponse<Usuario>> {
    return this.http.post<ApiResponse<Usuario>>(this.apiUrl, value);
  }

  update(id: number, value: UpdateUsuarioValue): Observable<ApiResponse<Usuario>> {
    return this.http.put<ApiResponse<Usuario>>(`${this.apiUrl}/${id}`, value);
  }

  updateEstado(id: number, activo: boolean): Observable<ApiResponse<Usuario>> {
    return this.http.put<ApiResponse<Usuario>>(`${this.apiUrl}/${id}/estado`, { activo });
  }

  bloquear(id: number, motivo: string): Observable<ApiResponse<Usuario>> {
    return this.http.put<ApiResponse<Usuario>>(`${this.apiUrl}/${id}/bloquear`, { motivo });
  }

  desbloquear(id: number): Observable<ApiResponse<Usuario>> {
    return this.http.put<ApiResponse<Usuario>>(`${this.apiUrl}/${id}/desbloquear`, {});
  }

  eliminar(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}`);
  }

  getEmpresas(id: number): Observable<ApiResponse<UsuarioEmpresa[]>> {
    return this.http.get<ApiResponse<UsuarioEmpresa[]>>(`${this.apiUrl}/${id}/empresas`);
  }

  asignarEmpresa(id: number, empresaId: number, rolId: number): Observable<ApiResponse<UsuarioEmpresa>> {
    return this.http.post<ApiResponse<UsuarioEmpresa>>(`${this.apiUrl}/${id}/empresas`, { empresaId, rolId });
  }

  cambiarRolEmpresa(id: number, empresaId: number, rolId: number): Observable<ApiResponse<UsuarioEmpresa>> {
    return this.http.put<ApiResponse<UsuarioEmpresa>>(`${this.apiUrl}/${id}/empresas/${empresaId}/rol`, { rolId });
  }

  cambiarEstadoEmpresa(id: number, empresaId: number, activa: boolean): Observable<ApiResponse<UsuarioEmpresa>> {
    return this.http.put<ApiResponse<UsuarioEmpresa>>(`${this.apiUrl}/${id}/empresas/${empresaId}/estado`, { activa });
  }
}
