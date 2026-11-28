
import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { StorefrontIdentidadService } from './storefront-identidad.service';
import { StorefrontCarritoService } from './storefront-carrito.service';
import { StorefrontHeaderComponent } from './storefront-header.component';
import { EstadoRecursoPublico, ReciboPedidoPublico, TiendaPedidoCuenta } from './storefront.models';
import { StorefrontCuentaService } from './storefront-cuenta.service';
import { STOREFRONT_PATHS } from './storefront.paths';
import { StorefrontPedidoService } from './storefront-pedido.service';
import { IconoTiendaComponent } from './storefront.visual';

type ReciboPedidoVista = ReciboPedidoPublico | {
  referencia: string; estado: 'pedido-registrado'; estadoErp: string; moneda: string;
  creadoUtc: string; total: number; costoEnvio?: number; fiscal?: TiendaPedidoCuenta['fiscal'];
  lineas: Array<{ productoId: number; productoVarianteId?: number | null; modeloId?: number | null;
    nombre: string; modelo?: string | null; unidades: number; precioUnitario: number; total: number }>;
};

@Component({
  selector: 'app-storefront-pedido',
  standalone: true,
  imports: [StorefrontHeaderComponent, IconoTiendaComponent],
  templateUrl: './storefront-pedido.component.html',
  styleUrl: './storefront-pedido.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class StorefrontPedidoComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly pedidos = inject(StorefrontPedidoService);
  private readonly cuenta = inject(StorefrontCuentaService);
  private readonly destroyRef = inject(DestroyRef);

  readonly identidad = inject(StorefrontIdentidadService);
  readonly carrito = inject(StorefrontCarritoService);
  readonly recibo = signal<ReciboPedidoVista | null>(null);
  readonly estado = signal<EstadoRecursoPublico>('loading');
  readonly error = signal('');
  readonly cargando = computed(() => this.estado() === 'loading');
  readonly totalUnidades = computed<number | null>(() => this.carrito.listo() ? this.carrito.totalUnidades() : null);
  readonly subtotal = computed<number | null>(() => this.carrito.listo() ? this.carrito.subtotal() : null);

  readonly enlaces = {
    inicio: STOREFRONT_PATHS.inicio,
    productos: STOREFRONT_PATHS.productos,
    carrito: STOREFRONT_PATHS.carrito
  } as const;

  ngOnInit(): void {
    this.identidad.cargar().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => {
        const referencia = this.route.snapshot.paramMap.get('id') || '';
        const real = /^erp-([1-9][0-9]{0,9})$/.exec(referencia);
        if (real) {
          const id = Number(real[1]);
          this.cuenta.pedidoCheckout(id, this.pedidos.accesoPedido(id)).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
            next: pedido => {
              this.recibo.set(this.mapPedido(pedido));
              this.estado.set('success');
            },
            error: () => {
              this.error.set('No pudimos verificar este pedido. Si lo hiciste con tu cuenta, inicia sesión; si fue como invitado, vuelve a la sesión donde lo registraste.');
              this.estado.set('error');
            }
          });
          return;
        }
        const recibo = this.pedidos.obtener(referencia);
        this.recibo.set(recibo);
        this.estado.set(recibo ? 'success' : 'not-found');
      },
      error: () => {
        this.error.set('No pudimos cargar la confirmación en este momento. Intenta nuevamente.');
        this.estado.set('error');
      }
    });
  }

  abrirCarrito(): void {
    void this.router.navigateByUrl(STOREFRONT_PATHS.carrito);
  }

  private mapPedido(pedido: TiendaPedidoCuenta): ReciboPedidoVista {
    return {
      referencia: `erp-${pedido.id}`, estado: 'pedido-registrado', estadoErp: pedido.estado,
      moneda: pedido.moneda || 'HNL', creadoUtc: pedido.fechaUtc, total: pedido.total, fiscal: pedido.fiscal,
      ...(typeof pedido.costoEnvio === 'number' ? { costoEnvio: pedido.costoEnvio } : {}),
      lineas: pedido.lineas.map(l => ({
        productoId: l.productoId, productoVarianteId: l.productoVarianteId, nombre: l.nombre,
        modelo: l.modelo, unidades: l.cantidad, precioUnitario: l.precioUnitario, total: l.total
      }))
    };
  }

  titulo(recibo: ReciboPedidoVista): string {
    if (recibo.estado === 'pedido-registrado') return 'Tu pedido quedó registrado';
    if (recibo.estado === 'whatsapp-preparado') return 'Tu solicitud quedó preparada para WhatsApp';
    if (recibo.estado === 'tarjeta-redirigida') return 'Tu pago seguro fue iniciado';
    return 'Vista previa completada';
  }

  descripcion(recibo: ReciboPedidoVista): string {
    if (recibo.estado === 'pedido-registrado')
      return recibo.estadoErp === 'Borrador' ? 'Tu pedido está pendiente de confirmación del comercio. No representa un cobro ni una reserva de inventario.' :
        `Estado del pedido en el ERP: ${recibo.estadoErp}. Esta pantalla no acredita un pago.`;
    if (recibo.estado === 'whatsapp-preparado') {
      return 'La referencia resume lo que validamos antes de abrir WhatsApp. La compra queda sujeta a confirmación del comercio; esta pantalla no representa una factura ni una reserva de inventario.';
    }
    if (recibo.estado === 'tarjeta-redirigida') {
      return 'La referencia corresponde al inicio del flujo con el proveedor seguro. La confirmación definitiva del pago debe provenir del proveedor o del comercio.';
    }
    return 'Este recorrido fue ejecutado con datos de demostración. No se generó un cobro, reserva ni pedido real.';
  }

  moneda(valor: number, moneda: string): string {
    try {
      return new Intl.NumberFormat('es-HN', { style: 'currency', currency: moneda || 'HNL' }).format(valor);
    } catch {
      return new Intl.NumberFormat('es-HN', { style: 'currency', currency: 'HNL' }).format(valor);
    }
  }
}
