import { CommonModule } from '@angular/common';
import { Component, Input, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { ProductoVariante } from '../../core/models/producto.model';
import { LoteInventario, SerieInventario } from '../../core/models/trazabilidad-inventario.model';
import { TrazabilidadInventarioService } from '../../services/trazabilidad-inventario.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-producto-variante-trazabilidad',
  standalone: true,
  imports: [
    CommonModule, ReactiveFormsModule, MatButtonModule, MatCheckboxModule, MatFormFieldModule,
    MatIconModule, MatInputModule, MatProgressSpinnerModule, MatSelectModule, MatTableModule
  ],
  templateUrl: './producto-variante-trazabilidad.component.html',
  styleUrl: './producto-variante-trazabilidad.component.scss'
})
export class ProductoVarianteTrazabilidadComponent {
  private readonly fb = inject(FormBuilder);
  private readonly service = inject(TrazabilidadInventarioService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly alerts = inject(AppAlertService);

  @Input({ required: true }) variantes: ProductoVariante[] = [];

  readonly varianteId = signal<number | null>(null);
  readonly loading = signal(false);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly lotes = signal<LoteInventario[]>([]);
  readonly series = signal<SerieInventario[]>([]);
  readonly loteColumns = ['codigo', 'fabricacion', 'vencimiento', 'estado', 'acciones'];
  readonly serieColumns = ['numero', 'lote', 'estado', 'acciones'];

  readonly configuracionForm = this.fb.group({
    controlaLote: [false],
    controlaNumeroSerie: [false],
    controlaFechaVencimiento: [false],
    diasAlertaVencimiento: [null as number | null, [Validators.min(1), Validators.max(3650)]]
  });

  readonly loteForm = this.fb.group({
    codigo: ['', [Validators.required, Validators.maxLength(100)]],
    fechaFabricacion: [''],
    fechaVencimiento: ['']
  });

  readonly serieForm = this.fb.group({
    numeroSerie: ['', [Validators.required, Validators.maxLength(160)]],
    loteInventarioId: [null as number | null]
  });

  seleccionarVariante(value: number | null): void {
    this.varianteId.set(value);
    this.error.set(null);
    this.lotes.set([]);
    this.series.set([]);
    if (!value) return;
    this.cargar(value);
  }

  cargar(productoVarianteId = this.varianteId()): void {
    if (!productoVarianteId) return;
    this.loading.set(true);
    forkJoin({
      configuracion: this.service.getConfiguracion(productoVarianteId),
      lotes: this.service.getLotes(productoVarianteId),
      series: this.service.getSeries(productoVarianteId)
    }).subscribe({
      next: ({ configuracion, lotes, series }) => {
        const c = configuracion.data;
        this.configuracionForm.reset({
          controlaLote: c.controlaLote,
          controlaNumeroSerie: c.controlaNumeroSerie,
          controlaFechaVencimiento: c.controlaFechaVencimiento,
          diasAlertaVencimiento: c.diasAlertaVencimiento ?? null
        });
        this.lotes.set(lotes.data.items ?? []);
        this.series.set(series.data.items ?? []);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'No se pudo cargar la trazabilidad de la variante.');
        this.loading.set(false);
      }
    });
  }

  guardarConfiguracion(): void {
    const id = this.varianteId();
    if (!id || this.configuracionForm.invalid) return;
    const raw = this.configuracionForm.getRawValue();
    if (raw.controlaFechaVencimiento && !raw.controlaLote) {
      this.error.set('El control de vencimiento requiere habilitar control por lote.');
      return;
    }
    if (!raw.controlaFechaVencimiento) this.configuracionForm.controls.diasAlertaVencimiento.setValue(null);
    this.saving.set(true);
    this.error.set(null);
    this.service.configurar(id, {
      controlaLote: !!raw.controlaLote,
      controlaNumeroSerie: !!raw.controlaNumeroSerie,
      controlaFechaVencimiento: !!raw.controlaFechaVencimiento,
      diasAlertaVencimiento: raw.controlaFechaVencimiento ? raw.diasAlertaVencimiento : null
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.snackBar.open('Política de trazabilidad actualizada.', 'Cerrar', { duration: 3000 });
        this.cargar(id);
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err.error?.message ?? 'No se pudo actualizar la política de trazabilidad.');
      }
    });
  }

  crearLote(): void {
    const id = this.varianteId();
    if (!id || this.loteForm.invalid) return;
    const raw = this.loteForm.getRawValue();
    this.saving.set(true);
    this.service.crearLote({
      productoVarianteId: id,
      codigo: raw.codigo!.trim(),
      fechaFabricacion: raw.fechaFabricacion || null,
      fechaVencimiento: raw.fechaVencimiento || null
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.loteForm.reset({ codigo: '', fechaFabricacion: '', fechaVencimiento: '' });
        this.snackBar.open('Lote registrado.', 'Cerrar', { duration: 2500 });
        this.cargar(id);
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err.error?.message ?? 'No se pudo registrar el lote.');
      }
    });
  }

  async desactivarLote(lote: LoteInventario): Promise<void> {
    if (!lote.activo) return;
    const confirmado = await this.alerts.confirmar({
      titulo: 'Desactivar lote',
      mensaje: `¿Desactivar el lote ${lote.codigo}?`,
      tipo: 'peligro',
      confirmarTexto: 'Desactivar lote'
    });
    if (!confirmado) return;
    this.service.desactivarLote(lote.id).subscribe({
      next: () => this.cargar(),
      error: (err) => this.error.set(err.error?.message ?? 'No se pudo desactivar el lote.')
    });
  }

  crearSerie(): void {
    const id = this.varianteId();
    if (!id || this.serieForm.invalid) return;
    const raw = this.serieForm.getRawValue();
    this.saving.set(true);
    this.service.crearSerie({
      productoVarianteId: id,
      loteInventarioId: raw.loteInventarioId ?? null,
      numeroSerie: raw.numeroSerie!.trim()
    }).subscribe({
      next: () => {
        this.saving.set(false);
        this.serieForm.reset({ numeroSerie: '', loteInventarioId: null });
        this.snackBar.open('Número de serie registrado.', 'Cerrar', { duration: 2500 });
        this.cargar(id);
      },
      error: (err) => {
        this.saving.set(false);
        this.error.set(err.error?.message ?? 'No se pudo registrar el número de serie.');
      }
    });
  }

  async darDeBajaSerie(serie: SerieInventario): Promise<void> {
    const confirmado = await this.alerts.confirmar({
      titulo: 'Dar de baja serie',
      mensaje: `¿Dar de baja la serie ${serie.numeroSerie}?`,
      tipo: 'peligro',
      confirmarTexto: 'Dar de baja'
    });
    if (!confirmado) return;
    this.service.darDeBajaSerie(serie.id).subscribe({
      next: () => this.cargar(),
      error: (err) => this.error.set(err.error?.message ?? 'No se pudo dar de baja la serie.')
    });
  }

  codigoLote(id?: number | null): string {
    if (!id) return '—';
    return this.lotes().find(x => x.id === id)?.codigo ?? `#${id}`;
  }

  etiquetaEstadoSerie(estado: number): string {
    return ({ 1: 'Disponible', 2: 'Reservada', 3: 'Vendida', 4: 'Consumida', 5: 'Baja' } as Record<number, string>)[estado] ?? `Estado ${estado}`;
  }
}
