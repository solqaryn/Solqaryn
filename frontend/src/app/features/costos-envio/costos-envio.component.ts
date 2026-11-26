import { ConsultaRegistroDirective } from '../../shared/consulta-registro/consulta-registro';
import { Component, ChangeDetectionStrategy, DestroyRef, computed, effect, inject, signal, untracked } from '@angular/core';
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
    MatFormFieldModule, MatIconModule, MatInputModule, MatProgressSpinnerModule, MatTableModule],
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
    effect(() => {
      const empresa = this.tenant.empresaIdVerificada();
      untracked(() => {
        this.items.set([]); this.ready.set(false); this.error.set(null); this.notice.set(null);
        this.saving.set(false); this.simulando.set(false); this.simulacion.set(null); this.simulacionError.set(null);
        this.historialItem.set(null); this.historial.set(null); this.historialLoading.set(false);
        this.cancelar();
        if (empresa) this.cargar(); else this.loading.set(false);
      });
    });
  }
  private vigente(empresa: number | null): boolean {
    return empresa !== null && this.tenant.empresaIdVerificada() === empresa;
  }
  cargar(): void {
    const empresa = this.tenant.empresaIdVerificada();
    if (!empresa) return;
    this.loading.set(true);
    this.error.set(null);
    this.service.getAdministracion().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { if (!this.vigente(empresa)) return; this.items.set(res.data); this.ready.set(true); this.loading.set(false); },
      error: err => { if (!this.vigente(empresa)) return; this.items.set([]); this.ready.set(false);
        this.error.set(err.status === 404 ? 'La gestión empresarial de tarifas todavía no está disponible en el servidor.' : err.error?.message ?? 'No se pudieron cargar las tarifas.'); this.loading.set(false); }
    });
  }
  nuevo(): void { if (!this.ready() || this.saving()) return; this.cancelar(); this.editor.set(true); }
  editar(item: CostoEnvio): void {
    if (!this.ready() || this.saving()) return;
    this.editandoId.set(item.id);
    this.form.reset({ nombre: item.nombre, descripcion: item.descripcion ?? '', departamento: item.departamento ?? '',
      ciudad: item.ciudad ?? '', zona: item.zona ?? '', modalidad: item.modalidad ?? '', monto: item.monto,
      vigenteDesde: this.fechaInput(item.vigenteDesde), vigenteHasta: this.fechaInput(item.vigenteHasta),
      prioridad: item.prioridad, esPredeterminado: item.esPredeterminado, activo: item.activo });
    this.editor.set(true);
  }
  cancelar(): void {
    this.editor.set(false); this.editandoId.set(null);
    this.form.reset({ nombre:'', descripcion:'', departamento:'', ciudad:'', zona:'', modalidad:'', monto:null,
      vigenteDesde:'', vigenteHasta:'', prioridad:1, esPredeterminado:false, activo:true });
  }
  guardar(): void {
    const empresa = this.tenant.empresaIdVerificada();
    if (!empresa || !this.ready() || this.saving()) return;
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    const raw = this.form.getRawValue();
    const monto = raw.monto;
    if (monto === null || !Number.isFinite(monto) || Math.abs(monto * 100 - Math.round(monto * 100)) > 0.000001) {
      this.error.set('Indique un monto con hasta dos decimales.'); return;
    }
    if (!raw.nombre?.trim() || (raw.esPredeterminado && (!raw.activo || [raw.departamento,raw.ciudad,raw.zona,raw.modalidad].some(x => x?.trim())))) {
      this.error.set('La tarifa requiere un nombre; la predeterminada debe estar activa y tener cobertura general.'); return;
    }
    if (raw.vigenteDesde && raw.vigenteHasta && raw.vigenteDesde > raw.vigenteHasta) {
      this.error.set('La fecha final no puede ser anterior a la inicial.'); return;
    }
    const value: GuardarCostoEnvio = {
      nombre:raw.nombre.trim(), descripcion:raw.descripcion?.trim() || undefined, departamento:raw.departamento?.trim() || undefined,
      ciudad:raw.ciudad?.trim() || undefined, zona:raw.zona?.trim() || undefined, modalidad:raw.modalidad?.trim() || undefined,
      monto, prioridad:Number(raw.prioridad), esPredeterminado:raw.esPredeterminado === true, activo:raw.activo === true,
      vigenteDesde:raw.vigenteDesde ? new Date(raw.vigenteDesde).toISOString() : null,
      vigenteHasta:raw.vigenteHasta ? new Date(raw.vigenteHasta).toISOString() : null
    };
    this.saving.set(true); this.error.set(null); this.notice.set(null);
    const id = this.editandoId();
    (id === null ? this.service.create(value) : this.service.update(id,value)).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { if (!this.vigente(empresa)) return; this.saving.set(false); this.cancelar(); this.notice.set('Tarifa guardada.'); this.cargar(); },
      error: err => { if (!this.vigente(empresa)) return; this.saving.set(false); this.error.set(err.error?.message ?? 'No se pudo guardar la tarifa.'); }
    });
  }
  cambiarEstado(item: CostoEnvio): void {
    const empresa = this.tenant.empresaIdVerificada();
    if (!empresa || !this.ready() || this.saving()) return;
    if (item.activo && item.esPredeterminado) { this.error.set('Asigne otra tarifa predeterminada antes de desactivar esta.'); return; }
    this.saving.set(true); this.error.set(null);
    this.service.cambiarEstado(item.id,!item.activo).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { if (!this.vigente(empresa)) return; this.saving.set(false); this.notice.set('Estado actualizado.'); this.cargar(); },
      error: err => { if (!this.vigente(empresa)) return; this.saving.set(false); this.error.set(err.error?.message ?? 'No se pudo cambiar el estado.'); }
    });
  }
  async eliminar(item: CostoEnvio): Promise<void> {
    const empresa = this.tenant.empresaIdVerificada();
    if (!empresa || !this.ready() || this.saving()) return;
    if (item.activo && item.esPredeterminado) { this.error.set('Asigne otra tarifa predeterminada antes de eliminar esta.'); return; }
    const confirmado = await this.alerts.confirmar({ titulo:'Eliminar costo de envío', mensaje:`Se eliminará lógicamente “${item.nombre}”.`,
      detalle:'Se conservarán los documentos históricos y el registro de auditoría.', tipo:'peligro', confirmarTexto:'Eliminar', cancelarTexto:'Cancelar' });
    if (!confirmado || !this.vigente(empresa) || !this.ready() || this.saving()) return;
    this.saving.set(true); this.error.set(null);
    this.service.delete(item.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { if (!this.vigente(empresa)) return; this.saving.set(false); this.notice.set('Tarifa eliminada.'); this.cargar(); },
      error: err => { if (!this.vigente(empresa)) return; this.saving.set(false); this.error.set(err.error?.message ?? 'No se pudo eliminar la tarifa.'); }
    });
  }
  simular(): void {
    const empresa=this.tenant.empresaIdVerificada();
    if (!empresa || !this.ready() || this.simulador.invalid || this.simulando()) return;
    const raw=this.simulador.getRawValue();
    this.simulacion.set(null); this.simulacionError.set(null); this.simulando.set(true);
    this.service.resolver({ departamento:raw.departamento?.trim() || undefined, ciudad:raw.ciudad?.trim() || undefined,
      zona:raw.zona?.trim() || undefined, modalidad:raw.modalidad?.trim() || undefined,
      fechaUtc:raw.fecha ? new Date(raw.fecha).toISOString() : undefined }).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { if (!this.vigente(empresa)) return; this.simulacion.set(res.data); this.simulando.set(false); },
      error: err => { if (!this.vigente(empresa)) return; this.simulando.set(false);
        this.simulacionError.set(err.error?.message ?? 'No se pudo resolver una tarifa.'); }
    });
  }
  verHistorial(item: CostoEnvio, page=1): void {
    const empresa=this.tenant.empresaIdVerificada();
    if (!empresa || !this.ready()) return;
    this.historialItem.set(item); this.historial.set(null); this.historialError.set(null); this.historialLoading.set(true);
    this.service.historial(item.id,page).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: res => { if (!this.vigente(empresa) || this.historialItem()?.id !== item.id) return; this.historial.set(res.data); this.historialLoading.set(false); },
      error: err => { if (!this.vigente(empresa) || this.historialItem()?.id !== item.id) return; this.historialLoading.set(false); this.historialError.set(err.error?.message ?? 'No se pudo consultar el historial.'); }
    });
  }
  cerrarHistorial(): void { this.historialItem.set(null); this.historial.set(null); this.historialLoading.set(false); }
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
