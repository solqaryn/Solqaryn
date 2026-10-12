import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize, forkJoin, of, Subscription } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { EstadoSolicitudCompra, SolicitudCompra, SolicitudCompraDetalle, SolicitudCompraFormValue } from '../../core/models/solicitud-compra.model';
import { Producto, ProductoVariante } from '../../core/models/producto.model';
import { Proveedor } from '../../core/models/proveedor.model';
import { ProductoService } from '../../services/producto.service';
import { SolicitudCompraService } from '../../services/solicitud-compra.service';
import { ProveedorService } from '../../services/proveedor.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

interface LineaEditorSolicitudCompra {
  productoId: number | null;
  productoVarianteId: number | null;
  cantidadSolicitada: number;
  costoEstimadoUnitario: number | null;
  observacion: string;
  variantes: ProductoVariante[];
  cargandoVariantes: boolean;
}

@Component({
  selector: 'app-solicitudes-compra-shell',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule
  ],
  templateUrl: './solicitudes-compra-shell.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './solicitudes-compra-shell.component.scss'
})
export class SolicitudesCompraShellComponent implements OnInit, OnDestroy {
  readonly solicitudes = signal<SolicitudCompra[]>([]);
  readonly proveedores = signal<Proveedor[]>([]);
  readonly productos = signal<Producto[]>([]);
  readonly totalCount = signal(0);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly detalleId = signal<number | null>(null);
  readonly detalle = signal<SolicitudCompra | null>(null);
  readonly detalleLoading = signal(false);
  readonly detalleError = signal<string | null>(null);
  readonly modoFormulario = signal<'nueva' | 'editar' | null>(null);
  readonly guardando = signal(false);
  readonly accionando = signal(false);
  readonly formError = signal<string | null>(null);
  readonly catalogosLoading = signal(false);

  readonly estados: EstadoSolicitudCompra[] = ['Borrador', 'Solicitada', 'Aprobada', 'Rechazada'];

  page = 1;
  pageSize = 10;
  numero = '';
  estado: EstadoSolicitudCompra | '' = '';
  proveedorId: number | null = null;
  desde = '';
  hasta = '';

  formProveedorId: number | null = null;
  formNotas = '';
  lineas: LineaEditorSolicitudCompra[] = [];

  private listaSubscription?: Subscription;
  private detalleSubscription?: Subscription;
  private routeSubscription?: Subscription;
  private secuenciaLista = 0;
  private secuenciaDetalle = 0;

  constructor(
    private solicitudService: SolicitudCompraService,
    private proveedorService: ProveedorService,
    private productoService: ProductoService,
    private permisosRuntime: PermisosRuntimeService,
    private route: ActivatedRoute,
    private router: Router,
    private alerts: AppAlertService
  ) {}

  ngOnInit(): void {
    this.cargarCatalogos();

    this.routeSubscription = this.route.queryParamMap.subscribe(params => {
      const raw = Number(params.get('detalle'));
      const id = Number.isInteger(raw) && raw > 0 ? raw : null;
      const modoRaw = params.get('modo');
      const modo = modoRaw === 'nueva' || modoRaw === 'editar' ? modoRaw : null;

      this.detalleId.set(id);
      this.modoFormulario.set(modo === 'editar' && !id ? null : modo);
      this.formError.set(null);

      if (modo === 'nueva') {
        this.detalleSubscription?.unsubscribe();
        this.detalle.set(null);
        this.detalleLoading.set(false);
        this.prepararNueva();
      } else if (id) {
        this.cargarDetalle(id, modo === 'editar');
      } else {
        this.detalleSubscription?.unsubscribe();
        this.detalle.set(null);
        this.detalleError.set(null);
        this.detalleLoading.set(false);
      }
    });

    this.cargar();
  }

  ngOnDestroy(): void {
    this.listaSubscription?.unsubscribe();
    this.detalleSubscription?.unsubscribe();
    this.routeSubscription?.unsubscribe();
  }

  puede(accion: string): boolean {
    return this.permisosRuntime.puede('Compras', accion);
  }

  aplicarFiltros(): void {
    this.page = 1;
    this.cargar();
  }

  limpiarFiltros(): void {
    this.page = 1;
    this.pageSize = 10;
    this.numero = '';
    this.estado = '';
    this.proveedorId = null;
    this.desde = '';
    this.hasta = '';
    this.cargar();
  }

  abrirNueva(): void {
    if (!this.puede('Crear')) return;
    void this.router.navigate([], { relativeTo: this.route, queryParams: { modo: 'nueva', detalle: null } });
  }

  abrirDetalle(id: number): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: { detalle: id, modo: null } });
  }

  abrirEdicion(id: number): void {
    if (!this.puede('Editar')) return;
    void this.router.navigate([], { relativeTo: this.route, queryParams: { detalle: id, modo: 'editar' } });
  }

  volverListado(): void {
    void this.router.navigate([], { relativeTo: this.route, queryParams: { detalle: null, modo: null } });
  }

  cargar(): void {
    if (this.desde && this.hasta && this.desde > this.hasta) {
      this.error.set('La fecha inicial no puede ser posterior a la fecha final.');
      return;
    }

    const secuencia = ++this.secuenciaLista;
    this.listaSubscription?.unsubscribe();
    this.loading.set(true);
    this.error.set(null);

    this.listaSubscription = this.solicitudService.getPaged({
      page: this.page,
      pageSize: this.pageSize,
      numero: this.numero,
      estado: this.estado || null,
      proveedorId: this.proveedorId,
      desde: this.normalizarFecha(this.desde, false),
      hasta: this.normalizarFecha(this.hasta, true)
    }).subscribe({
      next: res => {
        if (secuencia !== this.secuenciaLista) return;
        this.solicitudes.set(res.data.items ?? []);
        this.totalCount.set(res.data.totalCount ?? 0);
        this.loading.set(false);
      },
      error: () => {
        if (secuencia !== this.secuenciaLista) return;
        this.error.set('No fue posible cargar las solicitudes de compra.');
        this.solicitudes.set([]);
        this.totalCount.set(0);
        this.loading.set(false);
      }
    });
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.cargar();
  }

  recargarDetalle(): void {
    const id = this.detalleId();
    if (id) this.cargarDetalle(id, this.modoFormulario() === 'editar');
  }

  agregarLinea(): void {
    this.lineas = [...this.lineas, this.crearLinea()];
  }

  quitarLinea(index: number): void {
    if (this.lineas.length <= 1) {
      this.formError.set('La solicitud debe conservar al menos una línea.');
      return;
    }
    this.lineas = this.lineas.filter((_, i) => i !== index);
    this.formError.set(null);
  }

  productoCambiado(linea: LineaEditorSolicitudCompra): void {
    linea.productoVarianteId = null;
    linea.variantes = [];
    if (!linea.productoId) return;

    linea.cargandoVariantes = true;
    this.productoService.getVariantes(linea.productoId, false)
      .pipe(finalize(() => linea.cargandoVariantes = false))
      .subscribe({
        next: res => linea.variantes = (res.data ?? []).filter(v => v.activo && !v.eliminado),
        error: () => {
          linea.variantes = [];
          this.formError.set('No fue posible cargar las variantes del producto seleccionado.');
        }
      });
  }

  guardar(): void {
    const payload = this.construirPayload();
    if (!payload) return;

    const modo = this.modoFormulario();
    const id = this.detalleId();
    this.guardando.set(true);
    this.formError.set(null);

    const request = modo === 'editar' && id
      ? this.solicitudService.update(id, payload)
      : this.solicitudService.create(payload);

    request.pipe(finalize(() => this.guardando.set(false))).subscribe({
      next: res => {
        this.detalle.set(res.data);
        this.detalleId.set(res.data.id);
        this.modoFormulario.set(null);
        this.cargar();
        void this.router.navigate([], { relativeTo: this.route, queryParams: { detalle: res.data.id, modo: null } });
      },
      error: err => this.formError.set(this.extraerMensaje(err, 'No fue posible guardar la solicitud de compra.'))
    });
  }

  enviar(solicitud: SolicitudCompra): void {
    if (solicitud.estado !== 'Borrador' || !this.puede('Confirmar')) return;
    this.ejecutarAccion(() => this.solicitudService.enviar(solicitud.id), 'No fue posible enviar la solicitud.');
  }

  async aprobar(solicitud: SolicitudCompra): Promise<void> {
    if (solicitud.estado !== 'Solicitada' || !this.puede('Aprobar')) return;
    const confirmado = await this.alerts.confirmar({
      titulo: 'Aprobar solicitud de compra',
      mensaje: `¿Aprobar la solicitud ${solicitud.numeroSolicitud}?`,
      detalle: 'La solicitud podrá continuar al flujo de orden de compra con sus líneas y costos trazables.',
      confirmarTexto: 'Aprobar solicitud'
    });
    if (!confirmado) return;
    this.ejecutarAccion(() => this.solicitudService.aprobar(solicitud.id), 'No fue posible aprobar la solicitud.');
  }

  async rechazar(solicitud: SolicitudCompra): Promise<void> {
    if (solicitud.estado !== 'Solicitada' || !this.puede('Rechazar')) return;
    const motivo = await this.alerts.solicitarTexto({
      titulo: 'Rechazar solicitud de compra',
      mensaje: `La solicitud ${solicitud.numeroSolicitud} se marcará como rechazada.`,
      detalle: 'Indica el motivo para conservar la trazabilidad de la decisión.',
      tipo: 'peligro',
      entrada: { etiqueta: 'Motivo de rechazo', requerida: true },
      confirmarTexto: 'Rechazar solicitud'
    });
    if (!motivo) {
      this.detalleError.set('El motivo de rechazo es obligatorio.');
      return;
    }
    this.ejecutarAccion(() => this.solicitudService.rechazar(solicitud.id, motivo), 'No fue posible rechazar la solicitud.');
  }

  costoEstimadoTotal(solicitud: SolicitudCompra): number {
    return solicitud.detalles.reduce(
      (total, detalle) => total + (detalle.costoEstimadoUnitario ?? 0) * detalle.cantidadSolicitada,
      0
    );
  }

  costoEstimadoFormulario(): number {
    return this.lineas.reduce((total, linea) => total + (linea.costoEstimadoUnitario ?? 0) * (linea.cantidadSolicitada || 0), 0);
  }

  referenciaProducto(detalle: SolicitudCompraDetalle): string {
    return detalle.productoNombreSnapshot?.trim() || `Producto #${detalle.productoId}`;
  }

  atributosVariante(detalle: SolicitudCompraDetalle): string {
    return [
      detalle.productoMarcaSnapshot,
      detalle.productoModeloSnapshot,
      detalle.productoColorSnapshot,
      detalle.productoTallaSnapshot
    ].filter((valor): valor is string => Boolean(valor?.trim())).join(' · ');
  }

  etiquetaProducto(producto: Producto): string {
    return producto.nombre + (producto.marcaNombre ? ` · ${producto.marcaNombre}` : '');
  }

  etiquetaVariante(variante: ProductoVariante): string {
    return variante.etiqueta?.trim() || [variante.sku, variante.colorNombre, variante.tallaNombre].filter(Boolean).join(' · ');
  }

  private cargarCatalogos(): void {
    this.catalogosLoading.set(true);
    forkJoin({
      proveedores: this.proveedorService.getActivos().pipe(catchError(() => of({ data: [] as Proveedor[] } as never))),
      productos: this.productoService.getPaged({ page: 1, pageSize: 100, activo: true, tipoInventario: 1 }).pipe(catchError(() => of(null)))
    }).pipe(finalize(() => this.catalogosLoading.set(false))).subscribe(({ proveedores, productos }) => {
      this.proveedores.set(proveedores?.data ?? []);
      this.productos.set(productos?.data?.items ?? []);
    });
  }

  private prepararNueva(): void {
    this.formProveedorId = null;
    this.formNotas = '';
    this.lineas = [this.crearLinea()];
  }

  private cargarDetalle(id: number, prepararEdicion: boolean): void {
    const secuencia = ++this.secuenciaDetalle;
    this.detalleSubscription?.unsubscribe();
    this.detalleLoading.set(true);
    this.detalleError.set(null);
    this.detalle.set(null);

    this.detalleSubscription = this.solicitudService.getById(id).subscribe({
      next: res => {
        if (secuencia !== this.secuenciaDetalle) return;
        this.detalle.set(res.data);
        this.detalleLoading.set(false);
        if (prepararEdicion) {
          if (res.data.estado !== 'Borrador') {
            this.modoFormulario.set(null);
            this.detalleError.set('Solo las solicitudes en Borrador pueden editarse.');
            return;
          }
          this.prepararEdicion(res.data);
        }
      },
      error: () => {
        if (secuencia !== this.secuenciaDetalle) return;
        this.detalleError.set('No fue posible cargar el detalle de la solicitud.');
        this.detalleLoading.set(false);
      }
    });
  }

  private prepararEdicion(solicitud: SolicitudCompra): void {
    this.formProveedorId = solicitud.proveedorId ?? null;
    this.formNotas = solicitud.notas ?? '';
    this.lineas = solicitud.detalles.map(detalle => ({
      productoId: detalle.productoId,
      productoVarianteId: detalle.productoVarianteId ?? null,
      cantidadSolicitada: detalle.cantidadSolicitada,
      costoEstimadoUnitario: detalle.costoEstimadoUnitario ?? null,
      observacion: detalle.observacion ?? '',
      variantes: [],
      cargandoVariantes: true
    }));

    this.lineas.forEach(linea => {
      if (!linea.productoId) return;
      this.productoService.getVariantes(linea.productoId, true)
        .pipe(finalize(() => linea.cargandoVariantes = false))
        .subscribe({
          next: res => linea.variantes = res.data ?? [],
          error: () => linea.variantes = []
        });
    });
  }

  private construirPayload(): SolicitudCompraFormValue | null {
    if (this.lineas.length === 0) {
      this.formError.set('Agrega al menos una línea a la solicitud.');
      return null;
    }

    try {
      const detalles = this.lineas.map((linea, index) => {
        if (!linea.productoId) throw new Error(`Selecciona el producto de la línea ${index + 1}.`);
        if (!Number.isFinite(linea.cantidadSolicitada) || linea.cantidadSolicitada <= 0) throw new Error(`La cantidad de la línea ${index + 1} debe ser mayor que cero.`);
        if (linea.costoEstimadoUnitario != null && (!Number.isFinite(linea.costoEstimadoUnitario) || linea.costoEstimadoUnitario < 0)) throw new Error(`El costo estimado de la línea ${index + 1} no puede ser negativo.`);
        return {
          productoId: linea.productoId,
          productoVarianteId: linea.productoVarianteId,
          cantidadSolicitada: linea.cantidadSolicitada,
          costoEstimadoUnitario: linea.costoEstimadoUnitario,
          observacion: linea.observacion.trim() || null
        };
      });

      return {
        proveedorId: this.formProveedorId,
        notas: this.formNotas.trim() || null,
        detalles
      };
    } catch (error) {
      this.formError.set(error instanceof Error ? error.message : 'La solicitud contiene datos inválidos.');
      return null;
    }
  }

  private crearLinea(): LineaEditorSolicitudCompra {
    return {
      productoId: null,
      productoVarianteId: null,
      cantidadSolicitada: 1,
      costoEstimadoUnitario: null,
      observacion: '',
      variantes: [],
      cargandoVariantes: false
    };
  }

  private ejecutarAccion(requestFactory: () => ReturnType<SolicitudCompraService['enviar']>, fallback: string): void {
    if (this.accionando()) return;
    this.accionando.set(true);
    this.detalleError.set(null);
    requestFactory().pipe(finalize(() => this.accionando.set(false))).subscribe({
      next: res => {
        this.detalle.set(res.data);
        this.cargar();
      },
      error: err => this.detalleError.set(this.extraerMensaje(err, fallback))
    });
  }

  private extraerMensaje(error: unknown, fallback: string): string {
    const err = error as { error?: { message?: string; detail?: string; errors?: string[] } };
    return err?.error?.message || err?.error?.detail || err?.error?.errors?.[0] || fallback;
  }

  private normalizarFecha(valor: string, finDelDia: boolean): string | null {
    if (!valor) return null;
    return `${valor}T${finDelDia ? '23:59:59.999' : '00:00:00.000'}Z`;
  }
}
