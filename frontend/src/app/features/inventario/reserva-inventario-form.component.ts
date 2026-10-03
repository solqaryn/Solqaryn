
import { Component, OnInit } from '@angular/core';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { finalize } from 'rxjs';
import { ExistenciaVariante } from '../../core/models/existencia-variante.model';
import { ExistenciaVarianteService } from '../../services/existencia-variante.service';
import { ReservaInventarioService } from '../../services/reserva-inventario.service';

@Component({
  selector: 'app-reserva-inventario-form',
  standalone: true,
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule],
  template: `
    <section class="page" aria-labelledby="reserva-form-title">
      <header><button mat-button type="button" (click)="volver()"><mat-icon>arrow_back</mat-icon>Reservas</button><p class="eyebrow">Inventario empresarial</p><h1 id="reserva-form-title">{{ editando ? 'Editar reserva' : 'Nueva reserva' }}</h1><p>Selecciona existencias físicas reales y cantidades que quedarán reservadas al activar el documento.</p></header>
      @if (loading) {
        <div class="state"><mat-spinner diameter="36"></mat-spinner><span>Cargando…</span></div>
      }
      @if (error) {
        <div class="error" role="alert">{{ error }}</div>
      }
      @if (!loading) {
        <form [formGroup]="form" (ngSubmit)="guardar()">
          <section class="card general">
            <mat-form-field appearance="outline"><mat-label>Venta (opcional)</mat-label><input matInput type="number" min="1" formControlName="ventaId" /></mat-form-field>
            <mat-form-field appearance="outline"><mat-label>Fecha de expiración</mat-label><input matInput type="datetime-local" [min]="fechaExpiracionMinima" formControlName="fechaExpiracion" /><mat-hint>Debe ser futura.</mat-hint></mat-form-field>
          </section>
          <section class="card" formArrayName="detalles">
            <div class="section-title"><div><h2>Detalle físico</h2><p>La existencia seleccionada define variante, almacén y ubicación sin pedir IDs manuales.</p></div><button mat-stroked-button type="button" (click)="agregarDetalle()"><mat-icon>add</mat-icon>Agregar línea</button></div>
            @for (grupo of detalles.controls; track grupo; let i = $index) {
              <div class="line" [formGroupName]="i">
                <mat-form-field appearance="outline" class="physical-select">
                  <mat-label>Existencia física</mat-label>
                  <mat-select formControlName="existenciaVarianteId" required (selectionChange)="onExistenciaChange(i)">
                    @for (existencia of existencias; track existencia) {
                      <mat-option [value]="existencia.id" [disabled]="existencia.stockDisponible <= 0">
                        {{ etiquetaExistencia(existencia) }}
                      </mat-option>
                    }
                  </mat-select>
                  @if (!existencias.length) {
                    <mat-hint>No hay existencias físicas disponibles.</mat-hint>
                  }
                </mat-form-field>
                <mat-form-field appearance="outline"><mat-label>Cantidad</mat-label><input matInput type="number" min="1" formControlName="cantidad" required />@if (existenciaSeleccionada(i); as existencia) {
                <mat-hint>Disponible: {{ existencia.stockDisponible }}</mat-hint>
              }</mat-form-field>
              <button mat-icon-button color="warn" type="button" aria-label="Quitar línea" [disabled]="detalles.length === 1" (click)="quitarDetalle(i)"><mat-icon>delete_outline</mat-icon></button>
            </div>
          }
        </section>
        <footer><button mat-button type="button" (click)="volver()">Cancelar</button><button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || guardando || !hayExistenciasReservables">@if (guardando) {
        <mat-spinner diameter="20"></mat-spinner>
        }@if (!guardando) {
        <span>Guardar reserva</span>
      }</button></footer>
    </form>
    }
    </section>
    `,
  styles: [`.page{padding:24px;display:grid;gap:18px;max-width:1120px;margin:0 auto}.eyebrow{margin:12px 0 0;text-transform:uppercase;letter-spacing:.08em;font-size:.72rem;font-weight:700;color:var(--primary,#3f51b5)}h1{margin:4px 0}header p{color:#667085}.state{min-height:160px;display:flex;align-items:center;justify-content:center;gap:12px}.error{padding:12px;border-radius:10px;background:#fef3f2;color:#b42318}.card{border:1px solid #e4e7ec;border-radius:12px;padding:18px;background:#fff;display:grid;gap:14px}.general{grid-template-columns:1fr 1fr}.section-title{display:flex;justify-content:space-between;gap:12px;align-items:flex-start}.section-title h2{margin:0;font-size:1.1rem}.section-title p{margin:4px 0 0;color:#667085}.line{display:grid;grid-template-columns:minmax(0,3fr) minmax(150px,1fr) auto;gap:10px;align-items:start;border-top:1px solid #eaecf0;padding-top:14px}.physical-select{min-width:0}footer{display:flex;justify-content:flex-end;gap:8px;margin-top:16px}footer button mat-spinner{display:inline-block}@media(max-width:850px){.page{padding:16px}.general,.line{grid-template-columns:1fr 1fr}.line>button{justify-self:start}}@media(max-width:520px){.general,.line{grid-template-columns:1fr}.section-title{flex-direction:column}}`]
})
export class ReservaInventarioFormComponent implements OnInit {
  form: FormGroup;
  existencias: ExistenciaVariante[] = [];
  editando = false;
  loading = true;
  guardando = false;
  error = '';
  readonly fechaExpiracionMinima = this.toLocalInput(new Date().toISOString()) ?? '';
  private id = 0;

  constructor(
    private readonly fb: FormBuilder,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly service: ReservaInventarioService,
    private readonly existenciaService: ExistenciaVarianteService
  ) {
    this.form = this.fb.group({ ventaId: [null as number | null, [Validators.min(1)]], fechaExpiracion: [null as string | null], detalles: this.fb.array<FormGroup>([]) });
    this.agregarDetalle();
  }

  get detalles(): FormArray<FormGroup> { return this.form.controls['detalles'] as FormArray<FormGroup>; }
  get hayExistenciasReservables(): boolean { return this.existencias.some(x => x.stockDisponible > 0); }

  ngOnInit(): void {
    const raw = this.route.snapshot.paramMap.get('id');
    if (raw) {
      this.id = Number(raw);
      if (!Number.isInteger(this.id) || this.id <= 0) { this.error = 'Identificador de reserva inválido.'; this.loading = false; return; }
      this.editando = true;
    }
    this.cargarExistencias();
  }

  agregarDetalle(): void {
    this.detalles.push(this.fb.group({
      existenciaVarianteId: [null as number | null, [Validators.required]],
      productoVarianteId: [null as number | null, [Validators.required, Validators.min(1)]],
      almacenId: [null as number | null, [Validators.required, Validators.min(1)]],
      ubicacionAlmacenId: [null as number | null],
      cantidad: [1, [Validators.required, Validators.min(1)]]
    }));
  }

  quitarDetalle(index: number): void { if (this.detalles.length > 1) this.detalles.removeAt(index); }

  onExistenciaChange(index: number): void {
    const grupo = this.detalles.at(index);
    const existencia = this.existencias.find(x => x.id === Number(grupo.get('existenciaVarianteId')?.value));
    if (!existencia) {
      grupo.patchValue({ productoVarianteId: null, almacenId: null, ubicacionAlmacenId: null });
      return;
    }
    grupo.patchValue({ productoVarianteId: existencia.productoVarianteId, almacenId: existencia.almacenId, ubicacionAlmacenId: existencia.ubicacionAlmacenId ?? null });
  }

  existenciaSeleccionada(index: number): ExistenciaVariante | undefined {
    const id = Number(this.detalles.at(index).get('existenciaVarianteId')?.value ?? 0);
    return this.existencias.find(x => x.id === id);
  }

  etiquetaExistencia(existencia: ExistenciaVariante): string {
    const ubicacion = existencia.ubicacionCodigo ? ` / ${existencia.ubicacionCodigo}` : ' / raíz';
    const disponibilidad = existencia.stockDisponible > 0 ? `disponible ${existencia.stockDisponible}` : 'sin stock disponible';
    return `${existencia.productoNombre} · ${existencia.varianteSku} · ${existencia.almacenCodigo}${ubicacion} · ${disponibilidad}`;
  }

  cargar(): void {
    this.loading = true;
    this.error = '';
    this.service.getById(this.id).pipe(finalize(() => this.loading = false)).subscribe({
      next: r => {
        if (!r.success) { this.error = r.message || 'No se pudo cargar la reserva.'; return; }
        if (r.data.estado !== 'Borrador') { this.error = 'Sólo las reservas en Borrador pueden editarse.'; return; }
        this.detalles.clear();
        r.data.detalles.forEach(d => {
          const existencia = this.existencias.find(x => x.productoVarianteId === d.productoVarianteId && x.almacenId === d.almacenId && (x.ubicacionAlmacenId ?? null) === (d.ubicacionAlmacenId ?? null));
          this.detalles.push(this.fb.group({
            existenciaVarianteId: [existencia?.id ?? null, [Validators.required]],
            productoVarianteId: [d.productoVarianteId, [Validators.required, Validators.min(1)]],
            almacenId: [d.almacenId, [Validators.required, Validators.min(1)]],
            ubicacionAlmacenId: [d.ubicacionAlmacenId ?? null],
            cantidad: [d.cantidadReservada, [Validators.required, Validators.min(1)]]
          }));
        });
        if (!this.detalles.length) this.agregarDetalle();
        if (this.detalles.controls.some(x => !x.get('existenciaVarianteId')?.value)) this.error = 'Una o más existencias de la reserva ya no están disponibles en el catálogo operativo.';
        this.form.patchValue({ ventaId: r.data.ventaId ?? null, fechaExpiracion: this.toLocalInput(r.data.fechaExpiracion) });
      },
      error: () => this.error = 'No se pudo cargar la reserva.'
    });
  }

  guardar(): void {
    if (this.form.invalid || this.detalles.length === 0) { this.form.markAllAsTouched(); return; }
    const raw = this.form.getRawValue();
    const detalles = raw['detalles'].map((d: Record<string, unknown>) => ({ productoVarianteId: Number(d['productoVarianteId']), almacenId: Number(d['almacenId']), ubicacionAlmacenId: d['ubicacionAlmacenId'] ? Number(d['ubicacionAlmacenId']) : null, cantidad: Number(d['cantidad']) }));
    const claves = detalles.map((d: { productoVarianteId: number; almacenId: number; ubicacionAlmacenId: number | null }) => `${d.productoVarianteId}:${d.almacenId}:${d.ubicacionAlmacenId ?? 'root'}`);
    if (new Set(claves).size !== claves.length) { this.error = 'No puedes reservar dos veces la misma existencia física dentro del documento.'; return; }
    const sobreReserva = raw['detalles'].find((d: Record<string, unknown>) => {
      const existencia = this.existencias.find(x => x.id === Number(d['existenciaVarianteId']));
      return existencia && Number(d['cantidad']) > existencia.stockDisponible;
    });
    if (sobreReserva) { this.error = 'La cantidad solicitada supera el stock disponible de una de las existencias seleccionadas.'; return; }

    const fechaExpiracionRaw = raw['fechaExpiracion'] as string | null;
    if (fechaExpiracionRaw) {
      const fechaExpiracionDate = new Date(fechaExpiracionRaw);
      if (Number.isNaN(fechaExpiracionDate.getTime()) || fechaExpiracionDate.getTime() <= Date.now()) {
        this.error = 'La fecha de expiración debe ser futura.';
        return;
      }
    }
    const ventaIdRaw = raw['ventaId'] as number | null;
    const fechaExpiracion = fechaExpiracionRaw ? new Date(fechaExpiracionRaw).toISOString() : null;
    const request = this.editando ? this.service.update(this.id, { fechaExpiracion, detalles }) : this.service.create({ ventaId: ventaIdRaw ? Number(ventaIdRaw) : null, fechaExpiracion, detalles });
    this.guardando = true; this.error = '';
    request.pipe(finalize(() => this.guardando = false)).subscribe({ next: r => { if (!r.success) { this.error = r.message || 'No se pudo guardar la reserva.'; return; } void this.router.navigate(['/inventario/reservas', r.data.id]); }, error: () => this.error = 'No se pudo guardar la reserva.' });
  }

  volver(): void { void this.router.navigate(['/inventario/reservas']); }

  private cargarExistencias(page = 1, acumuladas: ExistenciaVariante[] = []): void {
    this.existenciaService.getPaged({ page, pageSize: 200, sortBy: 'productoNombre', sortDirection: 'asc' }).subscribe({
      next: response => {
        if (!response.success) { this.error = response.message || 'No se pudieron cargar las existencias físicas.'; this.loading = false; return; }
        const nuevas = [...acumuladas, ...response.data.items];
        if (page < response.data.totalPages && page < 50) {
          this.cargarExistencias(page + 1, nuevas);
          return;
        }
        this.existencias = nuevas;
        if (response.data.totalPages > 50) this.error = 'El catálogo físico supera el límite operativo de carga. Refina el inventario antes de crear la reserva.';
        if (this.editando) this.cargar();
        else this.loading = false;
      },
      error: () => { this.error = 'No se pudieron cargar las existencias físicas.'; this.loading = false; }
    });
  }

  private toLocalInput(value?: string | null): string | null { if (!value) return null; const date = new Date(value); const offset = date.getTimezoneOffset() * 60000; return new Date(date.getTime() - offset).toISOString().slice(0,16); }
}
