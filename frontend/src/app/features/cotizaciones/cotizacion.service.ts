import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../../core/models/api-response.model';
import { Cotizacion, CotizacionFiltro, CreateCotizacion, UpdateCotizacion } from './cotizacion.model';
@Injectable({providedIn:'root'})
export class CotizacionService {
  private readonly url=`${environment.apiUrl}/cotizaciones`;
  constructor(private http:HttpClient){}
  getPaged(f:CotizacionFiltro):Observable<ApiResponse<PagedResult<Cotizacion>>>{ let p=new HttpParams().set('page',Math.max(1,Math.trunc(f.page))).set('pageSize',Math.max(1,Math.min(100,Math.trunc(f.pageSize)))); if(f.clienteId)p=p.set('clienteId',f.clienteId); if(f.estado)p=p.set('estado',f.estado); if(f.fechaDesdeUtc)p=p.set('fechaDesdeUtc',f.fechaDesdeUtc); if(f.fechaHastaUtc)p=p.set('fechaHastaUtc',f.fechaHastaUtc); if(f.sortBy)p=p.set('sortBy',f.sortBy); if(f.sortDirection)p=p.set('sortDirection',f.sortDirection); return this.http.get<ApiResponse<PagedResult<Cotizacion>>>(this.url,{params:p}); }
  getById(id:number){ return this.http.get<ApiResponse<Cotizacion>>(`${this.url}/${id}`); }
  create(v:CreateCotizacion){ return this.http.post<ApiResponse<Cotizacion>>(this.url,v); }
  update(id:number,v:UpdateCotizacion){ return this.http.put<ApiResponse<Cotizacion>>(`${this.url}/${id}`,v); }
  delete(id:number){ return this.http.delete<ApiResponse<object>>(`${this.url}/${id}`); }
  enviar(id:number){ return this.http.post<ApiResponse<Cotizacion>>(`${this.url}/${id}/enviar`,{}); }
  aceptar(id:number){ return this.http.post<ApiResponse<Cotizacion>>(`${this.url}/${id}/aceptar`,{}); }
  rechazar(id:number,motivo:string){ return this.http.post<ApiResponse<Cotizacion>>(`${this.url}/${id}/rechazar`,{motivo:motivo.trim()}); }
  convertir(id:number){ return this.http.post<ApiResponse<Cotizacion>>(`${this.url}/${id}/convertir`,{}); }
  duplicar(id:number){ return this.http.post<ApiResponse<Cotizacion>>(`${this.url}/${id}/duplicar`,{}); }
}
