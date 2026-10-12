
import { Component, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import { Almacen } from '../../core/models/almacen.model';
import { Categoria } from '../../core/models/categoria.model';
import { ConteoInventarioFormValue, TipoConteoInventario } from '../../core/models/conteo-inventario.model';
import { UbicacionAlmacen } from '../../core/models/ubicacion-almacen.model';
import { AlmacenService } from '../../services/almacen.service';
import { CategoriaService } from '../../services/categoria.service';
import { ConteoInventarioService } from '../../services/conteo-inventario.service';
import { UbicacionAlmacenService } from '../../services/ubicacion-almacen.service';

@Component({
  selector: 'app-conteo-inventario-form',
  standalone: true,
  imports: [FormsModule, MatButtonModule, MatCheckboxModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule],
  template: `
    <section class="page" aria-labelledby="conteo-form-title">
      <header><button mat-icon-button type="button" aria-label="Volver" (click)="volver()"><mat-icon>arrow_back</mat-icon></button><div><p class="eyebrow">Conteos físicos</p><h1 id="conteo-form-title">{{ id ? 'Editar borrador' : 'Nuevo conteo' }}</h1><p>Define el alcance físico antes de iniciar el conteo.</p></div></header>
      <form #form="ngForm" (ngSubmit)="guardar()" class="card" novalidate>
        <div class="grid">
          <mat-form-field appearance="outline"><mat-label>Tipo</mat-label><mat-select required name="tipo" [(ngModel)]="model.tipo" (selectionChange)="sincronizarTipo()">@for (item of tipos; track item) {
          <mat-option [value]="item.value">{{ item.label }}</mat-option>
        }</mat-select></mat-form-field>
        <mat-form-field appearance="outline"><mat-label>Almacén</mat-label><mat-select required name="almacenId" [(ngModel)]="model.almacenId" (selectionChange)="onAlmacenChange()">@for (almacen of almacenes; track almacen) {
        <mat-option [value]="almacen.id">{{ almacen.codigo }} · {{ almacen.nombre }}</mat-option>
      }</mat-select>@if (!almacenes.length && !cargandoCatalogos) {
      <mat-hint>No hay almacenes activos disponibles.</mat-hint>
    }</mat-form-field>
    @if (requiereUbicacion) {
      <mat-form-field appearance="outline"><mat-label>Ubicación</mat-label><mat-select required name="ubicacionAlmacenId" [(ngModel)]="model.ubicacionAlmacenId" [disabled]="!model.almacenId || cargandoUbicaciones">@for (ubicacion of ubicaciones; track ubicacion) {
      <mat-option [value]="ubicacion.id">{{ ubicacion.codigo }} · {{ ubicacion.nombre }}</mat-option>
    }</mat-select>@if (model.almacenId && !ubicaciones.length && !cargandoUbicaciones) {
    <mat-hint>El almacén no tiene ubicaciones activas.</mat-hint>
    }</mat-form-field>
    }
    @if (requiereCategoria) {
      <mat-form-field appearance="outline"><mat-label>Categoría</mat-label><mat-select required name="categoriaId" [(ngModel)]="model.categoriaId">@for (categoria of categorias; track categoria) {
      <mat-option [value]="categoria.id">{{ categoria.nombre }}</mat-option>
    }</mat-select>@if (!categorias.length && !cargandoCatalogos) {
    <mat-hint>No hay categorías activas disponibles.</mat-hint>
    }</mat-form-field>
    }
    </div>
    <mat-checkbox name="esCiego" [(ngModel)]="model.esCiego">Ocultar stock esperado durante la captura (conteo ciego)</mat-checkbox>
    <mat-form-field appearance="outline" class="full"><mat-label>Variantes específicas</mat-label><input matInput name="variantes" [(ngModel)]="variantesTexto" placeholder="Ej. 101, 102, 103" /><mat-hint>Opcional para alcances automáticos; si se especifican, usa IDs separados por coma.</mat-hint></mat-form-field>
    <mat-form-field appearance="outline" class="full"><mat-label>Observaciones</mat-label><textarea matInput rows="4" name="observaciones" [(ngModel)]="model.observaciones"></textarea></mat-form-field>
    @if (error) {
      <div class="error" role="alert">{{ error }}</div>
    }
    <div class="actions"><button mat-button type="button" [disabled]="saving" (click)="volver()">Cancelar</button><button mat-flat-button color="primary" type="submit" [disabled]="saving || cargandoCatalogos || form.invalid">@if (saving) {
    <mat-spinner diameter="20"></mat-spinner>
    }@if (!saving) {
    <span>{{ id ? 'Guardar cambios' : 'Crear conteo' }}</span>
    }</button></div>
    </form>
    </section>
    `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .page{max-width:980px;margin:0 auto;padding:24px;display:grid;gap:20px}header{display:flex;align-items:flex-start;gap:12px}header h1{margin:0}header p{margin:5px 0;color:#667085}.eyebrow{text-transform:uppercase;letter-spacing:.08em;font-size:.72rem;font-weight:700;color:var(--primary,#3f51b5)!important}.card{display:grid;gap:18px;padding:24px;border:1px solid #e4e7ec;border-radius:14px;background:var(--surface,#fff)}.grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:14px}.full{width:100%}.actions{display:flex;justify-content:flex-end;gap:8px}.error{padding:12px;border-radius:8px;background:#fef3f2;color:#b42318}@media(max-width:640px){.page{padding:16px}.grid{grid-template-columns:1fr}.card{padding:16px}}
  `]
})
export class ConteoInventarioFormComponent implements OnInit {
  readonly tipos = [
    { value: TipoConteoInventario.General, label: 'General' },
    { value: TipoConteoInventario.Ciclico, label: 'Cíclico' },
    { value: TipoConteoInventario.PorUbicacion, label: 'Por ubicación' },
    { value: TipoConteoInventario.PorCategoria, label: 'Por categoría' },
    { value: TipoConteoInventario.Ciego, label: 'Ciego' }
  ];
  id: number | null = null;
  saving = false;
  cargandoCatalogos = true;
  cargandoUbicaciones = false;
  error = '';
  variantesTexto = '';
  almacenes: Almacen[] = [];
  ubicaciones: UbicacionAlmacen[] = [];
  categorias: Categoria[] = [];
  model: ConteoInventarioFormValue = { tipo: TipoConteoInventario.General, almacenId: 0, esCiego: false, productoVarianteIds: [], observaciones: '' };

  constructor(
    private readonly service: ConteoInventarioService,
    private readonly almacenesService: AlmacenService,
    private readonly ubicacionesService: UbicacionAlmacenService,
    private readonly categoriasService: CategoriaService,
    private readonly route: ActivatedRoute,
    private readonly router: Router
  ) {}

  ngOnInit(): void {
    this.cargarCatalogos();
    const value = Number(this.route.snapshot.paramMap.get('id'));
    this.id = Number.isInteger(value) && value > 0 ? value : null;
    if (!this.id) return;
    this.service.getById(this.id).subscribe({ next: response => {
      if (!response.success) { this.error = response.message || 'No se pudo cargar el conteo.'; return; }
      const item = response.data;
      this.model = { tipo: item.tipo, almacenId: item.almacenId, ubicacionAlmacenId: item.ubicacionAlmacenId, categoriaId: item.categoriaId, esCiego: item.esCiego, observaciones: item.observaciones, productoVarianteIds: item.detalles.map(x => x.productoVarianteId) };
      this.variantesTexto = this.model.productoVarianteIds.join(', ');
      this.cargarUbicaciones(false);
    }, error: () => this.error = 'No se pudo cargar el conteo.' });
  }

  get requiereUbicacion(): boolean { return this.model.tipo === TipoConteoInventario.PorUbicacion; }
  get requiereCategoria(): boolean { return this.model.tipo === TipoConteoInventario.PorCategoria; }

  sincronizarTipo(): void {
    this.model.esCiego = this.model.tipo === TipoConteoInventario.Ciego || this.model.esCiego;
    if (!this.requiereUbicacion) this.model.ubicacionAlmacenId = null;
    if (!this.requiereCategoria) this.model.categoriaId = null;
  }

  onAlmacenChange(): void {
    this.model.ubicacionAlmacenId = null;
    this.cargarUbicaciones(true);
  }

  guardar(): void {
    this.error = '';
    if (!this.model.almacenId || this.model.almacenId < 1) { this.error = 'Selecciona un almacén válido.'; return; }
    if (this.requiereUbicacion && (!this.model.ubicacionAlmacenId || this.model.ubicacionAlmacenId < 1)) { this.error = 'Selecciona una ubicación válida para el conteo por ubicación.'; return; }
    if (this.requiereCategoria && (!this.model.categoriaId || this.model.categoriaId < 1)) { this.error = 'Selecciona una categoría válida para el conteo por categoría.'; return; }
    const tokens = this.variantesTexto.split(',').map(x => x.trim()).filter(Boolean);
    const ids = tokens.map(x => Number(x)).filter(x => Number.isInteger(x) && x > 0);
    if (tokens.length !== ids.length) { this.error = 'Las variantes deben ser IDs numéricos positivos separados por coma.'; return; }
    const value: ConteoInventarioFormValue = { ...this.model, productoVarianteIds: [...new Set(ids)] };
    this.saving = true;
    const request = this.id ? this.service.update(this.id, value) : this.service.create(value);
    request.pipe(finalize(() => this.saving = false)).subscribe({ next: response => { if (!response.success) { this.error = response.message || 'No se pudo guardar el conteo.'; return; } void this.router.navigate(['/inventario/conteos', response.data.id]); }, error: err => this.error = err?.error?.message || 'No se pudo guardar el conteo.' });
  }

  volver(): void { void this.router.navigate(['/inventario/conteos']); }

  private cargarCatalogos(): void {
    this.cargandoCatalogos = true;
    let pendientes = 2;
    const finalizar = () => { pendientes--; if (pendientes === 0) this.cargandoCatalogos = false; };

    this.almacenesService.getActivos().subscribe({
      next: response => { if (response.success) this.almacenes = response.data; else this.error = response.message || 'No se pudieron cargar los almacenes activos.'; finalizar(); },
      error: () => { this.error = 'No se pudieron cargar los almacenes activos.'; finalizar(); }
    });
    this.categoriasService.getActivas().subscribe({
      next: response => { if (response.success) this.categorias = response.data; else this.error = response.message || 'No se pudieron cargar las categorías activas.'; finalizar(); },
      error: () => { this.error = 'No se pudieron cargar las categorías activas.'; finalizar(); }
    });
  }

  private cargarUbicaciones(limpiarSiFalla: boolean): void {
    if (!this.model.almacenId || this.model.almacenId < 1) { this.ubicaciones = []; return; }
    this.cargandoUbicaciones = true;
    this.ubicacionesService.getActivas(this.model.almacenId).pipe(finalize(() => this.cargandoUbicaciones = false)).subscribe({
      next: response => {
        if (!response.success) { this.ubicaciones = []; if (limpiarSiFalla) this.model.ubicacionAlmacenId = null; this.error = response.message || 'No se pudieron cargar las ubicaciones del almacén.'; return; }
        this.ubicaciones = response.data;
        if (this.model.ubicacionAlmacenId && !this.ubicaciones.some(x => x.id === this.model.ubicacionAlmacenId)) this.model.ubicacionAlmacenId = null;
      },
      error: () => { this.ubicaciones = []; if (limpiarSiFalla) this.model.ubicacionAlmacenId = null; this.error = 'No se pudieron cargar las ubicaciones del almacén.'; }
    });
  }
}
