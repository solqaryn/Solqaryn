import { CommonModule } from '@angular/common';
import { Component, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  CargaMasiva,
  CargaMasivaConfiguracion,
  CargaMasivaDetalle,
  CargaMasivaProgreso,
  CargaMasivaTipo,
  TipoCargaMasiva
} from '../../core/models/carga-masiva.model';
import { CargaMasivaService } from '../../services/carga-masiva.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';
import { FeedbackStateComponent } from '../../shared/feedback-state/feedback-state.component';

@Component({
  selector: 'app-cargas-masivas',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule,
    FeedbackStateComponent
  ],
  templateUrl: './cargas-masivas.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './cargas-masivas.component.scss'
})
export class CargasMasivasComponent implements OnInit {
  readonly configuracion = signal<CargaMasivaConfiguracion | null>(null);
  readonly historial = signal<CargaMasiva[]>([]);
  readonly detalle = signal<CargaMasivaDetalle | null>(null);
  readonly progreso = signal<CargaMasivaProgreso | null>(null);
  readonly archivo = signal<File | null>(null);
  readonly loading = signal(true);
  readonly validando = signal(false);
  readonly confirmando = signal(false);
  readonly descargando = signal(false);
  readonly error = signal<string | null>(null);
  readonly mensaje = signal<string | null>(null);

  tipoSeleccionado: TipoCargaMasiva = 'Clientes';
  busqueda = '';
  readonly columnasHistorial = ['archivo', 'tipo', 'estado', 'filas', 'resultado', 'fecha', 'acciones'];

  constructor(
    private readonly service: CargaMasivaService,
    private readonly alerts: AppAlertService
  ) {}

  ngOnInit(): void {
    this.service.getConfiguracion().subscribe({
      next: (res) => {
        this.configuracion.set(res.data);
        if (res.data.tipos.length) this.tipoSeleccionado = res.data.tipos[0].tipo;
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'No se pudo cargar la configuración de importación.');
        this.loading.set(false);
      }
    });
    this.cargarHistorial();
  }

  get tipoActual(): CargaMasivaTipo | undefined {
    return this.configuracion()?.tipos.find((item) => item.tipo === this.tipoSeleccionado);
  }

  get columnasVistaPrevia(): string[] {
    return this.tipoActual?.columnas ?? [];
  }

  get limiteVistaPrevia(): number {
    return Math.max(25, Math.min(this.configuracion()?.maximoFilasVistaPrevia ?? 100, 500));
  }

  seleccionarArchivo(event: Event): void {
    this.error.set(null);
    this.mensaje.set(null);
    this.detalle.set(null);
    this.progreso.set(null);
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    if (!file) {
      this.archivo.set(null);
      return;
    }

    const config = this.configuracion();
    const extension = `.${file.name.split('.').pop()?.toLowerCase() ?? ''}`;
    if (config && !config.extensionesPermitidas.includes(extension)) {
      this.error.set('Selecciona un archivo CSV o XLSX sin macros.');
      input.value = '';
      this.archivo.set(null);
      return;
    }
    if (config && file.size > config.maximoBytes) {
      this.error.set(`El archivo supera el máximo de ${this.formatearBytes(config.maximoBytes)}.`);
      input.value = '';
      this.archivo.set(null);
      return;
    }
    this.archivo.set(file);
  }

  validar(): void {
    const file = this.archivo();
    if (!file || this.validando()) return;
    this.validando.set(true);
    this.error.set(null);
    this.mensaje.set(null);
    this.service.validar(this.tipoSeleccionado, file).subscribe({
      next: (res) => {
        this.detalle.set(res.data);
        this.mensaje.set(res.message || 'Archivo validado.');
        this.validando.set(false);
        this.cargarProgreso(res.data.id);
        this.cargarHistorial();
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'No se pudo validar el archivo.');
        this.validando.set(false);
      }
    });
  }

  async confirmar(): Promise<void> {
    const carga = this.detalle();
    if (!carga?.puedeConfirmarse || this.confirmando()) return;
    const confirmado = await this.alerts.confirmar({
      titulo: 'Confirmar carga masiva',
      mensaje: `Se confirmarán ${carga.filasValidas} filas válidas.`,
      detalle: 'La operación conservará atomicidad transaccional y no aplicará cambios parciales.',
      tipo: 'advertencia',
      confirmarTexto: 'Confirmar carga',
      cancelarTexto: 'Cancelar'
    });
    if (!confirmado) return;

    this.confirmando.set(true);
    this.error.set(null);
    this.mensaje.set(null);
    this.service.confirmar(carga.id).subscribe({
      next: (res) => {
        this.detalle.set(res.data);
        this.mensaje.set(res.message || 'Carga confirmada correctamente.');
        this.confirmando.set(false);
        this.cargarProgreso(res.data.id);
        this.cargarHistorial();
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'No se pudo confirmar la carga. No se aplicaron cambios parciales.');
        this.confirmando.set(false);
        this.cargarProgreso(carga.id);
      }
    });
  }

  cargarHistorial(): void {
    this.service.getPaged(1, 20, this.busqueda).subscribe({
      next: (res) => this.historial.set(res.data.items),
      error: (err) => this.error.set(err.error?.message ?? 'No se pudo cargar el historial.')
    });
  }

  verCarga(item: CargaMasiva): void {
    this.error.set(null);
    this.service.getById(item.id).subscribe({
      next: (res) => {
        this.detalle.set(res.data);
        this.tipoSeleccionado = res.data.tipo;
        this.cargarProgreso(res.data.id);
      },
      error: (err) => this.error.set(err.error?.message ?? 'No se pudo abrir la carga seleccionada.')
    });
  }

  cargarProgreso(id: number): void {
    this.service.getProgreso(id).subscribe({
      next: (res) => this.progreso.set(res.data),
      error: () => this.progreso.set(null)
    });
  }

  descargarPlantilla(formato: 'csv' | 'xlsx'): void {
    this.descargando.set(true);
    const version = this.configuracion()?.versionPlantillaActual;
    this.service.descargarPlantilla(this.tipoSeleccionado, formato, version).subscribe({
      next: (blob) => {
        const sufijo = version ? `-${version.toLowerCase().replace('.', '-')}` : '';
        this.guardarBlob(blob, `plantilla-${this.tipoSeleccionado.toLowerCase()}${sufijo}.${formato}`);
        this.descargando.set(false);
      },
      error: () => {
        this.error.set('No se pudo descargar la plantilla vigente.');
        this.descargando.set(false);
      }
    });
  }

  descargarErrores(formato: 'csv' | 'xlsx'): void {
    const carga = this.detalle();
    if (!carga) return;
    this.descargando.set(true);
    this.service.descargarErrores(carga.id, formato).subscribe({
      next: (blob) => {
        this.guardarBlob(blob, `carga-${carga.id}-errores.${formato}`);
        this.descargando.set(false);
      },
      error: () => {
        this.error.set('No se pudo descargar el informe de errores.');
        this.descargando.set(false);
      }
    });
  }

  reiniciar(): void {
    this.detalle.set(null);
    this.progreso.set(null);
    this.archivo.set(null);
    this.error.set(null);
    this.mensaje.set(null);
  }

  valorFila(datos: Record<string, string | null>, columna: string): string {
    return datos[columna] ?? '—';
  }

  etiquetaEstado(estado: string): string {
    return ({
      PendienteValidacion: 'Pendiente',
      Validada: 'Validada',
      ConErrores: 'Con errores',
      Confirmada: 'Confirmada',
      Fallida: 'Fallida',
      Cancelada: 'Cancelada'
    } as Record<string, string>)[estado] ?? estado;
  }

  formatearBytes(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / 1024 / 1024).toFixed(1)} MB`;
  }

  private guardarBlob(blob: Blob, nombre: string): void {
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = nombre;
    link.click();
    URL.revokeObjectURL(url);
  }
}
