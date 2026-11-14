import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../../core/models/api-response.model';
import { ConfirmarPedidoVenta, CreatePedidoVenta, PedidoVenta, PedidoVentaFiltro, UpdatePedidoVenta } from './pedido-venta.model';

@Injectable({providedIn:'root'})
export class PedidoVentaService {
  private readonly url=`${environment.apiUrl}/pedidos-venta`;
  constructor(private http:HttpClient){}
  getPaged(f:PedidoVentaFiltro):Observable<ApiResponse<PagedResult<PedidoVenta>>>{
    let p=new HttpParams().set('page',Math.max(1,Math.trunc(f.page))).set('pageSize',Math.max(1,Math.min(100,Math.trunc(f.pageSize))));
    if(f.cotizacionId)p=p.set('cotizacionId',f.cotizacionId);
    if(f.clienteId)p=p.set('clienteId',f.clienteId);
    if(f.estado)p=p.set('estado',f.estado);
    if(f.fechaDesdeUtc)p=p.set('fechaDesdeUtc',f.fechaDesdeUtc);
    if(f.fechaHastaUtc)p=p.set('fechaHastaUtc',f.fechaHastaUtc);
    if(f.sortBy)p=p.set('sortBy',f.sortBy);
    if(f.sortDirection)p=p.set('sortDirection',f.sortDirection);
    return this.http.get<ApiResponse<PagedResult<PedidoVenta>>>(this.url,{params:p});
  }
  getById(id:number){ return this.http.get<ApiResponse<PedidoVenta>>(`${this.url}/${id}`); }
  create(v:CreatePedidoVenta,idempotencyKey:string){ const headers=new HttpHeaders().set('Idempotency-Key',idempotencyKey.trim()); return this.http.post<ApiResponse<PedidoVenta>>(this.url,v,{headers}); }
  update(id:number,v:UpdatePedidoVenta){ return this.http.put<ApiResponse<PedidoVenta>>(`${this.url}/${id}`,{...v,id}); }
  confirmar(id:number,v:ConfirmarPedidoVenta){ return this.http.post<ApiResponse<PedidoVenta>>(`${this.url}/${id}/confirmar`,v); }
  anular(id:number,motivo:string){ return this.http.post<ApiResponse<PedidoVenta>>(`${this.url}/${id}/anular`,{motivo:motivo.trim()}); }
}
