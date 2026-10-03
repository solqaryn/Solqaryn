import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, Validators, FormGroup } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ClienteService } from '../../services/cliente.service';
import { TipoClienteService } from '../../services/tipo-cliente.service';
import { TipoCliente } from '../../core/models/tipo-cliente.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { CreditoCliente, CreditoClienteService } from './credito-cliente.service';

@Component({
  selector: 'app-cliente-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatSelectModule],
  templateUrl: './cliente-form.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './cliente-form.component.scss'
})
export class ClienteFormComponent implements OnInit {
  readonly isEdit = signal(false);
  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly tiposClientes = signal<TipoCliente[]>([]);
  readonly credito = signal<CreditoCliente | null>(null);
  readonly creditoLoading = signal(false);
  readonly creditoSaving = signal(false);
  readonly puedeCrearCredito = signal(false);
  readonly puedeEditarCredito = signal(false);
  private clienteId: number | null = null;

  form!: FormGroup;
  creditoForm!: FormGroup;
  bloqueoForm!: FormGroup;
  excepcionForm!: FormGroup;

  constructor(
    private fb: FormBuilder,
    private clienteService: ClienteService,
    private tipoClienteService: TipoClienteService,
    private creditoService: CreditoClienteService,
    private permisosRuntime: PermisosRuntimeService,
    private snackBar: MatSnackBar,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      nombre: ['', Validators.required],
      telefono: [''],
      identidadORTN: [''],
      correo: [''],
      direccion: [''],
      tipoClienteId: [null]
    });
    this.creditoForm = this.fb.group({
      moneda: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(3)]],
      limiteCredito: [0, [Validators.required, Validators.min(0)]],
      diasCredito: [0, [Validators.required, Validators.min(0)]],
      umbralAlertaPorcentaje: [null, [Validators.min(0.0001), Validators.max(100)]]
    });
    this.bloqueoForm = this.fb.group({ motivo: ['', Validators.required] });
    this.excepcionForm = this.fb.group({ monto: [0, [Validators.required, Validators.min(0.01)]], vigenteHastaLocal: ['', Validators.required] });

    this.puedeCrearCredito.set(this.permisosRuntime.puede('Clientes', 'Crear'));
    this.puedeEditarCredito.set(this.permisosRuntime.puede('Clientes', 'Editar'));

    this.tipoClienteService.getActivos().subscribe({ next: (res) => this.tiposClientes.set(res.data) });

    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.isEdit.set(true);
      this.clienteId = Number(idParam);
      this.deshabilitarFormulariosCredito();
      this.clienteService.getById(this.clienteId).subscribe((res) => this.form.patchValue(res.data));
      this.cargarCredito();
    }
  }

  submit(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.errorMessage.set(null);
    const value = this.form.getRawValue() as any;
    const request$ = this.isEdit()
      ? this.clienteService.update(this.clienteId!, { ...value, activo: true })
      : this.clienteService.create(value);
    request$.subscribe({
      next: () => { this.saving.set(false); this.router.navigate(['/clientes']); },
      error: (err) => { this.saving.set(false); this.errorMessage.set(err.error?.message ?? 'No se pudo guardar el cliente.'); }
    });
  }

  cargarCredito(): void {
    if (!this.clienteId) return;
    this.creditoLoading.set(true);
    this.creditoService.getByCliente(this.clienteId).subscribe({
      next: (res) => {
        const actual = res.data?.[0] ?? null;
        this.credito.set(actual);
        if (actual) {
          this.creditoForm.patchValue({
            moneda: actual.moneda,
            limiteCredito: actual.limiteCredito,
            diasCredito: actual.diasCredito,
            umbralAlertaPorcentaje: actual.umbralAlertaPorcentaje
          });
        }
        this.aplicarPermisosCredito();
        this.creditoLoading.set(false);
      },
      error: () => {
        this.deshabilitarFormulariosCredito();
        this.creditoLoading.set(false);
        this.snackBar.open('No se pudo cargar la política de crédito.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  guardarCredito(): void {
    const puedeMutar = this.credito() ? this.puedeEditarCredito() : this.puedeCrearCredito();
    if (!puedeMutar || !this.clienteId || this.creditoForm.invalid || this.creditoSaving()) return;
    const value = this.creditoForm.getRawValue();
    this.creditoSaving.set(true);
    const op = this.credito()
      ? this.creditoService.actualizar(this.credito()!.id, value)
      : this.creditoService.crear(this.clienteId, value);
    op.subscribe({
      next: (res) => {
        this.credito.set(res.data);
        this.aplicarPermisosCredito();
        this.creditoSaving.set(false);
        this.snackBar.open('Política de crédito guardada.', 'Cerrar', { duration: 3000 });
      },
      error: (err) => { this.creditoSaving.set(false); this.snackBar.open(err.error?.message ?? 'No se pudo guardar la política de crédito.', 'Cerrar', { duration: 5000 }); }
    });
  }

  aplicarBloqueo(): void {
    const actual = this.credito();
    if (!this.puedeEditarCredito() || !actual || this.bloqueoForm.invalid) return;
    this.creditoService.bloquear(actual.id, String(this.bloqueoForm.value.motivo).trim()).subscribe({
      next: (res) => { this.credito.set(res.data); this.bloqueoForm.reset({ motivo: '' }); },
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo aplicar el bloqueo.', 'Cerrar', { duration: 5000 })
    });
  }

  liberarBloqueo(): void {
    const actual = this.credito();
    if (!this.puedeEditarCredito() || !actual) return;
    this.creditoService.liberarBloqueo(actual.id).subscribe({
      next: (res) => this.credito.set(res.data),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo liberar el bloqueo.', 'Cerrar', { duration: 5000 })
    });
  }

  autorizarExcepcion(): void {
    const actual = this.credito();
    if (!this.puedeEditarCredito() || !actual || this.excepcionForm.invalid) return;
    const value = this.excepcionForm.getRawValue();
    const vigenteHastaUtc = new Date(value.vigenteHastaLocal).toISOString();
    this.creditoService.autorizarExcepcion(actual.id, Number(value.monto), vigenteHastaUtc).subscribe({
      next: (res) => { this.credito.set(res.data); this.excepcionForm.reset({ monto: 0, vigenteHastaLocal: '' }); },
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo autorizar la excepción.', 'Cerrar', { duration: 5000 })
    });
  }

  revocarExcepcion(): void {
    const actual = this.credito();
    if (!this.puedeEditarCredito() || !actual) return;
    this.creditoService.revocarExcepcion(actual.id).subscribe({
      next: (res) => this.credito.set(res.data),
      error: (err) => this.snackBar.open(err.error?.message ?? 'No se pudo revocar la excepción.', 'Cerrar', { duration: 5000 })
    });
  }

  private aplicarPermisosCredito(): void {
    const puedeMutarPolitica = this.credito() ? this.puedeEditarCredito() : this.puedeCrearCredito();
    if (puedeMutarPolitica) {
      this.creditoForm.enable({ emitEvent: false });
    } else {
      this.creditoForm.disable({ emitEvent: false });
    }

    if (this.puedeEditarCredito()) {
      this.bloqueoForm.enable({ emitEvent: false });
      this.excepcionForm.enable({ emitEvent: false });
    } else {
      this.bloqueoForm.disable({ emitEvent: false });
      this.excepcionForm.disable({ emitEvent: false });
    }
  }

  private deshabilitarFormulariosCredito(): void {
    this.creditoForm.disable({ emitEvent: false });
    this.bloqueoForm.disable({ emitEvent: false });
    this.excepcionForm.disable({ emitEvent: false });
  }
}
