import { crearEditorInstitucional, crearAccionesInstitucionales } from '../../shared/modal-institucional/acciones-institucionales';
import { modalInstitucional } from '../../shared/modal-institucional/modal-institucional';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { firstValueFrom } from 'rxjs';
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
  readonly editor = crearEditorInstitucional('Ventas');
  private readonly acciones = crearAccionesInstitucionales('Ventas');
  readonly errorOperacion = signal<string | null>(null);
  private modificado = false;

  cerrarModal(): void { if (!this.procesando()) this.editor.cerrar(this.modificado); }

  async editarBorrador(): Promise<void> {
    if (!this.editor.valido('Editar') || this.venta()?.estado !== 'Borrador' || this.procesando()) return;
    if (await this.acciones.abrir(() => import('./venta-form.component').then(m => m.VentaFormComponent), 'xl', 'Editar', this.ventaId)) {
      this.modificado = true; this.cargar();
    }
  }

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

    this.ventaId = Number(this.editor.ref ? this.editor.id : this.route.snapshot.paramMap.get('id'));
    if (!this.editor.valido('Ver') || !Number.isInteger(this.ventaId) || this.ventaId <= 0) {
      this.editor.cerrar(false); this.loading.set(false); this.errorOperacion.set('No se pudo autorizar la consulta.'); return;
    }
    this.cargar();
  }

  cargar(): void {
    this.loading.set(true);
    this.ventaService.getById(this.ventaId).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: (res) => { this.venta.set(res.data); this.loading.set(false); },
      error: err => { this.loading.set(false); this.errorOperacion.set(err.error?.message ?? 'No se pudo consultar el registro.'); }
    });
  }

  async confirmar(): Promise<void> {
    if (!this.puedeConfirmar() || this.procesando() || this.venta()?.estado !== 'Borrador') return;
    this.procesando.set(true); this.errorOperacion.set(null);
    if (!await this.editor.confirmar('Confirmar', { titulo: 'Confirmar venta', mensaje: '¿Deseas confirmar esta venta? Se aplicarán las reglas de inventario y facturación vigentes.', tipo: 'advertencia', confirmarTexto: 'Confirmar' }) || this.venta()?.estado !== 'Borrador') { this.procesando.set(false); return; }
    this.ventaService.confirmar(this.ventaId).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: res => {
        this.venta.set(res.data); this.modificado = true; this.procesando.set(false);
        if (!this.editor.ref && res.data.facturaId && this.puedeVerFactura()) void this.router.navigate(['/facturas', res.data.facturaId]);
      },
      error: err => { this.procesando.set(false); this.errorOperacion.set(err.error?.message ?? 'No se pudo confirmar la venta.'); }
    });
  }

  async anular(): Promise<void> {
    if (!this.puedeAnular() || !this.editor.valido('Anular') || this.procesando() || this.venta()?.estado !== 'Confirmada') return;
    this.procesando.set(true); this.errorOperacion.set(null);
    let motivo: string | undefined;
    try {
      motivo = await firstValueFrom(this.dialog.open(AnularDialogComponent, modalInstitucional('s', {
        data: { title: 'Anular venta', message: 'Esta acción revertirá el stock y anulará la factura. Escribe el motivo:' }
      })).afterClosed().pipe(takeUntilDestroyed(this.editor.destroyRef)));
    } catch { motivo = undefined; }
    if (!motivo?.trim() || !this.editor.valido('Anular') || this.venta()?.estado !== 'Confirmada') { this.procesando.set(false); return; }
    this.ventaService.anular(this.ventaId, motivo.trim()).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: res => { this.venta.set(res.data); this.modificado = true; this.procesando.set(false); },
      error: err => { this.procesando.set(false); this.errorOperacion.set(err.error?.message ?? 'No se pudo anular la venta.'); }
    });
  }

  async eliminarBorrador(): Promise<void> {
    if (!this.puedeEliminar() || this.procesando() || this.venta()?.estado !== 'Borrador') return;
    this.procesando.set(true); this.errorOperacion.set(null);
    if (!await this.editor.confirmar('EliminarLogico', { titulo: 'Eliminar borrador', mensaje: '¿Deseas eliminar este borrador de venta?', tipo: 'peligro', confirmarTexto: 'Eliminar' }) || this.venta()?.estado !== 'Borrador') { this.procesando.set(false); return; }
    this.ventaService.deleteBorrador(this.ventaId).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: () => { this.modificado = true; this.procesando.set(false); if (this.editor.ref) this.editor.cerrar(true); else void this.router.navigate(['/ventas']); },
      error: err => { this.procesando.set(false); this.errorOperacion.set(err.error?.message ?? 'No se pudo eliminar el borrador.'); }
    });
  }
}
