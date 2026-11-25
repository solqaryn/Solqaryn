import { ConsultaRegistroDirective } from '../../shared/consulta-registro/consulta-registro';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { Component, OnInit, signal, computed, ChangeDetectionStrategy } from '@angular/core';

import { FormsModule } from '@angular/forms';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PermisoService } from '../../services/permiso.service';
import { RolService } from '../../services/rol.service';
import { PermisoMatrizItem, PermisoCatalogo } from '../../core/models/permiso.model';
import { Rol } from '../../core/models/rol.model';

@Component({
  selector: 'app-permisos-matrix',
  standalone: true,
  imports: [ConsultaRegistroDirective,
    FormsModule,
    MatCheckboxModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatIconModule
],
  templateUrl: './permisos-matrix.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './permisos-matrix.component.scss'
})
export class PermisosMatrixComponent implements OnInit {
  readonly roles = signal<Rol[]>([]);
  readonly catalogo = signal<PermisoCatalogo[]>([]);
  readonly loadingCatalogo = signal(false);
  readonly errorCatalogo = signal('');
  readonly errorRoles = signal('');
  readonly errorMatriz = signal('');
  readonly filtroCatalogo = signal('');
  readonly catalogoFiltrado = computed(() => {
    const filtro = this.filtroCatalogo().trim().toLocaleLowerCase('es');
    return this.catalogo().filter(item => !filtro || [item.codigo, item.nombre, item.modulo, item.accion, item.descripcion ?? ''].some(valor => valor.toLocaleLowerCase('es').includes(filtro)));
  });
  readonly permiteGuardar = computed(() => this.rolSeleccionado()?.activo === true && !this.loadingMatriz() && !this.errorMatriz() && this.items().length > 0 && !this.saving());
  readonly rolSeleccionadoId = signal<number | null>(null);
  readonly items = signal<PermisoMatrizItem[]>([]);
  readonly loadingRoles = signal(true);
  readonly loadingMatriz = signal(false);
  readonly saving = signal(false);
  readonly filtroModulo = signal<string>('');

  readonly modulos = computed(() => [...new Set(this.items().map((i) => i.modulo))]);
  readonly rolSeleccionado = computed(() => this.roles().find((r) => r.id === this.rolSeleccionadoId()) ?? null);

  private readonly etiquetasModulos: Record<string, string> = {
    Categorias: 'Categorías',
    Facturacion: 'Facturación',
    MovimientosInventario: 'Movimientos de inventario',
    Auditoria: 'Auditoría',
    Configuracion: 'Configuración'
  };

  private readonly etiquetasAcciones: Record<string, string> = {
    Ver: 'Ver',
    Crear: 'Crear',
    Editar: 'Editar',
    Actualizar: 'Actualizar',
    Activar: 'Activar',
    Desactivar: 'Desactivar',
    Confirmar: 'Confirmar',
    Anular: 'Anular',
    Exportar: 'Exportar',
    Imprimir: 'Imprimir',
    Compartir: 'Compartir',
    Administrar: 'Administrar',
    Aplicar: 'Aplicar',
    Duplicar: 'Duplicar',
    EliminarLogico: 'Eliminar lógicamente',
    EliminarPermanente: 'Eliminar permanentemente',
    ConsultarHistorial: 'Consultar historial',
    AsignarRol: 'Asignar rol',
    RestablecerContrasena: 'Restablecer contraseña',
    CambiarEstado: 'Cambiar estado'
  };

  constructor(
    private permisoService: PermisoService,
    private rolService: RolService,
    private snackBar: MatSnackBar,
    private permisosRuntime: PermisosRuntimeService
  ) {}

  ngOnInit(): void {
    this.cargarCatalogo();
    this.loadingRoles.set(true);
    this.errorRoles.set('');
    this.rolService.getAll().subscribe({
      next: (res) => {
        if (!res.success) { this.errorRoles.set(res.message || 'No fue posible cargar los roles.'); this.loadingRoles.set(false); return; }
        const seleccionables = (res.data ?? []).filter((r) => !r.esAdministrador);
        this.roles.set(seleccionables);
        this.loadingRoles.set(false);
        if (seleccionables.length > 0) {
          this.seleccionarRol(seleccionables[0].id);
        }
      },
      error: () => { this.errorRoles.set('No fue posible cargar los roles.'); this.loadingRoles.set(false); }
    });
  }

  cargarCatalogo(): void {
    this.catalogo.set([]);
    this.errorCatalogo.set('');
    if (!this.permisosRuntime.puede('Permisos', 'Ver')) {
      this.errorCatalogo.set('Tu rol no tiene autorización para consultar el catálogo de permisos.');
      return;
    }
    this.loadingCatalogo.set(true);
    this.permisoService.getCatalogo().subscribe({
      next: res => {
        if (res.success) this.catalogo.set(res.data ?? []);
        else this.errorCatalogo.set(res.message || 'No fue posible consultar el catálogo.');
        this.loadingCatalogo.set(false);
      },
      error: () => { this.errorCatalogo.set('No fue posible consultar el catálogo.'); this.loadingCatalogo.set(false); }
    });
  }

  seleccionarRol(rolId: number): void {
    if (this.saving() || !this.roles().some(r => r.id === rolId)) return;
    this.rolSeleccionadoId.set(rolId);
    this.cargarMatriz(rolId);
  }

  cargarMatriz(rolId: number): void {
    this.items.set([]);
    this.errorMatriz.set('');
    this.loadingMatriz.set(true);
    this.permisoService.getMatriz(rolId).subscribe({
      next: (res) => {
        if (this.rolSeleccionadoId() !== rolId) return;
        if (res.success) this.items.set(res.data ?? []);
        else this.errorMatriz.set(res.message || 'No fue posible cargar los permisos del rol.');
        this.loadingMatriz.set(false);
      },
      error: () => { if (this.rolSeleccionadoId() === rolId) { this.errorMatriz.set('No fue posible cargar los permisos del rol.'); this.loadingMatriz.set(false); } }
    });
  }

  accionesDelModulo(modulo: string): PermisoMatrizItem[] {
    return this.items().filter((i) => i.modulo === modulo);
  }

  modulosFiltrados(): string[] {
    const filtro = this.filtroModulo().trim().toLowerCase();
    if (!filtro) return this.modulos();
    return this.modulos().filter((modulo) =>
      modulo.toLowerCase().includes(filtro) ||
      this.nombreModulo(modulo).toLowerCase().includes(filtro));
  }

  nombreModulo(modulo: string): string {
    return this.etiquetasModulos[modulo] ?? this.separarPalabras(modulo);
  }

  nombreAccion(accion: string): string {
    return this.etiquetasAcciones[accion] ?? this.separarPalabras(accion);
  }

  toggle(item: PermisoMatrizItem): void {
    if (!this.permiteGuardar()) return;
    this.items.set(this.items().map((i) => (i === item ? { ...i, permitido: !i.permitido } : i)));
  }

  guardar(): void {
    const rolId = this.rolSeleccionadoId();
    if (!rolId || !this.permiteGuardar()) return;

    this.saving.set(true);
    this.permisoService.updateMatriz(rolId, this.items()).subscribe({
      next: (res) => {
        if (!res.success) { this.saving.set(false); this.snackBar.open(res.message || 'No se pudo actualizar la matriz.', 'Cerrar', { duration: 5000 }); return; }
        this.items.set(res.data ?? []);
        this.saving.set(false);
        this.snackBar.open(
          'Matriz actualizada. Las siguientes solicitudes de los usuarios con este rol usarán estos permisos.',
          'Cerrar', { duration: 6000 });
      },
      error: (err) => {
        this.saving.set(false);
        this.snackBar.open(err.error?.message ?? 'No se pudo actualizar la matriz.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  private separarPalabras(valor: string): string {
    return valor.replace(/([a-záéíóúñ])([A-ZÁÉÍÓÚÑ])/g, '$1 $2');
  }
}
