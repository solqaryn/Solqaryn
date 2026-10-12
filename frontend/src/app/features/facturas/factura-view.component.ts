import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FacturaService } from '../../services/factura.service';
import {
  EnlaceCompartir,
  EstadoConfiguracionSmtp,
  Factura,
  FacturaFormatoCodigo,
  FacturaFormatoPdf,
  HistorialEnvio,
  ResultadoDiagnosticoSmtp
} from '../../core/models/factura.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { ejecutarFacturaWhatsAppHandoff, normalizarTelefonoWhatsApp, WhatsAppShareService, WHATSAPP_AUDIT_FAILURE_MESSAGE } from '../../core/services/whatsapp-share.service';
import { descargarBlobSeguro } from '../../shared/descarga-segura';
import { FacturaFiscalEmisionComponent } from './factura-fiscal-emision.component';

const FORMATO_STORAGE_KEY = 'solqaryn_factura_formato_pdf';
const FORMATOS_FALLBACK: FacturaFormatoPdf[] = [
  { codigo: 'a4', nombre: 'A4', descripcion: '210 × 297 mm', anchoMm: 210, altoMm: 297, esContinuo: false, usoRecomendado: 'Impresoras convencionales y archivo digital' },
  { codigo: 'carta', nombre: 'Carta', descripcion: '8.5 × 11 pulgadas', anchoMm: 215.9, altoMm: 279.4, esContinuo: false, usoRecomendado: 'Impresoras de oficina' },
  { codigo: 'legal', nombre: 'Legal', descripcion: '8.5 × 14 pulgadas', anchoMm: 215.9, altoMm: 355.6, esContinuo: false, usoRecomendado: 'Documentos extensos en papel legal' },
  { codigo: 'oficio', nombre: 'Oficio', descripcion: '8.5 × 13 pulgadas', anchoMm: 215.9, altoMm: 330.2, esContinuo: false, usoRecomendado: 'Impresoras configuradas para papel oficio' },
  { codigo: 'a5', nombre: 'A5', descripcion: '148 × 210 mm', anchoMm: 148, altoMm: 210, esContinuo: false, usoRecomendado: 'Comprobantes compactos' },
  { codigo: 'pos58', nombre: 'POS 58 mm', descripcion: 'Rollo continuo de 58 mm', anchoMm: 58, esContinuo: true, usoRecomendado: 'Impresoras térmicas móviles y handheld' },
  { codigo: 'pos80', nombre: 'POS 80 mm', descripcion: 'Rollo continuo de 80 mm', anchoMm: 80, esContinuo: true, usoRecomendado: 'Impresoras térmicas POS e industriales' }
];

interface DimensionesPdf {
  anchoMm: number;
  altoMm: number;
}

@Component({
  selector: 'app-factura-view',
  standalone: true,
  imports: [
    CommonModule, FormsModule, RouterLink, MatButtonModule, MatIconModule,
    MatFormFieldModule, MatInputModule, MatSelectModule, MatProgressSpinnerModule,
    FacturaFiscalEmisionComponent
  ],
  templateUrl: './factura-view.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './factura-view.component.scss'
})
export class FacturaViewComponent implements OnInit {
  private readonly permisosRuntime = inject(PermisosRuntimeService);
  private readonly whatsappShare = inject(WhatsAppShareService);

  readonly factura = signal<Factura | null>(null);
  readonly loading = signal(true);
  readonly formatosPdf = signal<FacturaFormatoPdf[]>(FORMATOS_FALLBACK);
  formatoSeleccionado: FacturaFormatoCodigo = 'a4';

  readonly descargandoPdf = signal(false);
  readonly imprimiendoPdf = signal(false);
  readonly revocandoEnlaces = signal(false);
  readonly puedeExportar = signal(false);
  readonly puedeImprimir = signal(false);
  readonly puedeCompartir = signal(false);
  readonly alturaTermicaMm = signal<number | null>(null);

  readonly mostrarPanelWhatsApp = signal(false);
  readonly preparandoWhatsApp = signal(false);
  readonly enlaceCompartir = signal<EnlaceCompartir | null>(null);
  readonly enlaceWhatsAppFallback = signal('');
  telefonoEditable = '';
  mensajeEditable = '';

  readonly historial = signal<HistorialEnvio[]>([]);
  readonly mostrarHistorial = signal(false);

  readonly mostrarPanelCorreo = signal(false);
  readonly enviandoCorreo = signal(false);
  readonly cargandoEstadoSmtp = signal(false);
  readonly diagnosticandoSmtp = signal(false);
  readonly estadoSmtp = signal<EstadoConfiguracionSmtp | null>(null);
  readonly diagnosticoSmtp = signal<ResultadoDiagnosticoSmtp | null>(null);
  readonly correoUltimoError = signal('');
  correoEditable = '';
  private correoIdempotencyKey = '';

  constructor(
    private facturaService: FacturaService,
    private route: ActivatedRoute,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.puedeExportar.set(this.permisosRuntime.puede('Facturacion', 'Exportar'));
    this.puedeImprimir.set(this.permisosRuntime.puede('Facturacion', 'Imprimir'));
    this.puedeCompartir.set(this.permisosRuntime.puede('Facturacion', 'Compartir'));
    this.restaurarFormatoPreferido();

    this.facturaService.getFormatosPdf().subscribe({
      next: (res) => {
        if (res.data?.length) {
          this.formatosPdf.set(res.data);
          if (!res.data.some((x) => x.codigo === this.formatoSeleccionado)) {
            this.formatoSeleccionado = 'a4';
          }
        }
      }
    });

    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.facturaService.getById(id).subscribe({
      next: (res) => { this.factura.set(res.data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  formatoActual(): FacturaFormatoPdf {
    return this.formatosPdf().find((x) => x.codigo === this.formatoSeleccionado)
      ?? FORMATOS_FALLBACK[0];
  }

  esTermico(): boolean {
    return this.formatoActual().esContinuo;
  }

  onFormatoChange(codigo: FacturaFormatoCodigo): void {
    this.formatoSeleccionado = codigo;
    this.alturaTermicaMm.set(null);
    try {
      window.localStorage.setItem(FORMATO_STORAGE_KEY, codigo);
    } catch {
      // La selección sigue activa durante la sesión aunque el navegador bloquee almacenamiento.
    }
  }

  imprimir(): void {
    if (!this.puedeImprimir() || this.imprimiendoPdf()) return;

    const factura = this.factura();
    if (!factura) return;

    const formato = this.formatoActual();
    const ventanaPdf = window.open('', '_blank');
    if (!ventanaPdf) {
      this.snackBar.open('El navegador bloqueó la ventana de impresión. Habilita las ventanas emergentes.', 'Cerrar', { duration: 6000 });
      return;
    }

    ventanaPdf.opener = null;
    ventanaPdf.document.title = `Preparando ${factura.numeroFactura} — ${formato.nombre}`;
    ventanaPdf.document.body.textContent = `Preparando PDF ${formato.nombre} para impresión...`;
    this.imprimiendoPdf.set(true);

    this.facturaService.descargarPdf(factura.id, formato.codigo).subscribe({
      next: async (blob) => {
        this.imprimiendoPdf.set(false);
        const url = window.URL.createObjectURL(blob);

        if (formato.esContinuo) {
          const dimensiones = await this.leerDimensionesPdf(blob);
          if (dimensiones) {
            this.alturaTermicaMm.set(dimensiones.altoMm);
            this.mostrarPreparacionTermica(ventanaPdf, url, factura, formato, dimensiones);
            return;
          }
        }

        ventanaPdf.location.href = url;
        window.setTimeout(() => window.URL.revokeObjectURL(url), 120_000);
      },
      error: () => {
        this.imprimiendoPdf.set(false);
        ventanaPdf.close();
        this.snackBar.open(`No se pudo abrir el PDF ${formato.nombre} para imprimir.`, 'Cerrar', { duration: 5000 });
      }
    });
  }

  descargarPdf(): void {
    if (!this.puedeExportar() || this.descargandoPdf()) return;

    const factura = this.factura();
    if (!factura) return;

    const formato = this.formatoActual();
    this.descargandoPdf.set(true);
    this.facturaService.descargarPdf(factura.id, formato.codigo).subscribe({
      next: async (blob) => {
        this.descargandoPdf.set(false);
        if (formato.esContinuo) {
          const dimensiones = await this.leerDimensionesPdf(blob);
          if (dimensiones) this.alturaTermicaMm.set(dimensiones.altoMm);
        }
        if (!descargarBlobSeguro(blob, `${factura.numeroFactura}-${formato.codigo}.pdf`, 'factura.pdf')) {
          this.snackBar.open('El PDF generado está vacío.', 'Cerrar', { duration: 5000 });
        }
      },
      error: () => {
        this.descargandoPdf.set(false);
        this.snackBar.open(`No se pudo generar el PDF ${formato.nombre} de la factura.`, 'Cerrar', { duration: 5000 });
      }
    });
  }

  toggleWhatsApp(): void {
    if (!this.puedeCompartir()) return;

    if (this.mostrarPanelWhatsApp()) {
      this.mostrarPanelWhatsApp.set(false);
      this.enlaceWhatsAppFallback.set('');
      return;
    }

    const factura = this.factura();
    if (!factura) return;

    const enlaceActual = this.enlaceCompartir();
    if (enlaceActual && new Date(enlaceActual.fechaExpiracion).getTime() > Date.now()) {
      this.mostrarPanelWhatsApp.set(true);
      return;
    }

    this.preparandoWhatsApp.set(true);
    this.facturaService.prepararWhatsApp(factura.id).subscribe({
      next: (res) => {
        this.preparandoWhatsApp.set(false);
        this.enlaceCompartir.set(res.data);
        this.telefonoEditable = res.data.telefonoSugerido;
        this.mensajeEditable = res.data.mensajeWhatsApp;
        this.enlaceWhatsAppFallback.set('');
        this.mostrarPanelWhatsApp.set(true);
        this.snackBar.open('Enlace temporal A4 creado. Los enlaces anteriores fueron revocados.', 'Cerrar', { duration: 4500 });
      },
      error: (err) => {
        this.preparandoWhatsApp.set(false);
        this.snackBar.open(err.error?.message ?? 'No se pudo preparar el envío por WhatsApp.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  revocarEnlaces(): void {
    if (!this.puedeCompartir() || this.revocandoEnlaces()) return;

    const factura = this.factura();
    if (!factura) return;

    this.revocandoEnlaces.set(true);
    this.facturaService.revocarEnlaces(factura.id).subscribe({
      next: (res) => {
        this.revocandoEnlaces.set(false);
        this.enlaceCompartir.set(null);
        this.mostrarPanelWhatsApp.set(false);
        const cantidad = res.data.enlacesRevocados;
        this.snackBar.open(
          cantidad > 0 ? 'Los enlaces públicos fueron revocados.' : 'No había enlaces públicos vigentes.',
          'Cerrar',
          { duration: 4500 }
        );
      },
      error: (err) => {
        this.revocandoEnlaces.set(false);
        this.snackBar.open(err.error?.message ?? 'No se pudieron revocar los enlaces.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  telefonoValido(): boolean {
    return Boolean(normalizarTelefonoWhatsApp(this.telefonoEditable));
  }

  abrirWhatsApp(): void {
    if (!this.puedeCompartir()) return;

    const factura = this.factura();
    if (!factura || !this.telefonoValido() || !this.mensajeEditable.trim()) return;

    const numero = normalizarTelefonoWhatsApp(this.telefonoEditable);
    const resultado = ejecutarFacturaWhatsAppHandoff({
      numero,
      mensaje: this.mensajeEditable,
      construirEnlace: (telefono, mensaje) => this.whatsappShare.construirEnlace(telefono, mensaje),
      abrir: (telefono, mensaje) => this.whatsappShare.abrir(telefono, mensaje),
      registrarAuditoria: (payload) => this.facturaService.registrarIntentoEnvio(
        factura.id, payload.canal, payload.destinatario, payload.resultado
      ),
      onAuditError: () => this.snackBar.open(WHATSAPP_AUDIT_FAILURE_MESSAGE, 'Cerrar', { duration: 5000 })
    });
    if (!resultado) {
      this.snackBar.open('Ingresa un teléfono válido y un mensaje permitido.', 'Cerrar', { duration: 4500 });
      return;
    }
    const aperturaAceptada = resultado.aperturaAceptada;
    if (!aperturaAceptada) {
      this.enlaceWhatsAppFallback.set(resultado.url);
      this.snackBar.open('No se pudo abrir WhatsApp automáticamente. Usa el enlace generado desde el navegador.', 'Cerrar', { duration: 5000 });
      return;
    }
    this.enlaceWhatsAppFallback.set('');
    this.mostrarPanelWhatsApp.set(false);
  }

  toggleHistorial(): void {
    if (!this.puedeCompartir()) return;

    if (this.mostrarHistorial()) {
      this.mostrarHistorial.set(false);
      return;
    }

    this.cargarHistorial(true);
  }

  toggleCorreo(): void {
    if (!this.puedeCompartir()) return;

    if (this.mostrarPanelCorreo()) {
      this.mostrarPanelCorreo.set(false);
      this.correoUltimoError.set('');
      this.correoIdempotencyKey = '';
      return;
    }

    const factura = this.factura();
    if (!factura) return;

    this.correoEditable = factura.clienteCorreo || '';
    this.correoUltimoError.set('');
    this.correoIdempotencyKey = '';
    this.diagnosticoSmtp.set(null);
    this.mostrarPanelCorreo.set(true);
    this.cargarEstadoCorreo();
  }

  onCorreoChange(value: string): void {
    this.correoEditable = value;
    this.correoUltimoError.set('');
    this.correoIdempotencyKey = '';
  }

  correoValido(): boolean {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(this.correoEditable.trim());
  }

  puedeEnviarCorreo(): boolean {
    return this.correoValido() &&
      this.estadoSmtp()?.configurado === true &&
      this.diagnosticoSmtp()?.exito === true &&
      !this.cargandoEstadoSmtp() &&
      !this.diagnosticandoSmtp() &&
      !this.enviandoCorreo();
  }

  probarConexionCorreo(): void {
    if (this.diagnosticandoSmtp() || this.estadoSmtp()?.configurado !== true) return;

    this.diagnosticandoSmtp.set(true);
    this.diagnosticoSmtp.set(null);
    this.correoUltimoError.set('');
    this.facturaService.probarConexionCorreo().subscribe({
      next: (res) => {
        this.diagnosticandoSmtp.set(false);
        this.diagnosticoSmtp.set(res.data);
        if (!res.data.exito) this.correoUltimoError.set(res.data.mensaje);
      },
      error: (err) => {
        this.diagnosticandoSmtp.set(false);
        const mensaje = err.error?.message ?? 'No se pudo ejecutar el diagnóstico SMTP.';
        this.correoUltimoError.set(mensaje);
        this.diagnosticoSmtp.set({
          exito: false,
          codigo: 'SMTP_DIAGNOSTICO_ERROR',
          mensaje,
          host: this.estadoSmtp()?.host ?? 'No disponible',
          puerto: this.estadoSmtp()?.puerto ?? 0,
          modoSeguridad: this.estadoSmtp()?.modoSeguridad ?? '',
          autenticado: false,
          duracionMilisegundos: 0
        });
      }
    });
  }

  enviarCorreo(): void {
    if (!this.puedeCompartir() || !this.puedeEnviarCorreo()) return;

    const factura = this.factura();
    if (!factura) return;

    if (!this.correoIdempotencyKey) {
      this.correoIdempotencyKey = this.crearClaveIdempotencia();
    }

    this.correoUltimoError.set('');
    this.enviandoCorreo.set(true);
    this.facturaService
      .enviarPorCorreo(factura.id, this.correoEditable.trim(), this.correoIdempotencyKey)
      .subscribe({
        next: (res) => {
          this.enviandoCorreo.set(false);
          this.mostrarPanelCorreo.set(false);
          this.correoIdempotencyKey = '';
          this.snackBar.open(res.message || res.data.mensaje || 'Correo enviado correctamente.', 'Cerrar', { duration: 5000 });
          if (this.mostrarHistorial()) this.cargarHistorial(false);
        },
        error: (err) => {
          this.enviandoCorreo.set(false);
          const mensaje = err.error?.message ?? 'No se pudo enviar el correo.';
          this.correoUltimoError.set(mensaje);
          this.snackBar.open(mensaje, 'Cerrar', { duration: 7000 });
        }
      });
  }

  private cargarEstadoCorreo(): void {
    this.cargandoEstadoSmtp.set(true);
    this.estadoSmtp.set(null);
    this.diagnosticoSmtp.set(null);
    this.facturaService.getEstadoCorreo().subscribe({
      next: (res) => {
        this.cargandoEstadoSmtp.set(false);
        this.estadoSmtp.set(res.data);
        if (res.data.configurado) {
          this.probarConexionCorreo();
        } else {
          this.correoUltimoError.set(res.data.mensaje);
        }
      },
      error: (err) => {
        this.cargandoEstadoSmtp.set(false);
        this.estadoSmtp.set(null);
        this.correoUltimoError.set(err.error?.message ?? 'No se pudo verificar la configuración de correo.');
      }
    });
  }

  private cargarHistorial(mostrar: boolean): void {
    const factura = this.factura();
    if (!factura) return;

    this.facturaService.getHistorialEnvios(factura.id).subscribe({
      next: (res) => {
        this.historial.set(res.data);
        if (mostrar) this.mostrarHistorial.set(true);
      },
      error: () => this.snackBar.open('No se pudo cargar el historial de envíos.', 'Cerrar', { duration: 5000 })
    });
  }

  private async leerDimensionesPdf(blob: Blob): Promise<DimensionesPdf | null> {
    try {
      const contenido = new TextDecoder('latin1').decode(await blob.arrayBuffer());
      const match = contenido.match(/\/MediaBox\s*\[\s*0(?:\.0+)?\s+0(?:\.0+)?\s+([0-9]+(?:\.[0-9]+)?)\s+([0-9]+(?:\.[0-9]+)?)\s*\]/);
      if (!match) return null;

      const puntosAMilimetros = 25.4 / 72;
      return {
        anchoMm: Number(match[1]) * puntosAMilimetros,
        altoMm: Number(match[2]) * puntosAMilimetros
      };
    } catch {
      return null;
    }
  }

  private mostrarPreparacionTermica(
    ventana: Window,
    url: string,
    factura: Factura,
    formato: FacturaFormatoPdf,
    dimensiones: DimensionesPdf
  ): void {
    const ancho = dimensiones.anchoMm.toFixed(1);
    const alto = dimensiones.altoMm.toFixed(1);
    const altoDriver = Math.ceil(dimensiones.altoMm + 5);
    const numero = this.escaparHtml(factura.numeroFactura);
    const nombreFormato = this.escaparHtml(formato.nombre);

    ventana.document.open();
    ventana.document.write(`<!doctype html>
<html lang="es">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width,initial-scale=1">
  <title>${numero} — ${nombreFormato}</title>
  <style>
    body{margin:0;background:#eef1f4;color:#17212b;font:16px/1.5 Arial,sans-serif;display:grid;min-height:100vh;place-items:center;padding:24px;box-sizing:border-box}
    main{width:min(680px,100%);background:white;border:1px solid #cbd5e1;border-radius:14px;padding:24px;box-shadow:0 16px 45px rgba(15,23,42,.16)}
    h1{font-size:22px;margin:0 0 10px} .medida{font-size:20px;font-weight:700;color:#075985}
    .aviso{background:#fff7ed;border-left:4px solid #ea580c;padding:12px 14px;margin:16px 0}
    ol{padding-left:22px} button{border:0;border-radius:9px;background:#075985;color:white;padding:12px 18px;font-weight:700;cursor:pointer;font-size:15px}
    small{display:block;color:#475569;margin-top:14px}
  </style>
</head>
<body>
  <main>
    <h1>Ticket térmico listo</h1>
    <p>Solqaryn generó <strong>${numero}</strong> con tamaño real:</p>
    <p class="medida">${ancho} × ${alto} mm</p>
    <div class="aviso"><strong>Importante:</strong> si el diálogo de impresión muestra 80 × 297 mm, ese largo lo está imponiendo el controlador de la impresora, no el PDF de Solqaryn.</div>
    <ol>
      <li>Abre el PDF con el botón inferior.</li>
      <li>En el diálogo usa papel <strong>Receipt / Rollo / Continuous</strong>, escala 100 % y márgenes desactivados.</li>
      <li>Si el controlador exige un alto fijo, crea un tamaño personalizado de aproximadamente <strong>${ancho} × ${altoDriver} mm</strong>.</li>
      <li>Si Chrome no muestra esas opciones, usa “Imprimir mediante el sistema de diálogo” (Ctrl+Shift+P).</li>
    </ol>
    <button id="abrir-pdf" type="button">Abrir PDF e imprimir</button>
    <small>La aplicación no puede modificar desde el navegador la configuración del controlador POS instalado en Windows.</small>
  </main>
</body>
</html>`);
    ventana.document.close();
    ventana.document.getElementById('abrir-pdf')?.addEventListener('click', () => {
      ventana.location.href = url;
      window.setTimeout(() => window.URL.revokeObjectURL(url), 120_000);
    });
  }

  private escaparHtml(valor: string): string {
    return valor
      .replaceAll('&', '&amp;')
      .replaceAll('<', '&lt;')
      .replaceAll('>', '&gt;')
      .replaceAll('"', '&quot;')
      .replaceAll("'", '&#039;');
  }

  private crearClaveIdempotencia(): string {
    try {
      return crypto.randomUUID();
    } catch {
      return `${Date.now()}-${Math.random().toString(36).slice(2)}-${Math.random().toString(36).slice(2)}`;
    }
  }

  private restaurarFormatoPreferido(): void {
    try {
      const guardado = window.localStorage.getItem(FORMATO_STORAGE_KEY) as FacturaFormatoCodigo | null;
      if (guardado && FORMATOS_FALLBACK.some((x) => x.codigo === guardado)) {
        this.formatoSeleccionado = guardado;
      }
    } catch {
      this.formatoSeleccionado = 'a4';
    }
  }
}
