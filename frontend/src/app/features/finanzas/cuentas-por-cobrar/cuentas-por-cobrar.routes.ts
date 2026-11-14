import { Routes } from '@angular/router';
import { authGuard } from '../../../core/guards/auth.guard';
import { permisoGuard } from '../../../core/guards/permiso.guard';

export const CUENTAS_POR_COBRAR_ROUTES: Routes = [
  {
    path: 'cuentas-por-cobrar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Facturacion', accion: 'Ver' },
    loadComponent: () => import('./cuentas-por-cobrar.component').then(m => m.CuentasPorCobrarComponent)
  }
];
