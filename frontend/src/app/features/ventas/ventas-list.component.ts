import { Component, OnDestroy, OnInit, signal, ChangeDetectionStrategy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { debounceTime, Subject, Subscription } from 'rxjs';
import { VentaService } from '../../services/venta.service';
import { Venta } from '../../core/models/venta.model';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { ListNavigationStateService } from '../../core/navigation/list-navigation-state.service';
import { ProductoImagenComponent } from '../../shared/producto-imagen/producto-imagen.component';

@Component({
  selector: 'app-ventas-list',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule, MatIconModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatPaginatorModule, MatProgressSpinnerModule, ProductoImagenComponent],
  templateUrl: './ventas-list.component.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './ventas-list.component.scss'
})
export class VentasListComponent implements OnInit, OnDestroy {
  readonly ventas = signal<Venta[]>([]);
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
    private ventaService: VentaService,
    private permisosRuntime: PermisosRuntimeService,
    private route: ActivatedRoute,
    private navigationState: ListNavigationStateService
  ) {
    this.searchSubscription = this.searchSubject.pipe(debounceTime(350)).subscribe(() => { this.page = 1; this.cargar(); });
  }

  ngOnInit(): void {
    const state = this.navigationState.restore('ventas', this.route, this.navigationDefaults);
    this.page = Math.max(1, Math.trunc(state.page));
    this.pageSize = [10, 25, 50].includes(state.pageSize) ? state.pageSize : 10;
    this.search = state.search;
    this.sortBy = ['Fecha', 'ClienteNombre', 'Total'].includes(state.sortBy) ? state.sortBy : 'Fecha';
    this.sortDirection = state.sortDirection === 'asc' ? 'asc' : 'desc';
    this.puedeCrear.set(this.permisosRuntime.puede('Ventas', 'Crear'));
    this.cargar();
  }

  ngOnDestroy(): void {
    this.consultaActual?.unsubscribe();
    this.searchSubscription.unsubscribe();
    this.searchSubject.complete();
  }

  onSearchChange(value: string): void { this.search = value; this.searchSubject.next(value); }

  limpiarFiltros(): void {
    this.navigationState.clear('ventas');
    this.page = 1;
    this.pageSize = 10;
    this.search = '';
    this.sortBy = 'Fecha';
    this.sortDirection = 'desc';
    this.cargar();
  }

  ordenarPor(campo: 'Fecha' | 'ClienteNombre' | 'Total'): void {
    if (this.sortBy === campo) this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    else { this.sortBy = campo; this.sortDirection = campo === 'Fecha' ? 'desc' : 'asc'; }
    this.page = 1;
    this.cargar();
  }

  cargar(): void {
    this.navigationState.persist('ventas', this.route, {
      page: this.page, pageSize: this.pageSize, search: this.search,
      sortBy: this.sortBy, sortDirection: this.sortDirection
    }, this.navigationDefaults);

    const secuencia = ++this.secuenciaConsulta;
    this.consultaActual?.unsubscribe();
    this.loading.set(true);
    this.consultaActual = this.ventaService.getPaged({ page: this.page, pageSize: this.pageSize, search: this.search, sortBy: this.sortBy, sortDirection: this.sortDirection })
      .subscribe({
        next: (res) => {
          if (secuencia !== this.secuenciaConsulta) return;
          this.ventas.set(res.data.items);
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
