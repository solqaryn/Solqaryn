
import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { forkJoin, finalize } from 'rxjs';
import { Almacen } from '../../core/models/almacen.model';
import { Producto, ProductoVariante } from '../../core/models/producto.model';
import { UbicacionAlmacen } from '../../core/models/ubicacion-almacen.model';
import { AlmacenService } from '../../services/almacen.service';
import { ExistenciaVarianteService } from '../../services/existencia-variante.service';
import { ProductoService } from '../../services/producto.service';
import { UbicacionAlmacenService } from '../../services/ubicacion-almacen.service';

@Component({
  selector: 'app-existencia-variante-form',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule],
  template: `
    <section class="page" aria-labelledby="form-title">
      <header class="header">
        <div>
          <p class="eyebrow">Inventario empresarial</p>
          <h1 id="form-title">{{ editando() ? 'Configurar existencia' : 'Nueva existencia por variante' }}</h1>
          @if (!editando()) {
            <p class="subtitle">Define la clave física variante + almacén + ubicación y sus niveles iniciales.</p>
          }
          @if (editando()) {
            <p class="subtitle">La existencia viva conserva su stock; aquí sólo se modifica ubicación y política mín./máx.</p>
          }
        </div>
        <button mat-stroked-button type="button" (click)="volver()"><mat-icon>arrow_back</mat-icon> Volver</button>
      </header>
    
      @if (error()) {
        <div class="feedback error" role="alert"><mat-icon>error_outline</mat-icon><span>{{ error() }}</span></div>
      }
      @if (loading()) {
        <div class="loading" aria-live="polite"><mat-spinner diameter="36"></mat-spinner><span>Cargando datos operativos…</span></div>
      }
    
      @if (!loading()) {
        <form class="card" (ngSubmit)="guardar()" #form="ngForm">
          <fieldset [disabled]="saving()">
            <legend>Clave física</legend>
            <div class="grid">
              <mat-form-field appearance="outline">
                <mat-label>Producto</mat-label>
                <mat-select name="productoId" [(ngModel)]="productoId" required [disabled]="editando()" (selectionChange)="onProductoChange()">
                  @for (producto of productos(); track producto) {
                    <mat-option [value]="producto.id">{{ producto.nombre }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Variante</mat-label>
                <mat-select name="productoVarianteId" [(ngModel)]="productoVarianteId" required [disabled]="editando() || !productoId || cargandoVariantes()">
                  @for (variante of variantes(); track variante) {
                    <mat-option [value]="variante.id">{{ etiquetaVariante(variante) }}</mat-option>
                  }
                </mat-select>
                @if (productoId && !cargandoVariantes() && variantes().length === 0) {
                  <mat-hint>No hay variantes activas.</mat-hint>
                }
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Almacén</mat-label>
                <mat-select name="almacenId" [(ngModel)]="almacenId" required [disabled]="editando()" (selectionChange)="onAlmacenChange()">
                  @for (almacen of almacenes(); track almacen) {
                    <mat-option [value]="almacen.id">{{ almacen.codigo }} · {{ almacen.nombre }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Ubicación (opcional)</mat-label>
                <mat-select name="ubicacionAlmacenId" [(ngModel)]="ubicacionAlmacenId" [disabled]="!almacenId || cargandoUbicaciones()">
                  <mat-option [value]="null">Raíz de almacén</mat-option>
                  @for (ubicacion of ubicaciones(); track ubicacion) {
                    <mat-option [value]="ubicacion.id">{{ ubicacion.codigo }} · {{ ubicacion.nombre }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
            </div>
          </fieldset>
          <fieldset>
            <legend>{{ editando() ? 'Stock actual (solo lectura)' : 'Stock inicial' }}</legend>
            <div class="grid stock-grid">
              <mat-form-field appearance="outline">
                <mat-label>Stock físico</mat-label>
                <input matInput type="number" min="0" step="1" name="stockFisico" [(ngModel)]="stockFisico" required [readonly]="editando()" />
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Stock reservado</mat-label>
                <input matInput type="number" min="0" step="1" name="stockReservado" [(ngModel)]="stockReservado" required [readonly]="editando()" />
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Stock disponible</mat-label>
                <input matInput [value]="stockDisponible" readonly aria-label="Stock disponible calculado" />
                <mat-hint>Derivado: físico − reservado.</mat-hint>
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Stock en tránsito</mat-label>
                <input matInput type="number" min="0" step="1" name="stockTransito" [(ngModel)]="stockTransito" required [readonly]="editando()" />
              </mat-form-field>
            </div>
          </fieldset>
          <fieldset>
            <legend>Política de inventario</legend>
            <div class="grid policy-grid">
              <mat-form-field appearance="outline">
                <mat-label>Stock mínimo</mat-label>
                <input matInput type="number" min="0" step="1" name="stockMinimo" [(ngModel)]="stockMinimo" required />
              </mat-form-field>
              <mat-form-field appearance="outline">
                <mat-label>Stock máximo (opcional)</mat-label>
                <input matInput type="number" min="0" step="1" name="stockMaximo" [(ngModel)]="stockMaximo" />
              </mat-form-field>
            </div>
          </fieldset>
          <div class="actions">
            <button mat-button type="button" (click)="volver()" [disabled]="saving()">Cancelar</button>
            <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || saving() || !puedeGuardar()">
              @if (saving()) {
                <mat-spinner diameter="20"></mat-spinner>
              }
              @if (!saving()) {
                <mat-icon>save</mat-icon>
              }
              {{ saving() ? 'Guardando…' : (editando() ? 'Guardar configuración' : 'Crear existencia') }}
            </button>
          </div>
        </form>
      }
    </section>
    `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    :host{display:block}.page{padding:24px;max-width:1180px;margin:0 auto}.header{display:flex;justify-content:space-between;gap:16px;align-items:flex-start;margin-bottom:24px}.eyebrow{margin:0 0 4px;font-size:12px;font-weight:700;text-transform:uppercase;letter-spacing:.08em;opacity:.65}h1{margin:0;font-size:clamp(24px,3vw,34px)}.subtitle{margin:6px 0 0;opacity:.72}.card{border:1px solid rgba(127,127,127,.22);border-radius:14px;padding:22px}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:14px}.stock-grid{grid-template-columns:repeat(4,minmax(0,1fr))}.policy-grid{max-width:600px}fieldset{border:0;padding:0;margin:0 0 24px}legend{font-size:14px;font-weight:700;margin-bottom:14px}.actions{display:flex;justify-content:flex-end;gap:10px;padding-top:4px}.feedback,.loading{display:flex;align-items:center;gap:10px;padding:22px;border-radius:12px}.feedback.error{border:1px solid rgba(244,67,54,.32);background:rgba(244,67,54,.06);margin-bottom:16px}.loading{justify-content:center;min-height:180px}@media(max-width:900px){.stock-grid{grid-template-columns:repeat(2,minmax(0,1fr))}}@media(max-width:640px){.page{padding:16px}.header{flex-direction:column}.card{padding:16px}.grid,.stock-grid{grid-template-columns:1fr}.actions{flex-direction:column-reverse}.actions button{width:100%}}
  `]
})
export class ExistenciaVarianteFormComponent implements OnInit {
  readonly productos = signal<Producto[]>([]);
  readonly variantes = signal<ProductoVariante[]>([]);
  readonly almacenes = signal<Almacen[]>([]);
  readonly ubicaciones = signal<UbicacionAlmacen[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly cargandoVariantes = signal(false);
  readonly cargandoUbicaciones = signal(false);
  readonly error = signal('');
  readonly editando = signal(false);

  id = 0;
  productoId: number | null = null;
  productoVarianteId: number | null = null;
  almacenId: number | null = null;
  ubicacionAlmacenId: number | null = null;
  stockFisico = 0;
  stockReservado = 0;
  stockTransito = 0;
  stockMinimo = 0;
  stockMaximo: number | null = null;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly existenciaService: ExistenciaVarianteService,
    private readonly productoService: ProductoService,
    private readonly almacenService: AlmacenService,
    private readonly ubicacionService: UbicacionAlmacenService
  ) {}

  ngOnInit(): void {
    this.id = Number(this.route.snapshot.paramMap.get('id') ?? 0);
    this.editando.set(this.id > 0);

    const productoSolicitado = Number(this.route.snapshot.queryParamMap.get('productoId') ?? 0);
    const varianteSolicitada = Number(this.route.snapshot.queryParamMap.get('productoVarianteId') ?? 0);

    forkJoin({
      productos: this.productoService.getPaged({ page: 1, pageSize: 200, activo: true, sortBy: 'nombre', sortDirection: 'asc' }),
      almacenes: this.almacenService.getActivos()
    }).subscribe({
      next: ({ productos, almacenes }) => {
        this.productos.set(productos.data.items);
        this.almacenes.set(almacenes.data);
        if (this.editando()) {
          this.cargarExistencia();
          return;
        }

        if (Number.isInteger(productoSolicitado) && productoSolicitado > 0 && this.productos().some(p => p.id === productoSolicitado)) {
          this.productoId = productoSolicitado;
          this.productoVarianteId = Number.isInteger(varianteSolicitada) && varianteSolicitada > 0 ? varianteSolicitada : null;
          this.cargarVariantes(productoSolicitado);
        }
        this.loading.set(false);
      },
      error: err => {
        this.loading.set(false);
        this.error.set(this.extraerError(err, 'No fue posible cargar productos y almacenes.'));
      }
    });
  }

  get stockDisponible(): number { return Math.max(0, Number(this.stockFisico || 0) - Number(this.stockReservado || 0)); }

  onProductoChange(): void {
    this.productoVarianteId = null;
    this.variantes.set([]);
    if (!this.productoId) return;
    this.cargarVariantes(this.productoId);
  }

  onAlmacenChange(): void {
    this.ubicacionAlmacenId = null;
    this.ubicaciones.set([]);
    if (!this.almacenId) return;
    this.cargarUbicaciones(this.almacenId);
  }

  guardar(): void {
    this.error.set('');
    if (!this.puedeGuardar()) {
      this.error.set('Completa una combinación operativa válida antes de guardar.');
      return;
    }
    if (this.stockReservado > this.stockFisico) {
      this.error.set('El stock reservado no puede superar el stock físico.');
      return;
    }
    if (this.stockMaximo != null && this.stockMaximo < this.stockMinimo) {
      this.error.set('El stock máximo no puede ser menor que el stock mínimo.');
      return;
    }

    this.saving.set(true);
    const request$ = this.editando()
      ? this.existenciaService.updateConfiguracion(this.id, {
          ubicacionAlmacenId: this.ubicacionAlmacenId,
          stockMinimo: Number(this.stockMinimo),
          stockMaximo: this.stockMaximo == null ? null : Number(this.stockMaximo)
        })
      : this.existenciaService.create({
          productoVarianteId: Number(this.productoVarianteId),
          almacenId: Number(this.almacenId),
          ubicacionAlmacenId: this.ubicacionAlmacenId,
          stockFisico: Number(this.stockFisico),
          stockReservado: Number(this.stockReservado),
          stockTransito: Number(this.stockTransito),
          stockMinimo: Number(this.stockMinimo),
          stockMaximo: this.stockMaximo == null ? null : Number(this.stockMaximo)
        });

    request$.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: response => {
        if (!response.success) {
          this.error.set(response.message || 'No fue posible guardar la existencia.');
          return;
        }
        void this.router.navigate(['/inventario/existencias']);
      },
      error: err => this.error.set(this.extraerError(err, 'No fue posible guardar la existencia.'))
    });
  }

  volver(): void { void this.router.navigate(['/inventario/existencias']); }

  puedeGuardar(): boolean {
    if (!this.almacenId || this.stockMinimo < 0 || (this.stockMaximo != null && this.stockMaximo < 0)) return false;
    if (this.editando()) return true;
    return !!this.productoId && !!this.productoVarianteId && this.stockFisico >= 0 && this.stockReservado >= 0 && this.stockTransito >= 0;
  }

  etiquetaVariante(variante: ProductoVariante): string {
    return variante.etiqueta?.trim() || variante.sku || `Variante #${variante.id}`;
  }

  private cargarExistencia(): void {
    this.existenciaService.getById(this.id).subscribe({
      next: response => {
        if (!response.success || !response.data) {
          this.loading.set(false);
          this.error.set(response.message || 'Existencia no encontrada.');
          return;
        }
        const e = response.data;
        this.productoVarianteId = e.productoVarianteId;
        this.almacenId = e.almacenId;
        this.ubicacionAlmacenId = e.ubicacionAlmacenId ?? null;
        this.stockFisico = e.stockFisico;
        this.stockReservado = e.stockReservado;
        this.stockTransito = e.stockTransito;
        this.stockMinimo = e.stockMinimo;
        this.stockMaximo = e.stockMaximo ?? null;
        const producto = this.productos().find(p => p.variantes?.some(v => v.id === e.productoVarianteId));
        this.productoId = producto?.id ?? null;
        if (this.productoId) this.cargarVariantes(this.productoId);
        this.cargarUbicaciones(e.almacenId, () => this.loading.set(false));
      },
      error: err => {
        this.loading.set(false);
        this.error.set(this.extraerError(err, 'No fue posible cargar la existencia.'));
      }
    });
  }

  private cargarVariantes(productoId: number): void {
    this.cargandoVariantes.set(true);
    this.productoService.getVariantes(productoId, false).pipe(finalize(() => this.cargandoVariantes.set(false))).subscribe({
      next: response => {
        const activas = (response.data ?? []).filter(v => v.activo);
        this.variantes.set(activas);
        if (this.productoVarianteId) {
          const preseleccionada = activas.find(v => v.id === this.productoVarianteId);
          if (preseleccionada) this.stockMinimo = preseleccionada.umbralStockBajo;
          else this.productoVarianteId = null;
        }
      },
      error: err => this.error.set(this.extraerError(err, 'No fue posible cargar las variantes activas.'))
    });
  }

  private cargarUbicaciones(almacenId: number, done?: () => void): void {
    this.cargandoUbicaciones.set(true);
    this.ubicacionService.getActivas(almacenId).pipe(finalize(() => {
      this.cargandoUbicaciones.set(false);
      done?.();
    })).subscribe({
      next: response => this.ubicaciones.set(response.data ?? []),
      error: err => this.error.set(this.extraerError(err, 'No fue posible cargar las ubicaciones activas.'))
    });
  }

  private extraerError(err: any, fallback: string): string {
    return err?.error?.message || err?.error?.title || err?.message || fallback;
  }
}
