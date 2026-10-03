
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { finalize } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { CapturarConteoInventarioLinea, ConteoInventario, ConteoInventarioDetalle, EstadoConteoInventario } from '../../core/models/conteo-inventario.model';
import { ConteoInventarioService } from '../../services/conteo-inventario.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-conteo-inventario-detail',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule],
  template: `
    @if (!loading && conteo; as item) {
      <section class="page" aria-labelledby="conteo-detail-title">
        <header class="header">
          <div class="title"><button mat-icon-button type="button" aria-label="Volver" (click)="volver()"><mat-icon>arrow_back</mat-icon></button><div><p class="eyebrow">Conteo físico</p><h1 id="conteo-detail-title">{{ item.numero }}</h1><p>{{ item.tipoNombre }} · {{ item.almacenNombre || ('Almacén #' + item.almacenId) }}</p></div></div>
          <span class="badge">{{ item.estadoNombre }}</span>
        </header>
        @if (error) {
          <div class="error" role="alert">{{ error }}</div>
        }
        <section class="summary" aria-label="Resumen del conteo">
          <article><span>Líneas</span><strong>{{ item.cantidadLineas }}</strong></article>
          <article><span>Capturadas</span><strong>{{ item.cantidadCapturadas }}</strong></article>
          <article><span>Con diferencia</span><strong>{{ item.cantidadConDiferencia }}</strong></article>
          <article><span>Diferencia neta</span><strong>{{ item.diferenciaNeta }}</strong></article>
        </section>
        <section class="meta">
          <div><span>Ubicación</span><strong>{{ item.ubicacionNombre || 'Todas / no aplica' }}</strong></div>
          <div><span>Categoría</span><strong>{{ item.categoriaNombre || 'Todas / no aplica' }}</strong></div>
          <div><span>Modo</span><strong>{{ item.esCiego ? 'Ciego' : 'Visible' }}</strong></div>
          <div><span>Observaciones</span><strong>{{ item.observaciones || 'Sin observaciones' }}</strong></div>
        </section>
        <div class="toolbar">
          @if (puedeEditar && item.estado === Estado.Borrador) {
            <button mat-stroked-button type="button" (click)="editar()"><mat-icon>edit</mat-icon>Editar</button>
          }
          @if (puedeCambiarEstado && item.estado === Estado.Borrador) {
            <button mat-flat-button color="primary" type="button" [disabled]="accionando" (click)="iniciar()"><mat-icon>play_arrow</mat-icon>Iniciar</button>
          }
          @if (puedeCerrar && item.estado === Estado.EnProceso) {
            <button mat-flat-button color="primary" type="button" [disabled]="accionando || item.cantidadCapturadas !== item.cantidadLineas" (click)="cerrar()"><mat-icon>lock</mat-icon>Cerrar</button>
          }
          @if (puedeAprobar && item.estado === Estado.Cerrado) {
            <button mat-flat-button color="primary" type="button" [disabled]="accionando" (click)="aprobar()"><mat-icon>verified</mat-icon>Aprobar</button>
          }
          @if (puedeCrear && item.estado === Estado.Aprobado && item.cantidadConDiferencia > 0) {
            <button mat-stroked-button color="primary" type="button" [disabled]="accionando" (click)="generarAjuste()"><mat-icon>tune</mat-icon>Generar ajuste</button>
          }
          @if (puedeAnular && item.estado !== Estado.Aprobado && item.estado !== Estado.Cancelado) {
            <button mat-stroked-button color="warn" type="button" [disabled]="accionando" (click)="cancelar()"><mat-icon>cancel</mat-icon>Cancelar</button>
          }
        </div>
        <section class="table-wrap" aria-labelledby="lineas-title">
          <div class="section-title"><div><h2 id="lineas-title">Líneas de conteo</h2><p>La captura se persiste por clave física. El stock esperado se oculta en modo ciego mientras el conteo está en proceso.</p></div>@if (puedeEditar && item.estado === Estado.EnProceso) {
          <button mat-flat-button color="primary" type="button" [disabled]="accionando || !hayCapturasPendientes" (click)="guardarCapturas()">Guardar capturas</button>
        }</div>
        <div class="scroll"><table><thead><tr><th>Variante</th><th>Ubicación</th><th>Stock esperado</th><th>Conteo</th><th>Diferencia</th><th>Ajuste</th></tr></thead><tbody>@for (detalle of item.detalles; track detalle) {
        <tr><td><strong>{{ detalle.productoSku || ('Variante #' + detalle.productoVarianteId) }}</strong><small>{{ descripcion(detalle) }}</small></td><td>{{ detalle.ubicacionAlmacenId ? ('#' + detalle.ubicacionAlmacenId) : 'Sin ubicación' }}</td><td>{{ stockEsperadoVisible(detalle) }}</td><td>@if (puedeEditar && item.estado === Estado.EnProceso) {
        <mat-form-field appearance="outline" class="cantidad"><input matInput type="number" min="0" [name]="'cantidad-' + detalle.id" [(ngModel)]="capturas[detalle.id]" [attr.aria-label]="'Cantidad contada para ' + (detalle.productoSku || detalle.productoVarianteId)" /></mat-form-field>
        }@if (item.estado !== Estado.EnProceso || !puedeEditar) {
        <span>{{ detalle.cantidadContada ?? 'Pendiente' }}</span>
        }</td><td [class.diff]="detalle.diferencia !== 0 && detalle.diferencia != null">{{ detalle.diferencia ?? '—' }}</td><td>@if (detalle.ajusteInventarioId) {
        <button mat-button type="button" (click)="verAjuste(detalle.ajusteInventarioId)">#{{ detalle.ajusteInventarioId }}</button>
        }@if (!detalle.ajusteInventarioId) {
        <span>—</span>
      }</td></tr>
    }</tbody></table></div>
    </section>
    </section>
    } @else {
      @if (loading) {
        <div class="state"><mat-spinner diameter="38"></mat-spinner><span>Cargando conteo…</span></div>
        }@if (!loading && error) {
        <div class="state error"><mat-icon>error_outline</mat-icon>{{ error }}<button mat-button type="button" (click)="cargar()">Reintentar</button></div>
      }
    }
    
    `,
  styles: [`
    .page{padding:24px;display:grid;gap:20px}.header,.title,.toolbar,.section-title{display:flex;align-items:center}.header,.section-title{justify-content:space-between;gap:16px}.title{gap:10px;align-items:flex-start}.title h1,.section-title h2{margin:0}.title p,.section-title p{margin:4px 0;color:#667085}.eyebrow{text-transform:uppercase;letter-spacing:.08em;font-size:.72rem;font-weight:700;color:var(--primary,#3f51b5)!important}.badge{padding:6px 12px;border-radius:999px;background:#f2f4f7;font-weight:700}.summary{display:grid;grid-template-columns:repeat(4,1fr);gap:12px}.summary article,.meta{border:1px solid #e4e7ec;border-radius:12px;padding:16px}.summary span,.meta span{display:block;font-size:.78rem;color:#667085}.summary strong{font-size:1.45rem}.meta{display:grid;grid-template-columns:repeat(4,1fr);gap:18px}.toolbar{gap:8px;flex-wrap:wrap}.toolbar mat-icon{margin-right:5px}.table-wrap{border:1px solid #e4e7ec;border-radius:12px;overflow:hidden}.section-title{padding:16px}.scroll{overflow:auto}table{width:100%;border-collapse:collapse;min-width:850px}th,td{padding:12px 14px;text-align:left;border-top:1px solid #eaecf0}th{font-size:.78rem;color:#667085;background:#f9fafb}.cantidad{width:120px;margin-bottom:-20px}td small{display:block;color:#667085;margin-top:3px}.diff{font-weight:700;color:#b54708}.state{min-height:260px;display:flex;gap:12px;align-items:center;justify-content:center}.error{color:#b42318}.page>.error{padding:12px;border-radius:8px;background:#fef3f2}@media(max-width:850px){.page{padding:16px}.summary{grid-template-columns:1fr 1fr}.meta{grid-template-columns:1fr 1fr}.header,.section-title{align-items:flex-start;flex-direction:column}}@media(max-width:520px){.summary,.meta{grid-template-columns:1fr}}
  `]
})
export class ConteoInventarioDetailComponent implements OnInit {
  readonly Estado = EstadoConteoInventario;
  conteo: ConteoInventario | null = null;
  capturas: Record<number, number | null> = {};
  loading = true;
  accionando = false;
  error = '';
  private id = 0;
  private capturasOriginales: Record<number, number | null> = {};

  constructor(
    private readonly service: ConteoInventarioService,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly permisos: PermisosRuntimeService,
    private readonly alerts: AppAlertService
  ) {}
  ngOnInit(): void { this.id = Number(this.route.snapshot.paramMap.get('id')); this.cargar(); }
  get puedeEditar(): boolean { return this.permisos.puede('MovimientosInventario', 'Editar'); }
  get puedeCrear(): boolean { return this.permisos.puede('MovimientosInventario', 'Crear'); }
  get puedeCerrar(): boolean { return this.permisos.puede('MovimientosInventario', 'Cerrar'); }
  get puedeAprobar(): boolean { return this.permisos.puede('MovimientosInventario', 'Aprobar'); }
  get puedeAnular(): boolean { return this.permisos.puede('MovimientosInventario', 'Anular'); }
  get puedeCambiarEstado(): boolean { return this.permisos.puede('MovimientosInventario', 'CambiarEstado'); }
  get hayCapturasPendientes(): boolean {
    return Object.entries(this.capturas).some(([detalleId, value]) => {
      const id = Number(detalleId);
      return value !== null && value !== undefined && Number.isInteger(value) && value >= 0 && value !== this.capturasOriginales[id];
    });
  }

  cargar(): void {
    if (!Number.isInteger(this.id) || this.id <= 0) { this.loading = false; this.error = 'Identificador de conteo inválido.'; return; }
    this.loading = true; this.error = '';
    this.service.getById(this.id).pipe(finalize(() => this.loading = false)).subscribe({
      next: response => {
        if (!response.success) { this.error = response.message || 'No se pudo cargar el conteo.'; return; }
        this.conteo = response.data;
        this.capturas = {};
        this.capturasOriginales = {};
        for (const detalle of response.data.detalles) {
          const cantidad = detalle.cantidadContada ?? null;
          this.capturas[detalle.id] = cantidad;
          this.capturasOriginales[detalle.id] = cantidad;
        }
      },
      error: () => this.error = 'No se pudo cargar el conteo.'
    });
  }

  guardarCapturas(): void {
    if (!this.conteo) return;
    const lineas: CapturarConteoInventarioLinea[] = [];
    for (const detalle of this.conteo.detalles) {
      const cantidadContada = this.capturas[detalle.id];
      if (cantidadContada === null || cantidadContada === undefined || cantidadContada === this.capturasOriginales[detalle.id]) continue;
      if (!Number.isInteger(cantidadContada) || cantidadContada < 0) { this.error = 'Las cantidades capturadas deben ser enteros mayores o iguales a cero.'; return; }
      lineas.push({ detalleId: detalle.id, cantidadContada });
    }
    if (!lineas.length) { this.error = 'No hay cambios de captura pendientes.'; return; }
    this.ejecutar(this.service.capturarLote(this.id, lineas));
  }
  async iniciar(): Promise<void> {
    const confirmado = await this.alerts.confirmar({
      titulo: 'Iniciar conteo físico',
      mensaje: '¿Iniciar el conteo? El alcance quedará bloqueado para captura.',
      tipo: 'advertencia',
      confirmarTexto: 'Iniciar'
    });
    if (confirmado) this.ejecutar(this.service.iniciar(this.id));
  }

  async cerrar(): Promise<void> {
    const confirmado = await this.alerts.confirmar({
      titulo: 'Cerrar conteo físico',
      mensaje: '¿Cerrar el conteo? Verifica que todas las líneas estén capturadas.',
      tipo: 'advertencia',
      confirmarTexto: 'Cerrar conteo'
    });
    if (confirmado) this.ejecutar(this.service.cerrar(this.id));
  }

  async aprobar(): Promise<void> {
    const confirmado = await this.alerts.confirmar({
      titulo: 'Aprobar diferencias',
      mensaje: '¿Aprobar las diferencias de este conteo?',
      tipo: 'advertencia',
      confirmarTexto: 'Aprobar'
    });
    if (confirmado) this.ejecutar(this.service.aprobar(this.id));
  }

  async generarAjuste(): Promise<void> {
    const confirmado = await this.alerts.confirmar({
      titulo: 'Generar ajuste de inventario',
      mensaje: '¿Generar un ajuste borrador con las diferencias? El ajuste requerirá confirmación formal posterior.',
      tipo: 'advertencia',
      confirmarTexto: 'Generar ajuste'
    });
    if (!confirmado) return;
    this.accionando = true;
    this.error = '';
    this.service.generarAjuste(this.id).pipe(finalize(() => this.accionando = false)).subscribe({
      next: response => {
        if (!response.success) {
          this.error = response.message || 'No se pudo generar el ajuste.';
          return;
        }
        void this.router.navigate(['/inventario/ajustes', response.data.id]);
      },
      error: err => this.error = err?.error?.message || 'No se pudo generar el ajuste.'
    });
  }

  async cancelar(): Promise<void> {
    const motivo = await this.alerts.solicitarTexto({
      titulo: 'Cancelar conteo físico',
      mensaje: 'Indica el motivo de cancelación. Esta acción no se puede deshacer.',
      tipo: 'peligro',
      confirmarTexto: 'Cancelar conteo',
      entrada: { etiqueta: 'Motivo de cancelación', requerida: true }
    });
    if (motivo) this.ejecutar(this.service.cancelar(this.id, motivo));
  }
  editar(): void { void this.router.navigate(['/inventario/conteos', this.id, 'editar']); }
  volver(): void { void this.router.navigate(['/inventario/conteos']); }
  verAjuste(id: number): void { void this.router.navigate(['/inventario/ajustes', id]); }
  descripcion(detalle: ConteoInventarioDetalle): string { return [detalle.productoMarca, detalle.productoModelo, detalle.productoColor, detalle.productoTalla].filter(Boolean).join(' · ') || `Variante #${detalle.productoVarianteId}`; }
  stockEsperadoVisible(detalle: ConteoInventarioDetalle): string | number { return this.conteo?.esCiego && this.conteo.estado === EstadoConteoInventario.EnProceso ? 'Oculto' : (detalle.stockEsperado ?? '—'); }

  private ejecutar(request: ReturnType<ConteoInventarioService['iniciar']>): void {
    this.accionando = true; this.error = '';
    request.pipe(finalize(() => this.accionando = false)).subscribe({ next: response => { if (!response.success) { this.error = response.message || 'No se pudo completar la operación.'; return; } this.conteo = response.data; this.cargar(); }, error: err => this.error = err?.error?.message || 'No se pudo completar la operación.' });
  }
}
