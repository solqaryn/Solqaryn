
import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { EstadoPreparacionPedidoVenta, PreparacionPedidoVenta } from './preparacion-pedido-venta.model';
import { PreparacionPedidoVentaService } from './preparacion-pedido-venta.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-preparaciones-pedido-venta',
  standalone: true,
  imports: [RouterLink, MatButtonModule, MatCardModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule, MatSnackBarModule],
  template: `
    <section class="page">
      <header class="hero">
        <div>
          <p class="eyebrow">Ventas · logística</p>
          <h1>Preparación y despacho</h1>
          <p>Picking, packing, despacho y entrega vinculados al pedido confirmado y su reserva de inventario.</p>
        </div>
        <a mat-stroked-button routerLink="/pedidos-venta"><mat-icon>shopping_bag</mat-icon> Pedidos</a>
      </header>

      <mat-card class="lookup">
        <mat-card-content>
          <form class="lookup-form" (submit)="buscar($event)">
            <mat-form-field appearance="outline">
              <mat-label>ID del pedido de venta</mat-label>
              <input matInput type="number" min="1" [value]="pedidoBuscado() ?? ''" (input)="actualizarPedido($any($event.target).value)" aria-describedby="pedido-ayuda">
              <mat-hint id="pedido-ayuda">Consulta una preparación existente o inicia una para un pedido confirmado.</mat-hint>
            </mat-form-field>
            <button mat-flat-button color="primary" type="submit" [disabled]="!pedidoBuscado() || loading()">
              <mat-icon>search</mat-icon> Consultar
            </button>
          </form>
        </mat-card-content>
      </mat-card>

      @if (loading()) {
        <div class="center" role="status" aria-live="polite"><mat-spinner diameter="44"></mat-spinner><span>Cargando preparación…</span></div>
      } @else if (error()) {
        <mat-card class="empty" role="alert">
          <mat-card-content>
            <mat-icon>warehouse</mat-icon>
            <h2>No hay preparación disponible</h2>
            <p>{{ error() }}</p>
            @if (pedidoBuscado() && puede('Crear')) {
              <button mat-flat-button color="primary" (click)="iniciar()" [disabled]="saving()">
                <mat-icon>play_arrow</mat-icon> Iniciar preparación
              </button>
            }
          </mat-card-content>
        </mat-card>
      } @else if (preparacion(); as p) {
        <div class="summary">
          <mat-card><mat-card-content><span>Preparación</span><strong>PREP-{{p.id}}</strong></mat-card-content></mat-card>
          <mat-card><mat-card-content><span>Pedido</span><strong>PED-{{p.pedidoVentaId}}</strong></mat-card-content></mat-card>
          <mat-card><mat-card-content><span>Reserva</span><strong>RES-{{p.reservaInventarioId}}</strong></mat-card-content></mat-card>
          <mat-card><mat-card-content><span>Estado</span><strong>{{ nombreEstado(p.estado) }}</strong></mat-card-content></mat-card>
        </div>

        <mat-card class="workflow">
          <mat-card-header><mat-card-title>Flujo operativo</mat-card-title></mat-card-header>
          <mat-card-content>
            <ol class="steps" aria-label="Progreso de preparación">
              @for (s of pasos; track s.estado) {
                <li [class.done]="p.estado >= s.estado && p.estado !== Estado.Cancelado" [class.current]="p.estado === s.estado">
                  <span>{{s.numero}}</span><div><strong>{{s.titulo}}</strong><small>{{s.descripcion}}</small></div>
                </li>
              }
            </ol>
            @if (p.estado === Estado.Cancelado) {
              <div class="cancelled" role="status"><mat-icon>cancel</mat-icon><div><strong>Preparación cancelada</strong><span>{{p.motivoCancelacion || 'Sin motivo registrado.'}}</span></div></div>
            }
            <div class="actions">
              @if (p.estado === Estado.PendientePicking && puede('Editar')) {
                <button mat-flat-button color="primary" (click)="accion('picking')" [disabled]="saving()"><mat-icon>checklist</mat-icon> Completar picking</button>
              }
              @if (p.estado === Estado.PickingCompletado && puede('Editar')) {
                <button mat-flat-button color="primary" (click)="accion('packing')" [disabled]="saving()"><mat-icon>check_circle</mat-icon> Completar packing</button>
              }
              @if (p.estado === Estado.PackingCompletado && puede('Confirmar')) {
                <button mat-flat-button color="primary" (click)="accion('despachar')" [disabled]="saving()"><mat-icon>local_shipping</mat-icon> Marcar despachado</button>
              }
              @if (p.estado === Estado.Despachado && puede('Confirmar')) {
                <button mat-flat-button color="primary" (click)="accion('entregar')" [disabled]="saving()"><mat-icon>task_alt</mat-icon> Marcar entregado</button>
              }
              @if (p.estado < Estado.Despachado && puede('Anular')) {
                <button mat-stroked-button color="warn" (click)="cancelar()" [disabled]="saving()"><mat-icon>cancel</mat-icon> Cancelar</button>
              }
            </div>
          </mat-card-content>
        </mat-card>

        <mat-card>
          <mat-card-header><mat-card-title>Detalle físico</mat-card-title></mat-card-header>
          <mat-card-content>
            <div class="table-wrap">
              <table>
                <thead><tr><th>Variante</th><th>SKU / descripción</th><th>Almacén</th><th>Ubicación</th><th>Cantidad</th></tr></thead>
                <tbody>
                  @for (d of p.detalles; track d.id) {
                    <tr>
                      <td>#{{d.productoVarianteId}}</td>
                      <td>{{ descripcion(d) }}</td>
                      <td>ALM-{{d.almacenId}}</td>
                      <td>{{d.ubicacionAlmacenId ? ('UBI-' + d.ubicacionAlmacenId) : '—'}}</td>
                      <td>{{d.cantidadPreparar}}</td>
                    </tr>
                  } @empty {
                    <tr><td colspan="5">La preparación no contiene detalles físicos.</td></tr>
                  }
                </tbody>
              </table>
            </div>
          </mat-card-content>
        </mat-card>
      }
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .page{max-width:1180px;margin:auto;padding:16px;display:grid;gap:16px}.hero{display:flex;justify-content:space-between;gap:20px;align-items:flex-start}.hero h1{margin:.15rem 0}.hero p{margin:.2rem 0;max-width:760px}.eyebrow{text-transform:uppercase;letter-spacing:.08em;font-weight:700;font-size:.78rem;opacity:.7}.lookup-form{display:flex;gap:12px;align-items:flex-start}.lookup-form mat-form-field{flex:1;max-width:520px}.center{display:flex;flex-direction:column;align-items:center;gap:12px;padding:40px}.empty{text-align:center}.empty mat-icon{font-size:40px;width:40px;height:40px}.summary{display:grid;grid-template-columns:repeat(4,1fr);gap:12px}.summary mat-card-content{display:flex;flex-direction:column;gap:4px}.summary span{font-size:.8rem;opacity:.7}.summary strong{font-size:1.05rem}.steps{list-style:none;margin:0;padding:0;display:grid;grid-template-columns:repeat(4,1fr);gap:10px}.steps li{display:flex;gap:10px;align-items:flex-start;padding:12px;border:1px solid var(--color-border);border-radius:10px;opacity:.62}.steps li>span{display:grid;place-items:center;width:28px;height:28px;border-radius:999px;border:1px solid currentColor;font-weight:700}.steps li div{display:flex;flex-direction:column}.steps small{opacity:.75}.steps .done,.steps .current{opacity:1}.steps .current{outline:2px solid currentColor;outline-offset:1px}.actions{display:flex;gap:10px;flex-wrap:wrap;margin-top:18px}.cancelled{display:flex;gap:10px;align-items:center;padding:14px;border:1px solid var(--color-border);border-radius:10px;margin-top:14px}.cancelled div{display:flex;flex-direction:column}.table-wrap{overflow:auto}table{width:100%;border-collapse:collapse}th,td{text-align:left;padding:11px;border-bottom:1px solid var(--color-border)}@media(max-width:850px){.summary,.steps{grid-template-columns:1fr 1fr}}@media(max-width:650px){.hero,.lookup-form{flex-direction:column}.summary,.steps{grid-template-columns:1fr}.lookup-form mat-form-field{width:100%}}
  `]
})
export class PreparacionesPedidoVentaComponent implements OnInit {
  private readonly svc = inject(PreparacionPedidoVentaService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);
  private readonly alerts = inject(AppAlertService);

  readonly preparacion = signal<PreparacionPedidoVenta | null>(null);
  readonly pedidoBuscado = signal<number | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly Estado = EstadoPreparacionPedidoVenta;
  readonly pasos = [
    { estado: EstadoPreparacionPedidoVenta.PendientePicking, numero: 1, titulo: 'Picking', descripcion: 'Preparar unidades reservadas.' },
    { estado: EstadoPreparacionPedidoVenta.PickingCompletado, numero: 2, titulo: 'Packing', descripcion: 'Validar y empacar el pedido.' },
    { estado: EstadoPreparacionPedidoVenta.Despachado, numero: 3, titulo: 'Despacho', descripcion: 'Entregar al transportista.' },
    { estado: EstadoPreparacionPedidoVenta.Entregado, numero: 4, titulo: 'Entrega', descripcion: 'Confirmar recepción final.' }
  ];

  ngOnInit(): void {
    const pedidoVentaId = Number(this.route.snapshot.paramMap.get('pedidoVentaId') ?? this.route.snapshot.queryParamMap.get('pedidoVentaId'));
    if (Number.isInteger(pedidoVentaId) && pedidoVentaId > 0) {
      this.pedidoBuscado.set(pedidoVentaId);
      this.cargar(pedidoVentaId);
    }
  }

  puede(accion: string): boolean { return this.permisos.puede('Ventas', accion); }

  actualizarPedido(raw: string): void {
    const id = Number(raw);
    this.pedidoBuscado.set(Number.isInteger(id) && id > 0 ? id : null);
  }

  buscar(event: Event): void {
    event.preventDefault();
    const id = this.pedidoBuscado();
    if (!id) return;
    void this.router.navigate(['/pedidos-venta', id, 'preparacion']);
    this.cargar(id);
  }

  cargar(pedidoVentaId: number): void {
    this.loading.set(true); this.error.set(null); this.preparacion.set(null);
    this.svc.getByPedidoVentaId(pedidoVentaId).subscribe({
      next: r => { this.preparacion.set(r.data); this.loading.set(false); },
      error: e => { this.loading.set(false); this.error.set(e.error?.message || e.error?.detail || 'No existe una preparación para este pedido.'); }
    });
  }

  iniciar(): void {
    const pedidoVentaId = this.pedidoBuscado();
    if (!pedidoVentaId || this.saving()) return;
    this.saving.set(true);
    this.svc.iniciar(pedidoVentaId).subscribe({
      next: r => { this.preparacion.set(r.data); this.error.set(null); this.saving.set(false); this.snack.open('Preparación iniciada.','Cerrar',{duration:3000}); },
      error: e => { this.saving.set(false); this.snack.open(e.error?.message || e.error?.detail || 'No fue posible iniciar la preparación.','Cerrar',{duration:5000}); }
    });
  }

  accion(tipo: 'picking' | 'packing' | 'despachar' | 'entregar'): void {
    const p = this.preparacion(); if (!p || this.saving()) return;
    this.saving.set(true);
    const request = tipo === 'picking' ? this.svc.completarPicking(p.id) : tipo === 'packing' ? this.svc.completarPacking(p.id) : tipo === 'despachar' ? this.svc.despachar(p.id) : this.svc.entregar(p.id);
    request.subscribe({
      next: r => { this.preparacion.set(r.data); this.saving.set(false); this.snack.open('Estado actualizado correctamente.','Cerrar',{duration:3000}); },
      error: e => { this.saving.set(false); this.snack.open(e.error?.message || e.error?.detail || 'No fue posible actualizar la preparación.','Cerrar',{duration:5000}); }
    });
  }

  async cancelar(): Promise<void> {
    const p = this.preparacion(); if (!p || this.saving()) return;
    const motivo = await this.alerts.solicitarTexto({
      titulo: 'Cancelar preparación',
      mensaje: 'La preparación se marcará como cancelada y dejará de avanzar en el despacho.',
      detalle: 'Indica un motivo trazable para la auditoría.',
      tipo: 'peligro',
      entrada: { etiqueta: 'Motivo de cancelación', requerida: true },
      confirmarTexto: 'Cancelar preparación'
    });
    if (!motivo) return;
    this.saving.set(true);
    this.svc.cancelar(p.id, motivo).subscribe({
      next: r => { this.preparacion.set(r.data); this.saving.set(false); this.snack.open('Preparación cancelada.','Cerrar',{duration:3000}); },
      error: e => { this.saving.set(false); this.snack.open(e.error?.message || e.error?.detail || 'No fue posible cancelar la preparación.','Cerrar',{duration:5000}); }
    });
  }

  nombreEstado(estado: EstadoPreparacionPedidoVenta): string {
    return ({1:'Pendiente de picking',2:'Picking completado',3:'Packing completado',4:'Despachado',5:'Entregado',6:'Cancelado'} as Record<number,string>)[estado] ?? String(estado);
  }

  descripcion(d: PreparacionPedidoVenta['detalles'][number]): string {
    return [d.productoSkuSnapshot, d.productoMarcaSnapshot, d.productoModeloSnapshot, d.productoColorSnapshot, d.productoTallaSnapshot].filter(Boolean).join(' · ') || `Variante #${d.productoVarianteId}`;
  }
}
