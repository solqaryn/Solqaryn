
import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
@Component({selector:'app-centro-reportes',standalone:true,imports: [RouterLink, RouterLinkActive, RouterOutlet],templateUrl:'./centro-reportes.component.html',changeDetection: ChangeDetectionStrategy.Eager,
styleUrl:'./centro-reportes.component.scss'})
export class CentroReportesComponent implements OnInit {
  readonly permisosRuntime = inject(PermisosRuntimeService);

  ngOnInit(): void {
    // Esta ruta sólo usa authGuard. Rehidratar aquí evita que un reload completo
    // evalúe el menú con el set vacío, sin convertir el estado local en autoridad.
    this.permisosRuntime.cargar().subscribe();
  }

  puedeVerFinancieros(): boolean {
    return this.permisosRuntime.puede('Finanzas', 'Ver');
  }

  puedeVerAdministrativos(): boolean {
    return this.permisosRuntime.esAdministrador() &&
      this.permisosRuntime.puede('ReportesAdministrativos', 'Ver');
  }

  puedeVerInventario(): boolean {
    return this.permisosRuntime.puede('Inventario', 'Ver');
  }

  puedeVerKardex(): boolean {
    return this.permisosRuntime.puede('MovimientosInventario', 'ConsultarHistorial');
  }

  tieneReportesDisponibles(): boolean {
    return this.puedeVerFinancieros() ||
      this.puedeVerAdministrativos() ||
      this.puedeVerInventario() ||
      this.puedeVerKardex();
  }
}
