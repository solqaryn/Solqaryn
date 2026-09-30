import { CommonModule, DOCUMENT } from '@angular/common';
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, map, of, switchMap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { StorefrontIdentidadService } from './storefront-identidad.service';
import { construirEnlaceWhatsApp } from '../../core/services/whatsapp-share.service';
import { crearCatalogoEjemplo, mapearProducto, telefonoWhatsapp } from './storefront.catalog';
import { StorefrontCarritoService } from './storefront-carrito.service';
import { mensajeWhatsappCheckout, normalizarDatosComprador, urlCheckoutPermitida } from './storefront-checkout.rules';
import { STOREFRONT_CONFIG } from './storefront.config';
import { StorefrontHeaderComponent } from './storefront-header.component';
import { CheckoutItemRequest, CheckoutValidado, DatosCompradorCheckout, EstadoConsultaPublica, ReciboPedidoPublico } from './storefront.models';
import { STOREFRONT_PATHS } from './storefront.paths';
import { StorefrontPedidoService } from './storefront-pedido.service';
import { StorefrontService } from './storefront.service';
import { IconoTiendaComponent } from './storefront.visual';

type IdentidadAgrupacion = { modeloNombre: string | null; marcaNombre: string | null };

@Component({
  selector: 'app-storefront-checkout',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, StorefrontHeaderComponent, IconoTiendaComponent],
  templateUrl: './storefront-checkout.component.html',
  styleUrl: './storefront-checkout.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorefrontCheckoutComponent implements OnInit {
  private readonly servicio = inject(StorefrontService);
  private readonly pedidos = inject(StorefrontPedidoService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly document = inject(DOCUMENT);

  readonly identidad = inject(StorefrontIdentidadService);
  readonly carrito = inject(StorefrontCarritoService);
  readonly config = inject(STOREFRONT_CONFIG);
  readonly utilizarDatosBaseDatos = signal(this.config.utilizarDatosBaseDatos);
  readonly estado = signal<EstadoConsultaPublica>('loading');
  readonly error = signal('');
  readonly procesando = signal(false);
  readonly validado = signal<CheckoutValidado | null>(null);
  readonly enlaceWhatsapp = signal('');
  readonly aviso = signal('');
  private readonly slugsProducto = new Map<number, string>();

  readonly permiteWhatsapp = computed(() => this.config.modoCarrito !== 'tarjeta');
  readonly permiteTarjeta = computed(() => this.config.modoCarrito !== 'whatsapp');
  readonly whatsappDisponible = computed(() => Boolean(telefonoWhatsapp(this.identidad.config().whatsApp)));
  readonly tarjetaConfigurada = computed(() => Boolean(this.config.endpointCheckoutTarjeta?.trim() && this.config.origenesCheckoutPermitidos.length));
  readonly totalUnidades = computed<number | null>(() => this.carrito.listo() ? this.carrito.totalUnidades() : null);
  readonly subtotalHeader = computed<number | null>(() => this.validado()?.subtotal ?? (this.carrito.listo() ? this.carrito.subtotal() : null));

  readonly formulario = new FormGroup({
    nombre: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.minLength(2), Validators.maxLength(120)] }),
    telefono: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.pattern(/^\+?[0-9 ()-]{8,24}$/)] }),
    correo: new FormControl('', { nonNullable: true, validators: [Validators.email, Validators.maxLength(160)] }),
    notas: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(600)] })
  });

  readonly enlaces = {
    inicio: STOREFRONT_PATHS.inicio,
    productos: STOREFRONT_PATHS.productos,
    carrito: STOREFRONT_PATHS.carrito
  } as const;

  ngOnInit(): void {
    if (!environment.production && this.config.mostrarControlesVistaPrevia
      && this.route.snapshot.queryParamMap.get('fuente') === 'bd') {
      this.utilizarDatosBaseDatos.set(true);
    }

    this.identidad.cargar().pipe(
      takeUntilDestroyed(this.destroyRef),
      switchMap(() => this.prepararCheckout())
    ).subscribe({
      next: validado => this.aplicarValidacion(validado),
      error: error => this.fallar(error)
    });
  }

  abrirCarrito(): void {
    void this.router.navigateByUrl(STOREFRONT_PATHS.carrito);
  }

  revalidar(): void {
    this.procesando.set(true);
    this.error.set('');
    this.enlaceWhatsapp.set('');
    this.prepararCheckout().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: validado => {
        this.procesando.set(false);
        this.aplicarValidacion(validado);
        this.aviso.set('Actualizamos precios y existencias antes de continuar.');
      },
      error: error => {
        this.procesando.set(false);
        this.fallar(error);
      }
    });
  }

  prepararWhatsapp(): void {
    if (!this.permiteWhatsapp() || !this.validarFormulario()) return;
    const destino = telefonoWhatsapp(this.identidad.config().whatsApp);
    if (!destino) {
      this.error.set('Este comercio no tiene un número de WhatsApp válido configurado.');
      return;
    }
    const validado = this.validado();
    if (!validado || !this.validacionVigente()) {
      this.error.set('La validación del carrito venció. Actualízala antes de continuar.');
      return;
    }

    const comprador = this.datosComprador();
    const moneda = this.identidad.config().moneda || 'HNL';
    const mensaje = mensajeWhatsappCheckout(
      this.identidad.config().nombreComercial || 'Tienda',
      comprador,
      validado.validacionId,
      moneda,
      validado.lineas,
      validado.total,
      this.enlacesProductos(validado)
    );
    const enlace = construirEnlaceWhatsApp(destino, mensaje);
    if (!enlace) {
      this.error.set('No se pudo generar un enlace seguro para WhatsApp. Verifica el número configurado.');
      return;
    }
    this.enlaceWhatsapp.set(enlace);
    this.aviso.set('Solicitud preparada. Revisa el resumen y abre WhatsApp para continuar.');
  }

  confirmarSalidaWhatsapp(evento: Event): void {
    const validado = this.validado();
    if (!validado || !this.enlaceWhatsapp()) {
      evento.preventDefault();
      return;
    }
    if (!this.validacionVigente()) {
      evento.preventDefault();
      this.enlaceWhatsapp.set('');
      this.error.set('La validación del carrito venció antes de abrir WhatsApp. Actualiza precios y existencias para continuar.');
      this.aviso.set('');
      return;
    }
    this.guardarRecibo(validado, this.utilizarDatosBaseDatos() ? 'whatsapp-preparado' : 'demo');
    queueMicrotask(() => void this.router.navigateByUrl(STOREFRONT_PATHS.pedido(validado.validacionId)));
  }

  continuarTarjeta(): void {
    if (!this.permiteTarjeta() || !this.validarFormulario()) return;
    const validado = this.validado();
    if (!validado || !this.validacionVigente()) {
      this.error.set('La validación del carrito venció. Actualízala antes de continuar.');
      return;
    }

    if (!this.utilizarDatosBaseDatos()) {
      this.guardarRecibo(validado, 'demo');
      void this.router.navigateByUrl(STOREFRONT_PATHS.pedido(validado.validacionId));
      return;
    }

    const endpoint = this.config.endpointCheckoutTarjeta?.trim();
    if (!endpoint || !this.tarjetaConfigurada()) {
      this.error.set('El pago con tarjeta todavía no tiene un proveedor seguro configurado para este comercio.');
      return;
    }

    this.procesando.set(true);
    this.error.set('');
    const idempotencyKey = this.generarReferencia('pay');
    this.servicio.crearCheckoutTarjeta(endpoint, {
      validacionId: validado.validacionId,
      items: this.referenciasCheckout(),
      comprador: this.datosComprador(),
      idempotencyKey
    }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: respuesta => {
        this.procesando.set(false);
        const segura = urlCheckoutPermitida(respuesta.checkoutUrl, this.config.origenesCheckoutPermitidos);
        if (!segura) {
          this.error.set('El proveedor devolvió un destino de pago que no está autorizado. No se realizó ninguna redirección.');
          return;
        }
        const referencia = respuesta.referencia?.trim() || validado.validacionId;
        this.guardarRecibo({ ...validado, validacionId: referencia }, 'tarjeta-redirigida');
        this.document.defaultView?.location.assign(segura);
      },
      error: error => {
        this.procesando.set(false);
        this.error.set(this.mensajeError(error, 'No pudimos iniciar el pago seguro. Intenta nuevamente.'));
      }
    });
  }

  validacionVigente(): boolean {
    const expira = Date.parse(this.validado()?.expiraUtc || '');
    return Number.isFinite(expira) && expira > Date.now();
  }

  moneda(valor: number): string {
    try {
      return new Intl.NumberFormat('es-HN', { style: 'currency', currency: this.identidad.config().moneda || 'HNL' }).format(valor);
    } catch {
      return new Intl.NumberFormat('es-HN', { style: 'currency', currency: 'HNL' }).format(valor);
    }
  }

  mostrarError(campo: 'nombre' | 'telefono' | 'correo' | 'notas'): boolean {
    const control = this.formulario.controls[campo];
    return control.invalid && (control.dirty || control.touched);
  }

  private prepararCheckout(): Observable<CheckoutValidado | null> {
    this.estado.set('loading');
    this.error.set('');
    this.validado.set(null);
    this.enlaceWhatsapp.set('');
    const idsPersistidos = this.carrito.productoIdsPersistidos(
      this.identidad.config().id,
      this.utilizarDatosBaseDatos()
    );
    this.carrito.reiniciarContexto();

    const catalogo$ = this.utilizarDatosBaseDatos()
      ? this.servicio.obtenerProductosContexto(idsPersistidos).pipe(map(productos => productos.map(mapearProducto)))
      : of(crearCatalogoEjemplo());

    return catalogo$.pipe(switchMap(productos => {
      this.slugsProducto.clear();
      for (const producto of productos) {
        const slug = producto.slug?.trim();
        if (slug) this.slugsProducto.set(producto.id, slug);
      }
      this.carrito.hidratar(productos, this.identidad.config().id, this.utilizarDatosBaseDatos());
      if (this.carrito.vacio()) return of(null);
      return this.utilizarDatosBaseDatos()
        ? this.servicio.validarCheckout(this.referenciasCheckout())
        : of(this.validacionDemo());
    }));
  }

  private enlacesProductos(validado: CheckoutValidado): Readonly<Record<number, string>> {
    const origen = this.document.defaultView?.location.origin;
    if (!origen) return {};
    const enlaces: Record<number, string> = {};
    for (const linea of validado.lineas) {
      const slug = this.slugsProducto.get(linea.productoId);
      if (!slug || enlaces[linea.productoId]) continue;
      try {
        enlaces[linea.productoId] = new URL(STOREFRONT_PATHS.producto(slug), origen).toString();
      } catch {
        // Si el navegador no puede construir una URL absoluta, omitimos el enlace sin bloquear la compra.
      }
    }
    return enlaces;
  }

  private aplicarValidacion(validado: CheckoutValidado | null): void {
    if (!validado) {
      this.estado.set('empty');
      return;
    }
    this.validado.set(validado);
    this.estado.set('success');
  }

  private fallar(error: unknown): void {
    this.estado.set('error');
    this.error.set(this.mensajeError(error, 'No pudimos validar precios y existencias para continuar. Tu carrito permanece guardado.'));
  }

  private referenciasCheckout(): CheckoutItemRequest[] {
    return this.carrito.items().map(item => {
      const productoVarianteId = item.productoVarianteId ?? null;
      const agrupacion = productoVarianteId === null
        ? this.identidadAgrupacion(item.modeloClave)
        : { modeloNombre: null, marcaNombre: null };
      return {
        productoId: item.productoId,
        productoVarianteId,
        modeloId: productoVarianteId === null ? item.modeloId : null,
        modeloNombre: agrupacion.modeloNombre,
        marcaNombre: agrupacion.marcaNombre,
        unidades: item.unidades
      };
    });
  }

  /**
   * modeloClave se reconstruyó previamente contra el catálogo vigente al hidratar el carrito.
   * Aquí solo recuperamos la identidad publicada del grupo; nunca precio, stock ni totales.
   */
  private identidadAgrupacion(modeloClave: string): IdentidadAgrupacion {
    if (modeloClave === 'base') return { modeloNombre: null, marcaNombre: null };
    try {
      const valor: unknown = JSON.parse(modeloClave);
      if (!Array.isArray(valor) || valor.length !== 3) return { modeloNombre: null, marcaNombre: null };
      const modeloNombre = typeof valor[1] === 'string' && valor[1].length ? valor[1] : null;
      const marcaNombre = typeof valor[2] === 'string' && valor[2].length ? valor[2] : null;
      return { modeloNombre, marcaNombre };
    } catch {
      return { modeloNombre: null, marcaNombre: null };
    }
  }

  private validacionDemo(): CheckoutValidado {
    const ahora = Date.now();
    return {
      validacionId: this.generarReferencia('demo'),
      expiraUtc: new Date(ahora + 10 * 60 * 1000).toISOString(),
      subtotal: this.carrito.subtotal(),
      total: this.carrito.total(),
      lineas: this.carrito.items().map(item => ({
        productoId: item.productoId,
        modeloId: item.modeloId,
        nombre: item.nombre,
        modelo: item.modelo,
        sku: null,
        unidades: item.unidades,
        stockDisponible: item.stock,
        precioUnitario: item.precio,
        total: item.precio * item.unidades
      }))
    };
  }

  private validarFormulario(): boolean {
    this.formulario.markAllAsTouched();
    if (this.formulario.invalid) {
      this.error.set('Revisa los datos de contacto marcados antes de continuar.');
      return false;
    }
    this.error.set('');
    return true;
  }

  private datosComprador(): DatosCompradorCheckout {
    return normalizarDatosComprador(this.formulario.getRawValue());
  }

  private guardarRecibo(validado: CheckoutValidado, estado: ReciboPedidoPublico['estado']): void {
    this.pedidos.guardar({
      referencia: validado.validacionId,
      estado,
      creadoUtc: new Date().toISOString(),
      expiraUtc: validado.expiraUtc,
      total: validado.total,
      moneda: this.identidad.config().moneda || 'HNL',
      lineas: validado.lineas
    });
  }

  private generarReferencia(prefijo: string): string {
    const crypto = this.document.defaultView?.crypto;
    const valor = crypto?.randomUUID?.().replace(/-/g, '') || `${Date.now()}${Math.random().toString(36).slice(2, 12)}`;
    return `${prefijo}-${valor}`.slice(0, 72);
  }

  private mensajeError(error: unknown, fallback: string): string {
    if (error && typeof error === 'object') {
      const posible = error as { error?: { message?: string }; message?: string };
      return posible.error?.message?.trim() || posible.message?.trim() || fallback;
    }
    return fallback;
  }
}
