
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, effect, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormBuilder, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute } from '@angular/router';
import { Subject, debounceTime, finalize } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import {
  CreateCuentaBancariaDto,
  CuentaBancaria,
  CuentaBancariaQueryFilter,
  EstadoCuentaBancaria,
  UpdateCuentaBancariaDto
} from '../../core/models/cuenta-bancaria';
import { ListNavigationStateService } from '../../core/navigation/list-navigation-state.service';
import { CuentaBancariaService } from '../../core/services/cuenta-bancaria.service';

interface CuentasBancariasNavigationState {
  search: string;
  estadoFilter: EstadoCuentaBancaria | null;
}

interface ImportacionMovimientoDto {
  fechaOperacion: string;
  monto: number;
  referenciaExterna: string;
  descripcion: string;
  identificadorExternoTransaccion: string;
}

interface ImportacionEstadoCuentaDto {
  cuentaBancariaId: number;
  idempotencyKey: string;
  movimientos: ImportacionMovimientoDto[];
}

interface MatchDto {
  movimientoInternoId: number;
  identificadorExternoTransaccion: string;
}

interface RegistrarMatchesDto {
  cuentaBancariaId: number;
  idempotencyKey: string;
  matches: MatchDto[];
}

@Component({
  selector: 'app-cuentas-bancarias',
  standalone: true,
  imports: [
    FormsModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatTableModule,
    MatProgressSpinnerModule
],
  templateUrl: './cuentas-bancarias.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrls: ['./cuentas-bancarias.component.scss']
})
export class CuentasBancariasComponent implements OnInit {
  private readonly fb = inject(FormBuilder).nonNullable;
  private readonly cuentaService = inject(CuentaBancariaService);
  private readonly permisos = inject(PermisosRuntimeService);
  private readonly route = inject(ActivatedRoute);
  private readonly navigationState = inject(ListNavigationStateService);
  private readonly searchSubject = new Subject<string>();
  private readonly http = inject(HttpClient, { optional: true });

  readonly cuentas = signal<CuentaBancaria[]>([]);
  readonly mostrarFormulario = signal(false);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly puedeCrear = signal(false);
  readonly puedeImportar = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeActivar = signal(false);
  readonly puedeDesactivar = signal(false);
  readonly cuentaEnEdicion = signal<CuentaBancaria | null>(null);
  readonly columnasMostradas = ['nombre', 'numeroCuenta', 'moneda', 'estado', 'acciones'];
  readonly errorMessage = signal<string | null>(null);
  readonly reconLoading = signal(false);
  readonly reconSubmitting = signal(false);
  readonly reconError = signal<string | null>(null);
  readonly reconSuccess = signal(false);
  readonly reconEmpty = signal(false);
  readonly reconSelectedAccount = signal<CuentaBancaria | null>(null);
  readonly reconMovementRows = signal<ImportacionMovimientoDto[]>([]);
  readonly reconMatchRows = signal<MatchDto[]>([]);

  search = '';
  estadoFilter: EstadoCuentaBancaria | null = null;

  private readonly navigationDefaults: CuentasBancariasNavigationState = {
    search: '',
    estadoFilter: null
  };

  readonly formulario = this.fb.group({
    bancoId: [0, [Validators.required, Validators.min(1)]],
    nombre: ['', Validators.required],
    numeroCuenta: ['', Validators.required],
    moneda: ['HNL', Validators.required],
    saldoInicial: [0, [Validators.required, Validators.min(0)]]
  });

  constructor() {
    this.searchSubject.pipe(debounceTime(250)).subscribe(() => this.aplicarFiltros());
    effect(() => {
      if (!this.mostrarFormulario()) {
        this.cuentaEnEdicion.set(null);
        this.formulario.reset({ moneda: 'HNL', saldoInicial: 0, bancoId: 0, nombre: '', numeroCuenta: '' });
        this.errorMessage.set(null);
      }
    });
  }

  ngOnInit(): void {
    this.puedeCrear.set(this.permisos.puede('Finanzas', 'Crear'));
    this.puedeImportar.set(this.permisos.puede('Finanzas', 'Importar'));
    this.puedeEditar.set(this.permisos.puede('Finanzas', 'Editar'));
    this.puedeActivar.set(this.permisos.puede('Finanzas', 'Activar'));
    this.puedeDesactivar.set(this.permisos.puede('Finanzas', 'Desactivar'));

    const state = this.navigationState.restore<CuentasBancariasNavigationState>(
      'cuentas-bancarias',
      this.route,
      this.navigationDefaults
    );
    this.search = state.search;
    this.estadoFilter = state.estadoFilter;

    this.cargarCuentas();
  }

  cargarCuentas(): void {
    this.errorMessage.set(null);
    this.loading.set(true);
    const filtros: CuentaBancariaQueryFilter = {
      page: 1,
      pageSize: 50,
      searchTerm: this.search || undefined,
      estado: this.estadoFilter ?? undefined
    };

    this.cuentaService.getAll(filtros).subscribe({
      next: (res) => {
        this.cuentas.set(res.items);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(this.extractErrorMessage(err, 'Ocurrió un error al cargar las cuentas.'));
      }
    });
    this.persistirEstado();
  }

  onSearchChange(value: string): void {
    this.search = value;
    this.searchSubject.next(value);
  }

  aplicarFiltros(): void {
    this.cargarCuentas();
  }

  limpiarFiltros(): void {
    this.search = '';
    this.estadoFilter = null;
    this.navigationState.clear('cuentas-bancarias');
    this.aplicarFiltros();
  }

  guardarCuenta(): void {
    if (this.formulario.invalid) {
      this.formulario.markAllAsTouched();
      return;
    }
    if (this.submitting()) return;

    this.errorMessage.set(null);
    this.submitting.set(true);
    const formValue = this.formulario.getRawValue();
    const cuentaEditada = this.cuentaEnEdicion();

    if (cuentaEditada) {
      const dto: UpdateCuentaBancariaDto = { nombre: formValue.nombre };
      this.cuentaService.update(cuentaEditada.id, dto)
        .pipe(finalize(() => this.submitting.set(false)))
        .subscribe({
          next: () => {
            this.mostrarFormulario.set(false);
            this.cuentaEnEdicion.set(null);
            this.formulario.reset({ moneda: 'HNL', saldoInicial: 0, bancoId: 0, nombre: '', numeroCuenta: '' });
            this.cargarCuentas();
          },
          error: (err) => this.errorMessage.set(this.extractErrorMessage(err, 'Ocurrió un error al actualizar la cuenta.'))
        });
      return;
    }

    const dto: CreateCuentaBancariaDto = formValue;
    this.cuentaService.create(dto)
      .pipe(finalize(() => this.submitting.set(false)))
      .subscribe({
        next: () => {
          this.mostrarFormulario.set(false);
          this.formulario.reset({ moneda: 'HNL', saldoInicial: 0, bancoId: 0, nombre: '', numeroCuenta: '' });
          this.cargarCuentas();
        },
        error: (err) => this.errorMessage.set(this.extractErrorMessage(err, 'Ocurrió un error al crear la cuenta.'))
      });
  }

  editar(cuenta: CuentaBancaria): void {
    if (!this.puedeEditar()) return;
    this.cuentaEnEdicion.set(cuenta);
    this.formulario.patchValue({
      nombre: cuenta.nombre,
      numeroCuenta: cuenta.numeroCuenta,
      moneda: cuenta.moneda,
      saldoInicial: cuenta.saldoInicial,
      bancoId: cuenta.bancoId
    });
    this.mostrarFormulario.set(true);
  }

  activar(cuenta: CuentaBancaria): void {
    if (!this.puedeActivar()) return;
    this.errorMessage.set(null);
    this.cuentaService.activar(cuenta.id).subscribe({
      next: () => this.cargarCuentas(),
      error: (err) => this.errorMessage.set(this.extractErrorMessage(err, 'Ocurrió un error al activar la cuenta.'))
    });
  }

  desactivar(cuenta: CuentaBancaria): void {
    if (!this.puedeDesactivar()) return;
    this.errorMessage.set(null);
    this.cuentaService.desactivar(cuenta.id).subscribe({
      next: () => this.cargarCuentas(),
      error: (err) => this.errorMessage.set(this.extractErrorMessage(err, 'Ocurrió un error al desactivar la cuenta.'))
    });
  }

  seleccionarCuentaParaConciliacion(cuenta: CuentaBancaria): void {
    this.reconSelectedAccount.set(cuenta);
    this.reconError.set(null);
    this.reconSuccess.set(false);
    this.reconEmpty.set(false);
    this.reconMovementRows.set([]);
    this.reconMatchRows.set([]);
  }

  importarEstadoCuenta(idempotencyKey: string, movimientos: ImportacionMovimientoDto[]): void {
    if (!this.puedeImportar() || !this.http) return;
    const cuenta = this.reconSelectedAccount();
    if (!cuenta) return;

    this.reconSubmitting.set(true);
    this.reconError.set(null);
    this.reconSuccess.set(false);

    const dto: ImportacionEstadoCuentaDto = {
      cuentaBancariaId: cuenta.id,
      idempotencyKey,
      movimientos
    };

    this.http.post<void>(`${environment.apiUrl}/conciliaciones-bancarias/importaciones-estado-cuenta`, dto)
      .pipe(finalize(() => this.reconSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.reconSuccess.set(true);
          this.reconMovementRows.set(movimientos);
          this.reconEmpty.set(movimientos.length === 0);
        },
        error: (err) => this.reconError.set(this.extractErrorMessage(err, 'Error al importar estado de cuenta.'))
      });
  }

  registrarMatches(idempotencyKey: string, matches: MatchDto[]): void {
    if (!this.puedeCrear() || !this.http) return;
    const cuenta = this.reconSelectedAccount();
    if (!cuenta) return;

    this.reconSubmitting.set(true);
    this.reconError.set(null);
    this.reconSuccess.set(false);

    const dto: RegistrarMatchesDto = {
      cuentaBancariaId: cuenta.id,
      idempotencyKey,
      matches
    };

    this.http.post<void>(`${environment.apiUrl}/conciliaciones-bancarias/matches`, dto)
      .pipe(finalize(() => this.reconSubmitting.set(false)))
      .subscribe({
        next: () => {
          this.reconSuccess.set(true);
          this.reconMatchRows.set(matches);
          this.reconEmpty.set(matches.length === 0);
        },
        error: (err) => this.reconError.set(this.extractErrorMessage(err, 'Error al registrar matches.'))
      });
  }

  private extractErrorMessage(err: unknown, fallback: string): string {
    if (err instanceof HttpErrorResponse && typeof err.error?.detail === 'string' && err.error.detail.trim().length > 0) {
      return err.error.detail.trim();
    }
    return fallback;
  }

  private persistirEstado(): void {
    const state: CuentasBancariasNavigationState = {
      search: this.search,
      estadoFilter: this.estadoFilter
    };
    this.navigationState.persist('cuentas-bancarias', this.route, state, this.navigationDefaults);
  }
}
