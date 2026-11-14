import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';

export interface EmpresaPlataforma {
  id: number;
  nombre: string;
  activa: boolean;
  usuariosActivos: number;
}

export interface DirectorioPlataforma {
  items: EmpresaPlataforma[];
  pagina: number;
  tamano: number;
  total: number;
  puedeCrear: boolean;
}

@Injectable({ providedIn: 'root' })
export class PlataformaEmpresasService {
  private readonly url = environment.apiUrl + '/plataforma/empresas';

  constructor(private readonly http: HttpClient) {}

  listar(pagina = 1, tamano = 25): Observable<DirectorioPlataforma> {
    const params = new HttpParams().set('pagina', pagina).set('tamano', tamano);
    return this.http.get<ApiResponse<DirectorioPlataforma>>(this.url, { params }).pipe(
      map(respuesta => {
        if (respuesta?.success !== true || !respuesta.data ||
            !Array.isArray(respuesta.data.items) ||
            !Number.isSafeInteger(respuesta.data.total) ||
            respuesta.data.total < 0 ||
            typeof respuesta.data.puedeCrear !== 'boolean' ||
            respuesta.data.items.some(e =>
              !Number.isSafeInteger(e.id) || e.id <= 0 ||
              typeof e.nombre !== 'string' ||
              typeof e.activa !== 'boolean' ||
              !Number.isSafeInteger(e.usuariosActivos) || e.usuariosActivos < 0)) {
          throw new Error('El servidor no entregó un directorio de plataforma válido.');
        }
        return respuesta.data;
      })
    );
  }
}
