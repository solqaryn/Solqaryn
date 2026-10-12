import { Component, OnInit, inject, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { VentaService } from '../../services/venta.service';
import { Venta } from '../../core/models/venta.model';
import { AnularDialogComponent } from '../../shared/anular-dialog.component';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { ProductoImagenComponent } from '../../shared/producto-imagen/producto-imagen.component';

@Component({
  selector: 'app-venta-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatDialogModule, ProductoImagenComponent],
  templateUrl: './venta-detail.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './venta-detail.component.scss'
})
export class VentaDetailComponent implements OnInit {
  private readonly permisosRuntime = inject(PermisosRuntimeService);

  readonly venta = signal<Venta | null>(null);
  readonly loading = signal(true);
  readonly procesando = signal(false);
  readonly puedeEditar = signal(false);
  readonly puedeConfirmar = signal(false);
  readonly puedeAnular = signal(false);
  readonly puedeEliminar = signal(false);
  readonly puedeVerFactura = signal(false);
  readonly puedeVerUtilidad = signal(false);
  private ventaId!: number;

  constructor(
    private ventaService: VentaService,
    private route: ActivatedRoute,
    private router: Router,
    private dialog: MatDialog
  ) {}

  ngOnInit(): void {
    this.puedeEditar.set(this.permisosRuntime.puede('Ventas', 'Editar'));
    this.puedeConfirmar.set(this.permisosRuntime.puede('Ventas', 'Confirmar'));
    this.puedeAnular.set(this.permisosRuntime.puede('Ventas', 'Anular'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Ventas', 'EliminarLogico'));
    this.puedeVerFactura.set(this.permisosRuntime.puede('Facturacion', 'Ver'));
    this.puedeVerUtilidad.set(
      this.permisosRuntime.esAdministrador() ||
      this.permisosRuntime.puede('Finanzas', 'Administrar')
    );

    this.ventaId = Number(this.route.snapshot.paramMap.get('id'));
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.ventaService.getById(this.ventaId).subscribe({
      next: (res) => { this.venta.set(res.data); this.loading.set(false); },
      error: () => this.loading.set(false)
    });
  }

  confirmar(): void {
    if (!this.puedeConfirmar() || this.procesando()) return;

    this.procesando.set(true);
    this.ventaService.confirmar(this.ventaId).subscribe({
      next: (res) => {
        this.venta.set(res.data);
        this.procesando.set(false);
        if (res.data.facturaId && this.puedeVerFactura()) {
          this.router.navigate(['/facturas', res.data.facturaId]);
        }
      },
      error: () => this.procesando.set(false)
    });
  }

  anular(): void {
    if (!this.puedeAnular() || this.procesando()) return;

    const ref = this.dialog.open(AnularDialogComponent, {
      data: { title: 'Anular venta', message: 'Esta acción revertirá el stock y anulará la factura. Escribe el motivo:' }
    });

    ref.afterClosed().subscribe((motivo: string | undefined) => {
      if (!motivo) return;
      this.procesando.set(true);
      this.ventaService.anular(this.ventaId, motivo).subscribe({
        next: (res) => { this.venta.set(res.data); this.procesando.set(false); },
        error: () => this.procesando.set(false)
      });
    });
  }

  eliminarBorrador(): void {
    if (!this.puedeEliminar() || this.procesando()) return;

    this.procesando.set(true);
    this.ventaService.deleteBorrador(this.ventaId).subscribe({
      next: () => this.router.navigate(['/ventas']),
      error: () => this.procesando.set(false)
    });
  }
}
