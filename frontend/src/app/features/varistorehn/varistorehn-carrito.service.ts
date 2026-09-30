import { DOCUMENT } from '@angular/common';
import { Injectable, computed, inject, signal } from '@angular/core';
import {
  ItemCarrito,
  ModeloTienda,
  ProductoTienda,
  ReferenciaCarrito,
  agregarItem,
  referenciasCarrito,
  restaurarCarrito,
  totalCarrito
} from './varistorehn.catalog';

export interface ResultadoHidratacionCarrito {
  ajustado: boolean;
  referenciasOriginales: unknown;
}

/**
 * Fuente única de verdad del carrito público de VariStoreHn.
 *
 * Persistencia deliberadamente mínima: localStorage conserva únicamente referencias
 * { productoId, modeloClave, unidades }. Precio, stock, imagen y disponibilidad siempre
 * se reconstruyen desde el catálogo público vigente al hidratar el store.
 */
@Injectable({ providedIn: 'root' })
export class VaristorehnCarritoService {
  private readonly document = inject(DOCUMENT);

  private readonly _items = signal<ItemCarrito[]>([]);
  private readonly _listo = signal(false);
  private readonly _claveContexto = signal('');
  private readonly _aviso = signal('');

  readonly items = this._items.asReadonly();
  readonly listo = this._listo.asReadonly();
  readonly aviso = this._aviso.asReadonly();
  readonly totalUnidades = computed(() => this._items().reduce((total, item) => total + item.unidades, 0));
  readonly subtotal = computed(() => totalCarrito(this._items()));
  /** En Fase 5 no existen cargos de entrega/impuestos del checkout; por eso total === subtotal. */
  readonly total = computed(() => this.subtotal());
  readonly vacio = computed(() => this._listo() && this._items().length === 0);

  hidratar(productos: ProductoTienda[], empresaId: number, utilizarDatosBaseDatos: boolean): ResultadoHidratacionCarrito {
    const clave = this.clave(empresaId, utilizarDatosBaseDatos);
    this._claveContexto.set(clave);
    this._listo.set(false);
    this._aviso.set('');

    const originales = this.leer(clave);
    const restaurados = restaurarCarrito(originales, productos);
    const referenciasRestauradas = referenciasCarrito(restaurados);
    const ajustado = Array.isArray(originales)
      && JSON.stringify(originales) !== JSON.stringify(referenciasRestauradas);

    this._items.set(restaurados);
    this._listo.set(true);
    this.persistir(referenciasRestauradas, false);

    if (ajustado) {
      this._aviso.set('Actualizamos tu carrito con los precios, modelos y existencias disponibles actualmente.');
    }

    return { ajustado, referenciasOriginales: originales };
  }

  reiniciarContexto(): void {
    this._items.set([]);
    this._listo.set(false);
    this._claveContexto.set('');
    this._aviso.set('');
  }

  productoIdsPersistidos(empresaId: number, utilizarDatosBaseDatos: boolean): number[] {
    const referencias = this.leer(this.clave(empresaId, utilizarDatosBaseDatos));
    if (!Array.isArray(referencias)) return [];

    const ids = referencias
      .slice(0, 500)
      .map(ref => ref && typeof ref === 'object' ? Number((ref as Record<string, unknown>)['productoId']) : 0)
      .filter(id => Number.isSafeInteger(id) && id > 0);
    return [...new Set(ids)];
  }

  unidadesDe(productoId: number, modeloClave: string): number {
    return this._items().find(item => item.productoId === productoId && item.modeloClave === modeloClave)?.unidades || 0;
  }

  disponibleParaAgregar(producto: ProductoTienda, modelo: ModeloTienda, unidades = 1): boolean {
    if (!this._listo() || !producto.activo || !modelo.disponible || modelo.stock < 1) return false;
    const cantidad = Number.isFinite(unidades) ? Math.max(1, Math.floor(unidades)) : 1;
    return this.unidadesDe(producto.id, modelo.clave) + cantidad <= modelo.stock;
  }

  agregar(producto: ProductoTienda, modelo: ModeloTienda, unidades = 1): number {
    if (!this._listo() || !producto.activo || !modelo.disponible || modelo.stock < 1) return 0;
    const actuales = this.unidadesDe(producto.id, modelo.clave);
    const restantes = Math.max(0, modelo.stock - actuales);
    const solicitadas = Number.isFinite(unidades) ? Math.max(1, Math.floor(unidades)) : 1;
    const agregar = Math.min(restantes, solicitadas);
    if (agregar <= 0) return 0;

    let items = this._items();
    for (let indice = 0; indice < agregar; indice += 1) {
      items = agregarItem(items, producto, modelo);
    }
    this.actualizar(items);
    return agregar;
  }

  incrementar(clave: string): void {
    const item = this._items().find(actual => actual.clave === clave);
    if (!item || item.unidades >= item.stock) return;
    this.establecerCantidad(clave, item.unidades + 1);
  }

  disminuir(clave: string): void {
    const item = this._items().find(actual => actual.clave === clave);
    if (!item || item.unidades <= 1) return;
    this.establecerCantidad(clave, item.unidades - 1);
  }

  establecerCantidad(clave: string, unidades: number): void {
    const item = this._items().find(actual => actual.clave === clave);
    if (!item || !Number.isFinite(unidades)) return;
    const cantidad = Math.max(1, Math.min(item.stock, Math.floor(unidades)));
    if (cantidad === item.unidades) return;
    this.actualizar(this._items().map(actual => actual.clave === clave ? { ...actual, unidades: cantidad } : actual));
  }

  quitar(clave: string): void {
    if (!this._items().some(item => item.clave === clave)) return;
    this.actualizar(this._items().filter(item => item.clave !== clave));
  }

  vaciar(): void {
    if (!this._items().length) return;
    this.actualizar([]);
  }

  private actualizar(items: ItemCarrito[]): void {
    this._items.set(items);
    this._aviso.set('');
    this.persistir(referenciasCarrito(items), true);
  }

  private clave(empresaId: number, utilizarDatosBaseDatos: boolean): string {
    return `varistorehn:carrito:v2:${empresaId}:${utilizarDatosBaseDatos ? 'bd' : 'demo'}`;
  }

  private leer(clave: string): unknown {
    try {
      return JSON.parse(this.document.defaultView?.localStorage.getItem(clave) || '[]');
    } catch {
      return [];
    }
  }

  private persistir(referencias: ReferenciaCarrito[], anunciarError: boolean): void {
    const clave = this._claveContexto();
    if (!clave) return;
    try {
      this.document.defaultView?.localStorage.setItem(clave, JSON.stringify(referencias));
    } catch {
      if (anunciarError) {
        this._aviso.set('El navegador no permitió guardar el carrito. La selección seguirá disponible solo en esta sesión.');
      }
    }
  }
}
