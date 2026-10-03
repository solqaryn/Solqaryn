
import { Component, OnDestroy, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

const TAMANO_MAXIMO_BYTES = 10 * 1024 * 1024;
const DIMENSION_MAXIMA = 4096;
const PIXELES_MAXIMOS = 16_000_000;
const EXTENSIONES_PERMITIDAS = new Set(['jpg', 'jpeg', 'png', 'webp']);
const MIME_PERMITIDOS = new Set(['image/jpeg', 'image/png', 'image/webp']);

@Component({
  selector: 'app-codigo-scanner-dialog',
  standalone: true,
  imports: [
    MatButtonModule,
    MatDialogModule,
    MatIconModule,
    MatProgressSpinnerModule
],
  templateUrl: './codigo-scanner-dialog.component.html',
  styleUrl: './codigo-scanner-dialog.component.scss'
})
export class CodigoScannerDialogComponent implements OnDestroy {
  private static secuencia = 0;
  readonly readerId = `codigo-scanner-reader-${++CodigoScannerDialogComponent.secuencia}`;
  readonly iniciando = signal(false);
  readonly camaraActiva = signal(false);
  readonly procesandoArchivo = signal(false);
  readonly error = signal<string | null>(null);

  private lector?: import('html5-qrcode').Html5Qrcode;
  private resultadoEntregado = false;

  constructor(private readonly dialogRef: MatDialogRef<CodigoScannerDialogComponent>) {}

  async alternarCamara(): Promise<void> {
    if (this.camaraActiva()) {
      await this.detenerCamara();
      return;
    }

    this.error.set(null);
    this.iniciando.set(true);
    try {
      const modulo = await import('html5-qrcode');
      await this.liberarLector();
      this.lector = new modulo.Html5Qrcode(this.readerId, {
        verbose: false,
        formatsToSupport: [
          modulo.Html5QrcodeSupportedFormats.QR_CODE,
          modulo.Html5QrcodeSupportedFormats.EAN_13,
          modulo.Html5QrcodeSupportedFormats.EAN_8,
          modulo.Html5QrcodeSupportedFormats.UPC_A,
          modulo.Html5QrcodeSupportedFormats.UPC_E,
          modulo.Html5QrcodeSupportedFormats.CODE_128,
          modulo.Html5QrcodeSupportedFormats.CODE_39
        ]
      });

      await this.lector.start(
        { facingMode: 'environment' },
        {
          fps: 10,
          qrbox: (ancho, alto) => {
            const lado = Math.max(160, Math.floor(Math.min(ancho, alto) * 0.72));
            return { width: lado, height: Math.max(120, Math.floor(lado * 0.62)) };
          }
        },
        (codigo) => void this.entregarResultado(codigo),
        () => undefined
      );
      this.camaraActiva.set(true);
    } catch {
      await this.liberarLector();
      this.error.set(
        'No se pudo activar la cámara. Verifica el permiso del navegador, usa HTTPS o selecciona una imagen.'
      );
    } finally {
      this.iniciando.set(false);
    }
  }

  async seleccionarArchivo(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0];
    input.value = '';
    if (!archivo || this.procesandoArchivo()) return;

    this.error.set(null);
    this.procesandoArchivo.set(true);
    try {
      await this.validarArchivo(archivo);
      const modulo = await import('html5-qrcode');
      await this.detenerCamara();
      this.lector = new modulo.Html5Qrcode(this.readerId, {
        verbose: false,
        formatsToSupport: [
          modulo.Html5QrcodeSupportedFormats.QR_CODE,
          modulo.Html5QrcodeSupportedFormats.EAN_13,
          modulo.Html5QrcodeSupportedFormats.EAN_8,
          modulo.Html5QrcodeSupportedFormats.UPC_A,
          modulo.Html5QrcodeSupportedFormats.UPC_E,
          modulo.Html5QrcodeSupportedFormats.CODE_128,
          modulo.Html5QrcodeSupportedFormats.CODE_39
        ]
      });
      const codigo = await this.lector.scanFile(archivo, true);
      await this.entregarResultado(codigo);
    } catch (error) {
      await this.liberarLector();
      this.error.set(
        error instanceof Error
          ? error.message
          : 'No se encontró un código compatible en la imagen seleccionada.'
      );
    } finally {
      this.procesandoArchivo.set(false);
    }
  }

  async cerrar(): Promise<void> {
    await this.liberarLector();
    this.dialogRef.close();
  }

  ngOnDestroy(): void {
    void this.liberarLector();
  }

  private async entregarResultado(codigo: string): Promise<void> {
    const normalizado = codigo.trim();
    if (!normalizado || this.resultadoEntregado) return;
    this.resultadoEntregado = true;
    await this.liberarLector();
    this.dialogRef.close(normalizado);
  }

  private async detenerCamara(): Promise<void> {
    if (!this.lector || !this.camaraActiva()) return;
    try {
      await this.lector.stop();
    } finally {
      this.camaraActiva.set(false);
      try {
        this.lector.clear();
      } catch {
        // El contenedor puede haberse liberado al cerrar el diálogo.
      }
    }
  }

  private async liberarLector(): Promise<void> {
    if (!this.lector) {
      this.camaraActiva.set(false);
      return;
    }
    try {
      if (this.camaraActiva()) await this.lector.stop();
    } catch {
      // El stream ya pudo haberse detenido por el navegador.
    }
    try {
      this.lector.clear();
    } catch {
      // El elemento puede haber sido destruido.
    }
    this.lector = undefined;
    this.camaraActiva.set(false);
  }

  private async validarArchivo(archivo: File): Promise<void> {
    const extension = archivo.name.split('.').pop()?.toLowerCase() ?? '';
    if (!EXTENSIONES_PERMITIDAS.has(extension) || (archivo.type && !MIME_PERMITIDOS.has(archivo.type))) {
      throw new Error('Selecciona una imagen JPG, JPEG, PNG o WEBP válida.');
    }
    if (archivo.size <= 0 || archivo.size > TAMANO_MAXIMO_BYTES) {
      throw new Error('La imagen debe pesar más de 0 bytes y como máximo 10 MB.');
    }

    const dimensiones = await this.leerDimensiones(archivo);
    if (
      dimensiones.ancho > DIMENSION_MAXIMA
      || dimensiones.alto > DIMENSION_MAXIMA
      || dimensiones.ancho * dimensiones.alto > PIXELES_MAXIMOS
    ) {
      throw new Error('La imagen no puede superar 4096 px por lado ni 16 megapíxeles.');
    }
  }

  private leerDimensiones(archivo: File): Promise<{ ancho: number; alto: number }> {
    return new Promise((resolve, reject) => {
      const url = URL.createObjectURL(archivo);
      const imagen = new Image();
      imagen.onload = () => {
        URL.revokeObjectURL(url);
        resolve({ ancho: imagen.naturalWidth, alto: imagen.naturalHeight });
      };
      imagen.onerror = () => {
        URL.revokeObjectURL(url);
        reject(new Error('La imagen está dañada o no puede leerse.'));
      };
      imagen.src = url;
    });
  }
}
