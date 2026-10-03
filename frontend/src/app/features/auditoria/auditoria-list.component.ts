import { Component, OnDestroy, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatInputModule } from '@angular/material/input';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuditoriaService } from '../../services/auditoria.service';
import { RegistroAuditoria } from '../../core/models/auditoria.model';
import { ReportesAdministrativosComponent } from '../reportes-administrativos/reportes-administrativos.component';

@Component({
  selector: 'app-auditoria-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule, MatFormFieldModule, MatSelectModule, MatInputModule,
    MatIconModule, MatButtonModule, MatPaginatorModule, MatProgressSpinnerModule,
    ReportesAdministrativosComponent
  ],
  templateUrl: './auditoria-list.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './auditoria-list.component.scss'
})
export class AuditoriaListComponent implements OnInit, OnDestroy {
  readonly registros = signal<RegistroAuditoria[]>([]);
  readonly loading = signal(true);
  readonly totalCount = signal(0);
  readonly expandidoId = signal<number | null>(null);

  page = 1;
  pageSize = 25;
  filtroModulo = '';
  filtroAccion = '';
  filtroResultado = '';
  filtroTexto = '';
  filtroDesde = '';
  filtroHasta = '';
  private filtroTimer?: ReturnType<typeof setTimeout>;

  readonly modulos = [
    'Dashboard', 'Productos', 'Categorias', 'Compras', 'Ventas', 'Facturacion', 'Finanzas',
    'Inventario', 'MovimientosInventario', 'Proveedores', 'Clientes', 'Usuarios', 'Roles',
    'Permisos', 'Auditoria', 'ReportesAdministrativos', 'Configuracion', 'Descuentos', 'Impuestos',
    'Colores', 'Tallas', 'Marcas', 'Modelos', 'CargasMasivas'
  ];
  readonly acciones = [
    'Ver', 'Crear', 'Editar', 'Actualizar', 'Activar', 'Desactivar', 'EliminarLogico',
    'EliminarPermanente', 'Confirmar', 'Anular', 'Aprobar', 'Rechazar', 'Exportar', 'Imprimir',
    'Administrar', 'AsignarRol', 'RestablecerContrasena', 'CambiarEstado', 'ConsultarHistorial',
    'Aplicar', 'Duplicar', 'Compartir'
  ];

  constructor(private auditoriaService: AuditoriaService) {}

  ngOnInit(): void {
    this.cargar();
  }

  ngOnDestroy(): void {
    if (this.filtroTimer) clearTimeout(this.filtroTimer);
  }

  cargar(): void {
    this.loading.set(true);
    this.auditoriaService.getFiltered({
      page: this.page,
      pageSize: this.pageSize,
      modulo: this.filtroModulo || undefined,
      accion: this.filtroAccion || undefined,
      resultado: this.filtroResultado || undefined,
      texto: this.filtroTexto || undefined,
      desde: this.filtroDesde || undefined,
      hasta: this.filtroHasta || undefined
    }).subscribe({
      next: (res) => {
        this.registros.set(res.data.items);
        this.totalCount.set(res.data.totalCount);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  aplicarFiltros(): void {
    this.page = 1;
    this.cargar();
  }

  programarFiltro(): void {
    if (this.filtroTimer) clearTimeout(this.filtroTimer);
    this.filtroTimer = setTimeout(() => this.aplicarFiltros(), 300);
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.cargar();
  }

  toggleDetalle(id: number): void {
    this.expandidoId.set(this.expandidoId() === id ? null : id);
  }

  formatearJson(json: string | null | undefined): string {
    if (!json) return '—';
    try {
      return JSON.stringify(JSON.parse(json), null, 2);
    } catch {
      return json;
    }
  }
}
