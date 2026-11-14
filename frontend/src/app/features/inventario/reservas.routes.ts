import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { permisoGuard } from '../../core/guards/permiso.guard';

export const RESERVAS_INVENTARIO_ROUTES: Routes = [
  {
    path: 'inventario/reservas',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'MovimientosInventario', accion: 'Ver' },
    loadComponent: () => import('./reservas-inventario-list.component').then(m => m.ReservasInventarioListComponent)
  },
  {
    path: 'inventario/reservas/nueva',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'MovimientosInventario', accion: 'Crear' },
    loadComponent: () => import('./reserva-inventario-form.component').then(m => m.ReservaInventarioFormComponent)
  },
  {
    path: 'inventario/reservas/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'MovimientosInventario', accion: 'Editar' },
    loadComponent: () => import('./reserva-inventario-form.component').then(m => m.ReservaInventarioFormComponent)
  },
  {
    path: 'inventario/reservas/:id',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'MovimientosInventario', accion: 'Ver' },
    loadComponent: () => import('./reserva-inventario-detail.component').then(m => m.ReservaInventarioDetailComponent)
  }
];
