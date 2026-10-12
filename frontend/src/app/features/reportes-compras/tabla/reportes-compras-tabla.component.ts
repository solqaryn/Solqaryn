import { Component, Input, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule, DecimalPipe, DatePipe } from '@angular/common';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatIconModule } from '@angular/material/icon';
import { ReporteComprasDetalleDto } from '../../../core/models/reporte-compras.models';
import { EventEmitter, Output } from '@angular/core';
import { PagedResult } from '../../../core/models/api-response.model';

@Component({
  selector: 'app-reportes-compras-tabla',
  standalone: true,
  imports: [
    CommonModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatTooltipModule,
    MatIconModule
  ],
  templateUrl: './reportes-compras-tabla.component.html',
  styleUrls: ['./reportes-compras-tabla.component.scss'],
  changeDetection: ChangeDetectionStrategy.Eager,
  providers: [DecimalPipe, DatePipe]
})
export class ReportesComprasTablaComponent {
  @Input() result: PagedResult<ReporteComprasDetalleDto> | null = null;
  @Input() isLoading: boolean = false;
  @Output() pageChanged = new EventEmitter<PageEvent>();

  displayedColumns: string[] = [
    'orden',
    'proveedor',
    'producto',
    'cantidades',
    'precios',
    'desviacion'
  ];
  
  onPageChange(event: PageEvent): void {
    this.pageChanged.emit(event);
  }
}
