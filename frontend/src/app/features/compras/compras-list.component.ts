import { PAGINADOR_ESPANOL_PROVIDER } from '../../shared/paginador-institucional';
import { crearAccionesInstitucionales } from '../../shared/modal-institucional/acciones-institucionales';
import { MatSnackBar } from '@angular/material/snack-bar';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Component, OnDestroy, OnInit, signal, ChangeDetectionStrategy, inject, DestroyRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { debounceTime, Subject, Subscription } from 'rxjs';
import { CompraService } from '../../services/compra.service';
import { Compra } from '../../core/models/compra.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { ListNavigationStateService } from '../../core/navigation/list-navigation-state.service';
import { ProductoImagenComponent } from '../../shared/producto-imagen/producto-imagen.component';

@Component({
  viewProviders: [PAGINADOR_ESPANOL_PROVIDER],
  selector: 'app-compras-list',
  standalone: true,
  imports: [CommonModule, FormsModule, MatIconModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatPaginatorModule, MatProgressSpinnerModule, ProductoImagenComponent],
  templateUrl: './compras-list.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './compras-list.component.scss'
})
export class ComprasListComponent implements OnInit, OnDestroy {
  private readonly acciones = crearAccionesInstitucionales('Compras');
  private readonly snackBar = inject(MatSnackBar);
  private readonly destroyRef = inject(DestroyRef);
  readonly puedeEditar = signal(false);
  readonly puedeEliminar = signal(false);
  readonly eliminandoId = signal<number | null>(null);

  async abrir(modo: 'Ver' | 'Crear' | 'Editar', id?: number): Promise<void> {
    const item = this.compras().find(x => x.id === id);
    if (modo === 'Editar' && item?.estado !== 'Borrador') return;
    const actualizado = await this.acciones.abrir(
      modo === 'Ver'
        ? () => import('./compra-detail.component').then(m => m.CompraDetailComponent)
        : () => import('./compra-form.component').then(m => m.CompraFormComponent),
      'xl', modo, id
    );
    if (actualizado) this.cargar();
  }

  async eliminarBorrador(item: Compra): Promise<void> {
    if (item.estado !== 'Borrador' || !this.puedeEliminar() || this.eliminandoId() !== null) return;
    this.eliminandoId.set(item.id);
    const autorizado = await this.acciones.confirmar('EliminarLogico', {
      titulo: 'Eliminar borrador', mensaje: '¿Deseas eliminar el borrador ' + item.numeroCompra + '?',
      tipo: 'peligro', confirmarTexto: 'Eliminar'
    });
    if (!autorizado || this.compras().find(x => x.id === item.id)?.estado !== 'Borrador') { this.eliminandoId.set(null); return; }
    this.compraService.deleteBorrador(item.id).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: () => { this.eliminandoId.set(null); this.cargar(); },
      error: err => { this.eliminandoId.set(null); this.snackBar.open(err.error?.message ?? 'No se pudo eliminar el borrador.', 'Cerrar', { duration: 5000 }); }
    });
  }

  readonly compras = signal<Compra[]>([]);
  readonly loading = signal(true);
  readonly totalCount = signal(0);
  readonly puedeCrear = signal(false);

  page = 1;
  pageSize = 10;
  search = '';
  sortBy = 'Fecha';
  sortDirection: 'asc' | 'desc' = 'desc';

  private readonly navigationDefaults = { page: 1, pageSize: 10, search: '', sortBy: 'Fecha', sortDirection: 'desc' };
  private readonly searchSubject = new Subject<string>();
  private readonly searchSubscription: Subscription;
  private consultaActual?: Subscription;
  private secuenciaConsulta = 0;

  constructor(
    private compraService: CompraService,
    private permisosRuntime: PermisosRuntimeService,
    private route: ActivatedRoute,
    private navigationState: ListNavigationStateService
  ) {
    this.searchSubscription = this.searchSubject.pipe(debounceTime(350)).subscribe(() => { this.page = 1; this.cargar(); });
  }

  ngOnInit(): void {
    const state = this.navigationState.restore('compras', this.route, this.navigationDefaults);
    this.page = Math.max(1, Math.trunc(state.page));
    this.pageSize = [10, 25, 50].includes(state.pageSize) ? state.pageSize : 10;
    this.search = state.search;
    this.sortBy = ['Fecha', 'ProveedorNombre', 'Total'].includes(state.sortBy) ? state.sortBy : 'Fecha';
    this.sortDirection = state.sortDirection === 'asc' ? 'asc' : 'desc';
    this.puedeCrear.set(this.permisosRuntime.puede('Compras', 'Crear'));
    this.puedeEditar.set(this.permisosRuntime.puede('Compras', 'Editar'));
    this.puedeEliminar.set(this.permisosRuntime.puede('Compras', 'EliminarLogico'));
    this.cargar();
  }

  ngOnDestroy(): void {
    this.consultaActual?.unsubscribe();
    this.searchSubscription.unsubscribe();
    this.searchSubject.complete();
  }

  onSearchChange(value: string): void { this.search = value; this.searchSubject.next(value); }

  limpiarFiltros(): void {
    this.navigationState.clear('compras');
    this.page = 1;
    this.pageSize = 10;
    this.search = '';
    this.sortBy = 'Fecha';
    this.sortDirection = 'desc';
    this.cargar();
  }

  ordenarPor(campo: 'Fecha' | 'ProveedorNombre' | 'Total'): void {
    if (this.sortBy === campo) this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    else { this.sortBy = campo; this.sortDirection = campo === 'Fecha' ? 'desc' : 'asc'; }
    this.page = 1;
    this.cargar();
  }

  cargar(): void {
    this.navigationState.persist('compras', this.route, {
      page: this.page, pageSize: this.pageSize, search: this.search,
      sortBy: this.sortBy, sortDirection: this.sortDirection
    }, this.navigationDefaults);

    const secuencia = ++this.secuenciaConsulta;
    this.consultaActual?.unsubscribe();
    this.loading.set(true);
    this.consultaActual = this.compraService.getPaged({ page: this.page, pageSize: this.pageSize, search: this.search, sortBy: this.sortBy, sortDirection: this.sortDirection })
      .subscribe({
        next: (res) => {
          if (secuencia !== this.secuenciaConsulta) return;
          this.compras.set(res.data.items);
          this.totalCount.set(res.data.totalCount);
          this.loading.set(false);
        },
        error: () => {
          if (secuencia === this.secuenciaConsulta) this.loading.set(false);
        }
      });
  }

  onPageChange(event: PageEvent): void {
    this.page = event.pageIndex + 1;
    this.pageSize = event.pageSize;
    this.cargar();
  }
}
