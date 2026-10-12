import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin } from 'rxjs';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import {
  AuditoriaResumen,
  ResumenAdministrativo,
  RolPermisosReporte,
  UsuarioAccesoReporte
} from '../../core/models/reporte-administrativo.model';
import { ReporteAdministrativoService } from '../../services/reporte-administrativo.service';

@Component({
  selector: 'app-reportes-administrativos',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatButtonModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTableModule,
    MatTabsModule,
    MatTooltipModule
  ],
  templateUrl: './reportes-administrativos.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './reportes-administrativos.component.scss'
})
export class ReportesAdministrativosComponent implements OnInit {
  private readonly service = inject(ReporteAdministrativoService);
  private latestLoadId = 0;

  readonly loading = signal(true);
  readonly exporting = signal<string | null>(null);
  readonly error = signal<string | null>(null);
  readonly resumen = signal<ResumenAdministrativo | null>(null);
  readonly usuarios = signal<UsuarioAccesoReporte[]>([]);
  readonly roles = signal<RolPermisosReporte[]>([]);
  readonly auditoria = signal<AuditoriaResumen | null>(null);
  readonly rolExpandido = signal<number | null>(null);

  desde = this.fechaInput(new Date(Date.now() - 29 * 24 * 60 * 60 * 1000));
  hasta = this.fechaInput(new Date());
  filtroUsuarios = '';
  filtroRoles = '';

  readonly columnasUsuarios = ['usuario', 'rol', 'estado', 'permisos', 'sensibles'];
  readonly columnasRoles = ['rol', 'usuarios', 'permisos', 'cobertura', 'privilegio', 'estado', 'detalle'];
  readonly columnasActividad = ['modulo', 'total', 'exitosos', 'rechazados', 'errores'];

  ngOnInit(): void {
    this.cargar();
  }

  cargar(): void {
    if (!this.periodoValido()) return;

    const loadId = ++this.latestLoadId;
    const requestedDesde = this.desde;
    const requestedHasta = this.hasta;

    this.loading.set(true);
    this.error.set(null);

    forkJoin({
      resumen: this.service.getResumen(requestedDesde, requestedHasta),
      usuarios: this.service.getUsuariosAccesos(),
      roles: this.service.getRolesPermisos(),
      auditoria: this.service.getAuditoriaResumen(requestedDesde, requestedHasta)
    }).subscribe({
      next: ({ resumen, usuarios, roles, auditoria }) => {
        if (loadId !== this.latestLoadId) return;
        if (requestedDesde !== this.desde || requestedHasta !== this.hasta) {
          this.cargar();
          return;
        }

        this.resumen.set(resumen.data);
        this.usuarios.set(usuarios.data);
        this.roles.set(roles.data);
        this.auditoria.set(auditoria.data);
        this.loading.set(false);
      },
      error: (err) => {
        if (loadId !== this.latestLoadId) return;
        if (requestedDesde !== this.desde || requestedHasta !== this.hasta) {
          this.cargar();
          return;
        }

        this.error.set(err.error?.message ?? 'No fue posible cargar los reportes administrativos.');
        this.loading.set(false);
      }
    });
  }

  usuariosFiltrados(): UsuarioAccesoReporte[] {
    const termino = this.filtroUsuarios.trim().toLocaleLowerCase();
    if (!termino) return this.usuarios();
    return this.usuarios().filter(item =>
      item.nombreUsuario.toLocaleLowerCase().includes(termino) ||
      item.nombreCompleto.toLocaleLowerCase().includes(termino) ||
      item.rol.toLocaleLowerCase().includes(termino) ||
      item.estadoAcceso.toLocaleLowerCase().includes(termino)
    );
  }

  rolesFiltrados(): RolPermisosReporte[] {
    const termino = this.filtroRoles.trim().toLocaleLowerCase();
    if (!termino) return this.roles();
    return this.roles().filter(item =>
      item.rol.toLocaleLowerCase().includes(termino) ||
      item.nivelPrivilegio.toLocaleLowerCase().includes(termino) ||
      item.estadoConfiguracion.toLocaleLowerCase().includes(termino)
    );
  }

  toggleRol(id: number): void {
    this.rolExpandido.set(this.rolExpandido() === id ? null : id);
  }

  exportar(tipo: 'usuarios' | 'roles' | 'auditoria', formato: 'csv' | 'xlsx' | 'pdf'): void {
    if (!this.periodoValido()) return;
    const clave = `${tipo}-${formato}`;
    this.exporting.set(clave);
    this.error.set(null);

    this.service.exportar(tipo, formato, this.desde, this.hasta).subscribe({
      next: response => {
        const blob = response.body;
        if (!blob) {
          this.error.set('No se recibió contenido en la exportación.');
          this.exporting.set(null);
          return;
        }

        const url = URL.createObjectURL(blob);
        const enlace = document.createElement('a');
        enlace.href = url;

        let filename = `${tipo}-${this.desde}-${this.hasta}.${formato}`;
        const contentDisposition = response.headers.get('Content-Disposition');
        if (contentDisposition) {
          const match = contentDisposition.match(/filename="?([^"]+)"?/);
          if (match && match[1]) {
            filename = match[1];
          }
        }

        enlace.download = filename;
        enlace.click();
        URL.revokeObjectURL(url);
        this.exporting.set(null);
      },
      error: (err) => {
        this.error.set(err.error?.message ?? 'No fue posible exportar el reporte.');
        this.exporting.set(null);
      }
    });
  }

  porcentaje(valor: number, total: number): number {
    if (total <= 0) return 0;
    return Math.round(valor * 100 / total);
  }

  claseEstado(valor: string): string {
    const normalizado = valor.toLocaleLowerCase();
    if (normalizado.includes('habilitado') || normalizado.includes('configurado') || normalizado === 'bajo') return 'estado-ok';
    if (normalizado.includes('medio') || normalizado.includes('sin usuarios') || normalizado.includes('informativa')) return 'estado-warn';
    return 'estado-danger';
  }

  private periodoValido(): boolean {
    if (!this.desde || !this.hasta) {
      this.error.set('Debes seleccionar las fechas desde y hasta.');
      return false;
    }
    if (this.desde > this.hasta) {
      this.error.set('La fecha desde no puede ser posterior a la fecha hasta.');
      return false;
    }
    return true;
  }

  private fechaInput(fecha: Date): string {
    const year = fecha.getFullYear();
    const month = String(fecha.getMonth() + 1).padStart(2, '0');
    const day = String(fecha.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }
}
