
import { Component, OnInit, inject } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { PageEvent } from '@angular/material/paginator';
import { PagedResult } from '../../core/models/api-response.model';
import { ReporteComprasDetalleDto, ReporteComprasFiltroDto } from '../../core/models/reporte-compras.models';
import { ReporteComprasService } from '../../core/services/reporte-compras.service';
import { PurchaseReportDisclosureComponent, PurchaseReportState } from './disclosure/purchase-report-disclosure.component';
import { EstadoSelectorComponent } from './estados/estado-selector.component';
import { ReporteComprasFiltrosComponent } from './filtros/reporte-compras-filtros.component';
import { ReportesComprasTablaComponent } from './tabla/reportes-compras-tabla.component';

@Component({
  selector: 'app-reportes-compras',
  standalone: true,
  imports: [ReactiveFormsModule, ReporteComprasFiltrosComponent, EstadoSelectorComponent, PurchaseReportDisclosureComponent, ReportesComprasTablaComponent],
  templateUrl: './reportes-compras.component.html',
  styleUrls: ['./reportes-compras.component.scss'],
})
export class ReportesComprasComponent implements OnInit {
  private readonly reporteService = inject(ReporteComprasService);

  readonly estadoOrden = new FormControl<any>(null);
  readonly estadoFactura = new FormControl<any>(null);
  readonly estadoRecepcion = new FormControl<any>(null);
  readonly estadoDevolucion = new FormControl<any>(null);

  filtro: ReporteComprasFiltroDto = { page: 1, pageSize: 50 };
  result: PagedResult<ReporteComprasDetalleDto> | null = null;
  state: PurchaseReportState = 'loading';
  errorMessage = '';

  ngOnInit(): void {
    this.cargar();
  }

  aplicarFiltros(filtro: ReporteComprasFiltroDto): void {
    this.filtro = { ...filtro, ...this.estadosSeleccionados(), page: 1 };
    this.cargar();
  }

  aplicarEstados(): void {
    this.filtro = { ...this.filtro, ...this.estadosSeleccionados(), page: 1 };
    this.cargar();
  }

  cambiarPagina(event: PageEvent): void {
    this.filtro = { ...this.filtro, page: event.pageIndex + 1, pageSize: event.pageSize };
    this.cargar();
  }

  private estadosSeleccionados(): Partial<ReporteComprasFiltroDto> {
    return {
      estadoOrden: this.estadoOrden.value ?? undefined,
      estadoFactura: this.estadoFactura.value ?? undefined,
      estadoRecepcion: this.estadoRecepcion.value ?? undefined,
      estadoDevolucion: this.estadoDevolucion.value ?? undefined,
    };
  }

  private cargar(): void {
    this.state = 'loading';
    this.errorMessage = '';
    this.reporteService.getDetalle(this.filtro).subscribe({
      next: response => {
        if (!response.success || !response.data) {
          this.result = null;
          this.state = 'error';
          this.errorMessage = response.message || response.errors?.join(' ') || 'No fue posible cargar el reporte de compras.';
          return;
        }
        this.result = response.data;
        this.state = response.data.items.length === 0 ? 'empty' : 'loaded';
      },
      error: () => {
        this.result = null;
        this.state = 'error';
        this.errorMessage = 'No fue posible cargar el reporte de compras.';
      },
    });
  }
}
