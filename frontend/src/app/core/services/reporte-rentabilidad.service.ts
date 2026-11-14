import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../models/api-response.model';
import { ReporteVentasFiltroDto } from '../models/reporte-ventas.models';
import { RentabilidadAgrupacion, ReporteRentabilidadDto } from '../models/reporte-rentabilidad.models';

@Injectable({ providedIn: 'root' })
export class ReporteRentabilidadService {
  private readonly endpoint = `${environment.apiUrl}/ventas/rentabilidad`;

  constructor(private readonly http: HttpClient) {}

  obtener(
    agrupacion: RentabilidadAgrupacion,
    filtro: ReporteVentasFiltroDto,
  ): Observable<ApiResponse<ReporteRentabilidadDto[]>> {
    return this.http.get<ApiResponse<ReporteRentabilidadDto[]>>(
      `${this.endpoint}/${this.endpointPorAgrupacion(agrupacion)}`,
      { params: this.toParams(filtro) },
    );
  }

  getVendedores(filtro: ReporteVentasFiltroDto): Observable<ApiResponse<ReporteRentabilidadDto[]>> {
    return this.obtener('vendedor', filtro);
  }

  getClientes(filtro: ReporteVentasFiltroDto): Observable<ApiResponse<ReporteRentabilidadDto[]>> {
    return this.obtener('cliente', filtro);
  }

  getProductos(filtro: ReporteVentasFiltroDto): Observable<ApiResponse<ReporteRentabilidadDto[]>> {
    return this.obtener('producto', filtro);
  }

  getCategorias(filtro: ReporteVentasFiltroDto): Observable<ApiResponse<ReporteRentabilidadDto[]>> {
    return this.obtener('categoria', filtro);
  }

  private endpointPorAgrupacion(agrupacion: RentabilidadAgrupacion): string {
    switch (agrupacion) {
      case 'vendedor': return 'vendedores';
      case 'cliente': return 'clientes';
      case 'producto': return 'productos';
      case 'categoria': return 'categorias';
    }
  }

  private toParams(filtro: ReporteVentasFiltroDto): HttpParams {
    let params = new HttpParams();
    Object.entries(filtro).forEach(([key, value]) => {
      if (value !== undefined && value !== null && value !== '') {
        params = params.set(key, String(value));
      }
    });
    return params;
  }
}
