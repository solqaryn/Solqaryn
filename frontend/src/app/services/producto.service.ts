import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, PagedRequest, PagedResult } from '../core/models/api-response.model';
import { AjusteStockRequest, AjusteStockResultado, Producto, ProductoFormValue, ProductoImagen, ProductoVariante, ProductoVarianteFormValue, TipoInventario } from '../core/models/producto.model';

export interface ProductoPagedRequest extends PagedRequest {
  categoriaId?: number;
  colorId?: number;
  tallaId?: number;
  marcaId?: number;
  modeloId?: number;
  activo?: boolean;
  agotado?: boolean;
  tipoInventario?: TipoInventario;
}

@Injectable({ providedIn: 'root' })
export class ProductoService {
  private readonly apiUrl = `${environment.apiUrl}/productos`;

  constructor(private http: HttpClient) {}

  getPaged(request: ProductoPagedRequest): Observable<ApiResponse<PagedResult<Producto>>> {
    let params = new HttpParams().set('page', request.page).set('pageSize', request.pageSize);
    if (request.search) params = params.set('search', request.search);
    if (request.sortBy) params = params.set('sortBy', request.sortBy);
    if (request.sortDirection) params = params.set('sortDirection', request.sortDirection);
    if (request.categoriaId != null) params = params.set('categoriaId', request.categoriaId);
    if (request.colorId != null) params = params.set('colorId', request.colorId);
    if (request.tallaId != null) params = params.set('tallaId', request.tallaId);
    if (request.marcaId != null) params = params.set('marcaId', request.marcaId);
    if (request.modeloId != null) params = params.set('modeloId', request.modeloId);
    if (request.activo != null) params = params.set('activo', request.activo);
    if (request.agotado != null) params = params.set('agotado', request.agotado);
    if (request.tipoInventario != null) params = params.set('tipoInventario', request.tipoInventario);
    return this.http.get<ApiResponse<PagedResult<Producto>>>(this.apiUrl, { params });
  }

  getById(id: number): Observable<ApiResponse<Producto>> {
    return this.http.get<ApiResponse<Producto>>(`${this.apiUrl}/${id}`);
  }

  create(value: ProductoFormValue): Observable<ApiResponse<Producto>> {
    const formData = new FormData();
    this.appendCamposBase(formData, value);
    (value.imagenesNuevas ?? []).forEach((file) => formData.append('Imagenes', file));
    return this.http.post<ApiResponse<Producto>>(this.apiUrl, formData);
  }

  update(id: number, value: ProductoFormValue): Observable<ApiResponse<Producto>> {
    const formData = new FormData();
    this.appendCamposBase(formData, value);
    (value.imagenesNuevas ?? []).forEach((file) => formData.append('ImagenesNuevas', file));
    (value.imagenesAEliminarIds ?? []).forEach((imagenId) => formData.append('ImagenesAEliminarIds', String(imagenId)));
    if (value.imagenPrincipalId != null) formData.append('ImagenPrincipalId', String(value.imagenPrincipalId));
    return this.http.put<ApiResponse<Producto>>(`${this.apiUrl}/${id}`, formData);
  }

  getVariantes(productoId: number, incluirInactivas = true): Observable<ApiResponse<ProductoVariante[]>> {
    const params = new HttpParams().set('incluirInactivas', incluirInactivas);
    return this.http.get<ApiResponse<ProductoVariante[]>>(`${this.apiUrl}/${productoId}/variantes`, { params });
  }

  crearVariante(productoId: number, value: ProductoVarianteFormValue): Observable<ApiResponse<ProductoVariante>> {
    return this.http.post<ApiResponse<ProductoVariante>>(`${this.apiUrl}/${productoId}/variantes`, value);
  }

  actualizarVariante(productoId: number, varianteId: number, value: ProductoVarianteFormValue): Observable<ApiResponse<ProductoVariante>> {
    return this.http.put<ApiResponse<ProductoVariante>>(`${this.apiUrl}/${productoId}/variantes/${varianteId}`, value);
  }

  cambiarEstadoVariante(productoId: number, varianteId: number, activo: boolean): Observable<ApiResponse<ProductoVariante>> {
    return this.http.patch<ApiResponse<ProductoVariante>>(`${this.apiUrl}/${productoId}/variantes/${varianteId}/estado`, { activo });
  }

  eliminarVariante(productoId: number, varianteId: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${productoId}/variantes/${varianteId}`);
  }

  getImagenesVariante(productoId: number, varianteId: number): Observable<ApiResponse<ProductoImagen[]>> {
    return this.http.get<ApiResponse<ProductoImagen[]>>(`${this.apiUrl}/${productoId}/variantes/${varianteId}/imagenes`);
  }

  agregarImagenesVariante(productoId: number, varianteId: number, archivos: File[]): Observable<ApiResponse<ProductoImagen[]>> {
    const formData = new FormData();
    archivos.forEach((archivo) => formData.append('archivos', archivo));
    return this.http.post<ApiResponse<ProductoImagen[]>>(`${this.apiUrl}/${productoId}/variantes/${varianteId}/imagenes`, formData);
  }

  marcarImagenPrincipalVariante(productoId: number, varianteId: number, imagenId: number): Observable<ApiResponse<object>> {
    return this.http.patch<ApiResponse<object>>(`${this.apiUrl}/${productoId}/variantes/${varianteId}/imagenes/${imagenId}/principal`, {});
  }

  eliminarImagenVariante(productoId: number, varianteId: number, imagenId: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${productoId}/variantes/${varianteId}/imagenes/${imagenId}`);
  }

  ajustarStockProducto(productoId: number, request: AjusteStockRequest): Observable<ApiResponse<AjusteStockResultado>> {
    return this.http.post<ApiResponse<AjusteStockResultado>>(`${this.apiUrl}/${productoId}/ajustes-stock`, request);
  }

  ajustarStockVariante(productoId: number, varianteId: number, request: AjusteStockRequest): Observable<ApiResponse<AjusteStockResultado>> {
    return this.http.post<ApiResponse<AjusteStockResultado>>(`${this.apiUrl}/${productoId}/variantes/${varianteId}/ajustes-stock`, request);
  }

  activar(id: number): Observable<ApiResponse<Producto>> {
    return this.http.patch<ApiResponse<Producto>>(`${this.apiUrl}/${id}/activar`, {});
  }

  desactivar(id: number): Observable<ApiResponse<Producto>> {
    return this.http.patch<ApiResponse<Producto>>(`${this.apiUrl}/${id}/desactivar`, {});
  }

  delete(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(`${this.apiUrl}/${id}`);
  }

  descargarImagen(productoId: number, imagenId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${productoId}/imagenes/${imagenId}/descargar`, { responseType: 'blob' });
  }

  descargarTodasLasImagenes(productoId: number): Observable<Blob> {
    return this.http.get(`${this.apiUrl}/${productoId}/imagenes/descargar-todas`, { responseType: 'blob' });
  }

  private appendCamposBase(formData: FormData, value: ProductoFormValue): void {
    formData.append('Nombre', value.nombre);
    formData.append('Marca', value.marca);
    formData.append('Modelo', value.modelo);
    if (value.descripcion) formData.append('Descripcion', value.descripcion);
    formData.append('TipoInventario', String(value.tipoInventario ?? TipoInventario.MercaderiaVenta));
    formData.append('EsDestacado', String(Boolean(value.esDestacado)));
    formData.append('Cantidad', String(value.cantidad));
    formData.append('Costo', String(value.costo));
    formData.append('Precio', String(value.precio));
    formData.append('UmbralStockBajo', String(value.umbralStockBajo));
    if (value.categoriaId != null) formData.append('CategoriaId', String(value.categoriaId));
    if (value.colorId != null) formData.append('ColorId', String(value.colorId));
    if (value.tallaId != null) formData.append('TallaId', String(value.tallaId));
    if (value.marcaId != null) formData.append('MarcaId', String(value.marcaId));
    if (value.modeloId != null) formData.append('ModeloId', String(value.modeloId));

    value.variantes.forEach((variante, index) => {
      if (variante.id != null) formData.append(`Variantes[${index}].Id`, String(variante.id));
      if (variante.marcaId != null) formData.append(`Variantes[${index}].MarcaId`, String(variante.marcaId));
      if (variante.modeloId != null) formData.append(`Variantes[${index}].ModeloId`, String(variante.modeloId));
      if (variante.colorId != null) formData.append(`Variantes[${index}].ColorId`, String(variante.colorId));
      if (variante.tallaId != null) formData.append(`Variantes[${index}].TallaId`, String(variante.tallaId));
      if (variante.sku?.trim()) formData.append(`Variantes[${index}].Sku`, variante.sku.trim());
      if (variante.codigoBarras?.trim()) formData.append(`Variantes[${index}].CodigoBarras`, variante.codigoBarras.trim());
      formData.append(`Variantes[${index}].Cantidad`, String(variante.cantidad));
      formData.append(`Variantes[${index}].UmbralStockBajo`, String(variante.umbralStockBajo));
      formData.append(`Variantes[${index}].Costo`, String(variante.costo));
      formData.append(`Variantes[${index}].Precio`, String(variante.precio));
      formData.append(`Variantes[${index}].Activo`, String(variante.activo ?? true));
    });
  }
}
