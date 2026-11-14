import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import { ReporteInventarioFiltroBaseDto } from '../models/reporte-inventario.models';
export interface ReporteKardexFiltro extends ReporteInventarioFiltroBaseDto { productoId?:number;productoVarianteId?:number;tipo?:string;causa?:string;correlationId?:string;origenTipo?:string;origenId?:number; }
export interface MovimientoInventarioReporteDto { id:number;productoId:number;productoVarianteId:number|null;almacenId:number|null;ubicacionAlmacenId:number|null;productoNombre:string;productoColor:string|null;productoSku:string|null;tipo:string;causa:string;cantidad:number;stockAnterior:number;stockNuevo:number;costoUnitario:number|null;precioUnitario:number|null;correlationId:string;origenTipo:string|null;origenId:number|null;referenciaTipo:string;referenciaId:number;descripcion:string|null;creadoPorNombreUsuario:string|null;fecha:string; }
@Injectable({providedIn:'root'}) export class ReporteInventarioKardexService{private readonly endpoint=`${environment.apiUrl}/inventario/reportes/kardex`;constructor(private readonly http:HttpClient){}get(filtro:ReporteKardexFiltro):Observable<ApiResponse<PagedResult<MovimientoInventarioReporteDto>>>{let params=new HttpParams();Object.entries(filtro).forEach(([k,v])=>{if(v!==undefined&&v!==null&&v!=='')params=params.set(k,String(v));});return this.http.get<ApiResponse<PagedResult<MovimientoInventarioReporteDto>>>(this.endpoint,{params});}}
