import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { CostoEnvioService } from '../../services/costo-envio.service';
import { CostoEnvio, GuardarCostoEnvio } from '../../core/models/costo-envio.model';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-costos-envio',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatButtonModule, MatCheckboxModule,
    MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule, MatTableModule
  ],
  templateUrl: './costos-envio.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './costos-envio.component.scss'
})
export class CostosEnvioComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly items = signal<CostoEnvio[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly editandoId = signal<number | null>(null);
  readonly columnas = ['nombre', 'cobertura', 'monto', 'vigencia', 'estado', 'predeterminado', 'acciones'];

  form = this.fb.group({
    nombre: ['', [Validators.required, Validators.maxLength(150)]],
    descripcion: ['', Validators.maxLength(500)],
    departamento: ['', Validators.maxLength(120)],
    ciudad: ['', Validators.maxLength(120)],
    zona: ['', Validators.maxLength(150)],
    modalidad: ['', Validators.maxLength(80)],
    monto: [80, [Validators.required, Validators.min(0)]],
    vigenteDesde: [''],
    vigenteHasta: [''],
    prioridad: [1, [Validators.required, Validators.min(0)]],
    esPredeterminado: [true],
    activo: [true]
  });

  constructor(
    private readonly service: CostoEnvioService,
    private readonly alerts: AppAlertService
  ) {}

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.service.getAll().subscribe({
      next: (res) => {
        this.items.set(res.data);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'No se pudieron cargar los costos de envío.');
        this.loading.set(false);
      }
    });
  }

  editar(item: CostoEnvio): void {
    this.editandoId.set(item.id);
    this.form.reset({
      nombre: item.nombre,
      descripcion: item.descripcion ?? '',
      departamento: item.departamento ?? '',
      ciudad: item.ciudad ?? '',
      zona: item.zona ?? '',
      modalidad: item.modalidad ?? '',
      monto: item.monto,
      vigenteDesde: this.fechaInput(item.vigenteDesde),
      vigenteHasta: this.fechaInput(item.vigenteHasta),
      prioridad: item.prioridad,
      esPredeterminado: item.esPredeterminado,
      activo: item.activo
    });
  }

  cancelar(): void {
    this.editandoId.set(null);
    this.form.reset({
      nombre: '', descripcion: '', departamento: '', ciudad: '', zona: '', modalidad: '', monto: 80, vigenteDesde: '', vigenteHasta: '',
      prioridad: 1, esPredeterminado: false, activo: true
    });
  }

  guardar(): void {
    if (this.form.invalid || this.saving()) return;
    this.saving.set(true);
    this.error.set(null);
    const raw = this.form.getRawValue();
    const value: GuardarCostoEnvio = {
      nombre: raw.nombre!.trim(),
      descripcion: raw.descripcion?.trim() || undefined,
      departamento: raw.departamento?.trim() || undefined,
      ciudad: raw.ciudad?.trim() || undefined,
      zona: raw.zona?.trim() || undefined,
      modalidad: raw.modalidad?.trim() || undefined,
      monto: Number(raw.monto),
      vigenteDesde: raw.vigenteDesde ? new Date(raw.vigenteDesde).toISOString() : null,
      vigenteHasta: raw.vigenteHasta ? new Date(raw.vigenteHasta).toISOString() : null,
      prioridad: Number(raw.prioridad),
      esPredeterminado: raw.esPredeterminado === true,
      activo: raw.activo === true
    };
    const id = this.editandoId();
    const request$ = id ? this.service.update(id, value) : this.service.create(value);
    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.cancelar();
        this.cargar();
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err.error?.message ?? 'No se pudo guardar el costo de envío.');
      }
    });
  }

  cambiarEstado(item: CostoEnvio): void {
    this.service.cambiarEstado(item.id, !item.activo).subscribe({
      next: () => this.cargar(),
      error: (err) => this.error.set(err.error?.message ?? 'No se pudo cambiar el estado.')
    });
  }

  async eliminar(item: CostoEnvio): Promise<void> {
    const confirmado = await this.alerts.confirmar({
      titulo: 'Eliminar costo de envío',
      mensaje: `Se eliminará lógicamente “${item.nombre}”.`,
      detalle: 'El historial de envíos se conservará.',
      tipo: 'peligro',
      confirmarTexto: 'Eliminar',
      cancelarTexto: 'Cancelar'
    });
    if (!confirmado) return;
    this.service.delete(item.id).subscribe({
      next: () => this.cargar(),
      error: (err) => this.error.set(err.error?.message ?? 'No se pudo eliminar el costo de envío.')
    });
  }

  cobertura(item: CostoEnvio): string {
    const ubicacion = [item.departamento, item.ciudad, item.zona].filter(Boolean).join(' · ') || 'Cobertura general';
    return item.modalidad ? `${ubicacion} · ${item.modalidad}` : ubicacion;
  }

  private fechaInput(fecha?: string): string {
    return fecha ? fecha.slice(0, 10) : '';
  }
}
