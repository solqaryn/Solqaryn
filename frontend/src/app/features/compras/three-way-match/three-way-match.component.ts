import { Component, OnInit } from '@angular/core';

import { ActivatedRoute, RouterModule } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatChipsModule } from '@angular/material/chips';
import { ThreeWayMatchService } from '../../../services/three-way-match.service';
import { ThreeWayMatchResultDto, ThreeWayMatchStatus, ThreeWayMatchDiscrepancyType } from '../../../core/models/three-way-match.model';
import { finalize } from 'rxjs/operators';

@Component({
  selector: 'app-three-way-match',
  standalone: true,
  imports: [
    RouterModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatIconModule,
    MatButtonModule,
    MatTableModule,
    MatChipsModule
],
  templateUrl: './three-way-match.component.html',
  styleUrls: ['./three-way-match.component.scss']
})
export class ThreeWayMatchComponent implements OnInit {
  ordenCompraId: number | null = null;
  loading = false;
  error: string | null = null;
  result: ThreeWayMatchResultDto | null = null;

  displayedColumns: string[] = ['tipo', 'esperado', 'recibidoFacturado', 'mensaje'];

  ThreeWayMatchStatus = ThreeWayMatchStatus;
  ThreeWayMatchDiscrepancyType = ThreeWayMatchDiscrepancyType;

  constructor(
    private route: ActivatedRoute,
    private threeWayMatchService: ThreeWayMatchService
  ) {}

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    if (idParam) {
      this.ordenCompraId = parseInt(idParam, 10);
      this.loadMatchResult();
    } else {
      this.error = 'ID de Orden de Compra no proporcionado';
    }
  }

  loadMatchResult(): void {
    if (!this.ordenCompraId) return;

    this.loading = true;
    this.error = null;
    this.result = null;

    this.threeWayMatchService.getThreeWayMatchResult(this.ordenCompraId)
      .pipe(finalize(() => this.loading = false))
      .subscribe({
        next: (response) => {
          if (response.success && response.data) {
            this.result = response.data;
          } else {
            this.error = response.message || 'Error al cargar el resultado de conciliación';
          }
        },
        error: (err) => {
          console.error('Error fetching three-way match', err);
          this.error = err.error?.message || 'Error de conexión al cargar la conciliación';
        }
      });
  }

  getStatusClass(status: ThreeWayMatchStatus): string {
    switch (status) {
      case ThreeWayMatchStatus.Pendiente: return 'status-pendiente';
      case ThreeWayMatchStatus.Aprobado: return 'status-aprobado';
      case ThreeWayMatchStatus.Discrepancia: return 'status-discrepancia';
      default: return '';
    }
  }

  getStatusLabel(status: ThreeWayMatchStatus): string {
    switch (status) {
      case ThreeWayMatchStatus.Pendiente: return 'Pendiente';
      case ThreeWayMatchStatus.Aprobado: return 'Aprobado';
      case ThreeWayMatchStatus.Discrepancia: return 'Discrepancia';
      default: return 'Desconocido';
    }
  }

  getDiscrepancyTypeLabel(type: ThreeWayMatchDiscrepancyType): string {
    switch (type) {
      case ThreeWayMatchDiscrepancyType.Cantidad: return 'Cantidad';
      case ThreeWayMatchDiscrepancyType.Precio: return 'Precio';
      case ThreeWayMatchDiscrepancyType.Descuento: return 'Descuento';
      case ThreeWayMatchDiscrepancyType.Impuesto: return 'Impuesto';
      case ThreeWayMatchDiscrepancyType.Moneda: return 'Moneda';
      default: return 'Desconocido';
    }
  }
}
