import { Component, OnInit, inject, signal } from '@angular/core';

import { ReactiveFormsModule, FormsModule, FormBuilder, Validators } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { EmpresaConfiguracionService } from '../../services/empresa-configuracion.service';
import { TemaVisualService } from '../../services/tema-visual.service';
import { ThemeApplierService } from '../../services/theme-applier.service';
import { EmpresaIdentidadService } from '../../services/empresa-identidad.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { CAMPOS_TEMA, TemaVisual } from '../../core/models/tema-visual.model';
import { EmpresaConfiguracion } from '../../core/models/empresa-configuracion.model';
import { AppAlertService } from '../../shared/alerts/app-alert.service';
import { AutomatizacionConfiguracionCardComponent } from './automatizacion-configuracion-card.component';
import { EmailEmpresarialCardComponent } from './email-empresarial-card.component';
import { EmpresaAdministracionCardComponent } from './empresa-administracion-card.component';
import { EmpresaConfiguracionTenantCardComponent } from './empresa-configuracion-tenant-card.component';
import { EmpresaConfiguracionPlantillasTenantCardComponent } from './empresa-configuracion-plantillas-tenant-card.component';
import { SecuenciaDocumentoCardComponent } from './secuencia-documento-card.component';
import { SuscripcionSaaSCardComponent } from './suscripcion-saas-card.component';
import { WhatsappBusinessCardComponent } from './whatsapp-business-card.component';

@Component({
  selector: 'app-configuracion',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    FormsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    AutomatizacionConfiguracionCardComponent,
    EmailEmpresarialCardComponent,
    EmpresaAdministracionCardComponent,
    EmpresaConfiguracionTenantCardComponent,
    EmpresaConfiguracionPlantillasTenantCardComponent,
    SecuenciaDocumentoCardComponent,
    SuscripcionSaaSCardComponent,
    WhatsappBusinessCardComponent
],
  templateUrl: './configuracion.component.html',
  styleUrl: './configuracion.component.scss'
})
export class ConfiguracionComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly uploadingLogo = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);
  readonly puedeEditar = signal(false);
  readonly logoUrl = signal<string | undefined>(undefined);
  readonly logoPreview = signal<string | undefined>(undefined);
  private logoSeleccionado?: File;

  form = this.fb.group({
    nombreComercial: ['', Validators.required],
    razonSocial: [''],
    eslogan: [''],
    rtn: [''],
    telefono: [''],
    correo: ['', Validators.email],
    direccion: [''],
    sitioWeb: [''],
    facebook: [''],
    instagram: [''],
    whatsApp: [''],
    nombreVisibleSistema: ['', Validators.required],
    descripcionSistema: [''],
    mensajeLogin: [''],
    copyright: ['', Validators.required],
    mostrarCopyright: [true],
    usarAnioAutomaticoCopyright: [true],
    encabezadoActivo: [true],
    encabezadoTexto: [''],
    piePaginaActivo: [true],
    piePaginaTexto: [''],
    moneda: ['HNL', Validators.required],
    zonaHoraria: ['America/Tegucigalpa', Validators.required],
    formatoFecha: ['dd/MM/yyyy', Validators.required],
    informacionFiscal: [''],
    textoLegal: [''],
    textoFactura: [''],
    textoReportes: ['']
  });

  readonly campos = CAMPOS_TEMA;
  readonly loadingTema = signal(true);
  readonly guardandoTema = signal(false);
  readonly restaurandoTema = signal(false);
  readonly errorTema = signal<string | null>(null);
  tema: TemaVisual = this.temaVacio();
  private temaOriginal: TemaVisual = this.temaVacio();

  constructor(
    private empresaService: EmpresaConfiguracionService,
    private temaVisualService: TemaVisualService,
    private themeApplier: ThemeApplierService,
    private identidadService: EmpresaIdentidadService,
    private permisosRuntime: PermisosRuntimeService,
    private alerts: AppAlertService,
    private snackBar: MatSnackBar
  ) {}

  private temaVacio(): TemaVisual {
    return Object.fromEntries(this.campos?.map((c) => [c.clave, '']) ?? []) as unknown as TemaVisual;
  }

  ngOnInit(): void {
    this.puedeEditar.set(this.permisosRuntime.puede('Configuracion', 'Editar'));
    if (!this.puedeEditar()) this.form.disable();

    this.empresaService.get().subscribe({
      next: (res) => {
        this.cargarEmpresa(res.data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });

    this.temaVisualService.get().subscribe({
      next: (res) => {
        this.tema = { ...res.data };
        this.temaOriginal = { ...res.data };
        this.loadingTema.set(false);
      },
      error: () => this.loadingTema.set(false)
    });
  }

  private cargarEmpresa(config: EmpresaConfiguracion): void {
    this.logoUrl.set(config.logoUrl);
    this.logoPreview.set(undefined);
    this.logoSeleccionado = undefined;
    this.form.patchValue({
      nombreComercial: config.nombreComercial,
      razonSocial: config.razonSocial ?? '',
      eslogan: config.eslogan ?? '',
      rtn: config.rtn ?? '',
      telefono: config.telefono ?? '',
      correo: config.correo ?? '',
      direccion: config.direccion ?? '',
      sitioWeb: config.sitioWeb ?? '',
      facebook: config.facebook ?? '',
      instagram: config.instagram ?? '',
      whatsApp: config.whatsApp ?? '',
      nombreVisibleSistema: config.nombreVisibleSistema,
      descripcionSistema: config.descripcionSistema ?? '',
      mensajeLogin: config.mensajeLogin ?? '',
      copyright: config.copyright,
      mostrarCopyright: config.mostrarCopyright,
      usarAnioAutomaticoCopyright: config.usarAnioAutomaticoCopyright,
      encabezadoActivo: config.encabezadoActivo,
      encabezadoTexto: config.encabezadoTexto ?? '',
      piePaginaActivo: config.piePaginaActivo,
      piePaginaTexto: config.piePaginaTexto ?? '',
      moneda: config.moneda,
      zonaHoraria: config.zonaHoraria,
      formatoFecha: config.formatoFecha,
      informacionFiscal: config.informacionFiscal ?? '',
      textoLegal: config.textoLegal ?? '',
      textoFactura: config.textoFactura ?? '',
      textoReportes: config.textoReportes ?? ''
    });
  }

  guardar(): void {
    if (this.form.invalid) return;
    this.saving.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);
    const v = this.form.getRawValue();
    this.empresaService.update({
      nombreComercial: v.nombreComercial!, razonSocial: v.razonSocial || undefined, eslogan: v.eslogan || '',
      rtn: v.rtn || undefined, telefono: v.telefono || undefined, correo: v.correo || undefined,
      direccion: v.direccion || undefined, sitioWeb: v.sitioWeb || undefined, facebook: v.facebook || undefined,
      instagram: v.instagram || undefined, whatsApp: v.whatsApp || undefined, nombreVisibleSistema: v.nombreVisibleSistema!,
      descripcionSistema: v.descripcionSistema || '', mensajeLogin: v.mensajeLogin || '', copyright: v.copyright!,
      mostrarCopyright: !!v.mostrarCopyright, usarAnioAutomaticoCopyright: !!v.usarAnioAutomaticoCopyright,
      encabezadoActivo: !!v.encabezadoActivo, encabezadoTexto: v.encabezadoTexto || undefined,
      piePaginaActivo: !!v.piePaginaActivo, piePaginaTexto: v.piePaginaTexto || undefined,
      moneda: v.moneda || 'HNL', zonaHoraria: v.zonaHoraria || 'America/Tegucigalpa', formatoFecha: v.formatoFecha || 'dd/MM/yyyy',
      informacionFiscal: v.informacionFiscal || undefined, textoLegal: v.textoLegal || undefined,
      textoFactura: v.textoFactura || undefined, textoReportes: v.textoReportes || undefined
    }).subscribe({
      next: (res) => {
        this.saving.set(false); this.cargarEmpresa(res.data); this.identidadService.refrescarDespuesDeGuardar(res.data);
        this.successMessage.set('Configuración guardada correctamente.');
        this.snackBar.open('Configuración de empresa actualizada.', 'Cerrar', { duration: 4000 });
      },
      error: (err) => { this.saving.set(false); this.errorMessage.set(err.error?.message ?? 'No se pudo guardar la configuración.'); }
    });
  }

  seleccionarLogo(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;
    const permitidos = ['image/png', 'image/jpeg', 'image/webp'];
    if (!permitidos.includes(file.type)) { this.errorMessage.set('El logo debe ser PNG, JPG o WEBP.'); input.value = ''; return; }
    if (file.size > 5 * 1024 * 1024) { this.errorMessage.set('El logo no puede superar 5 MB.'); input.value = ''; return; }
    this.logoSeleccionado = file; this.logoPreview.set(URL.createObjectURL(file)); this.errorMessage.set(null);
  }

  subirLogo(): void {
    if (!this.logoSeleccionado) return;
    this.uploadingLogo.set(true);
    this.empresaService.updateLogo(this.logoSeleccionado).subscribe({
      next: (res) => { this.uploadingLogo.set(false); this.cargarEmpresa(res.data); this.identidadService.refrescarDespuesDeGuardar(res.data); this.snackBar.open('Logo actualizado correctamente.', 'Cerrar', { duration: 4000 }); },
      error: (err) => { this.uploadingLogo.set(false); this.errorMessage.set(err.error?.message ?? 'No se pudo actualizar el logo.'); }
    });
  }

  async restaurarLogo(): Promise<void> {
    if (!await this.alerts.confirmar({ titulo: 'Restaurar logo', mensaje: 'Se quitará el logo actual y se restaurará la configuración predeterminada de la empresa.', tipo: 'advertencia', confirmarTexto: 'Restaurar logo' })) return;
    this.uploadingLogo.set(true);
    this.empresaService.restaurarLogo().subscribe({
      next: (res) => { this.uploadingLogo.set(false); this.cargarEmpresa(res.data); this.identidadService.refrescarDespuesDeGuardar(res.data); this.snackBar.open('Logo restaurado correctamente.', 'Cerrar', { duration: 4000 }); },
      error: (err) => { this.uploadingLogo.set(false); this.errorMessage.set(err.error?.message ?? 'No se pudo restaurar el logo.'); }
    });
  }

  previsualizar(): void { this.themeApplier.aplicar(this.tema); }

  async guardarTema(): Promise<void> {
    if (!await this.alerts.confirmar({ titulo: 'Aplicar tema visual', mensaje: 'Los nuevos colores se aplicarán a todo el sistema y a todos los usuarios.', tipo: 'advertencia', confirmarTexto: 'Aplicar colores' })) return;
    this.guardandoTema.set(true); this.errorTema.set(null);
    this.temaVisualService.update(this.tema).subscribe({
      next: (res) => { this.guardandoTema.set(false); this.tema = { ...res.data }; this.temaOriginal = { ...res.data }; this.themeApplier.aplicar(res.data); this.snackBar.open('Tema visual actualizado para todo el sistema.', 'Cerrar', { duration: 4000 }); },
      error: (err) => { this.guardandoTema.set(false); this.errorTema.set(err.error?.message ?? 'No se pudo guardar el tema.'); }
    });
  }

  cancelarTema(): void { this.tema = { ...this.temaOriginal }; this.themeApplier.aplicar(this.tema); }

  async restaurarTema(): Promise<void> {
    if (!await this.alerts.confirmar({ titulo: 'Restaurar tema', mensaje: 'Se restaurarán los colores predeterminados del sistema para todos los usuarios.', tipo: 'advertencia', confirmarTexto: 'Restaurar colores' })) return;
    this.restaurandoTema.set(true);
    this.temaVisualService.restaurar().subscribe({
      next: (res) => { this.restaurandoTema.set(false); this.tema = { ...res.data }; this.temaOriginal = { ...res.data }; this.themeApplier.aplicar(res.data); this.snackBar.open('Tema visual restaurado a los valores predeterminados.', 'Cerrar', { duration: 4000 }); },
      error: (err) => { this.restaurandoTema.set(false); this.errorTema.set(err.error?.message ?? 'No se pudo restaurar el tema.'); }
    });
  }
}