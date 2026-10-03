import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { Factura, FacturaPago, RegistrarFacturaPago } from '../../core/models/factura.model';
import { BancoLookup, MetodoPago } from '../../core/models/metodo-pago.model';
import { FacturaService } from '../../services/factura.service';
import { MetodoPagoService } from '../../services/metodo-pago.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';
import {
  EstadoPagoOnline,
  PagoOnline,
  PagoOnlineService
} from '../../services/pago-online.service';

@Component({
  selector: 'app-factura-pagos',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule
  ],
  templateUrl: './factura-pagos.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './factura-pagos.component.scss'
})
export class FacturaPagosComponent implements OnInit {
  readonly factura = signal<Factura | null>(null);
  readonly cargando = signal(true);
  readonly cargandoCatalogos = signal(true);
  readonly guardando = signal(false);
  readonly anulandoId = signal<number | null>(null);
  readonly metodosPago = signal<MetodoPago[]>([]);
  readonly bancos = signal<BancoLookup[]>([]);

  readonly pagosOnline = signal<PagoOnline[]>([]);
  readonly cargandoOnline = signal(false);
  readonly iniciandoOnline = signal(false);
  readonly errorOnline = signal<string | null>(null);
  readonly estadoOnlineFiltro = signal<EstadoPagoOnline | undefined>(undefined);
  readonly estadosOnline = [
    { value: EstadoPagoOnline.Pendiente, label: 'Pendiente' },
    { value: EstadoPagoOnline.Confirmado, label: 'Confirmado' },
    { value: EstadoPagoOnline.Fallido, label: 'Fallido' },
    { value: EstadoPagoOnline.Cancelado, label: 'Cancelado' },
    { value: EstadoPagoOnline.Expirado, label: 'Expirado' }
  ];

  pago: RegistrarFacturaPago = this.nuevoPago();
  pagoOnline = { monto: 0, moneda: 'HNL', proveedor: 'demo' };

  constructor(
    private readonly route: ActivatedRoute,
    private readonly facturaService: FacturaService,
    private readonly metodoPagoService: MetodoPagoService,
    private readonly pagoOnlineService: PagoOnlineService,
    private readonly tenantContext: TenantContextService,
    private readonly snackBar: MatSnackBar,
    private readonly alerts: AppAlertService
  ) {}

  ngOnInit(): void {
    this.cargarCatalogos();
    this.cargar();
  }

  cargar(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.cargando.set(true);
    this.facturaService.getById(id).subscribe({
      next: (respuesta) => {
        this.factura.set(respuesta.data);
        const saldo = Math.max(0, respuesta.data.saldoPendiente);
        this.pago.monto = saldo;
        this.pagoOnline.monto = saldo;
        this.cargando.set(false);
        this.cargarPagosOnline();
      },
      error: (error) => {
        this.cargando.set(false);
        this.snackBar.open(error.error?.message ?? 'No se pudo cargar la factura.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  cargarCatalogos(): void {
    this.cargandoCatalogos.set(true);
    forkJoin({
      metodos: this.metodoPagoService.getActivos(),
      bancos: this.metodoPagoService.getBancosActivos()
    }).subscribe({
      next: ({ metodos, bancos }) => {
        const activos = [...metodos.data]
          .filter((metodo) => metodo.activo)
          .sort((a, b) => a.orden - b.orden || a.codigo.localeCompare(b.codigo));
        this.metodosPago.set(activos);
        this.bancos.set([...bancos.data].sort((a, b) => a.nombre.localeCompare(b.nombre)));
        this.normalizarMetodoPago();
        this.cargandoCatalogos.set(false);
      },
      error: () => {
        this.cargandoCatalogos.set(false);
        this.snackBar.open('No se pudieron cargar los métodos de pago y bancos activos.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  metodoSeleccionado(): MetodoPago | undefined {
    const valor = this.pago.metodoPago?.trim().toLocaleLowerCase('es');
    return this.metodosPago().find((metodo) =>
      metodo.codigo.toLocaleLowerCase('es') === valor
      || metodo.nombre.toLocaleLowerCase('es') === valor
    );
  }

  onMetodoPagoChange(): void {
    const metodo = this.metodoSeleccionado();
    if (!metodo?.requiereBanco) this.pago.bancoId = undefined;
  }

  cambioEstimado(): number {
    const factura = this.factura();
    const metodo = this.metodoSeleccionado();
    if (!factura || !metodo?.permiteCambio || this.pago.monto <= factura.saldoPendiente) return 0;
    return Math.round((this.pago.monto - factura.saldoPendiente) * 100) / 100;
  }

  registrarPago(): void {
    const factura = this.factura();
    if (!factura || !this.puedeRegistrar()) return;

    this.guardando.set(true);
    this.facturaService.registrarPago(factura.id, {
      ...this.pago,
      bancoId: this.pago.bancoId || undefined,
      referencia: this.pago.referencia?.trim() || undefined,
      observaciones: this.pago.observaciones?.trim() || undefined
    }).subscribe({
      next: (respuesta) => {
        this.factura.set(respuesta.data);
        const saldo = Math.max(0, respuesta.data.saldoPendiente);
        this.pago = this.nuevoPago(saldo);
        this.pagoOnline.monto = saldo;
        this.normalizarMetodoPago();
        this.guardando.set(false);
        this.snackBar.open('Pago registrado correctamente.', 'Cerrar', { duration: 3500 });
      },
      error: (error) => {
        this.guardando.set(false);
        this.snackBar.open(error.error?.message ?? 'No se pudo registrar el pago.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  async anularPago(pago: FacturaPago): Promise<void> {
    const factura = this.factura();
    if (!factura || pago.anulado || this.anulandoId() !== null) return;

    const motivo = await this.alerts.solicitarTexto({
      titulo: 'Anular pago',
      mensaje: `El pago aplicado a ${factura.numeroFactura || `FAC-${factura.id}`} se revertirá.`,
      detalle: 'El saldo de la factura y el movimiento financiero se recalcularán; indica el motivo para la auditoría.',
      tipo: 'peligro',
      entrada: { etiqueta: 'Motivo de anulación', requerida: true },
      confirmarTexto: 'Anular pago'
    });
    if (!motivo) return;

    this.anulandoId.set(pago.id);
    this.facturaService.anularPago(factura.id, pago.id, motivo).subscribe({
      next: (respuesta) => {
        this.factura.set(respuesta.data);
        const saldo = Math.max(0, respuesta.data.saldoPendiente);
        this.pago.monto = saldo;
        this.pagoOnline.monto = saldo;
        this.anulandoId.set(null);
        this.snackBar.open('Pago anulado y saldo recalculado.', 'Cerrar', { duration: 3500 });
      },
      error: (error) => {
        this.anulandoId.set(null);
        this.snackBar.open(error.error?.message ?? 'No se pudo anular el pago.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  cargarPagosOnline(): void {
    const factura = this.factura();
    const empresaId = this.tenantContext.empresaIdVerificada();
    if (!factura || !empresaId) {
      this.pagosOnline.set([]);
      this.errorOnline.set('Selecciona y verifica una empresa para consultar pagos online.');
      return;
    }

    this.cargandoOnline.set(true);
    this.errorOnline.set(null);
    this.pagoOnlineService.listar(empresaId, factura.id, this.estadoOnlineFiltro()).subscribe({
      next: (respuesta) => {
        this.pagosOnline.set(respuesta.data.items);
        this.cargandoOnline.set(false);
      },
      error: (error) => {
        this.pagosOnline.set([]);
        this.cargandoOnline.set(false);
        this.errorOnline.set(error.error?.message ?? 'No se pudieron cargar los pagos online.');
      }
    });
  }

  iniciarPagoOnline(): void {
    const factura = this.factura();
    const empresaId = this.tenantContext.empresaIdVerificada();
    if (!factura || !empresaId || !this.puedeIniciarPagoOnline()) return;

    this.iniciandoOnline.set(true);
    this.errorOnline.set(null);
    this.pagoOnlineService.iniciar({
      empresaId,
      facturaId: factura.id,
      proveedor: this.pagoOnline.proveedor.trim(),
      monto: this.pagoOnline.monto,
      moneda: this.pagoOnline.moneda.trim().toUpperCase()
    }, this.crearIdempotencyKey()).subscribe({
      next: (respuesta) => {
        this.iniciandoOnline.set(false);
        const pagoCreado = respuesta.data.pago;
        this.snackBar.open(
          respuesta.data.reutilizado ? 'Solicitud de pago recuperada de forma idempotente.' : 'Pago online iniciado.',
          'Cerrar',
          { duration: 4000 }
        );
        this.cargarPagosOnline();
        if (!this.urlPagoSegura(pagoCreado)) {
          this.errorOnline.set('El proveedor no devolvió todavía una URL HTTPS de checkout.');
        }
      },
      error: (error) => {
        this.iniciandoOnline.set(false);
        this.errorOnline.set(error.error?.message ?? error.error?.detail ?? 'No se pudo iniciar el pago online.');
      }
    });
  }

  puedeRegistrar(): boolean {
    const factura = this.factura();
    const metodo = this.metodoSeleccionado();
    if (!factura || !metodo || this.cargandoCatalogos() || this.guardando()) return false;
    if (['Anulada', 'Cancelada'].includes(factura.estado) || factura.saldoPendiente <= 0 || this.pago.monto <= 0) return false;
    if (this.pago.monto > factura.saldoPendiente && !metodo.permiteCambio) return false;
    if (metodo.requiereReferencia && !this.pago.referencia?.trim()) return false;
    if (metodo.requiereBanco && (!this.pago.bancoId || this.pago.bancoId <= 0)) return false;
    return true;
  }

  puedeIniciarPagoOnline(): boolean {
    const factura = this.factura();
    const empresaId = this.tenantContext.empresaIdVerificada();
    return !!factura
      && !!empresaId
      && !['Anulada', 'Cancelada'].includes(factura.estado)
      && factura.saldoPendiente > 0
      && this.pagoOnline.monto > 0
      && this.pagoOnline.monto <= factura.saldoPendiente
      && this.pagoOnline.proveedor.trim().length > 0
      && /^[A-Z]{3}$/.test(this.pagoOnline.moneda.trim().toUpperCase())
      && !this.iniciandoOnline();
  }

  etiquetaEstadoOnline(estado: EstadoPagoOnline): string {
    return this.estadosOnline.find((item) => item.value === estado)?.label ?? `Estado ${estado}`;
  }

  urlPagoSegura(pago: PagoOnline): string | null {
    if (!pago.urlPago) return null;
    try {
      const url = new URL(pago.urlPago);
      return url.protocol === 'https:' ? url.toString() : null;
    } catch {
      return null;
    }
  }

  private crearIdempotencyKey(): string {
    const uuid = globalThis.crypto?.randomUUID?.();
    return uuid ? `n78e-${uuid}` : `n78e-${Date.now()}-${Math.random().toString(36).slice(2)}-${Math.random().toString(36).slice(2)}`;
  }

  private normalizarMetodoPago(): void {
    const activos = this.metodosPago();
    if (activos.length === 0) {
      this.pago.metodoPago = '';
      return;
    }

    const actual = this.metodoSeleccionado();
    if (!actual) this.pago.metodoPago = activos[0].codigo;
    this.onMetodoPagoChange();
  }

  private nuevoPago(monto = 0): RegistrarFacturaPago {
    return {
      monto,
      metodoPago: '',
      bancoId: undefined,
      referencia: '',
      observaciones: ''
    };
  }
}
