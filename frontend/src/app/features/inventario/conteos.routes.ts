import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { permisoGuard } from '../../core/guards/permiso.guard';

export const CONTEOS_INVENTARIO_ROUTES: Routes = [
  {
    path: 'inventario/conteos',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'MovimientosInventario', accion: 'Ver' },
    loadComponent: () => import('./conteos-inventario-list.component').then(m => m.ConteosInventarioListComponent)
  },
  {
    path: 'inventario/conteos/nuevo',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'MovimientosInventario', accion: 'Crear' },
    loadComponent: () => import('./conteo-inventario-form.component').then(m => m.ConteoInventarioFormComponent)
  },
  {
    path: 'inventario/conteos/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'MovimientosInventario', accion: 'Editar' },
    loadComponent: () => import('./conteo-inventario-form.component').then(m => m.ConteoInventarioFormComponent)
  },
  {
    path: 'inventario/conteos/:id',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'MovimientosInventario', accion: 'Ver' },
    loadComponent: () => import('./conteo-inventario-detail.component').then(m => m.ConteoInventarioDetailComponent)
  }
];
