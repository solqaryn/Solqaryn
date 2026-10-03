import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, SimpleChanges, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { forkJoin, finalize } from 'rxjs';
import {
  ConfiguracionTrazabilidadVariante,
  CrearLoteInventarioRequest,
  CrearSerieInventarioRequest,
  LoteInventario,
  SerieInventario
} from '../../core/models/trazabilidad-inventario.model';
import { TrazabilidadInventarioService } from '../../services/trazabilidad-inventario.service';

@Component({
  selector: 'app-trazabilidad-variante-panel',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule
  ],
  template: `
    <section class="trace-card" [attr.aria-busy]="loading()">
      <header class="trace-header">
        <div>
          <p class="eyebrow">ERP · Trazabilidad</p>
          <h3>Política de lote, serie y vencimiento</h3>
          <p class="subtitle">Activa únicamente las dimensiones que esta variante necesita. La operación diaria mostrará sólo los datos aplicables.</p>
        </div>
        <button mat-stroked-button type="button" (click)="recargar()" [disabled]="loading() || saving()">
          <mat-icon>refresh</mat-icon> Actualizar
        </button>
      </header>
    
      @if (error()) {
        <div class="feedback error" role="alert">
          <mat-icon>error_outline</mat-icon><span>{{ error() }}</span>
        </div>
      }
      @if (success()) {
        <div class="feedback success" role="status">
          <mat-icon>check_circle</mat-icon><span>{{ success() }}</span>
        </div>
      }
    
      @if (loading()) {
        <div class="loading">
          <mat-spinner diameter="32"></mat-spinner><span>Cargando trazabilidad de la variante…</span>
        </div>
      }
    
      @if (!loading()) {
        <fieldset class="policy" [disabled]="saving()">
          <legend>Política operativa</legend>
          <div class="checks">
            <mat-checkbox [(ngModel)]="controlaLote" name="controlaLote">Controlar lote</mat-checkbox>
            <mat-checkbox [(ngModel)]="controlaNumeroSerie" name="controlaNumeroSerie">Controlar número de serie</mat-checkbox>
            <mat-checkbox [(ngModel)]="controlaFechaVencimiento" name="controlaFechaVencimiento">Controlar fecha de vencimiento</mat-checkbox>
          </div>
          @if (controlaFechaVencimiento) {
            <mat-form-field appearance="outline">
              <mat-label>Días de alerta de vencimiento</mat-label>
              <input matInput type="number" min="0" step="1" [(ngModel)]="diasAlertaVencimiento" name="diasAlertaVencimiento" />
              <mat-hint>Opcional. Permite anticipar lotes próximos a vencer.</mat-hint>
            </mat-form-field>
          }
          <div class="policy-actions">
            <button mat-flat-button color="primary" type="button" (click)="guardarPolitica()" [disabled]="saving() || !politicaValida()">
              @if (saving()) {
                <mat-spinner diameter="18"></mat-spinner>
              }
              @if (!saving()) {
                <mat-icon>save</mat-icon>
              }
              Guardar política
            </button>
          </div>
        </fieldset>
        @if (controlaLote || controlaNumeroSerie) {
          <div class="trace-grid">
            @if (controlaLote) {
              <section class="subcard">
                <header>
                  <div><h4>Lotes</h4><p>Captura el lote y sus fechas sólo cuando esta variante lo requiera.</p></div>
                  <span class="count">{{ lotes().length }}</span>
                </header>
                <div class="form-grid">
                  <mat-form-field appearance="outline">
                    <mat-label>Código de lote</mat-label>
                    <input matInput [(ngModel)]="nuevoLoteCodigo" name="nuevoLoteCodigo" maxlength="100" />
                  </mat-form-field>
                  <mat-form-field appearance="outline">
                    <mat-label>Fabricación</mat-label>
                    <input matInput type="date" [(ngModel)]="nuevoLoteFabricacion" name="nuevoLoteFabricacion" />
                  </mat-form-field>
                  @if (controlaFechaVencimiento) {
                    <mat-form-field appearance="outline">
                      <mat-label>Vencimiento</mat-label>
                      <input matInput type="date" [(ngModel)]="nuevoLoteVencimiento" name="nuevoLoteVencimiento" />
                    </mat-form-field>
                  }
                </div>
                <button mat-stroked-button type="button" (click)="crearLote()" [disabled]="savingLote() || !nuevoLoteCodigo.trim()">
                  @if (savingLote()) {
                    <mat-spinner diameter="18"></mat-spinner>
                  }
                  @if (!savingLote()) {
                    <mat-icon>add</mat-icon>
                    } Registrar lote
                  </button>
                  @if (lotes().length === 0) {
                    <div class="empty">Aún no hay lotes registrados para esta variante.</div>
                  }
                  @if (lotes().length > 0) {
                    <div class="records">
                      @for (lote of lotes(); track lote) {
                        <article class="record">
                          <div>
                            <strong>{{ lote.codigo }}</strong>
                            <p>
                              @if (lote.fechaFabricacion) {
                                <span>Fab. {{ lote.fechaFabricacion | date:'yyyy-MM-dd' }}</span>
                              }
                              @if (lote.fechaVencimiento) {
                                <span> · Vence {{ lote.fechaVencimiento | date:'yyyy-MM-dd' }}</span>
                              }
                            </p>
                          </div>
                          <span class="status" [class.off]="!lote.activo">{{ lote.activo ? 'Activo' : 'Inactivo' }}</span>
                        </article>
                      }
                    </div>
                  }
                </section>
              }
              @if (controlaNumeroSerie) {
                <section class="subcard">
                  <header>
                    <div><h4>Series</h4><p>Una serie identifica una unidad individual; puede asociarse a un lote activo.</p></div>
                    <span class="count">{{ series().length }}</span>
                  </header>
                  <div class="form-grid">
                    <mat-form-field appearance="outline">
                      <mat-label>Número de serie</mat-label>
                      <input matInput [(ngModel)]="nuevaSerieNumero" name="nuevaSerieNumero" maxlength="160" />
                    </mat-form-field>
                    @if (controlaLote) {
                      <mat-form-field appearance="outline">
                        <mat-label>Lote</mat-label>
                        <mat-select [(ngModel)]="nuevaSerieLoteId" name="nuevaSerieLoteId">
                          <mat-option [value]="null">Sin lote</mat-option>
                          @for (lote of lotesActivos; track lote) {
                            <mat-option [value]="lote.id">{{ lote.codigo }}</mat-option>
                          }
                        </mat-select>
                      </mat-form-field>
                    }
                  </div>
                  <button mat-stroked-button type="button" (click)="crearSerie()" [disabled]="savingSerie() || !nuevaSerieNumero.trim()">
                    @if (savingSerie()) {
                      <mat-spinner diameter="18"></mat-spinner>
                    }
                    @if (!savingSerie()) {
                      <mat-icon>add</mat-icon>
                      } Registrar serie
                    </button>
                    @if (series().length === 0) {
                      <div class="empty">Aún no hay números de serie registrados para esta variante.</div>
                    }
                    @if (series().length > 0) {
                      <div class="records">
                        @for (serie of series(); track serie) {
                          <article class="record">
                            <div>
                              <strong>{{ serie.numeroSerie }}</strong>
                              <p>{{ loteCodigo(serie.loteInventarioId) }}</p>
                            </div>
                            <span class="status">Estado {{ serie.estado }}</span>
                          </article>
                        }
                      </div>
                    }
                  </section>
                }
              </div>
            }
            @if (!controlaLote && !controlaNumeroSerie) {
              <div class="empty policy-empty">
                La variante no exige captura de lote ni serie. Activa una política sólo si el negocio necesita esa trazabilidad.
              </div>
            }
          }
        </section>
    `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    :host{display:block}.trace-card{border:1px solid rgba(127,127,127,.24);border-radius:16px;padding:20px;background:rgba(127,127,127,.025)}.trace-header{display:flex;justify-content:space-between;gap:16px;align-items:flex-start;margin-bottom:18px}.eyebrow{margin:0 0 4px;font-size:11px;font-weight:800;letter-spacing:.1em;text-transform:uppercase;opacity:.62}h3,h4{margin:0}.subtitle,.subcard header p,.record p{margin:5px 0 0;opacity:.7}.feedback,.loading{display:flex;align-items:center;gap:9px;padding:12px 14px;border-radius:10px;margin:10px 0}.feedback.error{background:rgba(244,67,54,.08);border:1px solid rgba(244,67,54,.25)}.feedback.success{background:rgba(76,175,80,.08);border:1px solid rgba(76,175,80,.25)}.loading{justify-content:center;min-height:110px}.policy{border:0;padding:0;margin:0}.policy legend{font-weight:700;margin-bottom:10px}.checks{display:flex;flex-wrap:wrap;gap:8px 24px;margin-bottom:14px}.policy mat-form-field{width:min(330px,100%)}.policy-actions{display:flex;justify-content:flex-end}.trace-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:16px;margin-top:20px}.subcard{border:1px solid rgba(127,127,127,.2);border-radius:12px;padding:16px}.subcard>header{display:flex;justify-content:space-between;gap:12px;margin-bottom:14px}.count{display:inline-flex;align-items:center;justify-content:center;min-width:30px;height:30px;border-radius:999px;background:rgba(127,127,127,.12);font-weight:700}.form-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:10px}.records{display:grid;gap:8px;margin-top:14px}.record{display:flex;justify-content:space-between;align-items:center;gap:12px;padding:10px 12px;border-radius:9px;background:rgba(127,127,127,.07)}.record p{font-size:12px}.status{font-size:12px;font-weight:700;white-space:nowrap}.status.off{opacity:.5}.empty{padding:14px;border-radius:9px;background:rgba(127,127,127,.06);opacity:.75;margin-top:14px}.policy-empty{margin-top:20px}@media(max-width:900px){.trace-grid{grid-template-columns:1fr}}@media(max-width:640px){.trace-card{padding:15px}.trace-header{flex-direction:column}.form-grid{grid-template-columns:1fr}.policy-actions button,.subcard>button{width:100%}}
  `]
})
export class TrazabilidadVariantePanelComponent implements OnChanges {
  @Input({ required: true }) productoVarianteId!: number;

  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly savingLote = signal(false);
  readonly savingSerie = signal(false);
  readonly error = signal('');
  readonly success = signal('');
  readonly configuracion = signal<ConfiguracionTrazabilidadVariante | null>(null);
  readonly lotes = signal<LoteInventario[]>([]);
  readonly series = signal<SerieInventario[]>([]);

  controlaLote = false;
  controlaNumeroSerie = false;
  controlaFechaVencimiento = false;
  diasAlertaVencimiento: number | null = null;
  nuevoLoteCodigo = '';
  nuevoLoteFabricacion = '';
  nuevoLoteVencimiento = '';
  nuevaSerieNumero = '';
  nuevaSerieLoteId: number | null = null;

  constructor(private readonly trazabilidadService: TrazabilidadInventarioService) {}

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['productoVarianteId'] && Number(this.productoVarianteId) > 0) this.recargar();
  }

  get lotesActivos(): LoteInventario[] { return this.lotes().filter(l => l.activo); }

  recargar(): void {
    if (!Number(this.productoVarianteId)) return;
    this.loading.set(true);
    this.error.set('');
    this.success.set('');
    forkJoin({
      configuracion: this.trazabilidadService.getConfiguracion(this.productoVarianteId),
      lotes: this.trazabilidadService.getLotes(this.productoVarianteId),
      series: this.trazabilidadService.getSeries(this.productoVarianteId)
    }).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: ({ configuracion, lotes, series }) => {
        if (!configuracion.success || !configuracion.data) {
          this.error.set(configuracion.message || 'No fue posible obtener la política de trazabilidad.');
          return;
        }
        this.aplicarConfiguracion(configuracion.data);
        this.lotes.set(lotes.success ? (lotes.data?.items ?? []) : []);
        this.series.set(series.success ? (series.data?.items ?? []) : []);
      },
      error: err => this.error.set(this.extraerError(err, 'No fue posible cargar la trazabilidad de la variante.'))
    });
  }

  guardarPolitica(): void {
    if (!this.politicaValida()) return;
    this.saving.set(true);
    this.error.set('');
    this.success.set('');
    this.trazabilidadService.configurar(this.productoVarianteId, {
      controlaLote: this.controlaLote,
      controlaNumeroSerie: this.controlaNumeroSerie,
      controlaFechaVencimiento: this.controlaFechaVencimiento,
      diasAlertaVencimiento: this.controlaFechaVencimiento && this.diasAlertaVencimiento != null
        ? Number(this.diasAlertaVencimiento)
        : null
    }).pipe(finalize(() => this.saving.set(false))).subscribe({
      next: response => {
        if (!response.success || !response.data) {
          this.error.set(response.message || 'No fue posible guardar la política de trazabilidad.');
          return;
        }
        this.aplicarConfiguracion(response.data);
        this.success.set('Política de trazabilidad actualizada.');
      },
      error: err => this.error.set(this.extraerError(err, 'No fue posible guardar la política de trazabilidad.'))
    });
  }

  crearLote(): void {
    const codigo = this.nuevoLoteCodigo.trim();
    if (!codigo) return;
    const request: CrearLoteInventarioRequest = {
      productoVarianteId: this.productoVarianteId,
      codigo,
      fechaFabricacion: this.nuevoLoteFabricacion || null,
      fechaVencimiento: this.controlaFechaVencimiento ? (this.nuevoLoteVencimiento || null) : null
    };
    this.savingLote.set(true);
    this.error.set('');
    this.success.set('');
    this.trazabilidadService.crearLote(request).pipe(finalize(() => this.savingLote.set(false))).subscribe({
      next: response => {
        if (!response.success || !response.data) {
          this.error.set(response.message || 'No fue posible registrar el lote.');
          return;
        }
        this.lotes.update(actuales => [response.data!, ...actuales.filter(l => l.id !== response.data!.id)]);
        this.nuevoLoteCodigo = '';
        this.nuevoLoteFabricacion = '';
        this.nuevoLoteVencimiento = '';
        this.success.set(`Lote ${response.data.codigo} registrado.`);
      },
      error: err => this.error.set(this.extraerError(err, 'No fue posible registrar el lote.'))
    });
  }

  crearSerie(): void {
    const numeroSerie = this.nuevaSerieNumero.trim();
    if (!numeroSerie) return;
    const request: CrearSerieInventarioRequest = {
      productoVarianteId: this.productoVarianteId,
      loteInventarioId: this.controlaLote ? this.nuevaSerieLoteId : null,
      numeroSerie
    };
    this.savingSerie.set(true);
    this.error.set('');
    this.success.set('');
    this.trazabilidadService.crearSerie(request).pipe(finalize(() => this.savingSerie.set(false))).subscribe({
      next: response => {
        if (!response.success || !response.data) {
          this.error.set(response.message || 'No fue posible registrar la serie.');
          return;
        }
        this.series.update(actuales => [response.data!, ...actuales.filter(s => s.id !== response.data!.id)]);
        this.nuevaSerieNumero = '';
        this.nuevaSerieLoteId = null;
        this.success.set(`Serie ${response.data.numeroSerie} registrada.`);
      },
      error: err => this.error.set(this.extraerError(err, 'No fue posible registrar la serie.'))
    });
  }

  politicaValida(): boolean {
    if (!this.controlaFechaVencimiento) return true;
    return this.diasAlertaVencimiento == null || (Number.isInteger(Number(this.diasAlertaVencimiento)) && Number(this.diasAlertaVencimiento) >= 0);
  }

  loteCodigo(loteInventarioId?: number | null): string {
    if (!loteInventarioId) return 'Sin lote asociado';
    return `Lote ${this.lotes().find(l => l.id === loteInventarioId)?.codigo ?? `#${loteInventarioId}`}`;
  }

  private aplicarConfiguracion(configuracion: ConfiguracionTrazabilidadVariante): void {
    this.configuracion.set(configuracion);
    this.controlaLote = configuracion.controlaLote;
    this.controlaNumeroSerie = configuracion.controlaNumeroSerie;
    this.controlaFechaVencimiento = configuracion.controlaFechaVencimiento;
    this.diasAlertaVencimiento = configuracion.diasAlertaVencimiento ?? null;
    if (!this.controlaLote) this.nuevaSerieLoteId = null;
    if (!this.controlaFechaVencimiento) this.nuevoLoteVencimiento = '';
  }

  private extraerError(err: any, fallback: string): string {
    return err?.error?.message || err?.error?.title || err?.message || fallback;
  }
}
