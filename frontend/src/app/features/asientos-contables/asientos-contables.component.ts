import { CommonModule } from '@angular/common';
import { Component, OnInit, OnDestroy, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { Subject, debounceTime, takeUntil, firstValueFrom } from 'rxjs';
import { AsientoContableService } from '../../services/asiento-contable.service';
import { CrearAsientoContableDto, AsientoContableDto } from '../../core/models/asiento-contable.model';
import { CuentaContableService } from '../../services/cuenta-contable.service';
import { CuentaContable } from '../../core/models/cuenta-contable.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { EventoContablePanelComponent } from './evento-contable-panel.component';

export function asientoBalanceValidator(control: AbstractControl): ValidationErrors | null {
  const detalles = control.get('detalles') as FormArray | null;
  if (!detalles || detalles.length < 2) return { sinDetalles: true };

  let totalDebe = 0;
  let totalHaber = 0;
  let hasMovement = false;
  for (const detalle of detalles.controls) {
    const debe = Number(detalle.get('debe')?.value || 0);
    const haber = Number(detalle.get('haber')?.value || 0);
    if (debe < 0 || haber < 0 || (debe > 0 && haber > 0)) return { montosInvalidos: true };
    if (debe > 0 || haber > 0) hasMovement = true;
    totalDebe += debe;
    totalHaber += haber;
  }

  if (!hasMovement || totalDebe <= 0 || totalHaber <= 0 || Math.abs(totalDebe - totalHaber) > 0.005) {
    return { descuadrado: true };
  }
  return null;
}

@Component({
  selector: 'app-asientos-contables',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule, MatFormFieldModule,
    MatInputModule, MatSelectModule, MatButtonModule, MatIconModule,
    MatProgressSpinnerModule, MatTableModule, MatSnackBarModule,
    EventoContablePanelComponent
  ],
  templateUrl: './asientos-contables.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './asientos-contables.component.scss'
})
export class AsientosContablesComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly permisosRuntime = inject(PermisosRuntimeService);
  private readonly asientoService = inject(AsientoContableService);
  private readonly cuentaService = inject(CuentaContableService);
  private readonly snackBar = inject(MatSnackBar);

  readonly asientos = signal<AsientoContableDto[]>([]);
  readonly loading = signal(true);
  readonly guardando = signal(false);
  readonly mostrarFormulario = signal(false);
  readonly asientoSeleccionado = signal<AsientoContableDto | null>(null);
  readonly cuentasContables = signal<CuentaContable[]>([]);
  readonly puedeCrearAsiento = signal(false);

  search = '';
  fechaDesde = '';
  fechaHasta = '';
  Math = Math;

  private readonly searchSubject = new Subject<void>();
  private readonly destroy$ = new Subject<void>();

  readonly asientoForm = this.fb.group({
    fecha: [''],
    concepto: ['', Validators.required],
    numero: [''],
    detalles: this.fb.array([])
  }, { validators: asientoBalanceValidator });

  get detallesFormArray(): FormArray {
    return this.asientoForm.get('detalles') as FormArray;
  }

  get totalDebe(): number {
    return this.detallesFormArray.controls.reduce((total, detalle) => total + Number(detalle.get('debe')?.value || 0), 0);
  }

  get totalHaber(): number {
    return this.detallesFormArray.controls.reduce((total, detalle) => total + Number(detalle.get('haber')?.value || 0), 0);
  }

  ngOnInit(): void {
    this.puedeCrearAsiento.set(this.permisosRuntime.puede('Finanzas', 'Crear'));
    this.searchSubject.pipe(debounceTime(300), takeUntil(this.destroy$)).subscribe(() => this.cargarAsientos());
    this.cargarAsientos();
    void this.cargarCuentas();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  cargarAsientos(): void {
    this.loading.set(true);
    const params: { desde?: string; hasta?: string; numero?: string; pagina?: number; tamano?: number } = {};
    if (this.search) params.numero = this.search;
    if (this.fechaDesde) params.desde = this.fechaDesde;
    if (this.fechaHasta) params.hasta = this.fechaHasta;

    this.asientoService.getAll(params).subscribe({
      next: (res) => {
        this.asientos.set(res.data?.items ?? []);
        this.loading.set(false);
      },
      error: () => {
        this.asientos.set([]);
        this.loading.set(false);
        this.snackBar.open('Error al cargar asientos', 'Cerrar', { duration: 3000 });
      }
    });
  }

  async cargarCuentas(): Promise<void> {
    try {
      const res = await firstValueFrom(this.cuentaService.getAll());
      const flatten = (accounts: CuentaContable[]): CuentaContable[] => {
        const result: CuentaContable[] = [];
        for (const account of accounts) {
          if (account.activa && account.aceptaMovimientos) result.push(account);
          if (account.subcuentas?.length) result.push(...flatten(account.subcuentas));
        }
        return result;
      };
      this.cuentasContables.set(flatten(res.data ?? []));
    } catch {
      this.cuentasContables.set([]);
      this.snackBar.open('Error al cargar cuentas contables', 'Cerrar', { duration: 3000 });
    }
  }

  onSearchChange(_value: string): void {
    this.searchSubject.next();
  }

  onFiltroChange(): void {
    this.searchSubject.next();
  }

  nuevoAsiento(): void {
    if (!this.puedeCrearAsiento()) return;
    this.asientoForm.reset({ concepto: '' });
    this.detallesFormArray.clear();
    this.agregarDetalle();
    this.agregarDetalle();
    this.asientoSeleccionado.set(null);
    this.mostrarFormulario.set(true);
  }

  verDetalles(asiento: AsientoContableDto): void {
    this.asientoSeleccionado.set(asiento);
    this.mostrarFormulario.set(true);
  }

  cancelar(): void {
    this.mostrarFormulario.set(false);
    this.asientoSeleccionado.set(null);
  }

  agregarDetalle(): void {
    this.detallesFormArray.push(this.fb.group({
      cuentaContableId: [null, Validators.required],
      debe: [0, [Validators.required, Validators.min(0)]],
      haber: [0, [Validators.required, Validators.min(0)]],
      referencia: ['']
    }));
  }

  removerDetalle(index: number): void {
    if (this.detallesFormArray.length > 2) this.detallesFormArray.removeAt(index);
  }

  guardarAsiento(): void {
    if (this.asientoForm.invalid || !this.puedeCrearAsiento() || this.guardando()) return;
    const val = this.asientoForm.getRawValue();
    const dto: CrearAsientoContableDto = {
      concepto: val.concepto ?? '',
      detalles: (val.detalles ?? []).map((detalle: any) => ({
        cuentaContableId: Number(detalle.cuentaContableId),
        debe: Number(detalle.debe || 0),
        haber: Number(detalle.haber || 0),
        referencia: detalle.referencia || undefined
      }))
    };
    if (val.fecha) dto.fecha = val.fecha;
    if (val.numero) dto.numero = val.numero;

    this.asientoForm.disable();
    this.guardando.set(true);
    this.asientoService.create(dto).subscribe({
      next: () => {
        this.snackBar.open('Asiento creado exitosamente', 'Cerrar', { duration: 3000 });
        this.asientoForm.enable();
        this.guardando.set(false);
        this.mostrarFormulario.set(false);
        this.cargarAsientos();
      },
      error: (err) => {
        this.asientoForm.enable();
        this.guardando.set(false);
        this.snackBar.open(err?.error?.detail || 'Error al crear asiento', 'Cerrar', { duration: 5000 });
      }
    });
  }
}
