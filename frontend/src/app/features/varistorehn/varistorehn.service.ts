import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { EMPTY, Observable, expand, map, of, reduce, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ApiResponse, PagedResult } from '../../core/models/api-response.model';
import {
  CategoriaCatalogoPublico,
  CheckoutItemRequest,
  CheckoutTarjetaRequest,
  CheckoutTarjetaResponse,
  CheckoutValidado,
  ProductoCatalogoPublico
} from './varistorehn.models';

export type {
  CategoriaCatalogoPublico,
  ModeloCatalogoPublico,
  ProductoCatalogoPublico
} from './varistorehn.models';

export interface ConsultaProductosPublicos {
  search?: string;
  categoriaId?: number | null;
  agotado?: boolean | null;
  esDestacado?: boolean | null;
  sortBy?: 'Nombre' | 'Precio' | 'FechaCreacion';
  sortDirection?: 'asc' | 'desc';
}

@Injectable({ providedIn: 'root' })
export class VaristorehnService {
  private readonly http = inject(HttpClient);
  private readonly urlTienda = `${environment.apiUrl}/tienda`;
  private readonly urlProductos = `${this.urlTienda}/productos`;
  private readonly urlDestacados = `${this.urlProductos}/destacados`;
  private readonly urlCategorias = `${this.urlTienda}/categorias`;
  private readonly urlValidarCheckout = `${this.urlTienda}/checkout/validar`;

  obtenerProductos(
    page = 1,
    pageSize = 48,
    consulta: ConsultaProductosPublicos = {}
  ): Observable<ApiResponse<PagedResult<ProductoCatalogoPublico>>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    const search = consulta.search?.trim();
    if (search) params = params.set('search', search);
    if (Number.isSafeInteger(consulta.categoriaId) && (consulta.categoriaId ?? 0) > 0) {
      params = params.set('categoriaId', consulta.categoriaId!);
    }
    if (typeof consulta.agotado === 'boolean') params = params.set('agotado', consulta.agotado);
    if (typeof consulta.esDestacado === 'boolean') params = params.set('esDestacado', consulta.esDestacado);
    if (consulta.sortBy) params = params.set('sortBy', consulta.sortBy);
    if (consulta.sortDirection) params = params.set('sortDirection', consulta.sortDirection);
    return this.http.get<ApiResponse<PagedResult<ProductoCatalogoPublico>>>(this.urlProductos, { params });
  }

  obtenerProductosContexto(productoIds: number[]): Observable<ProductoCatalogoPublico[]> {
    const ids = [...new Set(productoIds.filter(id => Number.isSafeInteger(id) && id > 0))].slice(0, 100);
    if (!ids.length) return of([]);
    return this.http.post<ApiResponse<ProductoCatalogoPublico[]>>(
      `${this.urlProductos}/contexto`,
      { productoIds: ids }
    ).pipe(map(res => {
      if (!res.success || !Array.isArray(res.data)) throw new Error('Respuesta de contexto de productos no válida.');
      return res.data;
    }));
  }

  /** Lee todas las paginas para que busqueda/categorias no se corten silenciosamente. */
  obtenerCatalogo(): Observable<ProductoCatalogoPublico[]> {
    const leer = (pagina: number) => this.obtenerProductos(pagina, 96).pipe(map(res => {
      const datos = res.data;
      if (!res.success || !datos || !Array.isArray(datos.items) || datos.page !== pagina
        || !Number.isSafeInteger(datos.totalCount) || datos.totalCount < 0
        || !Number.isSafeInteger(datos.pageSize) || datos.pageSize <= 0) {
        throw new Error('Respuesta de catálogo no válida.');
      }
      const paginas = Math.ceil(datos.totalCount / datos.pageSize);
      return { ...datos, totalPages: paginas };
    }));
    return leer(1).pipe(
      expand(datos => datos.page < datos.totalPages ? leer(datos.page + 1) : EMPTY),
      reduce((todos, pagina) => {
        pagina.items.forEach(item => todos.set(item.id, item));
        return todos;
      }, new Map<number, ProductoCatalogoPublico>()),
      map(todos => [...todos.values()])
    );
  }

  obtenerDestacados(limite = 4): Observable<ProductoCatalogoPublico[]> {
    const cantidad = Math.max(1, Math.min(4, Math.floor(limite) || 4));
    const params = new HttpParams().set('limite', cantidad);
    return this.http.get<ApiResponse<ProductoCatalogoPublico[]>>(this.urlDestacados, { params }).pipe(map(res => {
      if (!res.success || !Array.isArray(res.data)) throw new Error('Respuesta de destacados no válida.');
      return res.data.filter(producto => producto.activo !== false && producto.esDestacado === true).slice(0, cantidad);
    }));
  }

  obtenerProductoPorSlug(slug: string): Observable<ProductoCatalogoPublico> {
    const seguro = this.slugSeguro(slug);
    if (!seguro) return throwError(() => new Error('Slug de producto no válido.'));
    return this.http.get<ApiResponse<ProductoCatalogoPublico>>(`${this.urlProductos}/${encodeURIComponent(seguro)}`).pipe(
      map(res => {
        if (!res.success || !res.data) throw new Error('Producto no encontrado.');
        return res.data;
      })
    );
  }

  obtenerCategorias(): Observable<CategoriaCatalogoPublico[]> {
    return this.http.get<ApiResponse<CategoriaCatalogoPublico[]>>(this.urlCategorias).pipe(map(res => {
      if (!res.success || !Array.isArray(res.data)) throw new Error('Respuesta de categorías no válida.');
      return res.data;
    }));
  }

  obtenerCategoriaPorSlug(slug: string): Observable<CategoriaCatalogoPublico> {
    const seguro = this.slugSeguro(slug);
    if (!seguro) return throwError(() => new Error('Slug de categoría no válido.'));
    return this.http.get<ApiResponse<CategoriaCatalogoPublico>>(`${this.urlCategorias}/${encodeURIComponent(seguro)}`).pipe(
      map(res => {
        if (!res.success || !res.data) throw new Error('Categoría no encontrada.');
        return res.data;
      })
    );
  }

  validarCheckout(items: CheckoutItemRequest[]): Observable<CheckoutValidado> {
    const referencias = items.map(item => ({
      productoId: item.productoId,
      productoVarianteId: item.productoVarianteId,
      modeloId: item.modeloId,
      modeloNombre: item.modeloNombre,
      marcaNombre: item.marcaNombre,
      unidades: item.unidades
    }));
    return this.http.post<ApiResponse<CheckoutValidado>>(this.urlValidarCheckout, { items: referencias }).pipe(
      map(res => {
        const data = res.data;
        if (!res.success || !data || !this.checkoutValidado(data)) {
          throw new Error(res.message || 'No pudimos validar el carrito.');
        }
        return data;
      })
    );
  }

  crearCheckoutTarjeta(endpoint: string, request: CheckoutTarjetaRequest): Observable<CheckoutTarjetaResponse> {
    const ruta = endpoint.trim().replace(/^\/+/, '');
    if (!ruta || !ruta.startsWith('tienda/') || /^https?:/i.test(ruta) || ruta.includes('..')) {
      return throwError(() => new Error('El endpoint de pago seguro no es válido.'));
    }
    return this.http.post<ApiResponse<CheckoutTarjetaResponse>>(`${environment.apiUrl}/${ruta}`, request).pipe(
      map(res => {
        if (!res.success || !res.data?.checkoutUrl) {
          throw new Error(res.message || 'El proveedor de pago no devolvió una sesión válida.');
        }
        return res.data;
      })
    );
  }

  private checkoutValidado(data: CheckoutValidado): boolean {
    return typeof data.validacionId === 'string'
      && /^[a-f0-9]{32}$/i.test(data.validacionId)
      && typeof data.expiraUtc === 'string'
      && Number.isFinite(Date.parse(data.expiraUtc))
      && Number.isFinite(data.subtotal)
      && data.subtotal >= 0
      && Number.isFinite(data.total)
      && data.total >= 0
      && Array.isArray(data.lineas)
      && data.lineas.length > 0
      && data.lineas.every(linea => Number.isSafeInteger(linea.productoId)
        && linea.productoId > 0
        && (linea.productoVarianteId == null || (Number.isSafeInteger(linea.productoVarianteId) && linea.productoVarianteId > 0))
        && (linea.modeloId === null || (Number.isSafeInteger(linea.modeloId) && linea.modeloId > 0))
        && Number.isSafeInteger(linea.unidades)
        && linea.unidades > 0
        && Number.isSafeInteger(linea.stockDisponible)
        && linea.stockDisponible >= linea.unidades
        && Number.isFinite(linea.precioUnitario)
        && linea.precioUnitario > 0
        && Number.isFinite(linea.total)
        && linea.total >= 0);
  }

  private slugSeguro(slug: string): string {
    const valor = slug.trim();
    return valor.length > 0 && valor.length <= 180 && /^[a-zA-Z0-9áéíóúüñÁÉÍÓÚÜÑ-]+$/.test(valor) ? valor : '';
  }
}
