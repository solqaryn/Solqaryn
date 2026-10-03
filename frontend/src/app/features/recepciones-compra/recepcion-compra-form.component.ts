
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize, forkJoin } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { Almacen } from '../../core/models/almacen.model';
import { OrdenCompra } from '../../core/models/orden-compra.model';
import { RecepcionCompraFormValue, RecepcionCompraSaldoLinea, RecepcionCompraSaldoOrden } from '../../core/models/recepcion-compra.model';
import { AlmacenService } from '../../services/almacen.service';
import { OrdenCompraService } from '../../services/orden-compra.service';
import { RecepcionCompraService } from '../../services/recepcion-compra.service';

@Component({
  selector: 'app-recepcion-compra-form',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule
],
  template: `
    <section class="page-shell" aria-labelledby="recepcion-form-title">
      <header class="page-header">
        <div>
          <p class="eyebrow">Compras empresariales</p>
          <h1 id="recepcion-form-title">Nueva recepción de mercancía</h1>
          <p>Registra una recepción total o parcial contra una orden aprobada. Las diferencias permanecen trazables y el stock cambia únicamente al confirmar.</p>
        </div>
        <button mat-stroked-button type="button" (click)="volver()"><mat-icon>arrow_back</mat-icon> Volver</button>
      </header>

      @if (!puedeCrear()) {
        <div class="state-panel error" role="alert"><mat-icon>lock</mat-icon><span>No tienes permiso para crear recepciones de compra.</span></div>
      } @else if (cargandoCatalogos()) {
        <div class="state-panel" role="status"><mat-spinner diameter="36"></mat-spinner><span>Cargando órdenes y almacenes…</span></div>
      } @else {
        <form [formGroup]="form" (ngSubmit)="guardar()" class="form-grid" novalidate>
          <mat-form-field appearance="outline" class="span-2">
            <mat-label>Orden de compra aprobada</mat-label>
            <mat-select formControlName="ordenCompraId" data-testid="orden-compra-select" (selectionChange)="seleccionarOrden($event.value)">
              @for (orden of ordenes(); track orden.id) {
                <mat-option [value]="orden.id">{{ orden.numeroOrden }} — {{ orden.proveedorNombre }}</mat-option>
              }
            </mat-select>
            @if (form.controls.ordenCompraId.touched && form.controls.ordenCompraId.invalid) { <mat-error>Selecciona una orden aprobada.</mat-error> }
          </mat-form-field>

          <mat-form-field appearance="outline" class="span-2">
            <mat-label>Observaciones</mat-label>
            <textarea matInput rows="2" formControlName="observaciones" maxlength="1000"></textarea>
            <mat-hint>Opcional. Máximo 1000 caracteres.</mat-hint>
          </mat-form-field>

          @if (cargandoSaldo()) {
            <div class="state-panel span-2" role="status"><mat-spinner diameter="32"></mat-spinner><span>Cargando saldo pendiente de la orden…</span></div>
          }

          @if (error()) {
            <div class="state-panel error span-2" role="alert"><mat-icon>error_outline</mat-icon><span>{{ error() }}</span></div>
          }

          @if (!cargandoSaldo() && saldo()) {
            <div class="summary span-2" aria-live="polite">
              <strong>{{ saldo()!.numeroOrden }}</strong>
              <span>{{ saldo()!.lineas.length }} línea(s) · {{ saldo()!.completa ? 'Orden completamente recibida' : 'Con saldo pendiente' }}</span>
            </div>

            <div class="lines span-2" formArrayName="detalles">
              @for (linea of saldo()!.lineas; track linea.ordenCompraDetalleId; let i = $index) {
                <article class="line-card" [formGroupName]="i">
                  <header>
                    <div>
                      <strong>{{ linea.productoSkuSnapshot || ('Producto #' + linea.productoId) }}</strong>
                      <span>{{ linea.productoNombreSnapshot || 'Producto de la orden' }}</span>
                    </div>
                    <div class="pending"><small>Pendiente</small><strong>{{ linea.cantidadPendiente }}</strong></div>
                  </header>

                  <div class="line-grid">
                    <mat-form-field appearance="outline" class="warehouse">
                      <mat-label>Almacén destino</mat-label>
                      <mat-select formControlName="almacenId" [attr.data-testid]="'almacen-' + i">
                        @for (almacen of almacenes(); track almacen.id) {
                          <mat-option [value]="almacen.id">{{ almacen.codigo }} — {{ almacen.nombre }}</mat-option>
                        }
                      </mat-select>
                      @if (detalle(i).controls['almacenId'].touched && detalle(i).controls['almacenId'].invalid) { <mat-error>Selecciona un almacén activo.</mat-error> }
                    </mat-form-field>
                    <mat-form-field appearance="outline"><mat-label>Recibida</mat-label><input matInput type="number" min="0" step="0.01" formControlName="cantidadRecibida" [attr.data-testid]="'recibida-' + i"></mat-form-field>
                    <mat-form-field appearance="outline"><mat-label>Dañada</mat-label><input matInput type="number" min="0" step="0.01" formControlName="cantidadDanada" [attr.data-testid]="'danada-' + i"></mat-form-field>
                    <mat-form-field appearance="outline"><mat-label>Faltante</mat-label><input matInput type="number" min="0" step="0.01" formControlName="cantidadFaltante" [attr.data-testid]="'faltante-' + i"></mat-form-field>
                    <mat-form-field appearance="outline"><mat-label>Sobrante</mat-label><input matInput type="number" min="0" step="0.01" formControlName="cantidadSobrante" [attr.data-testid]="'sobrante-' + i"></mat-form-field>
                  </div>

                  @if (errorLinea(i, linea)) { <p class="line-error" role="alert">{{ errorLinea(i, linea) }}</p> }
                </article>
              }
            </div>
          }

          <div class="actions span-2">
            <button mat-button type="button" (click)="volver()">Cancelar</button>
            <button mat-flat-button type="submit" [disabled]="!puedeGuardar()" data-testid="guardar-recepcion">
              @if (guardando()) { <mat-spinner diameter="20" aria-label="Guardando recepción"></mat-spinner> } @else { <mat-icon>save</mat-icon> }
              Guardar borrador
            </button>
          </div>
        </form>
      }
    </section>
  `,
  styles: [`
    .page-shell{display:grid;gap:1.25rem;max-width:1280px;margin:0 auto}.page-header{display:flex;justify-content:space-between;align-items:flex-start;gap:1rem}.eyebrow{margin:0 0 .25rem;text-transform:uppercase;letter-spacing:.08em;font-size:.75rem;font-weight:700;opacity:.7}h1{margin:.1rem 0}.form-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:1rem}.span-2{grid-column:1/-1}.state-panel{min-height:96px;display:flex;align-items:center;justify-content:center;gap:.75rem;border:1px solid rgba(127,127,127,.18);border-radius:14px;padding:1rem}.error,.line-error{color:var(--mat-sys-error,#b3261e)}.summary{display:flex;justify-content:space-between;gap:1rem;padding:1rem;border-radius:12px;background:rgba(127,127,127,.08)}.summary span{opacity:.78}.lines{display:grid;gap:1rem}.line-card{padding:1rem;border:1px solid rgba(127,127,127,.2);border-radius:14px}.line-card header{display:flex;justify-content:space-between;gap:1rem;margin-bottom:1rem}.line-card header div:first-child{display:grid;gap:.2rem}.line-card header span{opacity:.72}.pending{display:grid;text-align:right}.line-grid{display:grid;grid-template-columns:minmax(220px,1.5fr) repeat(4,minmax(110px,1fr));gap:.75rem}.line-error{margin:.1rem 0 0;font-size:.9rem}.actions{display:flex;justify-content:flex-end;gap:.75rem;align-items:center}@media(max-width:960px){.line-grid{grid-template-columns:repeat(2,minmax(140px,1fr))}.warehouse{grid-column:1/-1}}@media(max-width:680px){.page-header{flex-direction:column}.form-grid,.line-grid{grid-template-columns:1fr}.span-2,.warehouse{grid-column:1}.summary{flex-direction:column}.line-card header{flex-direction:column}.pending{text-align:left}}
  `]
})
export class RecepcionCompraFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly recepciones = inject(RecepcionCompraService);
  private readonly ordenesService = inject(OrdenCompraService);
  private readonly almacenesService = inject(AlmacenService);
  private readonly permisos = inject(PermisosRuntimeService);

  readonly ordenes = signal<OrdenCompra[]>([]);
  readonly almacenes = signal<Almacen[]>([]);
  readonly saldo = signal<RecepcionCompraSaldoOrden | null>(null);
  readonly cargandoCatalogos = signal(false);
  readonly cargandoSaldo = signal(false);
  readonly guardando = signal(false);
  readonly error = signal('');
  readonly puedeCrear = signal(false);

  readonly form = this.fb.group({
    ordenCompraId: [null as number | null, Validators.required],
    observaciones: ['', Validators.maxLength(1000)],
    detalles: this.fb.array([])
  });

  get detalles(): FormArray { return this.form.controls.detalles; }

  ngOnInit(): void {
    this.puedeCrear.set(this.permisos.puede('Compras', 'Crear'));
    if (!this.puedeCrear()) return;

    this.cargandoCatalogos.set(true);
    forkJoin({
      ordenes: this.ordenesService.getPaged({ page: 1, pageSize: 100, estado: 'Aprobada' }),
      almacenes: this.almacenesService.getActivos()
    }).pipe(finalize(() => this.cargandoCatalogos.set(false))).subscribe({
      next: ({ ordenes, almacenes }) => {
        this.ordenes.set(ordenes.data?.items ?? []);
        this.almacenes.set(almacenes.data ?? []);
        const preseleccionada = Number(this.route.snapshot.queryParamMap.get('ordenCompraId'));
        if (Number.isInteger(preseleccionada) && preseleccionada > 0 && this.ordenes().some(x => x.id === preseleccionada)) {
          this.form.controls.ordenCompraId.setValue(preseleccionada);
          this.seleccionarOrden(preseleccionada);
        }
      },
      error: () => this.error.set('No fue posible cargar las órdenes aprobadas y almacenes activos.')
    });
  }

  seleccionarOrden(ordenCompraId: number | null): void {
    if (!this.puedeCrear()) return;
    this.detalles.clear();
    this.saldo.set(null);
    this.error.set('');
    if (!ordenCompraId) return;

    this.cargandoSaldo.set(true);
    this.recepciones.getSaldoOrden(ordenCompraId).pipe(finalize(() => this.cargandoSaldo.set(false))).subscribe({
      next: response => {
        const saldo = response.data;
        if (!saldo) {
          this.error.set('No se encontró saldo disponible para la orden seleccionada.');
          return;
        }
        this.saldo.set(saldo);
        for (const linea of saldo.lineas) this.detalles.push(this.crearDetalle(linea));
        if (!saldo.lineas.length || saldo.completa) this.error.set('La orden seleccionada no tiene cantidades pendientes por recibir.');
      },
      error: () => this.error.set('No fue posible cargar el saldo pendiente de la orden.')
    });
  }

  detalle(index: number): FormGroup { return this.detalles.at(index) as FormGroup; }

  errorLinea(index: number, linea: RecepcionCompraSaldoLinea): string {
    const value = this.detalle(index).getRawValue();
    const recibida = this.numero(value.cantidadRecibida);
    const danada = this.numero(value.cantidadDanada);
    const faltante = this.numero(value.cantidadFaltante);
    const sobrante = this.numero(value.cantidadSobrante);
    if ([recibida, danada, faltante, sobrante].some(x => x < 0)) return 'Las cantidades no pueden ser negativas.';
    if (danada + sobrante > recibida) return 'Dañada + sobrante no puede superar la cantidad físicamente recibida.';
    if (recibida === 0 && faltante === 0) return 'Registra recepción física o una cantidad faltante.';
    const aceptada = recibida - danada - sobrante;
    if (aceptada > linea.cantidadPendiente) return `La cantidad aceptada (${aceptada}) supera el saldo pendiente (${linea.cantidadPendiente}).`;
    return '';
  }

  puedeGuardar(): boolean {
    const saldo = this.saldo();
    return this.puedeCrear() && !this.guardando() && !this.cargandoSaldo() && this.form.valid && !!saldo && !saldo.completa && saldo.lineas.length > 0
      && saldo.lineas.every((linea, index) => !this.errorLinea(index, linea));
  }

  guardar(): void {
    if (!this.puedeCrear() || !this.puedeGuardar()) {
      this.form.markAllAsTouched();
      return;
    }

    const ordenCompraId = Number(this.form.controls.ordenCompraId.value);
    const observaciones = this.form.controls.observaciones.value?.trim() || null;
    const payload: RecepcionCompraFormValue = {
      ordenCompraId,
      observaciones,
      detalles: this.detalles.controls.map(control => {
        const value = (control as FormGroup).getRawValue();
        return {
          ordenCompraDetalleId: Number(value.ordenCompraDetalleId),
          almacenId: Number(value.almacenId),
          ubicacionAlmacenId: null,
          cantidadRecibida: this.numero(value.cantidadRecibida),
          cantidadDanada: this.numero(value.cantidadDanada),
          cantidadFaltante: this.numero(value.cantidadFaltante),
          cantidadSobrante: this.numero(value.cantidadSobrante)
        };
      })
    };

    this.guardando.set(true);
    this.error.set('');
    this.recepciones.create(payload).pipe(finalize(() => this.guardando.set(false))).subscribe({
      next: response => void this.router.navigate(['/recepciones-compra', response.data.id]),
      error: () => this.error.set('No fue posible guardar la recepción de compra.')
    });
  }

  volver(): void { void this.router.navigate(['/recepciones-compra']); }

  private crearDetalle(linea: RecepcionCompraSaldoLinea): FormGroup {
    return this.fb.group({
      ordenCompraDetalleId: [linea.ordenCompraDetalleId, [Validators.required, Validators.min(1)]],
      almacenId: [null as number | null, [Validators.required, Validators.min(1)]],
      cantidadRecibida: [0, [Validators.required, Validators.min(0)]],
      cantidadDanada: [0, [Validators.required, Validators.min(0)]],
      cantidadFaltante: [0, [Validators.required, Validators.min(0)]],
      cantidadSobrante: [0, [Validators.required, Validators.min(0)]]
    });
  }

  private numero(value: unknown): number {
    const numero = Number(value);
    return Number.isFinite(numero) ? numero : 0;
  }
}
