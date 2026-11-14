import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { permisoGuard } from '../../core/guards/permiso.guard';

export const CENTROS_COSTO_ROUTES: Routes = [
  {
    path: 'centros-costo',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Finanzas', accion: 'Ver' },
    loadComponent: () => import('./centros-costo.component').then(m => m.CentrosCostoComponent)
  }
];
