
import { Component, OnInit, inject, ChangeDetectionStrategy } from '@angular/core';
import { forkJoin } from 'rxjs';
import { PagedResult } from '../../core/models/api-response.model';
import {
  ReporteVentasDetalleDto,
  ReporteVentasFiltroDto,
  ReporteVentasResumenDto,
} from '../../core/models/reporte-ventas.models';
import { ReporteVentasService } from '../../core/services/reporte-ventas.service';
import { ReporteVentasDetalleComponent } from './detalle/reporte-ventas-detalle.component';
import { ReporteVentasFiltrosComponent } from './filtros/reporte-ventas-filtros.component';
import { ReporteVentasResumenComponent } from './resumen/reporte-ventas-resumen.component';

@Component({
  selector: 'app-reportes-ventas',
  standalone: true,
  imports: [
    ReporteVentasFiltrosComponent,
    ReporteVentasResumenComponent,
    ReporteVentasDetalleComponent
],
  templateUrl: './reportes-ventas.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './reportes-ventas.component.scss',
})
export class ReportesVentasComponent implements OnInit {
  private readonly reporteVentasService = inject(ReporteVentasService);

  filtro: ReporteVentasFiltroDto = {
    page: 1,
    pageSize: 20,
    sortBy: 'Fecha',
    sortDirection: 'desc',
  };
  resumen: ReporteVentasResumenDto | null = null;
  detalle: PagedResult<ReporteVentasDetalleDto> | null = null;
  cargando = false;
  error = '';

  ngOnInit(): void {
    this.cargarReporte();
  }

  aplicarFiltros(filtro: ReporteVentasFiltroDto): void {
    this.filtro = { ...filtro, page: 1 };
    this.cargarReporte();
  }

  cambiarPagina(page: number): void {
    this.filtro = { ...this.filtro, page };
    this.cargarReporte();
  }

  private cargarReporte(): void {
    this.cargando = true;
    this.error = '';

    forkJoin({
      resumen: this.reporteVentasService.getResumen(this.filtro),
      detalle: this.reporteVentasService.getDetalle(this.filtro),
    }).subscribe({
      next: ({ resumen, detalle }) => {
        this.resumen = resumen.success ? resumen.data : null;
        this.detalle = detalle.success ? detalle.data : null;

        if (!resumen.success || !detalle.success) {
          this.error = [
            resumen.success ? '' : resumen.message,
            detalle.success ? '' : detalle.message,
          ].filter(Boolean).join(' ') || 'No fue posible cargar el reporte de ventas.';
        }

        this.cargando = false;
      },
      error: () => {
        this.resumen = null;
        this.detalle = null;
        this.error = 'No fue posible cargar el reporte de ventas.';
        this.cargando = false;
      },
    });
  }
}
