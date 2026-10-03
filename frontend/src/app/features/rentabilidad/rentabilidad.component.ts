
import { Component, OnInit, inject } from '@angular/core';
import { ReporteVentasFiltroDto } from '../../core/models/reporte-ventas.models';
import { RentabilidadAgrupacion, ReporteRentabilidadDto } from '../../core/models/reporte-rentabilidad.models';
import { ReporteRentabilidadService } from '../../core/services/reporte-rentabilidad.service';
import { RentabilidadAgrupacionComponent } from './agrupacion/rentabilidad-agrupacion.component';
import { RentabilidadDisclosureComponent } from './disclosure/rentabilidad-disclosure.component';
import { RentabilidadFiltrosComponent } from './filtros/rentabilidad-filtros.component';
import { RentabilidadResultadosTablaComponent } from './tabla/rentabilidad-resultados-tabla/rentabilidad-resultados-tabla.component';

@Component({
  selector: 'app-rentabilidad',
  standalone: true,
  imports: [
    RentabilidadAgrupacionComponent,
    RentabilidadDisclosureComponent,
    RentabilidadFiltrosComponent,
    RentabilidadResultadosTablaComponent
],
  templateUrl: './rentabilidad.component.html',
  styleUrl: './rentabilidad.component.scss',
})
export class RentabilidadComponent implements OnInit {
  private readonly reporteRentabilidadService = inject(ReporteRentabilidadService);

  agrupacion: RentabilidadAgrupacion = 'vendedor';
  filtro: ReporteVentasFiltroDto = { page: 1, pageSize: 20 };
  resultados: readonly ReporteRentabilidadDto[] = [];
  cargando = false;
  error = '';

  ngOnInit(): void {
    this.cargar();
  }

  aplicarFiltros(filtro: ReporteVentasFiltroDto): void {
    this.filtro = { ...filtro, page: 1 };
    this.cargar();
  }

  cambiarAgrupacion(agrupacion: RentabilidadAgrupacion): void {
    if (this.agrupacion === agrupacion) return;
    this.agrupacion = agrupacion;
    this.cargar();
  }

  private cargar(): void {
    this.cargando = true;
    this.error = '';

    this.reporteRentabilidadService.obtener(this.agrupacion, this.filtro).subscribe({
      next: response => {
        this.resultados = response.success ? response.data : [];
        this.error = response.success ? '' : (response.message || 'No fue posible cargar el reporte de rentabilidad.');
        this.cargando = false;
      },
      error: () => {
        this.resultados = [];
        this.error = 'No fue posible cargar el reporte de rentabilidad.';
        this.cargando = false;
      },
    });
  }
}
