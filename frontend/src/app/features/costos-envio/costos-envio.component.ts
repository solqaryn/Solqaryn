import { ConsultaRegistroDirective } from '../../shared/consulta-registro/consulta-registro';
import { Component, ChangeDetectionStrategy, DestroyRef, computed, effect, inject, signal, untracked, TemplateRef, ViewChild, ViewContainerRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatDialog, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { modalInstitucional, TamanoModal } from '../../shared/modal-institucional/modal-institucional';
import { CostoEnvioService } from '../../services/costo-envio.service';
import { CostoEnvio, GuardarCostoEnvio } from '../../core/models/costo-envio.model';
import { RegistroAuditoria } from '../../core/models/auditoria.model';
import { PagedResult } from '../../core/models/api-response.model';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';

@Component({
  selector: 'app-costos-envio',
  standalone: true,
  imports: [ConsultaRegistroDirective, CommonModule, ReactiveFormsModule, MatButtonModule, MatCheckboxModule,
    MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule, MatTableModule, MatDialogModule],
  templateUrl: './costos-envio.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  styleUrl: './costos-envio.component.scss'
})
export class CostosEnvioComponent {
  private readonly fb = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);
  private readonly tenant = inject(TenantContextService);
  private readonly service = inject(CostoEnvioService);
  private readonly alerts = inject(AppAlertService);
  private readonly dialog = inject(MatDialog);
  private readonly viewContainerRef = inject(ViewContainerRef);
  @ViewChild('editorModal', { static: true }) private editorModal!: TemplateRef<unknown>;
  @ViewChild('simuladorModal', { static: true }) private simuladorModal!: TemplateRef<unknown>;
  @ViewChild('historialModal', { static: true }) private historialModal!: TemplateRef<unknown>;
  private modalRef: MatDialogRef<unknown> | null = null;
  private modalTipo: 'editor' | 'simulador' | 'historial' | null = null;
  private historialRequest = 0;
  private simulacionRequest = 0;
  readonly modalListo = signal(false);
  readonly editorError = signal<string | null>(null);
  readonly items = signal<CostoEnvio[]>([]);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly ready = signal(false);
  readonly error = signal<string | null>(null);
  readonly notice = signal<string | null>(null);
  readonly editor = signal(false);
  readonly editandoId = signal<number | null>(null);
  readonly search = signal('');
  readonly status = signal('todos');
  readonly simulando = signal(false);
  readonly simulacion = signal<CostoEnvio | null>(null);
  readonly simulacionError = signal<string | null>(null);
  readonly historialItem = signal<CostoEnvio | null>(null);
  readonly historial = signal<PagedResult<RegistroAuditoria> | null>(null);
  readonly historialLoading = signal(false);
  readonly historialError = signal<string | null>(null);
  readonly columnas = ['nombre', 'cobertura', 'monto', 'vigencia', 'estado', 'predeterminado', 'acciones'];
  readonly predeterminado = computed(() => this.items().find(x => x.esPredeterminado && x.estaVigente));
  readonly activas = computed(() => this.items().filter(x => x.activo && x.estaVigente).length);
  readonly filtradas = computed(() => {
    const term = this.search().trim().toLocaleLowerCase('es');
    const status = this.status();
    return this.items().filter(x => (!term || [x.nombre, x.descripcion, this.cobertura(x)].some(s => s?.toLocaleLowerCase('es').includes(term)))
      && (status === 'todos' || (status === 'vigente' && x.activo && x.estaVigente)
        || (status === 'inactivo' && !x.activo) || (status === 'fuera' && x.activo && !x.estaVigente)));
  });
  readonly form = this.fb.group({
    nombre: ['', [Validators.required, Validators.maxLength(120)]],
    descripcion: ['', Validators.maxLength(500)],
    departamento: ['', Validators.maxLength(120)],
    ciudad: ['', Validators.maxLength(120)],
    zona: ['', Validators.maxLength(150)],
    modalidad: ['', Validators.maxLength(80)],
    monto: this.fb.control<number | null>(null, [Validators.required, Validators.min(0), Validators.max(9999999999999999)]),
    vigenteDesde: [''], vigenteHasta: [''],
    prioridad: [1, [Validators.required, Validators.min(0), Validators.pattern(/^\d+$/)]],
    esPredeterminado: [false], activo: [true]
  });
  readonly simulador = this.fb.group({
    departamento: ['', Validators.maxLength(120)], ciudad: ['', Validators.maxLength(120)],
    zona: ['', Validators.maxLength(150)], modalidad: ['', Validators.maxLength(80)], fecha: ['']
  });

  constructor() {
    this.destroyRef.onDestroy(() => this.invalidarModal());
    this.simulador.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      // Una respuesta de una cobertura anterior no puede reemplazar la consulta actual.
      ++this.simulacionRequest;
      this.simulando.set(false);
      this.simulacion.set(null);
      this.simulacionError.set(null);
    });
    effect(() => {
      const empresa = this.tenant.contextoVerificado();
      untracked(() => {
        this.items.set([]); this.ready.set(false); this.error.set(null); this.notice.set(null);
        this.saving.set(false); this.simulando.set(false); this.simulacion.set(null); this.simulacionError.set(null);
        this.invalidarModal();
        this.reiniciarEditor(); this.reiniciarSimulador(); this.reiniciarHistorial();
        if (empresa) this.cargar(); else this.loading.set(false);
      });
    });
  }
  private vigente(empresa: ReturnType<TenantContextService['contextoVerificado']>): boolean {
    return !this.destroyRef.destroyed && empresa !== null && this.tenant.contextoVerificado() === empresa;
  }

  private abrirModal(tipo: 'editor' | 'simulador' | 'historial', plantilla: TemplateRef<unknown>, tamano: TamanoModal, titulo: string): void {
    // No aceptar entradas mientras Material adjunta el formulario y estabiliza el foco.
    this.modalListo.set(false);
    const controles = tipo === 'editor' ? this.form : tipo === 'simulador' ? this.simulador : null;
    controles?.disable({ emitEvent: false });
    const ref = this.dialog.open(plantilla, modalInstitucional(tamano, { ariaLabel: titulo, viewContainerRef: this.viewContainerRef }));
    this.modalRef = ref;
    this.modalTipo = tipo;
    ref.afterOpened().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      if (this.modalRef !== ref || this.destroyRef.destroyed) return;
      controles?.enable({ emitEvent: false });
      this.modalListo.set(true);
    });
    ref.afterClosed().pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      if (this.modalRef !== ref) return;
      this.modalRef = null;
      this.modalTipo = null;
      this.modalListo.set(false);
      if (tipo === 'editor') this.reiniciarEditor();
      if (tipo === 'simulador') this.reiniciarSimulador();
      if (tipo === 'historial') this.reiniciarHistorial();
    });
  }

  private invalidarModal(): void {
    const ref = this.modalRef;
    this.modalRef = null;
    this.modalTipo = null;
    this.modalListo.set(false);
    ref?.close();
  }

  private reiniciarSimulador(): void {
    ++this.simulacionRequest;
    this.simulador.reset({ departamento: '', ciudad: '', zona: '', modalidad: '', fecha: '' });
    this.simulando.set(false); this.simulacion.set(null); this.simulacionError.set(null);
  }

  private reiniciarHistorial(): void {
    ++this.historialRequest;
    this.historialItem.set(null); this.historial.set(null); this.historialLoading.set(false); this.historialError.set(null);
  }

  abrirSimulador(): void {
    if (!this.ready() || this.saving() || this.modalRef || !this.tenant.contextoVerificado()) return;
    this.reiniciarSimulador();
    this.abrirModal('simulador', this.simuladorModal, 'm', 'Simular tarifa');
  }

  cerrarSimulador(): void {
    if (this.modalTipo === 'simulador') {
      ++this.simulacionRequest;
      this.modalRef?.close();
    }
  }
  cargar(): void {
    const empresa = this.tenant.contextoVerificado();
    if (!empresa) return;
    this.loading.set(true);
    this.error.set(null);
    this.service.getAdministracion().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { if (!this.vigente(empresa)) return; this.items.set(res.data); this.ready.set(true); this.loading.set(false); },
      error: err => { if (!this.vigente(empresa)) return; this.items.set([]); this.ready.set(false);
        this.error.set(err.status === 404 ? 'La gestión empresarial de tarifas todavía no está disponible en el servidor.' : err.error?.message ?? 'No se pudieron cargar las tarifas.'); this.loading.set(false); }
    });
  }
  nuevo(): void {
    if (!this.ready() || this.saving() || this.modalRef || !this.tenant.contextoVerificado()) return;
    this.reiniciarEditor(); this.editor.set(true);
    this.abrirModal('editor', this.editorModal, 'l', 'Nuevo costo de envío');
  }
  editar(item: CostoEnvio): void {
    if (!this.ready() || this.saving() || this.modalRef || !this.tenant.contextoVerificado()) return;
    this.reiniciarEditor();
    this.editandoId.set(item.id);
    this.form.reset({ nombre: item.nombre, descripcion: item.descripcion ?? '', departamento: item.departamento ?? '',
      ciudad: item.ciudad ?? '', zona: item.zona ?? '', modalidad: item.modalidad ?? '', monto: item.monto,
      vigenteDesde: this.fechaInput(item.vigenteDesde), vigenteHasta: this.fechaInput(item.vigenteHasta),
      prioridad: item.prioridad, esPredeterminado: item.esPredeterminado, activo: item.activo });
    this.editor.set(true);
    this.abrirModal('editor', this.editorModal, 'l', 'Editar costo de envío');
  }
  cancelar(): void {
    if (this.modalTipo === 'editor' && !this.saving()) this.modalRef?.close();
  }
  private reiniciarEditor(): void {
    this.editor.set(false); this.editandoId.set(null); this.editorError.set(null);
    this.form.reset({ nombre:'', descripcion:'', departamento:'', ciudad:'', zona:'', modalidad:'', monto:null,
      vigenteDesde:'', vigenteHasta:'', prioridad:1, esPredeterminado:false, activo:true });
  }
  private datosNativos(formulario: HTMLFormElement, campos: readonly string[]): FormData | null {
    const datos = new FormData(formulario);
    return campos.every(campo => typeof datos.get(campo) === 'string') ? datos : null;
  }

  guardar(formulario: HTMLFormElement): void {
    const empresa = this.tenant.contextoVerificado();
    if (!empresa || !this.ready() || this.saving() || this.modalTipo !== 'editor' || !this.editor() || !this.modalListo() || this.form.disabled) return;
    // Capturar los valores actuales del formulario enviado, también si el navegador
    // cambió un control nativo sin emitir el evento input al modelo reactivo.
    const datos = this.datosNativos(formulario, ['nombre','descripcion','departamento','ciudad','zona','modalidad','monto','prioridad','vigenteDesde','vigenteHasta']);
    const predeterminado = formulario.elements.namedItem('esPredeterminado');
    const activo = formulario.elements.namedItem('activo');
    if (!datos || !(predeterminado instanceof HTMLInputElement) || predeterminado.type !== 'checkbox'
      || predeterminado.disabled || !(activo instanceof HTMLInputElement) || activo.type !== 'checkbox' || activo.disabled) {
      this.editorError.set('El formulario no está listo. Vuelve a abrirlo antes de guardar.'); return;
    }
    const texto = (campo: string) => String(datos.get(campo));
    const numero = (campo: string) => texto(campo).trim() === '' ? null : Number(texto(campo));
    this.form.setValue({
      nombre: texto('nombre'), descripcion: texto('descripcion'), departamento: texto('departamento'),
      ciudad: texto('ciudad'), zona: texto('zona'), modalidad: texto('modalidad'),
      monto: numero('monto'), prioridad: numero('prioridad'), vigenteDesde: texto('vigenteDesde'),
      vigenteHasta: texto('vigenteHasta'), esPredeterminado: predeterminado.checked, activo: activo.checked
    }, { emitEvent: false });
    if (this.form.invalid) {
      this.form.markAllAsTouched(); this.editorError.set('Revisa los campos de la tarifa.'); return;
    }
    const raw = this.form.getRawValue();
    const monto = raw.monto;
    if (monto === null || !Number.isFinite(monto) || Math.abs(monto * 100 - Math.round(monto * 100)) > 0.000001) {
      this.editorError.set('Indique un monto con hasta dos decimales.'); return;
    }
    if (!raw.nombre?.trim() || (raw.esPredeterminado && (!raw.activo || [raw.departamento,raw.ciudad,raw.zona,raw.modalidad].some(x => x?.trim())))) {
      this.editorError.set('La tarifa requiere un nombre; la predeterminada debe estar activa y tener cobertura general.'); return;
    }
    if (raw.vigenteDesde && raw.vigenteHasta && raw.vigenteDesde > raw.vigenteHasta) {
      this.editorError.set('La fecha final no puede ser anterior a la inicial.'); return;
    }
    const value: GuardarCostoEnvio = {
      nombre:raw.nombre.trim(), descripcion:raw.descripcion?.trim() || undefined, departamento:raw.departamento?.trim() || undefined,
      ciudad:raw.ciudad?.trim() || undefined, zona:raw.zona?.trim() || undefined, modalidad:raw.modalidad?.trim() || undefined,
      monto, prioridad:Number(raw.prioridad), esPredeterminado:raw.esPredeterminado === true, activo:raw.activo === true,
      vigenteDesde:raw.vigenteDesde ? new Date(raw.vigenteDesde).toISOString() : null,
      vigenteHasta:raw.vigenteHasta ? new Date(raw.vigenteHasta).toISOString() : null
    };
    this.saving.set(true); this.editorError.set(null); this.error.set(null); this.notice.set(null);
    const id = this.editandoId();
    (id === null ? this.service.create(value) : this.service.update(id,value)).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { if (!this.vigente(empresa)) return; this.saving.set(false); this.modalRef?.close(); this.notice.set('Tarifa guardada.'); this.cargar(); },
      error: err => { if (!this.vigente(empresa)) return; this.saving.set(false); this.editorError.set(err.error?.message ?? 'No se pudo guardar la tarifa.'); }
    });
  }
  async cambiarEstado(item: CostoEnvio): Promise<void> {
    const empresa = this.tenant.contextoVerificado();
    if (!empresa || !this.ready() || this.saving()) return;
    if (item.activo && item.esPredeterminado) { this.error.set('Asigne otra tarifa predeterminada antes de desactivar esta.'); return; }
    this.saving.set(true); this.error.set(null);
    const accion = item.activo ? 'Desactivar' : 'Activar';
    const confirmado = await this.alerts.confirmar({ titulo: accion + ' costo de envío',
      mensaje: '¿Deseas ' + accion.toLowerCase() + ' «' + item.nombre + '»?', confirmarTexto: accion, cancelarTexto: 'Cancelar' }).catch(() => false);
    if (!this.vigente(empresa)) return;
    if (!confirmado || !this.ready()) { this.saving.set(false); return; }
    this.service.cambiarEstado(item.id,!item.activo).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { if (!this.vigente(empresa)) return; this.saving.set(false); this.notice.set('Estado actualizado.'); this.cargar(); },
      error: err => { if (!this.vigente(empresa)) return; this.saving.set(false); this.error.set(err.error?.message ?? 'No se pudo cambiar el estado.'); }
    });
  }
  async eliminar(item: CostoEnvio): Promise<void> {
    const empresa = this.tenant.contextoVerificado();
    if (!empresa || !this.ready() || this.saving()) return;
    if (item.activo && item.esPredeterminado) { this.error.set('Asigne otra tarifa predeterminada antes de eliminar esta.'); return; }
    this.saving.set(true);
    const confirmado = await this.alerts.confirmar({ titulo:'Eliminar costo de envío', mensaje:`Se eliminará lógicamente “${item.nombre}”.`,
      detalle:'Se conservarán los documentos históricos y el registro de auditoría.', tipo:'peligro', confirmarTexto:'Eliminar', cancelarTexto:'Cancelar' }).catch(() => false);
    if (!this.vigente(empresa)) return;
    if (!confirmado || !this.ready()) { this.saving.set(false); return; }
    this.error.set(null);
    this.service.delete(item.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { if (!this.vigente(empresa)) return; this.saving.set(false); this.notice.set('Tarifa eliminada.'); this.cargar(); },
      error: err => { if (!this.vigente(empresa)) return; this.saving.set(false); this.error.set(err.error?.message ?? 'No se pudo eliminar la tarifa.'); }
    });
  }
  simular(campos: { departamento: string; ciudad: string; zona: string; modalidad: string; fecha: string }): void {
    const empresa=this.tenant.contextoVerificado();
    if (!empresa || !this.ready() || this.modalTipo !== 'simulador' || !this.modalListo() || this.simulador.disabled || this.simulando()) return;
    const request = ++this.simulacionRequest;
    this.simulacion.set(null);
    // Los valores proceden de las referencias explícitas del template del modal activo.
    // No se reconstruyen a partir de FormData, nombres HTML ni asociaciones implícitas.
    this.simulador.setValue(campos, { emitEvent: false });
    if (this.simulador.invalid) {
      this.simulador.markAllAsTouched(); this.simulacionError.set('Revisa los datos de cobertura.'); return;
    }
    const raw=this.simulador.getRawValue();
    this.simulacion.set(null); this.simulacionError.set(null); this.simulando.set(true);
    this.service.resolver({ departamento:raw.departamento?.trim() || undefined, ciudad:raw.ciudad?.trim() || undefined,
      zona:raw.zona?.trim() || undefined, modalidad:raw.modalidad?.trim() || undefined,
      fechaUtc:raw.fecha ? new Date(raw.fecha).toISOString() : undefined }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { if (!this.vigente(empresa) || request !== this.simulacionRequest) return; this.simulacion.set(res.data); this.simulando.set(false); },
      error: err => { if (!this.vigente(empresa) || request !== this.simulacionRequest) return; this.simulando.set(false);
        this.simulacionError.set(err.error?.message ?? 'No se pudo resolver una tarifa.'); }
    });
  }
  verHistorial(item: CostoEnvio, page=1): void {
    const empresa=this.tenant.contextoVerificado();
    if (!empresa || !this.ready() || this.saving() || (this.modalRef && this.modalTipo !== 'historial')) return;
    if (this.modalRef && this.historialItem()?.id !== item.id) return;
    this.historialItem.set(item); this.historial.set(null); this.historialError.set(null); this.historialLoading.set(true);
    if (!this.modalRef) this.abrirModal('historial', this.historialModal, 'l', 'Historial: ' + item.nombre);
    const request = ++this.historialRequest;
    this.service.historial(item.id,page).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { if (!this.vigente(empresa) || this.historialItem()?.id !== item.id || request !== this.historialRequest) return; this.historial.set(res.data); this.historialLoading.set(false); },
      error: err => { if (!this.vigente(empresa) || this.historialItem()?.id !== item.id || request !== this.historialRequest) return; this.historialLoading.set(false); this.historialError.set(err.error?.message ?? 'No se pudo consultar el historial.'); }
    });
  }
  cerrarHistorial(): void { if (this.modalTipo === 'historial') { ++this.historialRequest; this.modalRef?.close(); } }
  cobertura(item: CostoEnvio): string {
    const ubicacion=[item.departamento,item.ciudad,item.zona].filter(Boolean).join(' · ') || 'Cobertura general';
    return item.modalidad ? `${ubicacion} · ${item.modalidad}` : ubicacion;
  }
  private fechaInput(fecha?: string): string {
    if (!fecha) return '';
    const d=new Date(fecha);
    if (!Number.isFinite(d.getTime())) return '';
    return new Date(d.getTime()-d.getTimezoneOffset()*60000).toISOString().slice(0,16);
  }
}
