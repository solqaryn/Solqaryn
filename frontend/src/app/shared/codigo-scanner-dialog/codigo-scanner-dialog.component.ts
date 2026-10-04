import { ChangeDetectionStrategy, Component, ElementRef, OnDestroy, ViewChild, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

const TAMANO_MAXIMO_BYTES = 10 * 1024 * 1024;
const DIMENSION_MAXIMA = 4096;
const PIXELES_MAXIMOS = 16_000_000;
const INTERVALO_ESCANEO_MS = 100;
const EXTENSIONES_PERMITIDAS = new Set(['jpg', 'jpeg', 'png', 'webp']);
const MIME_PERMITIDOS = new Set(['image/jpeg', 'image/png', 'image/webp']);
const FORMATOS_ESCANEO = ['qr_code', 'ean_13', 'ean_8', 'upc_a', 'upc_e', 'code_128', 'code_39'] as const;

type ResultadoCodigo = { rawValue: string };
type DetectorCodigo = {
  detect(source: HTMLVideoElement | Blob): Promise<ResultadoCodigo[]>;
};

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
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './codigo-scanner-dialog.component.scss'
})
export class CodigoScannerDialogComponent implements OnDestroy {
  @ViewChild('videoElement') private videoElement?: ElementRef<HTMLVideoElement>;

  readonly iniciando = signal(false);
  readonly camaraActiva = signal(false);
  readonly procesandoArchivo = signal(false);
  readonly error = signal<string | null>(null);

  private static inicializacionWasm?: Promise<void>;

  private detector?: DetectorCodigo;
  private stream?: MediaStream;
  private temporizadorEscaneo?: number;
  private escaneoEnCurso = false;
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
      if (!navigator.mediaDevices?.getUserMedia) {
        throw new Error('El navegador no permite acceso seguro a la cámara.');
      }

      await this.detenerCamara();
      const detector = await this.obtenerDetector();
      const stream = await navigator.mediaDevices.getUserMedia({
        video: { facingMode: { ideal: 'environment' } },
        audio: false
      });

      const video = this.videoElement?.nativeElement;
      if (!video) {
        stream.getTracks().forEach((track) => track.stop());
        throw new Error('No se pudo preparar la vista de cámara.');
      }

      this.detector = detector;
      this.stream = stream;
      video.srcObject = stream;
      await video.play();
      this.camaraActiva.set(true);
      this.programarEscaneo();
    } catch (error) {
      await this.detenerCamara();
      this.error.set(
        error instanceof Error && error.message.startsWith('El navegador')
          ? error.message
          : 'No se pudo activar la cámara. Verifica el permiso del navegador, usa HTTPS o selecciona una imagen.'
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
      await this.detenerCamara();
      const detector = await this.obtenerDetector();
      const resultados = await detector.detect(archivo);
      const codigo = resultados.find((resultado) => resultado.rawValue.trim())?.rawValue;
      if (!codigo) {
        throw new Error('No se encontró un código compatible en la imagen seleccionada.');
      }
      await this.entregarResultado(codigo);
    } catch (error) {
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
    await this.detenerCamara();
    this.dialogRef.close();
  }

  ngOnDestroy(): void {
    void this.detenerCamara();
  }

  private async obtenerDetector(): Promise<DetectorCodigo> {
    if (this.detector) return this.detector;

    const modulo = await import('barcode-detector/ponyfill');
    if (!CodigoScannerDialogComponent.inicializacionWasm) {
      CodigoScannerDialogComponent.inicializacionWasm = Promise.resolve(
        modulo.prepareZXingModule({
          overrides: {
            locateFile: (path: string, prefix: string) =>
              path.endsWith('.wasm')
                ? new URL('assets/wasm/zxing_reader.wasm', document.baseURI).toString()
                : prefix + path
          }
        })
      ).then(() => undefined);
    }

    try {
      await CodigoScannerDialogComponent.inicializacionWasm;
    } catch (error) {
      CodigoScannerDialogComponent.inicializacionWasm = undefined;
      throw error;
    }

    this.detector = new modulo.BarcodeDetector({ formats: [...FORMATOS_ESCANEO] });
    return this.detector;
  }

  private programarEscaneo(): void {
    const procesar = async (): Promise<void> => {
      if (!this.camaraActiva() || !this.stream || this.resultadoEntregado) return;

      const video = this.videoElement?.nativeElement;
      if (video && video.readyState >= HTMLMediaElement.HAVE_CURRENT_DATA && !this.escaneoEnCurso) {
        this.escaneoEnCurso = true;
        try {
          const detector = await this.obtenerDetector();
          const resultados = await detector.detect(video);
          const codigo = resultados.find((resultado) => resultado.rawValue.trim())?.rawValue;
          if (codigo) {
            await this.entregarResultado(codigo);
            return;
          }
        } catch {
          // Un frame sin lectura válida no debe interrumpir el escaneo continuo.
        } finally {
          this.escaneoEnCurso = false;
        }
      }

      if (this.camaraActiva()) {
        this.temporizadorEscaneo = window.setTimeout(() => void procesar(), INTERVALO_ESCANEO_MS);
      }
    };

    void procesar();
  }

  private async entregarResultado(codigo: string): Promise<void> {
    const normalizado = codigo.trim();
    if (!normalizado || this.resultadoEntregado) return;
    this.resultadoEntregado = true;
    await this.detenerCamara();
    this.dialogRef.close(normalizado);
  }

  private async detenerCamara(): Promise<void> {
    if (this.temporizadorEscaneo !== undefined) {
      window.clearTimeout(this.temporizadorEscaneo);
      this.temporizadorEscaneo = undefined;
    }

    this.camaraActiva.set(false);
    this.escaneoEnCurso = false;

    if (this.stream) {
      this.stream.getTracks().forEach((track) => track.stop());
      this.stream = undefined;
    }

    const video = this.videoElement?.nativeElement;
    if (video) {
      video.pause();
      video.srcObject = null;
    }
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
