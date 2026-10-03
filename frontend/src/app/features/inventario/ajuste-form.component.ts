
import { Component, OnInit, signal } from '@angular/core';
import {
  ReactiveFormsModule,
  UntypedFormArray,
  UntypedFormBuilder,
  UntypedFormGroup,
  Validators
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AjusteInventarioFormValue } from '../../core/models/ajuste-inventario.model';
import { ExistenciaVariante } from '../../core/models/existencia-variante.model';
import { Producto, ProductoVariante } from '../../core/models/producto.model';
import { AjusteInventarioService } from '../../services/ajuste-inventario.service';
import { ExistenciaVarianteService } from '../../services/existencia-variante.service';
import { ProductoService } from '../../services/producto.service';

@Component({
  selector: 'app-ajuste-form',
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
    <section class="form-page" aria-labelledby="ajuste-form-title">
      <header>
        <button mat-icon-button type="button" aria-label="Volver a ajustes" (click)="volver()">
          <mat-icon>arrow_back</mat-icon>
        </button>
        <div>
          <p class="eyebrow">Inventario empresarial</p>
          <h1 id="ajuste-form-title">{{ ajusteId ? 'Editar borrador' : 'Nuevo ajuste' }}</h1>
          <p>Cada detalle establece la cantidad física objetivo sobre una existencia concreta. El ajuste se aplicará únicamente al confirmar.</p>
        </div>
      </header>
    
      @if (error()) {
        <div class="error" role="alert">
          <mat-icon>error_outline</mat-icon>
          <span>{{ error() }}</span>
        </div>
      }
    
      @if (loading() || catalogLoading()) {
        <div class="loading" aria-live="polite">
          <mat-spinner diameter="36"></mat-spinner>
          <span>{{ loading() ? 'Cargando borrador…' : 'Cargando productos…' }}</span>
        </div>
      }
    
      @if (!loading() && !catalogLoading()) {
        <form [formGroup]="form" (ngSubmit)="guardar()" novalidate>
          <div class="grid two">
            <mat-form-field appearance="outline">
              <mat-label>Fecha de ajuste</mat-label>
              <input matInput type="datetime-local" formControlName="fechaAjuste" />
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Motivo</mat-label>
              <input matInput formControlName="motivo" maxlength="250" required />
              @if (form.get('motivo')?.hasError('required')) {
                <mat-error>El motivo es obligatorio.</mat-error>
              }
            </mat-form-field>
          </div>
          <mat-form-field appearance="outline" class="full">
            <mat-label>Observaciones</mat-label>
            <textarea matInput rows="3" formControlName="observaciones" maxlength="1000"></textarea>
          </mat-form-field>
          <div class="details-header">
            <div>
              <h2>Conteo físico</h2>
              <p>Selecciona producto, variante y la existencia exacta por almacén/ubicación antes de indicar la cantidad objetivo.</p>
            </div>
            <button mat-stroked-button color="primary" type="button" (click)="agregarDetalle()">
              <mat-icon>add</mat-icon>
              Agregar detalle
            </button>
          </div>
          <div formArrayName="detalles" class="details">
            @for (detail of detalles.controls; track detail; let i = $index) {
              <article class="detail" [formGroupName]="i">
                <span class="detail-number">#{{ i + 1 }}</span>
                <mat-form-field appearance="outline">
                  <mat-label>Producto</mat-label>
                  <mat-select formControlName="productoId" required (selectionChange)="onProductoChange(i, $event.value)">
                    @for (producto of productos(); track producto) {
                      <mat-option [value]="producto.id">
                        {{ etiquetaProducto(producto) }}
                      </mat-option>
                    }
                  </mat-select>
                  <mat-error>Selecciona un producto válido.</mat-error>
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Variante</mat-label>
                  <mat-select
                    formControlName="productoVarianteId"
                    required
                    (selectionChange)="onVarianteChange(i, $event.value)">
                    @for (variante of variantesProducto(detail.get('productoId')?.value); track variante) {
                      <mat-option [value]="variante.id">
                        {{ etiquetaVariante(variante) }}
                      </mat-option>
                    }
                  </mat-select>
                  @if (variantesProducto(detail.get('productoId')?.value).length > 0) {
                    <mat-hint>Selecciona la variante física concreta.</mat-hint>
                  }
                  @if (detail.get('productoId')?.value && variantesProducto(detail.get('productoId')?.value).length === 0) {
                    <mat-hint>El producto no tiene una variante operativa disponible.</mat-hint>
                  }
                  <mat-error>Selecciona una variante válida.</mat-error>
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Existencia física</mat-label>
                  <mat-select
                    formControlName="existenciaId"
                    required
                    [disabled]="!detail.get('productoVarianteId')?.value || cargandoExistenciasVariante(detail.get('productoVarianteId')?.value)"
                    (selectionChange)="onExistenciaChange(i, $event.value)">
                    @for (existencia of existenciasVariante(detail.get('productoVarianteId')?.value); track existencia) {
                      <mat-option
                        [value]="existencia.id">
                        {{ etiquetaExistencia(existencia) }}
                      </mat-option>
                    }
                  </mat-select>
                  @if (cargandoExistenciasVariante(detail.get('productoVarianteId')?.value)) {
                    <mat-hint>Cargando existencias físicas…</mat-hint>
                  }
                  @if (detail.get('productoVarianteId')?.value && !cargandoExistenciasVariante(detail.get('productoVarianteId')?.value) && existenciasVariante(detail.get('productoVarianteId')?.value).length === 0) {
                    <mat-hint>La variante no tiene existencias por almacén disponibles.</mat-hint>
                  }
                  <mat-error>Selecciona una existencia física válida.</mat-error>
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Cantidad objetivo</mat-label>
                  <input matInput type="number" min="0" step="1" formControlName="cantidadObjetivo" required />
                  <mat-error>La cantidad objetivo debe ser 0 o mayor.</mat-error>
                </mat-form-field>
                <button
                  mat-icon-button
                  color="warn"
                  type="button"
                  aria-label="Eliminar detalle"
                  [disabled]="detalles.length === 1"
                  (click)="eliminarDetalle(i)">
                  <mat-icon>delete</mat-icon>
                </button>
              </article>
            }
          </div>
          <div class="actions">
            <button mat-button type="button" (click)="volver()" [disabled]="saving()">Cancelar</button>
            <button mat-flat-button color="primary" type="submit" [disabled]="saving() || form.invalid || productos().length === 0">
              @if (saving()) {
                <mat-spinner diameter="20"></mat-spinner>
              }
              @if (!saving()) {
                <mat-icon>save</mat-icon>
              }
              {{ saving() ? 'Guardando…' : 'Guardar borrador' }}
            </button>
          </div>
        </form>
      }
    </section>
    `,
  styles: [`
    :host { display: block; }
    .form-page { max-width: 1240px; margin: 0 auto; padding: 24px; }
    header { display: flex; gap: 12px; align-items: flex-start; margin-bottom: 24px; }
    header h1, header p { margin: 0; }
    header p:not(.eyebrow) { margin-top: 6px; opacity: .72; }
    .eyebrow { margin: 0 0 4px; font-size: 12px; font-weight: 700; text-transform: uppercase; letter-spacing: .08em; opacity: .65; }
    .grid.two { display: grid; grid-template-columns: 1fr 2fr; gap: 16px; }
    .full { width: 100%; }
    .details-header { display: flex; justify-content: space-between; gap: 16px; align-items: center; margin: 16px 0 12px; }
    .details-header h2, .details-header p { margin: 0; }
    .details-header p { margin-top: 4px; opacity: .68; }
    .details { display: grid; gap: 10px; }
    .detail { display: grid; grid-template-columns: auto minmax(190px, 1.2fr) minmax(190px, 1.2fr) minmax(280px, 1.7fr) minmax(150px, .7fr) auto; align-items: start; gap: 10px; padding: 14px; border: 1px solid rgba(127,127,127,.22); border-radius: 12px; }
    .detail-number { padding-top: 18px; font-weight: 700; opacity: .55; }
    .actions { display: flex; justify-content: flex-end; gap: 10px; margin-top: 24px; }
    .actions button mat-spinner { display: inline-block; margin-right: 8px; }
    .loading, .error { display: flex; align-items: center; gap: 10px; padding: 24px; border-radius: 12px; }
    .loading { justify-content: center; }
    .error { margin-bottom: 16px; border: 1px solid rgba(244,67,54,.32); background: rgba(244,67,54,.06); }
    @media (max-width: 1080px) { .detail { grid-template-columns: 1fr 1fr; } .detail-number { grid-column: 1 / -1; padding-top: 0; } }
    @media (max-width: 760px) {
      .form-page { padding: 16px; }
      .grid.two, .detail { grid-template-columns: 1fr; }
      .detail-number { grid-column: auto; }
      .details-header { align-items: stretch; flex-direction: column; }
    }
  `]
})
export class AjusteFormComponent implements OnInit {
  readonly loading = signal(false);
  readonly catalogLoading = signal(true);
  readonly saving = signal(false);
  readonly error = signal('');
  readonly productos = signal<Producto[]>([]);
  readonly existenciasPorVariante = signal<Record<number, ExistenciaVariante[]>>({});
  readonly cargandoExistencias = signal<Record<number, boolean>>({});

  readonly ajusteId: number | null;
  readonly form: UntypedFormGroup;

  constructor(
    private readonly fb: UntypedFormBuilder,
    private readonly ajusteService: AjusteInventarioService,
    private readonly productoService: ProductoService,
    private readonly existenciaService: ExistenciaVarianteService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {
    const rawId = Number(this.route.snapshot.paramMap.get('id'));
    this.ajusteId = Number.isInteger(rawId) && rawId > 0 ? rawId : null;
    this.form = this.fb.group({
      fechaAjuste: [''],
      motivo: ['', [Validators.required, Validators.maxLength(250)]],
      observaciones: ['', Validators.maxLength(1000)],
      detalles: this.fb.array([this.crearDetalle()])
    });
  }

  get detalles(): UntypedFormArray {
    return this.form.get('detalles') as UntypedFormArray;
  }

  ngOnInit(): void {
    this.cargarProductos();
    if (this.ajusteId) this.cargar(this.ajusteId);
  }

  agregarDetalle(): void {
    this.detalles.push(this.crearDetalle());
  }

  eliminarDetalle(index: number): void {
    if (this.detalles.length > 1) this.detalles.removeAt(index);
  }

  onProductoChange(index: number, productoId: number): void {
    const detalle = this.detalles.at(index);
    detalle.patchValue({
      productoVarianteId: null,
      existenciaId: null,
      almacenId: null,
      ubicacionAlmacenId: null
    });

    const variantes = this.variantesProducto(productoId);
    if (variantes.length === 1) {
      detalle.get('productoVarianteId')?.setValue(variantes[0].id);
      this.onVarianteChange(index, variantes[0].id);
    }
  }

  onVarianteChange(index: number, varianteId: number | null): void {
    const detalle = this.detalles.at(index);
    detalle.patchValue({ existenciaId: null, almacenId: null, ubicacionAlmacenId: null });
    if (varianteId && varianteId > 0) this.cargarExistencias(varianteId, index);
  }

  onExistenciaChange(index: number, existenciaId: number | null): void {
    const detalle = this.detalles.at(index);
    const varianteId = Number(detalle.get('productoVarianteId')?.value);
    const existencia = this.existenciasVariante(varianteId).find(item => item.id === Number(existenciaId));
    if (!existencia) {
      detalle.patchValue({ almacenId: null, ubicacionAlmacenId: null });
      return;
    }

    detalle.patchValue({
      almacenId: existencia.almacenId,
      ubicacionAlmacenId: existencia.ubicacionAlmacenId ?? null
    });
  }

  variantesProducto(productoId: number | null | undefined): ProductoVariante[] {
    if (!productoId) return [];
    return this.productos().find(producto => producto.id === Number(productoId))?.variantes?.filter(v => v.activo && !v.eliminado) ?? [];
  }

  existenciasVariante(varianteId: number | null | undefined): ExistenciaVariante[] {
    const id = Number(varianteId);
    return Number.isInteger(id) && id > 0 ? this.existenciasPorVariante()[id] ?? [] : [];
  }

  cargandoExistenciasVariante(varianteId: number | null | undefined): boolean {
    const id = Number(varianteId);
    return Number.isInteger(id) && id > 0 ? this.cargandoExistencias()[id] === true : false;
  }

  etiquetaProducto(producto: Producto): string {
    const identidad = [producto.marcaNombre || producto.marca, producto.modeloNombre || producto.modelo]
      .filter(Boolean)
      .join(' · ');
    return identidad ? `${producto.nombre} — ${identidad}` : producto.nombre;
  }

  etiquetaVariante(variante: ProductoVariante): string {
    const identidad = [variante.marcaNombre, variante.modeloNombre, variante.colorNombre, variante.tallaNombre]
      .filter(Boolean)
      .join(' · ');
    const sku = variante.sku ? `SKU ${variante.sku}` : `Variante #${variante.id}`;
    return identidad ? `${identidad} — ${sku}` : sku;
  }

  etiquetaExistencia(existencia: ExistenciaVariante): string {
    const ubicacion = existencia.ubicacionAlmacenId
      ? `${existencia.ubicacionCodigo || `#${existencia.ubicacionAlmacenId}`} · ${existencia.ubicacionNombre || 'Ubicación'}`
      : 'Raíz / sin ubicación';
    return `${existencia.almacenCodigo} · ${existencia.almacenNombre} — ${ubicacion} — Stock ${existencia.stockFisico}`;
  }

  guardar(): void {
    if (this.form.invalid || this.saving()) {
      this.form.markAllAsTouched();
      return;
    }

    const raw = this.form.getRawValue();
    const detallesResueltos = raw.detalles.map((detalle: any) => {
      const varianteId = Number(detalle.productoVarianteId);
      const existencia = this.existenciasVariante(varianteId).find(item => item.id === Number(detalle.existenciaId));
      return { detalle, existencia };
    });

    if (detallesResueltos.some(({ existencia }: any) => !existencia)) {
      this.error.set('Selecciona una existencia física válida para cada detalle antes de guardar el borrador.');
      return;
    }

    const value: AjusteInventarioFormValue = {
      fechaAjuste: raw.fechaAjuste ? new Date(raw.fechaAjuste).toISOString() : null,
      motivo: String(raw.motivo).trim(),
      observaciones: String(raw.observaciones || '').trim() || null,
      detalles: detallesResueltos.map(({ detalle, existencia }: any) => ({
        productoId: Number(detalle.productoId),
        productoVarianteId: Number(detalle.productoVarianteId),
        almacenId: existencia.almacenId,
        ubicacionAlmacenId: existencia.ubicacionAlmacenId ?? null,
        cantidadObjetivo: Number(detalle.cantidadObjetivo)
      }))
    };

    this.saving.set(true);
    this.error.set('');
    const request = this.ajusteId
      ? this.ajusteService.update(this.ajusteId, value)
      : this.ajusteService.create(value);

    request.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: (response) => {
        if (!response.success) {
          this.error.set(response.message || response.errors?.join(' ') || 'No fue posible guardar el borrador.');
          return;
        }
        this.router.navigate(['/inventario/ajustes']);
      },
      error: (err) => this.error.set(this.extraerError(err, 'No fue posible guardar el borrador.'))
    });
  }

  volver(): void {
    this.router.navigate(['/inventario/ajustes']);
  }

  private cargarProductos(): void {
    this.catalogLoading.set(true);
    this.productoService.getPaged({
      page: 1,
      pageSize: 50,
      activo: true,
      sortBy: 'Nombre',
      sortDirection: 'asc'
    }).pipe(finalize(() => this.catalogLoading.set(false))).subscribe({
      next: (response) => {
        if (!response.success) {
          this.productos.set([]);
          this.error.set(response.message || 'No fue posible cargar los productos disponibles.');
          return;
        }
        this.productos.set(response.data.items);
        if (response.data.totalCount > response.data.items.length) {
          this.error.set('Hay más de 50 productos activos. Refina el catálogo antes de registrar el ajuste para evitar seleccionar un producto incorrecto.');
        }
      },
      error: (err) => {
        this.productos.set([]);
        this.error.set(this.extraerError(err, 'No fue posible cargar los productos disponibles.'));
      }
    });
  }

  private cargar(id: number): void {
    this.loading.set(true);
    this.error.set('');
    this.ajusteService.getById(id)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          const ajuste = response.data;
          if (!response.success || !ajuste) {
            this.error.set(response.message || 'No fue posible cargar el borrador.');
            return;
          }
          if (ajuste.estado !== 'Borrador') {
            this.error.set('Solo los ajustes en estado Borrador pueden editarse.');
            this.form.disable();
            return;
          }

          this.form.patchValue({
            fechaAjuste: this.toLocalDateTime(ajuste.fechaAjuste),
            motivo: ajuste.motivo,
            observaciones: ajuste.observaciones || ''
          });
          this.detalles.clear();
          ajuste.detalles.forEach((detalle) => {
            const index = this.detalles.length;
            this.detalles.push(this.crearDetalle({
              productoId: detalle.productoId,
              productoVarianteId: detalle.productoVarianteId ?? null,
              almacenId: detalle.almacenId,
              ubicacionAlmacenId: detalle.ubicacionAlmacenId ?? null,
              cantidadObjetivo: detalle.cantidadObjetivo
            }));
            if (detalle.productoVarianteId) {
              this.cargarExistencias(
                detalle.productoVarianteId,
                index,
                detalle.almacenId,
                detalle.ubicacionAlmacenId ?? null
              );
            }
          });
          if (this.detalles.length === 0) this.detalles.push(this.crearDetalle());
        },
        error: (err) => this.error.set(this.extraerError(err, 'No fue posible cargar el borrador.'))
      });
  }

  private cargarExistencias(
    varianteId: number,
    detalleIndex: number,
    almacenPersistido?: number | null,
    ubicacionPersistida?: number | null
  ): void {
    const existentes = this.existenciasPorVariante()[varianteId];
    if (existentes) {
      this.seleccionarExistenciaPersistida(detalleIndex, existentes, almacenPersistido, ubicacionPersistida);
      return;
    }

    this.cargandoExistencias.update(actual => ({ ...actual, [varianteId]: true }));
    this.existenciaService.getPaged({
      page: 1,
      pageSize: 200,
      productoVarianteId: varianteId
    }).pipe(finalize(() => {
      this.cargandoExistencias.update(actual => ({ ...actual, [varianteId]: false }));
    })).subscribe({
      next: response => {
        if (!response.success || !response.data) {
          this.existenciasPorVariante.update(actual => ({ ...actual, [varianteId]: [] }));
          this.error.set(response.message || 'No fue posible cargar las existencias físicas de la variante.');
          return;
        }
        const items = response.data.items ?? [];
        this.existenciasPorVariante.update(actual => ({ ...actual, [varianteId]: items }));
        this.seleccionarExistenciaPersistida(detalleIndex, items, almacenPersistido, ubicacionPersistida);
      },
      error: err => {
        this.existenciasPorVariante.update(actual => ({ ...actual, [varianteId]: [] }));
        this.error.set(this.extraerError(err, 'No fue posible cargar las existencias físicas de la variante.'));
      }
    });
  }

  private seleccionarExistenciaPersistida(
    detalleIndex: number,
    existencias: ExistenciaVariante[],
    almacenPersistido?: number | null,
    ubicacionPersistida?: number | null
  ): void {
    if (!almacenPersistido || detalleIndex < 0 || detalleIndex >= this.detalles.length) return;
    const ubicacion = ubicacionPersistida ?? null;
    const encontrada = existencias.find(item =>
      item.almacenId === almacenPersistido && (item.ubicacionAlmacenId ?? null) === ubicacion
    );
    if (!encontrada) {
      this.error.set('Una existencia física del borrador ya no está disponible. Revisa el detalle antes de guardarlo.');
      return;
    }
    this.detalles.at(detalleIndex).patchValue({
      existenciaId: encontrada.id,
      almacenId: encontrada.almacenId,
      ubicacionAlmacenId: encontrada.ubicacionAlmacenId ?? null
    });
  }

  private crearDetalle(value?: {
    productoId?: number | null;
    productoVarianteId?: number | null;
    existenciaId?: number | null;
    almacenId?: number | null;
    ubicacionAlmacenId?: number | null;
    cantidadObjetivo?: number;
  }): UntypedFormGroup {
    return this.fb.group({
      productoId: [value?.productoId ?? null, [Validators.required, Validators.min(1)]],
      productoVarianteId: [value?.productoVarianteId ?? null, [Validators.required, Validators.min(1)]],
      existenciaId: [value?.existenciaId ?? null, [Validators.required, Validators.min(1)]],
      almacenId: [value?.almacenId ?? null, [Validators.required, Validators.min(1)]],
      ubicacionAlmacenId: [value?.ubicacionAlmacenId ?? null],
      cantidadObjetivo: [value?.cantidadObjetivo ?? 0, [Validators.required, Validators.min(0)]]
    });
  }

  private toLocalDateTime(value: string): string {
    const date = new Date(value);
    const offset = date.getTimezoneOffset() * 60000;
    return new Date(date.getTime() - offset).toISOString().slice(0, 16);
  }

  private extraerError(error: any, fallback: string): string {
    const api = error?.error;
    if (typeof api?.message === 'string' && api.message.trim()) return api.message;
    if (Array.isArray(api?.errors) && api.errors.length) return api.errors.join(' ');
    return fallback;
  }
}
