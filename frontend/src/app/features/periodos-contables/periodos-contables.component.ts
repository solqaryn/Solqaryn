import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { EstadoPeriodoContable, PeriodoContable } from '../../core/models/periodo-contable.model';
import { PeriodoContableService } from '../../core/services/periodo-contable.service';

@Component({
  selector: 'app-periodos-contables',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatSnackBarModule,
    MatTableModule
  ],
  template: `
    <section class="page" aria-labelledby="periodos-title">
      <header class="page__header">
        <div>
          <h1 id="periodos-title">Períodos contables</h1>
          <p>Administra períodos abiertos y cerrados con control de fechas y permisos.</p>
        </div>
        @if (puedeCrear()) {
          <button mat-raised-button color="primary" type="button" (click)="mostrarAlta.set(!mostrarAlta())">
            <mat-icon>add</mat-icon> Nuevo período
          </button>
        }
      </header>
    
      <form class="filters" [formGroup]="filtros" (ngSubmit)="aplicarFiltros()" aria-label="Filtros de períodos contables">
        <mat-form-field appearance="outline">
          <mat-label>Desde</mat-label>
          <input matInput type="date" formControlName="fechaDesde">
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Hasta</mat-label>
          <input matInput type="date" formControlName="fechaHasta">
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Estado</mat-label>
          <mat-select formControlName="estado">
            <mat-option [value]="null">Todos</mat-option>
            <mat-option [value]="EstadoPeriodoContable.Abierto">Abierto</mat-option>
            <mat-option [value]="EstadoPeriodoContable.Cerrado">Cerrado</mat-option>
          </mat-select>
        </mat-form-field>
        <button mat-stroked-button type="submit">Aplicar</button>
      </form>
    
      @if (mostrarAlta()) {
        <form class="create" [formGroup]="alta" (ngSubmit)="crear()" aria-label="Crear período contable">
          <mat-form-field appearance="outline">
            <mat-label>Fecha inicial</mat-label>
            <input matInput type="date" formControlName="fechaInicio">
          </mat-form-field>
          <mat-form-field appearance="outline">
            <mat-label>Fecha final</mat-label>
            <input matInput type="date" formControlName="fechaFin">
          </mat-form-field>
          <button mat-raised-button color="primary" type="submit" [disabled]="alta.invalid || guardando()">
            @if (guardando()) {
              <mat-spinner diameter="18"></mat-spinner>
            }
            Guardar
          </button>
        </form>
      }
    
      @if (loading()) {
        <div class="loading" role="status" aria-live="polite">
          <mat-spinner diameter="40"></mat-spinner><span>Cargando períodos…</span>
        </div>
      }
    
      @if (!loading()) {
        @if (periodos().length) {
          <div class="table-wrap">
            <table mat-table [dataSource]="periodos()">
              <ng-container matColumnDef="rango">
                <th mat-header-cell *matHeaderCellDef>Rango</th>
                <td mat-cell *matCellDef="let p">{{ p.fechaInicio | date:'shortDate' }} — {{ p.fechaFin | date:'shortDate' }}</td>
              </ng-container>
              <ng-container matColumnDef="estado">
                <th mat-header-cell *matHeaderCellDef>Estado</th>
                <td mat-cell *matCellDef="let p">{{ estadoTexto(p.estado) }}</td>
              </ng-container>
              <ng-container matColumnDef="cierre">
                <th mat-header-cell *matHeaderCellDef>Cierre UTC</th>
                <td mat-cell *matCellDef="let p">{{ p.cerradoEnUtc ? (p.cerradoEnUtc | date:'short') : '—' }}</td>
              </ng-container>
              <ng-container matColumnDef="acciones">
                <th mat-header-cell *matHeaderCellDef>Acciones</th>
                <td mat-cell *matCellDef="let p">
                  @if (puedeCerrar()) {
                    <button mat-stroked-button color="warn" type="button" (click)="cerrar(p)"
                      [disabled]="p.estado === EstadoPeriodoContable.Cerrado || cerrandoId() === p.id"
                    >Cerrar</button>
                  }
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="columnas"></tr>
              <tr mat-row *matRowDef="let row; columns: columnas"></tr>
            </table>
          </div>
        } @else {
          <div class="empty" role="status">No hay períodos contables que coincidan con los filtros.</div>
        }
        @if (totalPages() > 1) {
          <nav class="pager" aria-label="Paginación">
            <button mat-button type="button" (click)="cambiarPagina(-1)" [disabled]="page() <= 1">Anterior</button>
            <span>Página {{ page() }} de {{ totalPages() }}</span>
            <button mat-button type="button" (click)="cambiarPagina(1)" [disabled]="page() >= totalPages()">Siguiente</button>
          </nav>
        }
      }
    </section>
    `,
  changeDetection: ChangeDetectionStrategy.Eager,
  styles: [`
    .page { padding: 24px; max-width: 1200px; margin: 0 auto; }
    .page__header, .filters, .create, .pager, .loading { display: flex; gap: 16px; align-items: center; flex-wrap: wrap; }
    .page__header { justify-content: space-between; margin-bottom: 20px; }
    .page__header h1 { margin: 0; }
    .page__header p { margin: 4px 0 0; }
    .filters, .create { margin-bottom: 16px; }
    .table-wrap { overflow-x: auto; }
    table { width: 100%; }
    .loading, .empty { justify-content: center; padding: 32px; text-align: center; }
    .pager { justify-content: flex-end; padding-top: 12px; }
    @media (max-width: 640px) { .page { padding: 16px; } mat-form-field { width: 100%; } .filters button, .create button { width: 100%; } }
  `]
})
export class PeriodosContablesComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(PeriodoContableService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly snack = inject(MatSnackBar);

  readonly EstadoPeriodoContable = EstadoPeriodoContable;
  readonly periodos = signal<PeriodoContable[]>([]);
  readonly loading = signal(false);
  readonly guardando = signal(false);
  readonly cerrandoId = signal<number | null>(null);
  readonly mostrarAlta = signal(false);
  readonly page = signal(1);
  readonly totalPages = signal(1);
  readonly pageSize = 20;
  readonly columnas = ['rango', 'estado', 'cierre', 'acciones'];

  readonly puedeCrear = signal(false);
  readonly puedeCerrar = signal(false);

  readonly filtros = this.fb.group({
    fechaDesde: [''],
    fechaHasta: [''],
    estado: [null as EstadoPeriodoContable | null]
  });

  readonly alta = this.fb.group({
    fechaInicio: ['', Validators.required],
    fechaFin: ['', Validators.required]
  });

  ngOnInit(): void {
    this.puedeCrear.set(this.permisos.puede('Configuracion', 'Crear'));
    this.puedeCerrar.set(this.permisos.puede('Configuracion', 'Cerrar'));
    this.cargar();
  }

  aplicarFiltros(): void {
    this.page.set(1);
    this.cargar();
  }

  cambiarPagina(delta: number): void {
    const siguiente = this.page() + delta;
    if (siguiente < 1 || siguiente > this.totalPages()) return;
    this.page.set(siguiente);
    this.cargar();
  }

  cargar(): void {
    const value = this.filtros.getRawValue();
    this.loading.set(true);
    this.service.getPaged({
      page: this.page(),
      pageSize: this.pageSize,
      fechaDesde: value.fechaDesde || undefined,
      fechaHasta: value.fechaHasta || undefined,
      estado: value.estado ?? undefined
    }).subscribe({
      next: response => {
        this.periodos.set(response.data?.items ?? []);
        this.totalPages.set(Math.max(response.data?.totalPages ?? 1, 1));
        this.loading.set(false);
      },
      error: () => {
        this.periodos.set([]);
        this.loading.set(false);
        this.snack.open('No se pudieron cargar los períodos contables.', 'Cerrar', { duration: 4000 });
      }
    });
  }

  crear(): void {
    if (this.alta.invalid || this.guardando() || !this.puedeCrear()) return;
    const value = this.alta.getRawValue();
    if (!value.fechaInicio || !value.fechaFin) return;
    if (value.fechaInicio > value.fechaFin) {
      this.snack.open('La fecha inicial no puede ser posterior a la fecha final.', 'Cerrar', { duration: 4000 });
      return;
    }
    this.guardando.set(true);
    this.service.create({ fechaInicio: value.fechaInicio, fechaFin: value.fechaFin }).subscribe({
      next: () => {
        this.guardando.set(false);
        this.mostrarAlta.set(false);
        this.alta.reset();
        this.page.set(1);
        this.snack.open('Período contable creado.', 'Cerrar', { duration: 3000 });
        this.cargar();
      },
      error: error => {
        this.guardando.set(false);
        this.snack.open(error?.error?.message || 'No se pudo crear el período contable.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  cerrar(periodo: PeriodoContable): void {
    if (!this.puedeCerrar() || periodo.estado === EstadoPeriodoContable.Cerrado || this.cerrandoId() !== null) return;
    this.cerrandoId.set(periodo.id);
    this.service.cerrar(periodo.id).subscribe({
      next: () => {
        this.cerrandoId.set(null);
        this.snack.open('Período contable cerrado.', 'Cerrar', { duration: 3000 });
        this.cargar();
      },
      error: error => {
        this.cerrandoId.set(null);
        this.snack.open(error?.error?.message || 'No se pudo cerrar el período contable.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  estadoTexto(estado: EstadoPeriodoContable): string {
    return estado === EstadoPeriodoContable.Cerrado ? 'Cerrado' : 'Abierto';
  }
}
