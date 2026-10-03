
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize, forkJoin } from 'rxjs';
import { Almacen } from '../../core/models/almacen.model';
import { ProductoVariante } from '../../core/models/producto.model';
import { TransferenciaInventarioDetalleInput, TransferenciaInventarioFormValue } from '../../core/models/transferencia-inventario.model';
import { UbicacionAlmacen } from '../../core/models/ubicacion-almacen.model';
import { AlmacenService } from '../../services/almacen.service';
import { ProductoService } from '../../services/producto.service';
import { TransferenciaInventarioService } from '../../services/transferencia-inventario.service';
import { UbicacionAlmacenService } from '../../services/ubicacion-almacen.service';

@Component({
  selector: 'app-transferencia-form',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule],
  template: `
    <section class="page" aria-labelledby="transferencia-form-title">
      <header class="header">
        <div>
          <p class="eyebrow">Inventario empresarial</p>
          <h1 id="transferencia-form-title">{{ editando ? 'Editar transferencia' : 'Nueva transferencia' }}</h1>
          <p>Selecciona almacenes, variantes y ubicaciones físicas. El lifecycle se ejecuta desde el detalle.</p>
        </div>
        <button mat-stroked-button type="button" (click)="volver()"><mat-icon>arrow_back</mat-icon>Volver</button>
      </header>
    
      @if (loading || catalogLoading) {
        <div class="state" aria-live="polite">
          <mat-spinner diameter="36"></mat-spinner><span>{{ loading ? 'Cargando transferencia…' : 'Cargando catálogos…' }}</span>
        </div>
      }
      @if (!loading && !catalogLoading) {
        <form class="card" (ngSubmit)="guardar()">
          <div class="grid two">
            <mat-form-field appearance="outline">
              <mat-label>Almacén origen</mat-label>
              <mat-select name="almacenOrigenId" [(ngModel)]="model.almacenOrigenId" (selectionChange)="onAlmacenOrigenChange()" required>
                @for (almacen of almacenes; track almacen) {
                  <mat-option [value]="almacen.id">{{ etiquetaAlmacen(almacen) }}</mat-option>
                }
              </mat-select>
              <mat-hint>Origen físico del stock.</mat-hint>
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Almacén destino</mat-label>
              <mat-select name="almacenDestinoId" [(ngModel)]="model.almacenDestinoId" (selectionChange)="onAlmacenDestinoChange()" required>
                @for (almacen of almacenes; track almacen) {
                  <mat-option [value]="almacen.id" [disabled]="almacen.id === model.almacenOrigenId">{{ etiquetaAlmacen(almacen) }}</mat-option>
                }
              </mat-select>
              <mat-hint>Debe ser diferente del origen.</mat-hint>
            </mat-form-field>
          </div>
          <mat-form-field appearance="outline" class="full">
            <mat-label>Observaciones</mat-label>
            <textarea matInput rows="3" name="observaciones" [(ngModel)]="model.observaciones" maxlength="1000"></textarea>
          </mat-form-field>
          <div class="details-header">
            <div><h2>Detalle solicitado</h2><p>Una línea por variante y contexto físico.</p></div>
            <button mat-stroked-button type="button" (click)="agregarDetalle()"><mat-icon>add</mat-icon>Agregar línea</button>
          </div>
          @for (detalle of model.detalles; track trackByIndex(i, detalle); let i = $index) {
            <div class="detail">
              <div class="grid detail-grid">
                <mat-form-field appearance="outline">
                  <mat-label>Variante</mat-label>
                  <mat-select [name]="'variante-' + i" [(ngModel)]="detalle.productoVarianteId" required>
                    @for (variante of variantes; track variante) {
                      <mat-option [value]="variante.id">{{ etiquetaVariante(variante) }}</mat-option>
                    }
                  </mat-select>
                  <mat-hint>SKU y atributos de la variante operativa.</mat-hint>
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Ubicación origen</mat-label>
                  <mat-select [name]="'ubicacion-origen-' + i" [(ngModel)]="detalle.ubicacionOrigenId" [disabled]="!model.almacenOrigenId">
                    <mat-option [value]="null">Sin ubicación específica</mat-option>
                    @for (ubicacion of ubicacionesOrigen; track ubicacion) {
                      <mat-option [value]="ubicacion.id">{{ etiquetaUbicacion(ubicacion) }}</mat-option>
                    }
                  </mat-select>
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Ubicación destino</mat-label>
                  <mat-select [name]="'ubicacion-destino-' + i" [(ngModel)]="detalle.ubicacionDestinoId" [disabled]="!model.almacenDestinoId">
                    <mat-option [value]="null">Sin ubicación específica</mat-option>
                    @for (ubicacion of ubicacionesDestino; track ubicacion) {
                      <mat-option [value]="ubicacion.id">{{ etiquetaUbicacion(ubicacion) }}</mat-option>
                    }
                  </mat-select>
                </mat-form-field>
                <mat-form-field appearance="outline"><mat-label>Cantidad</mat-label><input matInput type="number" min="1" step="1" [name]="'cantidad-' + i" [(ngModel)]="detalle.cantidadSolicitada" required /></mat-form-field>
                <button mat-icon-button type="button" aria-label="Eliminar línea" (click)="quitarDetalle(i)" [disabled]="model.detalles.length === 1"><mat-icon>delete</mat-icon></button>
              </div>
            </div>
          }
          @if (error) {
            <div class="error" role="alert">{{ error }}</div>
          }
          <div class="actions">
            <button mat-stroked-button type="button" (click)="volver()" [disabled]="saving">Cancelar</button>
            <button mat-flat-button color="primary" type="submit" [disabled]="saving || almacenes.length < 2 || variantes.length === 0">
              @if (saving) {
                <mat-spinner diameter="20"></mat-spinner>
              }
              @if (!saving) {
                <span>{{ editando ? 'Guardar cambios' : 'Crear transferencia' }}</span>
              }
            </button>
          </div>
        </form>
      }
    </section>
    `,
  styles: [`
    .page{padding:24px;display:grid;gap:20px}.header{display:flex;justify-content:space-between;gap:16px}.eyebrow{text-transform:uppercase;letter-spacing:.08em;font-size:12px;font-weight:700;margin:0}.header h1{margin:4px 0}.header p,.details-header p{margin:0;color:var(--text-secondary,#667085)}.card{display:grid;gap:18px;padding:20px;border:1px solid rgba(0,0,0,.12);border-radius:14px}.grid{display:grid;gap:12px}.two{grid-template-columns:1fr 1fr}.detail-grid{grid-template-columns:minmax(260px,1.5fr) minmax(190px,1fr) minmax(190px,1fr) minmax(120px,.6fr) auto;align-items:start}.full{width:100%}.details-header{display:flex;justify-content:space-between;align-items:center;gap:16px}.details-header h2{margin:0}.detail{padding:14px;border:1px solid rgba(0,0,0,.08);border-radius:10px}.actions{display:flex;justify-content:flex-end;gap:10px}.error{color:#b42318}.state{min-height:160px;display:flex;justify-content:center;align-items:center;gap:12px}@media(max-width:1050px){.detail-grid{grid-template-columns:1fr 1fr}.two{grid-template-columns:1fr}}@media(max-width:600px){.page{padding:16px}.header,.details-header{flex-direction:column;align-items:stretch}.detail-grid{grid-template-columns:1fr}}
  `]
})
export class TransferenciaFormComponent implements OnInit {
  readonly id: number;
  almacenes: Almacen[] = [];
  ubicaciones: UbicacionAlmacen[] = [];
  variantes: ProductoVariante[] = [];
  loading = false;
  catalogLoading = true;
  saving = false;
  error = '';
  model: TransferenciaInventarioFormValue = this.nuevoModelo();

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly service: TransferenciaInventarioService,
    private readonly almacenService: AlmacenService,
    private readonly ubicacionService: UbicacionAlmacenService,
    private readonly productoService: ProductoService
  ) {
    this.id = Number(this.route.snapshot.paramMap.get('id')) || 0;
  }

  get editando(): boolean { return this.id > 0; }
  get ubicacionesOrigen(): UbicacionAlmacen[] { return this.ubicaciones.filter(item => item.almacenId === this.model.almacenOrigenId); }
  get ubicacionesDestino(): UbicacionAlmacen[] { return this.ubicaciones.filter(item => item.almacenId === this.model.almacenDestinoId); }

  ngOnInit(): void {
    this.cargarCatalogos();
    if (this.editando) this.cargarTransferencia();
  }

  guardar(): void {
    this.error = '';
    if (!this.esValido()) return;
    this.saving = true;
    const operation = this.editando ? this.service.update(this.id, this.model) : this.service.create(this.model);
    operation.pipe(finalize(() => this.saving = false)).subscribe({
      next: response => {
        if (!response.success) { this.error = response.message || 'No se pudo guardar la transferencia.'; return; }
        void this.router.navigate(['/inventario/transferencias', response.data.id]);
      },
      error: error => this.error = error?.error?.message || 'No se pudo guardar la transferencia.'
    });
  }

  onAlmacenOrigenChange(): void {
    for (const detalle of this.model.detalles) detalle.ubicacionOrigenId = null;
    if (this.model.almacenDestinoId === this.model.almacenOrigenId) {
      this.model.almacenDestinoId = 0;
      for (const detalle of this.model.detalles) detalle.ubicacionDestinoId = null;
    }
  }

  onAlmacenDestinoChange(): void {
    for (const detalle of this.model.detalles) detalle.ubicacionDestinoId = null;
  }

  agregarDetalle(): void { this.model.detalles.push(this.nuevoDetalle()); }
  quitarDetalle(index: number): void { if (this.model.detalles.length > 1) this.model.detalles.splice(index, 1); }
  trackByIndex(index: number, _detalle?: TransferenciaInventarioDetalleInput): number { return index; }
  volver(): void { void this.router.navigate(['/inventario/transferencias']); }
  etiquetaAlmacen(almacen: Almacen): string { return `${almacen.codigo} — ${almacen.nombre} · ${almacen.sucursalNombre}`; }
  etiquetaUbicacion(ubicacion: UbicacionAlmacen): string { return `${ubicacion.codigo} — ${ubicacion.nombre}`; }
  etiquetaVariante(variante: ProductoVariante): string {
    const atributos = [variante.productoNombre, variante.marcaNombre, variante.modeloNombre, variante.colorNombre, variante.tallaNombre].filter(Boolean).join(' · ');
    return `${atributos || variante.productoNombre} — SKU ${variante.sku || variante.id}`;
  }

  private cargarCatalogos(): void {
    this.catalogLoading = true;
    forkJoin({
      almacenes: this.almacenService.getActivos(),
      ubicaciones: this.ubicacionService.getActivas(),
      productos: this.productoService.getPaged({ page: 1, pageSize: 200, activo: true, sortBy: 'Nombre', sortDirection: 'asc' })
    }).pipe(finalize(() => this.catalogLoading = false)).subscribe({
      next: result => {
        this.almacenes = result.almacenes.success ? result.almacenes.data : [];
        this.ubicaciones = result.ubicaciones.success ? result.ubicaciones.data : [];
        this.variantes = result.productos.success
          ? result.productos.data.items.flatMap(producto => producto.variantes?.filter(variante => variante.activo && !variante.eliminado) ?? [])
          : [];
        if (this.almacenes.length < 2) this.error = 'Se requieren al menos dos almacenes activos para crear una transferencia.';
        else if (this.variantes.length === 0) this.error = 'No hay variantes activas disponibles para transferir.';
      },
      error: () => this.error = 'No se pudieron cargar los catálogos necesarios para la transferencia.'
    });
  }

  private cargarTransferencia(): void {
    this.loading = true;
    this.service.getById(this.id).pipe(finalize(() => this.loading = false)).subscribe({
      next: response => {
        if (!response.success) { this.error = response.message || 'No se pudo cargar la transferencia.'; return; }
        if (response.data.estado !== 'Borrador') { this.error = 'Sólo las transferencias en borrador pueden editarse.'; return; }
        this.model = {
          almacenOrigenId: response.data.almacenOrigenId,
          almacenDestinoId: response.data.almacenDestinoId,
          observaciones: response.data.observaciones,
          detalles: response.data.detalles.map(item => ({
            productoVarianteId: item.productoVarianteId,
            ubicacionOrigenId: item.ubicacionOrigenId ?? null,
            ubicacionDestinoId: item.ubicacionDestinoId ?? null,
            cantidadSolicitada: item.cantidadSolicitada
          }))
        };
      },
      error: () => this.error = 'No se pudo cargar la transferencia.'
    });
  }

  private esValido(): boolean {
    if (this.model.almacenOrigenId <= 0 || this.model.almacenDestinoId <= 0) { this.error = 'Debes indicar almacenes válidos.'; return false; }
    if (this.model.almacenOrigenId === this.model.almacenDestinoId) { this.error = 'El almacén de origen y destino deben ser diferentes.'; return false; }
    if (!this.almacenes.some(item => item.id === this.model.almacenOrigenId) || !this.almacenes.some(item => item.id === this.model.almacenDestinoId)) { this.error = 'Los almacenes deben estar activos.'; return false; }
    if (!this.model.detalles.length || this.model.detalles.some(item => item.productoVarianteId <= 0 || item.cantidadSolicitada <= 0)) { this.error = 'Cada línea debe tener variante y cantidad positiva.'; return false; }
    if (this.model.detalles.some(item => item.ubicacionOrigenId && !this.ubicacionesOrigen.some(u => u.id === item.ubicacionOrigenId))) { this.error = 'Una ubicación de origen no pertenece al almacén origen.'; return false; }
    if (this.model.detalles.some(item => item.ubicacionDestinoId && !this.ubicacionesDestino.some(u => u.id === item.ubicacionDestinoId))) { this.error = 'Una ubicación de destino no pertenece al almacén destino.'; return false; }
    return true;
  }

  private nuevoModelo(): TransferenciaInventarioFormValue {
    return { almacenOrigenId: 0, almacenDestinoId: 0, observaciones: '', detalles: [this.nuevoDetalle()] };
  }

  private nuevoDetalle(): TransferenciaInventarioDetalleInput {
    return { productoVarianteId: 0, ubicacionOrigenId: null, ubicacionDestinoId: null, cantidadSolicitada: 1 };
  }
}
