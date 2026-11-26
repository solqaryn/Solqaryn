import { MatDialogModule } from '@angular/material/dialog';
import { crearEditorInstitucional } from '../../shared/modal-institucional/acciones-institucionales';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
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
  imports: [MatDialogModule, CommonModule, ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatSelectModule],
  templateUrl: './cliente-form.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './cliente-form.component.scss'
})
export class ClienteFormComponent implements OnInit {
  readonly editor = crearEditorInstitucional('Clientes');
  readonly loading = signal(false);
  cancelarModal(): void { if (!this.saving() && !this.creditoSaving()) this.editor.cerrar(false); }
  private activoOriginal = true;

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

    this.tipoClienteService.getActivos().pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({ next: (res) => this.tiposClientes.set(res.data) });

    const idParam = this.editor.ref ? this.editor.id : this.route.snapshot.paramMap.get('id');
    if (this.editor.ref && !this.editor.valido(idParam ? 'Editar' : 'Crear')) { this.editor.cerrar(false); return; }
    if (idParam) {
      this.isEdit.set(true);
      this.clienteId = Number(idParam);
      this.deshabilitarFormulariosCredito();
      this.loading.set(true);
      this.clienteService.getById(this.clienteId).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({ next: res => { this.activoOriginal = res.data.activo; this.form.patchValue(res.data); this.loading.set(false); }, error: () => { this.loading.set(false); this.form.disable(); this.errorMessage.set('No se pudo cargar el cliente.'); } });
      this.cargarCredito();
    }
  }

  async submit(): Promise<void> {
    if (this.form.invalid || this.form.disabled || this.loading() || this.saving()) return;
    this.saving.set(true);
    this.errorMessage.set(null);
    const value = this.form.getRawValue() as any;
    const accion = this.isEdit() ? 'Editar' : 'Crear';
    if (!await this.editor.confirmar(accion, { titulo: 'Confirmar ' + (this.isEdit() ? 'actualización' : 'creación'), mensaje: '¿Deseas guardar los datos del registro?', confirmarTexto: 'Guardar' })) { this.saving.set(false); return; }
    const request$ = this.isEdit()
      ? this.clienteService.update(this.clienteId!, { ...value, activo: this.activoOriginal })
      : this.clienteService.create(value);
    request$.pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: () => { this.saving.set(false); if (this.editor.ref) this.editor.cerrar(true); else void this.router.navigate(['/clientes']); },
      error: (err) => { this.saving.set(false); this.errorMessage.set(err.error?.message ?? 'No se pudo guardar el cliente.'); }
    });
  }

  cargarCredito(): void {
    if (!this.clienteId) return;
    this.creditoLoading.set(true);
    this.creditoService.getByCliente(this.clienteId).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
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

  async guardarCredito(): Promise<void> {
    const puedeMutar = this.credito() ? this.puedeEditarCredito() : this.puedeCrearCredito();
    if (!puedeMutar || !this.clienteId || this.creditoForm.invalid || this.creditoSaving()) return;
    const value = this.creditoForm.getRawValue();
    this.creditoSaving.set(true);
    if (!await this.editor.confirmar(this.credito() ? 'Editar' : 'Crear', { titulo: 'Confirmar política de crédito', mensaje: '¿Deseas guardar la política de crédito del cliente?', confirmarTexto: 'Guardar' })) { this.creditoSaving.set(false); return; }
    const op = this.credito()
      ? this.creditoService.actualizar(this.credito()!.id, value)
      : this.creditoService.crear(this.clienteId, value);
    op.pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: (res) => {
        this.credito.set(res.data);
        this.aplicarPermisosCredito();
        this.creditoSaving.set(false);
        this.snackBar.open('Política de crédito guardada.', 'Cerrar', { duration: 3000 });
      },
      error: (err) => { this.creditoSaving.set(false); this.snackBar.open(err.error?.message ?? 'No se pudo guardar la política de crédito.', 'Cerrar', { duration: 5000 }); }
    });
  }

  async aplicarBloqueo(): Promise<void> {
    const actual = this.credito();
    if (!this.puedeEditarCredito() || !actual || this.bloqueoForm.invalid) return;
    if (this.creditoSaving()) return;
    this.creditoSaving.set(true);
    if (!await this.editor.confirmar('Editar', { titulo: 'Aplicar bloqueo de crédito', mensaje: '¿Deseas realizar esta operación sobre el crédito del cliente?', confirmarTexto: 'Confirmar' })) { this.creditoSaving.set(false); return; }
    this.creditoService.bloquear(actual.id, String(this.bloqueoForm.value.motivo).trim()).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: (res) => { this.creditoSaving.set(false); this.credito.set(res.data); this.bloqueoForm.reset({ motivo: '' }); },
      error: (err) => { this.creditoSaving.set(false); this.snackBar.open(err.error?.message ?? 'No se pudo aplicar el bloqueo.', 'Cerrar', { duration: 5000 }); }
    });
  }

  async liberarBloqueo(): Promise<void> {
    const actual = this.credito();
    if (!this.puedeEditarCredito() || !actual) return;
    if (this.creditoSaving()) return;
    this.creditoSaving.set(true);
    if (!await this.editor.confirmar('Editar', { titulo: 'Liberar bloqueo de crédito', mensaje: '¿Deseas realizar esta operación sobre el crédito del cliente?', confirmarTexto: 'Confirmar' })) { this.creditoSaving.set(false); return; }
    this.creditoService.liberarBloqueo(actual.id).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: (res) => { this.creditoSaving.set(false); this.credito.set(res.data); },
      error: (err) => { this.creditoSaving.set(false); this.snackBar.open(err.error?.message ?? 'No se pudo liberar el bloqueo.', 'Cerrar', { duration: 5000 }); }
    });
  }

  async autorizarExcepcion(): Promise<void> {
    const actual = this.credito();
    if (!this.puedeEditarCredito() || !actual || this.excepcionForm.invalid) return;
    const value = this.excepcionForm.getRawValue();
    const vigenteHastaUtc = new Date(value.vigenteHastaLocal).toISOString();
    if (this.creditoSaving()) return;
    this.creditoSaving.set(true);
    if (!await this.editor.confirmar('Editar', { titulo: 'Autorizar excepción de crédito', mensaje: '¿Deseas realizar esta operación sobre el crédito del cliente?', confirmarTexto: 'Confirmar' })) { this.creditoSaving.set(false); return; }
    this.creditoService.autorizarExcepcion(actual.id, Number(value.monto), vigenteHastaUtc).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: (res) => { this.creditoSaving.set(false); this.credito.set(res.data); this.excepcionForm.reset({ monto: 0, vigenteHastaLocal: '' }); },
      error: (err) => { this.creditoSaving.set(false); this.snackBar.open(err.error?.message ?? 'No se pudo autorizar la excepción.', 'Cerrar', { duration: 5000 }); }
    });
  }

  async revocarExcepcion(): Promise<void> {
    const actual = this.credito();
    if (!this.puedeEditarCredito() || !actual) return;
    if (this.creditoSaving()) return;
    this.creditoSaving.set(true);
    if (!await this.editor.confirmar('Editar', { titulo: 'Revocar excepción de crédito', mensaje: '¿Deseas realizar esta operación sobre el crédito del cliente?', confirmarTexto: 'Confirmar' })) { this.creditoSaving.set(false); return; }
    this.creditoService.revocarExcepcion(actual.id).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: (res) => { this.creditoSaving.set(false); this.credito.set(res.data); },
      error: (err) => { this.creditoSaving.set(false); this.snackBar.open(err.error?.message ?? 'No se pudo revocar la excepción.', 'Cerrar', { duration: 5000 }); }
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
