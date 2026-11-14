import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { permisoGuard } from '../../core/guards/permiso.guard';

export const TRANSFERENCIAS_INVENTARIO_ROUTES: Routes = [
  {
    path: 'inventario/transferencias',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'MovimientosInventario', accion: 'Ver' },
    loadComponent: () => import('./transferencias-list.component').then(m => m.TransferenciasListComponent)
  },
  {
    path: 'inventario/transferencias/nueva',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'MovimientosInventario', accion: 'Crear' },
    loadComponent: () => import('./transferencia-form.component').then(m => m.TransferenciaFormComponent)
  },
  {
    path: 'inventario/transferencias/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'MovimientosInventario', accion: 'Editar' },
    loadComponent: () => import('./transferencia-form.component').then(m => m.TransferenciaFormComponent)
  },
  {
    path: 'inventario/transferencias/:id',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'MovimientosInventario', accion: 'Ver' },
    loadComponent: () => import('./transferencia-detail.component').then(m => m.TransferenciaDetailComponent)
  }
];
