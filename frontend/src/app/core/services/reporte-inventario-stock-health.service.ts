import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../models/api-response.model';
import { ReporteInventarioFiltroBaseDto } from '../models/reporte-inventario.models';
export type TipoReporteStockHealth = 1 | 2 | 3 | 4 | 5;
export interface ReporteStockHealthFiltro extends ReporteInventarioFiltroBaseDto { tipoReporte:TipoReporteStockHealth;productoId?:number;productoVarianteId?:number;dias:number; }
export interface ReporteStockHealthDto { productoVarianteId:number;productoId:number;productoNombre:string;sku:string|null;almacenId:number;almacenNombre:string;ubicacionAlmacenId:number|null;ubicacionNombre:string|null;stockDisponible:number;stockMinimo:number;stockBajo:boolean;agotado:boolean;ultimoMovimientoUtc:string|null;diasSinMovimiento:number;unidadesSalidaPeriodo:number;rotacionPeriodo:number; }
@Injectable({providedIn:'root'}) export class ReporteInventarioStockHealthService{private readonly endpoint=`${environment.apiUrl}/inventario/reportes/stock-health`;constructor(private readonly http:HttpClient){}get(filtro:ReporteStockHealthFiltro):Observable<ApiResponse<PagedResult<ReporteStockHealthDto>>>{let params=new HttpParams();Object.entries(filtro).forEach(([k,v])=>{if(v!==undefined&&v!==null&&v!=='')params=params.set(k,String(v));});return this.http.get<ApiResponse<PagedResult<ReporteStockHealthDto>>>(this.endpoint,{params});}}
