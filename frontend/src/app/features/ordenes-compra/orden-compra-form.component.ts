import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, computed, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormArray, FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize, forkJoin, Subscription } from 'rxjs';
import { OrdenCompra, OrdenCompraDetalleInput, OrdenCompraFormValue } from '../../core/models/orden-compra.model';
import { Producto, ProductoVariante } from '../../core/models/producto.model';
import { Proveedor } from '../../core/models/proveedor.model';
import { SolicitudCompra } from '../../core/models/solicitud-compra.model';
import { OrdenCompraService } from '../../services/orden-compra.service';
import { ProductoService } from '../../services/producto.service';
import { ProveedorService } from '../../services/proveedor.service';
import { SolicitudCompraService } from '../../services/solicitud-compra.service';

type LineaForm = FormGroup<{
  productoId: FormControl<number | null>;
  productoVarianteId: FormControl<number | null>;
  cantidadOrdenada: FormControl<number | null>;
  precioUnitario: FormControl<number | null>;
  descuento: FormControl<number | null>;
  impuesto: FormControl<number | null>;
  observacion: FormControl<string>;
}>;

@Component({
  selector: 'app-orden-compra-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule],
  template: `
    <section class="form-shell" aria-labelledby="orden-form-title">
      <header>
        <div>
          <p class="eyebrow">Compras empresariales</p>
          <h1 id="orden-form-title">{{ editando() ? 'Editar orden de compra' : 'Nueva orden de compra' }}</h1>
          <p>Documento comercial sin afectación de inventario hasta la recepción autorizada.</p>
        </div>
        <button mat-stroked-button type="button" (click)="volver()"><mat-icon>arrow_back</mat-icon> Volver</button>
      </header>

      @if (loading()) {
        <div class="state" role="status"><mat-spinner diameter="36"></mat-spinner><span>Cargando editor…</span></div>
      } @else {
        @if (error()) { <div class="error" role="alert">{{ error() }}</div> }
        <form [formGroup]="form" (ngSubmit)="guardar()" novalidate>
          <fieldset [disabled]="saving()">
            <legend>Datos generales</legend>
            <div class="grid">
              <mat-form-field appearance="outline">
                <mat-label>Proveedor</mat-label>
                <mat-select formControlName="proveedorId" required>
                  @for (proveedor of proveedores(); track proveedor.id) { <mat-option [value]="proveedor.id">{{ proveedor.nombre }}</mat-option> }
                </mat-select>
                @if (form.controls.proveedorId.touched && form.controls.proveedorId.invalid) { <mat-error>Seleccione un proveedor.</mat-error> }
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Solicitud aprobada</mat-label>
                <mat-select formControlName="solicitudCompraId">
                  <mat-option [value]="null">Sin solicitud origen</mat-option>
                  @for (solicitud of solicitudes(); track solicitud.id) { <mat-option [value]="solicitud.id">{{ solicitud.numeroSolicitud }}</mat-option> }
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Moneda</mat-label>
                <input matInput formControlName="moneda" maxlength="3" autocomplete="off" required>
                <mat-hint>Código ISO, por ejemplo HNL o USD.</mat-hint>
                @if (form.controls.moneda.touched && form.controls.moneda.invalid) { <mat-error>Use un código de 3 letras.</mat-error> }
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Fecha esperada</mat-label>
                <input matInput type="datetime-local" formControlName="fechaEsperadaUtc">
              </mat-form-field>

              <mat-form-field appearance="outline" class="wide">
                <mat-label>Condiciones de compra</mat-label>
                <textarea matInput formControlName="condicionesCompra" rows="2" maxlength="1000"></textarea>
              </mat-form-field>

              <mat-form-field appearance="outline" class="wide">
                <mat-label>Observaciones</mat-label>
                <textarea matInput formControlName="observaciones" rows="2" maxlength="1000"></textarea>
              </mat-form-field>
            </div>
          </fieldset>

          <section class="lines" aria-labelledby="lineas-title">
            <div class="section-heading">
              <div><h2 id="lineas-title">Líneas</h2><p>Producto, variante, cantidad y componentes de precio.</p></div>
              <button mat-stroked-button type="button" (click)="agregarLinea()"><mat-icon>add</mat-icon> Agregar línea</button>
            </div>

            <div formArrayName="detalles" class="line-list">
              @for (linea of detalles.controls; track $index; let i = $index) {
                <article [formGroupName]="i" class="line-card">
                  <div class="line-index">{{ i + 1 }}</div>
                  <div class="line-grid">
                    <mat-form-field appearance="outline">
                      <mat-label>Producto</mat-label>
                      <mat-select formControlName="productoId" (selectionChange)="productoCambiado(i)" required>
                        @for (producto of productos(); track producto.id) { <mat-option [value]="producto.id">{{ producto.nombre }}</mat-option> }
                      </mat-select>
                    </mat-form-field>

                    <mat-form-field appearance="outline">
                      <mat-label>Variante</mat-label>
                      <mat-select formControlName="productoVarianteId">
                        <mat-option [value]="null">Sin variante</mat-option>
                        @for (variante of variantesPorLinea()[i] || []; track variante.id) { <mat-option [value]="variante.id">{{ variante.etiqueta || variante.sku }}</mat-option> }
                      </mat-select>
                    </mat-form-field>

                    <mat-form-field appearance="outline"><mat-label>Cantidad</mat-label><input matInput type="number" min="0.0001" step="0.0001" formControlName="cantidadOrdenada" required></mat-form-field>
                    <mat-form-field appearance="outline"><mat-label>Precio unitario</mat-label><input matInput type="number" min="0" step="0.01" formControlName="precioUnitario" required></mat-form-field>
                    <mat-form-field appearance="outline"><mat-label>Descuento</mat-label><input matInput type="number" min="0" step="0.01" formControlName="descuento"></mat-form-field>
                    <mat-form-field appearance="outline"><mat-label>Impuesto</mat-label><input matInput type="number" min="0" step="0.01" formControlName="impuesto"></mat-form-field>
                    <mat-form-field appearance="outline" class="wide"><mat-label>Observación de línea</mat-label><input matInput formControlName="observacion" maxlength="500"></mat-form-field>
                  </div>
                  <div class="line-total" aria-label="Total de la línea">{{ totalLinea(i) | number:'1.2-2' }}</div>
                  <button mat-icon-button type="button" (click)="eliminarLinea(i)" [disabled]="detalles.length === 1" [attr.aria-label]="'Eliminar línea ' + (i + 1)"><mat-icon>delete</mat-icon></button>
                </article>
              }
            </div>
          </section>

          <aside class="totals" aria-label="Totales derivados">
            <div><span>Subtotal</span><strong>{{ subtotal() | number:'1.2-2' }}</strong></div>
            <div><span>Descuento</span><strong>{{ descuentoTotal() | number:'1.2-2' }}</strong></div>
            <div><span>Impuesto</span><strong>{{ impuestoTotal() | number:'1.2-2' }}</strong></div>
            <div class="grand"><span>Total</span><strong>{{ total() | number:'1.2-2' }}</strong></div>
          </aside>

          <div class="actions">
            <button mat-button type="button" (click)="volver()" [disabled]="saving()">Cancelar</button>
            <button mat-flat-button type="submit" [disabled]="saving()">
              @if (saving()) { <mat-spinner diameter="20"></mat-spinner> } @else { <mat-icon>save</mat-icon> }
              {{ editando() ? 'Guardar cambios' : 'Crear orden' }}
            </button>
          </div>
        </form>
      }
    </section>
  `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    :host { display:block; }
    .form-shell { display:grid; gap:1.25rem; max-width:1200px; margin:0 auto; }
    header,.section-heading,.actions { display:flex; justify-content:space-between; gap:1rem; align-items:flex-start; }
    h1,h2 { margin:.15rem 0 .35rem; }
    p { margin:0; color:var(--text-secondary,#5f6368); }
    .eyebrow { font-size:.75rem; font-weight:700; letter-spacing:.08em; text-transform:uppercase; color:var(--primary-color,#3157d5); }
    form { display:grid; gap:1.25rem; }
    fieldset { border:1px solid var(--border-color,#dfe3ea); border-radius:1rem; padding:1rem; }
    legend { padding:0 .4rem; font-weight:700; }
    .grid,.line-grid { display:grid; grid-template-columns:repeat(4,minmax(0,1fr)); gap:.75rem; }
    .wide { grid-column:span 2; }
    .lines { display:grid; gap:.8rem; }
    .line-list { display:grid; gap:.75rem; }
    .line-card { display:grid; grid-template-columns:auto 1fr auto auto; gap:.75rem; align-items:center; border:1px solid var(--border-color,#dfe3ea); border-radius:1rem; padding:1rem; }
    .line-index { width:2rem; height:2rem; border-radius:50%; display:grid; place-items:center; background:var(--surface-variant,#eef2ff); font-weight:700; }
    .line-total { min-width:7rem; text-align:right; font-weight:700; }
    .totals { justify-self:end; min-width:320px; display:grid; gap:.45rem; padding:1rem; border-radius:1rem; background:var(--surface-variant,#f6f7f9); }
    .totals div { display:flex; justify-content:space-between; gap:2rem; }
    .totals .grand { padding-top:.6rem; border-top:1px solid var(--border-color,#dfe3ea); font-size:1.1rem; }
    .actions { justify-content:flex-end; align-items:center; }
    .actions button { display:inline-flex; align-items:center; gap:.35rem; }
    .state { min-height:10rem; display:flex; align-items:center; justify-content:center; gap:.75rem; }
    .error { padding:.8rem 1rem; border-radius:.75rem; background:#fde8e7; color:#9b1c16; }
    @media(max-width:900px){ .grid,.line-grid{grid-template-columns:repeat(2,minmax(0,1fr));}.line-card{grid-template-columns:auto 1fr auto}.line-total{grid-column:2}.wide{grid-column:span 2;} }
    @media(max-width:600px){ header,.section-heading{flex-direction:column}.grid,.line-grid{grid-template-columns:1fr}.wide{grid-column:span 1}.line-card{grid-template-columns:auto 1fr}.line-total{grid-column:1/-1;text-align:left}.totals{justify-self:stretch;min-width:0}.actions{flex-wrap:wrap;} }
  `]
})
export class OrdenCompraFormComponent implements OnInit, OnDestroy {
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly proveedores = signal<Proveedor[]>([]);
  readonly solicitudes = signal<SolicitudCompra[]>([]);
  readonly productos = signal<Producto[]>([]);
  readonly variantesPorLinea = signal<ProductoVariante[][]>([]);
  readonly ordenId = signal<number | null>(null);
  readonly editando = computed(() => this.ordenId() !== null);

  private readonly fb = inject(FormBuilder);

  readonly form = this.fb.group({
    solicitudCompraId: this.fb.control<number | null>(null),
    proveedorId: this.fb.control<number | null>(null, Validators.required),
    moneda: this.fb.nonNullable.control('HNL', [Validators.required, Validators.pattern(/^[A-Za-z]{3}$/)]),
    condicionesCompra: this.fb.nonNullable.control('', Validators.maxLength(1000)),
    fechaEsperadaUtc: this.fb.control<string | null>(null),
    observaciones: this.fb.nonNullable.control('', Validators.maxLength(1000)),
    detalles: this.fb.array<LineaForm>([])
  });

  private readonly subscriptions = new Subscription();
  private idempotencyKey = this.nuevaIdempotencyKey();

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly ordenService: OrdenCompraService,
    private readonly proveedorService: ProveedorService,
    private readonly solicitudService: SolicitudCompraService,
    private readonly productoService: ProductoService
  ) {}

  get detalles(): FormArray<LineaForm> { return this.form.controls.detalles; }

  ngOnInit(): void {
    const rawId = Number(this.route.snapshot.paramMap.get('id'));
    this.ordenId.set(Number.isInteger(rawId) && rawId > 0 ? rawId : null);
    this.agregarLinea();

    const catalogos$ = forkJoin({
      proveedores: this.proveedorService.getActivos(),
      solicitudes: this.solicitudService.getPaged({ page: 1, pageSize: 100, estado: 'Aprobada' }),
      productos: this.productoService.getPaged({ page: 1, pageSize: 100, activo: true, sortBy: 'Nombre', sortDirection: 'asc' })
    });

    this.subscriptions.add(catalogos$.subscribe({
      next: ({ proveedores, solicitudes, productos }) => {
        this.proveedores.set(proveedores.success ? (proveedores.data ?? []) : []);
        this.solicitudes.set(solicitudes.success ? (solicitudes.data?.items ?? []) : []);
        this.productos.set(productos.success ? (productos.data?.items ?? []) : []);
        if (this.editando()) this.cargarOrden(this.ordenId()!); else this.loading.set(false);
      },
      error: () => {
        this.error.set('No fue posible cargar los catálogos necesarios para editar la orden.');
        this.loading.set(false);
      }
    }));
  }

  ngOnDestroy(): void { this.subscriptions.unsubscribe(); }

  agregarLinea(valor?: Partial<OrdenCompraDetalleInput>): void {
    this.detalles.push(this.fb.group({
      productoId: this.fb.control<number | null>(valor?.productoId ?? null, Validators.required),
      productoVarianteId: this.fb.control<number | null>(valor?.productoVarianteId ?? null),
      cantidadOrdenada: this.fb.control<number | null>(valor?.cantidadOrdenada ?? 1, [Validators.required, Validators.min(0.0001)]),
      precioUnitario: this.fb.control<number | null>(valor?.precioUnitario ?? 0, [Validators.required, Validators.min(0)]),
      descuento: this.fb.control<number | null>(valor?.descuento ?? 0, [Validators.required, Validators.min(0)]),
      impuesto: this.fb.control<number | null>(valor?.impuesto ?? 0, [Validators.required, Validators.min(0)]),
      observacion: this.fb.nonNullable.control(valor?.observacion ?? '', Validators.maxLength(500))
    }));
    this.variantesPorLinea.update(listas => [...listas, []]);
  }

  eliminarLinea(index: number): void {
    if (this.detalles.length <= 1) return;
    this.detalles.removeAt(index);
    this.variantesPorLinea.update(listas => listas.filter((_, i) => i !== index));
  }

  productoCambiado(index: number): void {
    const linea = this.detalles.at(index);
    linea.controls.productoVarianteId.setValue(null);
    const productoId = linea.controls.productoId.value;
    if (!productoId) {
      this.setVariantes(index, []);
      return;
    }
    this.subscriptions.add(this.productoService.getVariantes(productoId, false).subscribe({
      next: response => this.setVariantes(index, response.success ? (response.data ?? []).filter(v => v.activo && !v.eliminado) : []),
      error: () => this.setVariantes(index, [])
    }));
  }

  totalLinea(index: number): number {
    const v = this.detalles.at(index).getRawValue();
    return this.numero(v.cantidadOrdenada) * this.numero(v.precioUnitario) - this.numero(v.descuento) + this.numero(v.impuesto);
  }

  subtotal(): number {
    return this.detalles.controls.reduce((sum, linea) => {
      const v = linea.getRawValue();
      return sum + this.numero(v.cantidadOrdenada) * this.numero(v.precioUnitario);
    }, 0);
  }

  descuentoTotal(): number {
    return this.detalles.controls.reduce((sum, linea) => sum + this.numero(linea.controls.descuento.value), 0);
  }

  impuestoTotal(): number {
    return this.detalles.controls.reduce((sum, linea) => sum + this.numero(linea.controls.impuesto.value), 0);
  }

  total(): number { return this.subtotal() - this.descuentoTotal() + this.impuestoTotal(); }

  guardar(): void {
    this.error.set(null);
    if (this.form.invalid || this.detalles.length === 0) {
      this.form.markAllAsTouched();
      this.error.set('Revise los campos obligatorios y las líneas de la orden.');
      return;
    }
    const value = this.toRequest();
    this.saving.set(true);
    const request$ = this.editando()
      ? this.ordenService.update(this.ordenId()!, value)
      : this.ordenService.create(value, this.idempotencyKey);
    this.subscriptions.add(request$.pipe(finalize(() => this.saving.set(false))).subscribe({
      next: response => {
        if (!response.success || !response.data) {
          this.error.set(response.message || 'La orden no pudo guardarse.');
          return;
        }
        this.idempotencyKey = this.nuevaIdempotencyKey();
        void this.router.navigate(['/ordenes-compra'], { queryParams: { selected: response.data.id } });
      },
      error: err => this.error.set(err?.error?.message || 'Ocurrió un error al guardar la orden de compra.')
    }));
  }

  volver(): void { void this.router.navigate(['/ordenes-compra']); }

  private cargarOrden(id: number): void {
    this.subscriptions.add(this.ordenService.getById(id).pipe(finalize(() => this.loading.set(false))).subscribe({
      next: response => {
        if (!response.success || !response.data) {
          this.error.set(response.message || 'No fue posible cargar la orden.');
          return;
        }
        this.hidratar(response.data);
      },
      error: () => this.error.set('No fue posible cargar la orden de compra.')
    }));
  }

  private hidratar(orden: OrdenCompra): void {
    this.form.patchValue({
      solicitudCompraId: orden.solicitudCompraId ?? null,
      proveedorId: orden.proveedorId,
      moneda: orden.moneda,
      condicionesCompra: orden.condicionesCompra ?? '',
      fechaEsperadaUtc: this.toLocalDateTime(orden.fechaEsperadaUtc),
      observaciones: orden.observaciones ?? ''
    });
    this.detalles.clear();
    this.variantesPorLinea.set([]);
    orden.detalles.forEach(detalle => {
      this.agregarLinea(detalle);
      const index = this.detalles.length - 1;
      if (detalle.productoId) {
        this.subscriptions.add(this.productoService.getVariantes(detalle.productoId, true).subscribe({
          next: response => this.setVariantes(index, response.success ? (response.data ?? []) : []),
          error: () => this.setVariantes(index, [])
        }));
      }
    });
    if (this.detalles.length === 0) this.agregarLinea();
  }

  private toRequest(): OrdenCompraFormValue {
    const raw = this.form.getRawValue();
    return {
      solicitudCompraId: raw.solicitudCompraId,
      proveedorId: raw.proveedorId!,
      moneda: raw.moneda.trim().toUpperCase(),
      condicionesCompra: raw.condicionesCompra.trim() || null,
      fechaEsperadaUtc: raw.fechaEsperadaUtc ? new Date(raw.fechaEsperadaUtc).toISOString() : null,
      observaciones: raw.observaciones.trim() || null,
      detalles: raw.detalles.map(linea => ({
        productoId: linea.productoId!,
        productoVarianteId: linea.productoVarianteId,
        cantidadOrdenada: this.numero(linea.cantidadOrdenada),
        precioUnitario: this.numero(linea.precioUnitario),
        descuento: this.numero(linea.descuento),
        impuesto: this.numero(linea.impuesto),
        observacion: linea.observacion.trim() || null
      }))
    };
  }

  private setVariantes(index: number, variantes: ProductoVariante[]): void {
    this.variantesPorLinea.update(listas => listas.map((actual, i) => i === index ? variantes : actual));
  }

  private toLocalDateTime(value?: string | null): string | null {
    if (!value) return null;
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) return null;
    const local = new Date(date.getTime() - date.getTimezoneOffset() * 60000);
    return local.toISOString().slice(0, 16);
  }

  private numero(value: number | null | undefined): number { return Number.isFinite(Number(value)) ? Number(value) : 0; }

  private nuevaIdempotencyKey(): string {
    const uuid = globalThis.crypto?.randomUUID?.();
    return uuid ? `orden-compra-form:${uuid}` : `orden-compra-form:${Date.now()}:${Math.random().toString(36).slice(2)}`;
  }
}
