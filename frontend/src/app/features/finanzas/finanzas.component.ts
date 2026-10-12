import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { debounceTime, Subject } from 'rxjs';
import { FinanzasService } from '../../services/finanzas.service';
import { FinanzasResumen, MovimientoFinanciero, RevisionFinanciera } from '../../core/models/finanzas.model';
import { AnularDialogComponent } from '../../shared/anular-dialog.component';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { ListNavigationStateService } from '../../core/navigation/list-navigation-state.service';
import { MetodoPagoSelectComponent } from '../../shared/metodo-pago-select/metodo-pago-select.component';

type FiltroTipoMovimiento = 'todos' | 'Ingreso' | 'Egreso' | 'Ajuste';
type OrdenMovimientoFinanciero = 'fecha' | 'concepto' | 'tipo' | 'monto';

@Component({
  selector: 'app-finanzas',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatSelectModule,
    MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatDialogModule, MetodoPagoSelectComponent
  ],
  templateUrl: './finanzas.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './finanzas.component.scss'
})
export class FinanzasComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly permisosRuntime = inject(PermisosRuntimeService);

  readonly resumen = signal<FinanzasResumen | null>(null);
  readonly movimientos = signal<MovimientoFinanciero[]>([]);
  readonly revisiones = signal<RevisionFinanciera[]>([]);
  readonly loading = signal(true);
  readonly mostrarFormMovimiento = signal(false);
  readonly mostrarFormRevision = signal(false);
  readonly esAdministrador = this.permisosRuntime.esAdministrador;
  readonly puedeCrearMovimiento = signal(false);
  readonly puedeAnularMovimiento = signal(false);

  search = '';
  filtroTipo: FiltroTipoMovimiento = 'todos';
  sortBy: OrdenMovimientoFinanciero = 'fecha';
  sortDirection: 'asc' | 'desc' = 'desc';

  private movimientosOrigen: MovimientoFinanciero[] = [];
  private readonly navigationDefaults = { search: '', filtroTipo: 'todos', sortBy: 'fecha', sortDirection: 'desc' };
  private readonly searchSubject = new Subject<string>();

  readonly movimientoForm = this.fb.group({
    tipo: ['Egreso', Validators.required],
    categoria: ['GastoOperativo', Validators.required],
    concepto: ['', Validators.required],
    descripcion: [''],
    monto: [0, [Validators.required, Validators.min(0.01)]],
    metodoPago: ['Efectivo', Validators.required]
  });

  readonly revisionForm = this.fb.group({
    fechaDesde: ['', Validators.required],
    fechaHasta: ['', Validators.required],
    estadoRevision: ['Revisado', Validators.required],
    observaciones: ['']
  });

  constructor(
    private finanzasService: FinanzasService,
    private dialog: MatDialog,
    private route: ActivatedRoute,
    private navigationState: ListNavigationStateService
  ) {
    this.searchSubject.pipe(debounceTime(250)).subscribe(() => this.aplicarFiltrosMovimientos());
  }

  ngOnInit(): void {
    const state = this.navigationState.restore('finanzas', this.route, this.navigationDefaults);
    this.search = state.search;
    this.filtroTipo = ['todos', 'Ingreso', 'Egreso', 'Ajuste'].includes(state.filtroTipo)
      ? state.filtroTipo as FiltroTipoMovimiento
      : 'todos';
    this.sortBy = ['fecha', 'concepto', 'tipo', 'monto'].includes(state.sortBy)
      ? state.sortBy as OrdenMovimientoFinanciero
      : 'fecha';
    this.sortDirection = state.sortDirection === 'asc' ? 'asc' : 'desc';

    this.puedeCrearMovimiento.set(this.permisosRuntime.puede('Finanzas', 'Crear'));
    this.puedeAnularMovimiento.set(this.permisosRuntime.puede('Finanzas', 'Anular'));
    this.persistirEstado();
    this.cargarTodo();
  }

  cargarTodo(): void {
    this.loading.set(true);

    this.finanzasService.getResumen().subscribe({
      next: (res) => {
        this.resumen.set(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });

    this.finanzasService.getMovimientos().subscribe({
      next: (res) => {
        this.movimientosOrigen = res.data;
        this.aplicarFiltrosMovimientos();
      },
      error: () => {
        this.movimientosOrigen = [];
        this.aplicarFiltrosMovimientos();
      }
    });

    if (this.esAdministrador()) {
      this.finanzasService.getRevisiones().subscribe({
        next: (res) => this.revisiones.set(res.data),
        error: () => this.revisiones.set([])
      });
    } else {
      this.revisiones.set([]);
      this.mostrarFormRevision.set(false);
    }
  }

  onSearchChange(value: string): void {
    this.search = value;
    this.searchSubject.next(value);
  }

  aplicarFiltrosMovimientos(): void {
    const term = this.search.trim().toLowerCase();
    let data = this.movimientosOrigen.filter((m) => {
      const coincideTipo = this.filtroTipo === 'todos' || m.tipo === this.filtroTipo;
      if (!coincideTipo) return false;
      if (!term) return true;
      return [m.concepto, m.categoria, m.descripcion, m.metodoPago, m.moduloOrigen, m.creadoPorNombreUsuario]
        .some((value) => value?.toLowerCase().includes(term));
    });

    const direction = this.sortDirection === 'asc' ? 1 : -1;
    data = data.sort((a, b) => {
      if (this.sortBy === 'concepto') return a.concepto.localeCompare(b.concepto, 'es', { sensitivity: 'base' }) * direction;
      if (this.sortBy === 'tipo') return a.tipo.localeCompare(b.tipo, 'es', { sensitivity: 'base' }) * direction;
      if (this.sortBy === 'monto') return (a.monto - b.monto) * direction;
      return (Date.parse(a.fecha) - Date.parse(b.fecha)) * direction;
    });

    this.movimientos.set(data);
    this.persistirEstado();
  }

  limpiarFiltrosMovimientos(): void {
    this.navigationState.clear('finanzas');
    this.search = '';
    this.filtroTipo = 'todos';
    this.sortBy = 'fecha';
    this.sortDirection = 'desc';
    this.aplicarFiltrosMovimientos();
  }

  ordenarMovimientos(campo: OrdenMovimientoFinanciero): void {
    if (this.sortBy === campo) this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    else { this.sortBy = campo; this.sortDirection = campo === 'fecha' ? 'desc' : 'asc'; }
    this.aplicarFiltrosMovimientos();
  }

  onTipoMovimientoChange(tipo: string): void {
    if (tipo !== 'Egreso' && this.movimientoForm.controls.categoria.value === 'GastoOperativo') {
      this.movimientoForm.controls.categoria.setValue('Otro');
    }
  }

  onCategoriaMovimientoChange(categoria: string): void {
    if (categoria === 'GastoOperativo' && this.movimientoForm.controls.tipo.value !== 'Egreso') {
      this.movimientoForm.controls.tipo.setValue('Egreso');
    }
  }

  registrarMovimiento(): void {
    if (!this.puedeCrearMovimiento() || this.movimientoForm.invalid) return;

    this.finanzasService.registrarManual(this.movimientoForm.getRawValue() as any).subscribe(() => {
      this.movimientoForm.reset({ tipo: 'Egreso', categoria: 'GastoOperativo', metodoPago: 'Efectivo' });
      this.mostrarFormMovimiento.set(false);
      this.cargarTodo();
    });
  }

  anularMovimiento(m: MovimientoFinanciero): void {
    if (!this.puedeAnularMovimiento()) return;

    const ref = this.dialog.open(AnularDialogComponent, {
      data: { title: 'Anular movimiento', message: `¿Anular "${m.concepto}"?` }
    });
    ref.afterClosed().subscribe((motivo: string | undefined) => {
      if (!motivo) return;
      this.finanzasService.anularMovimiento(m.id, motivo).subscribe(() => this.cargarTodo());
    });
  }

  registrarRevision(): void {
    if (!this.esAdministrador() || !this.puedeCrearMovimiento() || this.revisionForm.invalid) return;

    this.finanzasService.registrarRevision(this.revisionForm.getRawValue() as any).subscribe(() => {
      this.revisionForm.reset({ estadoRevision: 'Revisado' });
      this.mostrarFormRevision.set(false);
      this.cargarTodo();
    });
  }

  private persistirEstado(): void {
    this.navigationState.persist('finanzas', this.route, {
      search: this.search,
      filtroTipo: this.filtroTipo,
      sortBy: this.sortBy,
      sortDirection: this.sortDirection
    }, this.navigationDefaults);
  }
}
