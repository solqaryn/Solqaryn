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
import { MatSnackBar } from '@angular/material/snack-bar';
import { CompraService } from '../../services/compra.service';
import { Compra, CompraDocumento } from '../../core/models/compra.model';
import { ImpuestoAplicado } from '../../core/models/venta.model';
import { AnularDialogComponent } from '../../shared/anular-dialog.component';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { ProductoImagenComponent } from '../../shared/producto-imagen/producto-imagen.component';
import { descargarBlobSeguro } from '../../shared/descarga-segura';

@Component({
  selector: 'app-compra-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, MatButtonModule, MatIconModule, MatProgressSpinnerModule, MatDialogModule, ProductoImagenComponent],
  templateUrl: './compra-detail.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './compra-detail.component.scss'
})
export class CompraDetailComponent implements OnInit {
  readonly editor = crearEditorInstitucional('Compras');
  private readonly acciones = crearAccionesInstitucionales('Compras');
  readonly errorOperacion = signal<string | null>(null);
  private modificado = false;

  cerrarModal(): void { if (!this.procesando() && !this.subiendoDocumento() && this.eliminandoDocumentoId() === null) this.editor.cerrar(this.modificado); }

  async editarBorrador(): Promise<void> {
    if (!this.editor.valido('Editar') || this.compra()?.estado !== 'Borrador' || this.procesando()) return;
    if (await this.acciones.abrir(() => import('./compra-form.component').then(m => m.CompraFormComponent), 'xl', 'Editar', this.compraId)) {
      this.modificado = true; this.cargar();
    }
  }

  private readonly permisosRuntime = inject(PermisosRuntimeService);

  readonly compra = signal<Compra | null>(null);
  readonly documentos = signal<CompraDocumento[]>([]);
  readonly loading = signal(true);
  readonly loadingDocumentos = signal(true);
  readonly documentosError = signal<string | null>(null);
  readonly procesando = signal(false);
  readonly subiendoDocumento = signal(false);
  readonly descargandoDocumentoId = signal<number | null>(null);
  readonly eliminandoDocumentoId = signal<number | null>(null);
  readonly puedeEditar = signal(false);
  readonly puedeConfirmar = signal(false);
  readonly puedeAnular = signal(false);
  readonly puedeEliminar = signal(false);
  readonly puedeExportar = signal(false);
  private compraId!: number;

  constructor(
    private compraService: CompraService,
    private route: ActivatedRoute,
    private router: Router,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.puedeEditar.set(this.permisosRuntime.puede('Compras', 'Editar'));
    this.puedeConfirmar.set(this.permisosRuntime.puede('Compras', 'Confirmar'));
    this.puedeAnular.set(this.permisosRuntime.puede('Compras', 'Anular'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Compras', 'EliminarLogico'));
    this.puedeExportar.set(this.permisosRuntime.puede('Compras', 'Exportar'));

    this.compraId = Number(this.editor.ref ? this.editor.id : this.route.snapshot.paramMap.get('id'));
    if (!this.editor.valido('Ver') || !Number.isInteger(this.compraId) || this.compraId <= 0) {
      this.editor.cerrar(false); this.loading.set(false); this.errorOperacion.set('No se pudo autorizar la consulta.'); return;
    }
    this.cargar();
    this.cargarDocumentos();
  }

  cargar(): void {
    this.loading.set(true);
    this.compraService.getById(this.compraId).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: (res) => { this.compra.set(res.data); this.loading.set(false); },
      error: err => { this.loading.set(false); this.errorOperacion.set(err.error?.message ?? 'No se pudo consultar el registro.'); }
    });
  }

  cargarDocumentos(): void {
    this.loadingDocumentos.set(true);
    this.documentosError.set(null);
    this.compraService.getDocumentos(this.compraId).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: (res) => {
        this.documentos.set(res.data);
        this.loadingDocumentos.set(false);
      },
      error: () => {
        this.documentos.set([]);
        this.documentosError.set('No se pudieron cargar los comprobantes. Verifica tu acceso e inténtalo de nuevo.');
        this.loadingDocumentos.set(false);
      }
    });
  }

  importeBruto(compra: Compra): number {
    return compra.detalles.reduce((total, detalle) => total + detalle.subtotal, 0);
  }

  impuestoEsIncluido(compra: Compra, impuesto: ImpuestoAplicado): boolean {
    if (impuesto.incluidoEnPrecio) return true;
    const despuesDescuento = Math.max(0, this.importeBruto(compra) - compra.descuento);
    return Math.abs(compra.total - despuesDescuento) <= 0.01;
  }

  impuestoIncluido(compra: Compra): number {
    return compra.impuestosAplicados
      .filter((impuesto) => this.impuestoEsIncluido(compra, impuesto))
      .reduce((total, impuesto) => total + impuesto.monto, 0);
  }

  impuestoAdicional(compra: Compra): number {
    return compra.impuestosAplicados
      .filter((impuesto) => !this.impuestoEsIncluido(compra, impuesto))
      .reduce((total, impuesto) => total + impuesto.monto, 0);
  }

  subtotalSinImpuesto(compra: Compra): number {
    const calculado = compra.total - this.impuestoIncluido(compra) - this.impuestoAdicional(compra);
    return Math.max(0, calculado);
  }

  async seleccionarDocumento(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    const archivo = input.files?.[0];
    input.value = '';
    if (!archivo || !this.puedeEditar() || this.subiendoDocumento()) return;

    if (archivo.size === 0) {
      this.snackBar.open('El comprobante está vacío y no se puede adjuntar.', 'Cerrar', { duration: 5000 });
      return;
    }

    const permitidos = ['image/jpeg', 'image/png', 'image/webp', 'application/pdf'];
    if (!permitidos.includes(archivo.type)) {
      this.snackBar.open('Solo se permiten archivos JPG, PNG, WebP o PDF.', 'Cerrar', { duration: 5000 });
      return;
    }
    if (archivo.size > 10 * 1024 * 1024) {
      this.snackBar.open('El comprobante no puede superar 10 MB.', 'Cerrar', { duration: 5000 });
      return;
    }

    this.subiendoDocumento.set(true);
    if (!await this.editor.confirmar('Editar', { titulo: 'Adjuntar comprobante', mensaje: '¿Deseas adjuntar «' + archivo.name + '» a esta compra?', confirmarTexto: 'Adjuntar' })) { this.subiendoDocumento.set(false); return; }
    this.compraService.subirDocumento(this.compraId, archivo).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: () => {
        this.subiendoDocumento.set(false);
        this.snackBar.open('Comprobante adjuntado correctamente.', 'Cerrar', { duration: 3500 });
        this.cargarDocumentos();
      },
      error: (err) => {
        this.subiendoDocumento.set(false);
        this.snackBar.open(err.error?.message ?? 'No se pudo adjuntar el comprobante.', 'Cerrar', { duration: 6000 });
      }
    });
  }

  descargarDocumento(documento: CompraDocumento): void {
    if (!this.editor.valido('Exportar') || !this.puedeExportar() || this.descargandoDocumentoId() !== null) return;

    this.descargandoDocumentoId.set(documento.id);
    this.compraService.descargarDocumento(this.compraId, documento.id).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: (blob) => {
        this.descargandoDocumentoId.set(null);
        if (!descargarBlobSeguro(blob, documento.nombreOriginal, `comprobante-${documento.id}`)) {
          this.snackBar.open('El comprobante descargado está vacío.', 'Cerrar', { duration: 5000 });
        }
      },
      error: () => {
        this.descargandoDocumentoId.set(null);
        this.snackBar.open('No se pudo descargar el comprobante.', 'Cerrar', { duration: 5000 });
      }
    });
  }

  async eliminarDocumento(documento: CompraDocumento): Promise<void> {
    if (!this.puedeEditar() || this.eliminandoDocumentoId() !== null) return;
    this.eliminandoDocumentoId.set(documento.id);
    if (!await this.editor.confirmar('Editar', { titulo: 'Retirar comprobante', mensaje: '¿Deseas retirar «' + documento.nombreOriginal + '»? La acción quedará registrada en auditoría.', tipo: 'peligro', confirmarTexto: 'Retirar' })) { this.eliminandoDocumentoId.set(null); return; }
    this.compraService.eliminarDocumento(this.compraId, documento.id).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: () => { this.eliminandoDocumentoId.set(null); this.snackBar.open('Comprobante retirado correctamente.', 'Cerrar', { duration: 3500 }); this.cargarDocumentos(); },
      error: err => { this.eliminandoDocumentoId.set(null); this.snackBar.open(err.error?.message ?? 'No se pudo retirar el comprobante.', 'Cerrar', { duration: 5000 }); }
    });
  }

  iconoDocumento(documento: CompraDocumento): string {
    return documento.esImagen ? 'image' : 'picture_as_pdf';
  }

  tamanoLegible(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
  }

  async confirmar(): Promise<void> {
    if (!this.puedeConfirmar() || this.procesando() || this.compra()?.estado !== 'Borrador') return;
    this.procesando.set(true); this.errorOperacion.set(null);
    if (!await this.editor.confirmar('Confirmar', { titulo: 'Confirmar compra', mensaje: '¿Deseas confirmar esta compra? Se aplicarán las reglas de inventario y facturación vigentes.', tipo: 'advertencia', confirmarTexto: 'Confirmar' }) || this.compra()?.estado !== 'Borrador') { this.procesando.set(false); return; }
    this.compraService.confirmar(this.compraId).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: res => {
        this.compra.set(res.data); this.modificado = true; this.procesando.set(false);
        
      },
      error: err => { this.procesando.set(false); this.errorOperacion.set(err.error?.message ?? 'No se pudo confirmar la compra.'); }
    });
  }

  async anular(): Promise<void> {
    if (!this.puedeAnular() || !this.editor.valido('Anular') || this.procesando() || this.compra()?.estado !== 'Confirmada') return;
    this.procesando.set(true); this.errorOperacion.set(null);
    let motivo: string | undefined;
    try {
      motivo = await firstValueFrom(this.dialog.open(AnularDialogComponent, modalInstitucional('s', {
        data: { title: 'Anular compra', message: 'Esta acción revertirá el stock de los productos. Escribe el motivo:' }
      })).afterClosed().pipe(takeUntilDestroyed(this.editor.destroyRef)));
    } catch { motivo = undefined; }
    if (!motivo?.trim() || !this.editor.valido('Anular') || this.compra()?.estado !== 'Confirmada') { this.procesando.set(false); return; }
    this.compraService.anular(this.compraId, motivo.trim()).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: res => { this.compra.set(res.data); this.modificado = true; this.procesando.set(false); },
      error: err => { this.procesando.set(false); this.errorOperacion.set(err.error?.message ?? 'No se pudo anular la compra.'); }
    });
  }

  async eliminarBorrador(): Promise<void> {
    if (!this.puedeEliminar() || this.procesando() || this.compra()?.estado !== 'Borrador') return;
    this.procesando.set(true); this.errorOperacion.set(null);
    if (!await this.editor.confirmar('EliminarLogico', { titulo: 'Eliminar borrador', mensaje: '¿Deseas eliminar este borrador de compra?', tipo: 'peligro', confirmarTexto: 'Eliminar' }) || this.compra()?.estado !== 'Borrador') { this.procesando.set(false); return; }
    this.compraService.deleteBorrador(this.compraId).pipe(takeUntilDestroyed(this.editor.destroyRef)).subscribe({
      next: () => { this.modificado = true; this.procesando.set(false); if (this.editor.ref) this.editor.cerrar(true); else void this.router.navigate(['/compras']); },
      error: err => { this.procesando.set(false); this.errorOperacion.set(err.error?.message ?? 'No se pudo eliminar el borrador.'); }
    });
  }
}
