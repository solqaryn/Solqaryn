import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';

export type EstadoCaja = 1 | 2;
export type EstadoCajaSesion = 1 | 2 | 3 | 4;
export type TipoMovimientoCaja = 1 | 2 | 3 | 4 | 5;

export interface CajaVista {
  id: number;
  nombre: string;
  estado: EstadoCaja;
  sesionActivaId: number | null;
}

export interface CajaMovimientoVista {
  id: number;
  cajaSesionId: number;
  usuarioId: number;
  tipo: TipoMovimientoCaja;
  monto: number;
  referencia: string;
  fechaOperacion: string | Date;
  impactoSaldo: number;
}

export interface CajaSesionVista {
  id: number;
  cajaId: number;
  usuarioId: number;
  fechaApertura: string | Date;
  fechaCierre: string | Date | null;
  estado: EstadoCajaSesion;
  fondoInicial: number;
  totalIngresos: number;
  totalRetiros: number;
  totalDepositos: number;
  saldoEsperado: number | null;
  saldoContado: number | null;
  diferencia: number | null;
  observacionesArqueo: string | null;
  movimientos: ReadonlyArray<CajaMovimientoVista>;
}

export interface AperturaCajaUi {
  fondoInicial: number;
}

export interface MovimientoCajaUi {
  tipo: TipoMovimientoCaja;
  monto: number;
  referencia: string;
}

export interface ArqueoCajaUi {
  saldoContado: number;
  observacionesArqueo: string;
}

export type CajaAccionUi = 'ABRIR' | 'INICIAR_OPERACIONES' | 'REGISTRAR_MOVIMIENTO' | 'INICIAR_ARQUEO' | 'CERRAR';

@Component({
  selector: 'app-caja-flujo-shell',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule
  ],
  template: `
    <section class="caja-shell" aria-labelledby="caja-title">
      <header class="hero">
        <div>
          <p class="eyebrow">Caja</p>
          <h1 id="caja-title">{{ caja?.nombre || 'Operación de caja' }}</h1>
          <p class="subtitle">Apertura → Operaciones → Arqueo → Cierre</p>
        </div>
        <span class="status" [class.active]="caja?.estado === 2">
          {{ caja?.estado === 2 ? 'Activa' : 'Inactiva' }}
        </span>
      </header>
    
      @if (loading) {
        <div class="state-panel" role="status" aria-live="polite">
          <mat-spinner diameter="32"></mat-spinner>
          <span>Cargando información de caja…</span>
        </div>
      }
    
      @if (!loading && error) {
        <div class="state-panel error" role="alert">
          <mat-icon aria-hidden="true">error_outline</mat-icon>
          <span>{{ error }}</span>
        </div>
      }
    
      @if (!loading && !error) {
        <nav class="steps" aria-label="Ciclo de vida de la sesión">
          @for (step of pasos; track step) {
            <div class="step" [class.current]="sesion?.estado === step.id" [class.done]="sesion && sesion.estado > step.id">
              <span>{{ step.id }}</span><strong>{{ step.label }}</strong>
            </div>
          }
        </nav>
        @if (!sesion) {
          <div class="state-panel empty">
            <mat-icon aria-hidden="true">point_of_sale</mat-icon>
            <div>
              <strong>No hay una sesión activa</strong>
              <p>La caja está lista para iniciar una nueva apertura cuando el flujo autorizado lo permita.</p>
            </div>
          </div>
        }
        @if (!sesion && caja?.estado === 2) {
          <form class="operation-form" #aperturaForm="ngForm" (ngSubmit)="solicitarApertura()" aria-labelledby="apertura-title">
            <div class="form-heading">
              <div><p class="eyebrow">Apertura</p><h2 id="apertura-title">Preparar nueva sesión</h2></div>
              <span>Defina el fondo inicial antes de continuar.</span>
            </div>
            <mat-form-field appearance="outline">
              <mat-label>Fondo inicial</mat-label>
              <input matInput type="number" name="fondoInicial" [(ngModel)]="fondoInicial" min="0" step="0.01" required autocomplete="off">
              <span matTextPrefix>L&nbsp;</span>
            </mat-form-field>
            <div class="form-actions">
              <button mat-flat-button type="submit" [disabled]="!puedeOperar || aperturaForm.invalid || !montoNoNegativo(fondoInicial)">Abrir sesión</button>
            </div>
          </form>
        }
        @if (sesion; as s) {
          <section class="metrics" aria-label="Resumen de sesión">
            <article><span>Fondo inicial</span><strong>{{ s.fondoInicial | currency:'HNL':'symbol-narrow':'1.2-2' }}</strong></article>
            <article><span>Ingresos</span><strong>{{ s.totalIngresos | currency:'HNL':'symbol-narrow':'1.2-2' }}</strong></article>
            <article><span>Retiros</span><strong>{{ s.totalRetiros | currency:'HNL':'symbol-narrow':'1.2-2' }}</strong></article>
            <article><span>Depósitos</span><strong>{{ s.totalDepositos | currency:'HNL':'symbol-narrow':'1.2-2' }}</strong></article>
            @if (s.saldoEsperado !== null) {
              <article><span>Saldo esperado</span><strong>{{ s.saldoEsperado | currency:'HNL':'symbol-narrow':'1.2-2' }}</strong></article>
            }
            @if (s.diferencia !== null) {
              <article><span>Diferencia</span><strong>{{ s.diferencia | currency:'HNL':'symbol-narrow':'1.2-2' }}</strong></article>
            }
          </section>
          <div class="actions" aria-label="Acciones disponibles">
            @if (s.estado === 1) {
              <button mat-stroked-button type="button" [disabled]="!puedeOperar" (click)="accion.emit('INICIAR_OPERACIONES')">Iniciar operaciones</button>
            }
            @if (s.estado === 2) {
              <button mat-stroked-button type="button" [disabled]="!puedeOperar" (click)="accion.emit('INICIAR_ARQUEO')">Iniciar arqueo</button>
            }
            @if (s.estado === 3) {
              <button mat-flat-button type="button" [disabled]="!puedeOperar || !montoNoNegativo(saldoContado)" (click)="accion.emit('CERRAR')">Cerrar sesión</button>
            }
          </div>
          @if (s.estado === 2) {
            <form class="operation-form" #movimientoForm="ngForm" (ngSubmit)="solicitarMovimiento()" aria-labelledby="movimiento-title">
              <div class="form-heading">
                <div><p class="eyebrow">Operaciones</p><h2 id="movimiento-title">Registrar movimiento</h2></div>
                <span>Los tipos disponibles corresponden al contrato de presentación de Caja.</span>
              </div>
              <div class="form-grid">
                <mat-form-field appearance="outline">
                  <mat-label>Tipo de movimiento</mat-label>
                  <mat-select name="tipoMovimientoSeleccionado" [(ngModel)]="tipoMovimientoSeleccionado" required>
                    @for (tipo of tiposMovimiento; track tipo) {
                      <mat-option [value]="tipo.id">{{ tipo.label }}</mat-option>
                    }
                  </mat-select>
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Monto</mat-label>
                  <input matInput type="number" name="montoMovimiento" [(ngModel)]="montoMovimiento" min="0.01" step="0.01" required autocomplete="off">
                  <span matTextPrefix>L&nbsp;</span>
                </mat-form-field>
                <mat-form-field appearance="outline" class="wide-field">
                  <mat-label>Referencia</mat-label>
                  <input matInput name="referenciaMovimiento" [(ngModel)]="referenciaMovimiento" maxlength="160" autocomplete="off">
                </mat-form-field>
              </div>
              <div class="form-actions">
                <button mat-flat-button type="submit" [disabled]="!puedeOperar || movimientoForm.invalid || !montoPositivo(montoMovimiento)">Registrar movimiento</button>
              </div>
            </form>
          }
          @if (s.estado === 3) {
            <form class="operation-form" #arqueoForm="ngForm" (ngSubmit)="solicitarArqueo()" aria-labelledby="arqueo-title">
              <div class="form-heading">
                <div><p class="eyebrow">Arqueo</p><h2 id="arqueo-title">Registrar saldo contado</h2></div>
                <span>El cierre permanece separado del registro de arqueo.</span>
              </div>
              <div class="form-grid">
                <mat-form-field appearance="outline">
                  <mat-label>Saldo contado</mat-label>
                  <input matInput type="number" name="saldoContado" [(ngModel)]="saldoContado" min="0" step="0.01" required autocomplete="off">
                  <span matTextPrefix>L&nbsp;</span>
                </mat-form-field>
                <mat-form-field appearance="outline" class="wide-field">
                  <mat-label>Observaciones de arqueo</mat-label>
                  <textarea matInput name="observacionesArqueo" [(ngModel)]="observacionesArqueo" maxlength="500" rows="3"></textarea>
                </mat-form-field>
              </div>
              <div class="form-actions">
                <button mat-stroked-button type="submit" [disabled]="!puedeOperar || arqueoForm.invalid || !montoNoNegativo(saldoContado)">Registrar arqueo</button>
              </div>
            </form>
          }
          <section class="movements" aria-labelledby="movements-title">
            <div class="section-heading">
              <div><p class="eyebrow">Trazabilidad</p><h2 id="movements-title">Movimientos</h2></div>
              <span>{{ s.movimientos.length }} registro{{ s.movimientos.length === 1 ? '' : 's' }}</span>
            </div>
            @if (s.movimientos.length === 0) {
              <div class="state-panel empty compact">
                <mat-icon aria-hidden="true">receipt_long</mat-icon>
                <span>Aún no hay movimientos registrados en esta sesión.</span>
              </div>
            }
            @if (s.movimientos.length > 0) {
              <div class="table-wrap">
                <table>
                  <thead><tr><th>Fecha</th><th>Tipo</th><th>Referencia</th><th class="amount">Monto</th><th class="amount">Impacto</th></tr></thead>
                  <tbody>
                    @for (movimiento of s.movimientos; track trackMovimiento($index, movimiento)) {
                      <tr>
                        <td>{{ movimiento.fechaOperacion | date:'dd/MM/yyyy HH:mm' }}</td>
                        <td><span class="movement-type">{{ tipoMovimiento(movimiento.tipo) }}</span></td>
                        <td>{{ movimiento.referencia || '—' }}</td>
                        <td class="amount">{{ movimiento.monto | currency:'HNL':'symbol-narrow':'1.2-2' }}</td>
                        <td class="amount" [class.negative]="movimiento.impactoSaldo < 0">{{ movimiento.impactoSaldo | currency:'HNL':'symbol-narrow':'1.2-2' }}</td>
                      </tr>
                    }
                  </tbody>
                </table>
              </div>
            }
          </section>
        }
      }
    </section>
    `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    :host { display: block; }
    .caja-shell { display: grid; gap: 1rem; padding: 1rem; color: var(--mat-app-text-color, #1f2937); }
    .hero, .section-heading, .actions, .form-heading, .form-actions { display: flex; align-items: center; justify-content: space-between; gap: 1rem; flex-wrap: wrap; }
    .hero h1, .section-heading h2, .form-heading h2 { margin: 0; }
    .eyebrow { margin: 0 0 .25rem; font-size: .75rem; font-weight: 700; letter-spacing: .08em; text-transform: uppercase; opacity: .65; }
    .subtitle { margin: .25rem 0 0; opacity: .72; }
    .status, .movement-type { border-radius: 999px; padding: .35rem .65rem; background: rgba(107,114,128,.12); font-weight: 600; }
    .status.active { background: rgba(22,163,74,.12); }
    .steps { display: grid; grid-template-columns: repeat(4, minmax(0,1fr)); gap: .5rem; }
    .step { display: flex; align-items: center; gap: .5rem; padding: .75rem; border: 1px solid rgba(107,114,128,.22); border-radius: .75rem; opacity: .62; }
    .step > span { width: 1.75rem; height: 1.75rem; display: grid; place-items: center; border-radius: 50%; background: rgba(107,114,128,.12); }
    .step.current, .step.done { opacity: 1; border-color: currentColor; }
    .metrics { display: grid; grid-template-columns: repeat(3, minmax(0,1fr)); gap: .75rem; }
    .metrics article { display: grid; gap: .35rem; padding: 1rem; border: 1px solid rgba(107,114,128,.18); border-radius: .85rem; }
    .metrics span { font-size: .8rem; opacity: .68; } .metrics strong { font-size: 1.2rem; }
    .state-panel { min-height: 7rem; display: flex; align-items: center; justify-content: center; gap: .75rem; padding: 1rem; border: 1px dashed rgba(107,114,128,.28); border-radius: .85rem; text-align: left; }
    .state-panel p { margin: .25rem 0 0; opacity: .7; } .state-panel.error { color: #b91c1c; } .state-panel.compact { min-height: 4rem; }
    .operation-form { display: grid; gap: 1rem; padding: 1rem; border: 1px solid rgba(107,114,128,.18); border-radius: .85rem; }
    .form-heading > span { max-width: 34rem; font-size: .85rem; opacity: .68; }
    .form-grid { display: grid; grid-template-columns: repeat(2,minmax(0,1fr)); gap: .75rem; align-items: start; }
    .wide-field { grid-column: 1 / -1; }
    .form-actions { justify-content: flex-end; }
    .movements { display: grid; gap: .75rem; } .section-heading > span { opacity: .65; font-size: .85rem; }
    .table-wrap { overflow-x: auto; border: 1px solid rgba(107,114,128,.18); border-radius: .85rem; }
    table { width: 100%; border-collapse: collapse; min-width: 680px; } th, td { padding: .8rem; text-align: left; border-bottom: 1px solid rgba(107,114,128,.14); } th { font-size: .75rem; text-transform: uppercase; letter-spacing: .04em; opacity: .65; }
    tbody tr:last-child td { border-bottom: 0; } .amount { text-align: right; font-variant-numeric: tabular-nums; } .negative { color: #b91c1c; }
    @media (max-width: 840px) { .steps { grid-template-columns: repeat(2,minmax(0,1fr)); } .metrics { grid-template-columns: repeat(2,minmax(0,1fr)); } }
    @media (max-width: 620px) { .form-grid { grid-template-columns: 1fr; } .wide-field { grid-column: auto; } }
    @media (max-width: 520px) { .caja-shell { padding: .5rem; } .steps, .metrics { grid-template-columns: 1fr; } .actions > button, .form-actions > button { width: 100%; } }
  `]
})
export class CajaFlujoShellComponent {
  @Input() caja: CajaVista | null = null;
  @Input() sesion: CajaSesionVista | null = null;
  @Input() loading = false;
  @Input() error = '';
  @Input() puedeOperar = false;
  @Output() readonly accion = new EventEmitter<CajaAccionUi>();
  @Output() readonly aperturaSolicitada = new EventEmitter<AperturaCajaUi>();
  @Output() readonly movimientoSolicitado = new EventEmitter<MovimientoCajaUi>();
  @Output() readonly arqueoSolicitado = new EventEmitter<ArqueoCajaUi>();

  fondoInicial: number | null = null;
  tipoMovimientoSeleccionado: TipoMovimientoCaja | null = null;
  montoMovimiento: number | null = null;
  referenciaMovimiento = '';
  saldoContado: number | null = null;
  observacionesArqueo = '';

  readonly pasos: ReadonlyArray<{ id: EstadoCajaSesion; label: string }> = [
    { id: 1, label: 'Apertura' },
    { id: 2, label: 'Operaciones' },
    { id: 3, label: 'Arqueo' },
    { id: 4, label: 'Cierre' }
  ];

  readonly tiposMovimiento: ReadonlyArray<{ id: TipoMovimientoCaja; label: string }> = [
    { id: 1, label: 'Ingreso' },
    { id: 2, label: 'Retiro' },
    { id: 3, label: 'Depósito bancario' },
    { id: 4, label: 'Diferencia sobrante' },
    { id: 5, label: 'Diferencia faltante' }
  ];

  solicitarApertura(): void {
    if (!this.puedeOperar || !this.montoNoNegativo(this.fondoInicial)) return;
    this.aperturaSolicitada.emit({ fondoInicial: this.fondoInicial });
    this.accion.emit('ABRIR');
  }

  solicitarMovimiento(): void {
    if (!this.puedeOperar || this.tipoMovimientoSeleccionado === null || !this.montoPositivo(this.montoMovimiento)) return;
    this.movimientoSolicitado.emit({
      tipo: this.tipoMovimientoSeleccionado,
      monto: this.montoMovimiento,
      referencia: this.referenciaMovimiento.trim()
    });
    this.accion.emit('REGISTRAR_MOVIMIENTO');
  }

  solicitarArqueo(): void {
    if (!this.puedeOperar || !this.montoNoNegativo(this.saldoContado)) return;
    this.arqueoSolicitado.emit({
      saldoContado: this.saldoContado,
      observacionesArqueo: this.observacionesArqueo.trim()
    });
  }

  montoPositivo(valor: number | null): valor is number {
    return valor !== null && Number.isFinite(valor) && valor > 0;
  }

  montoNoNegativo(valor: number | null): valor is number {
    return valor !== null && Number.isFinite(valor) && valor >= 0;
  }

  trackMovimiento(_: number, movimiento: CajaMovimientoVista): number { return movimiento.id; }

  tipoMovimiento(tipo: TipoMovimientoCaja): string {
    switch (tipo) {
      case 1: return 'Ingreso';
      case 2: return 'Retiro';
      case 3: return 'Depósito bancario';
      case 4: return 'Diferencia sobrante';
      case 5: return 'Diferencia faltante';
    }
  }
}
