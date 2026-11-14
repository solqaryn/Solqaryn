import { Routes } from '@angular/router';
import { authGuard } from '../../../core/guards/auth.guard';
import { permisoGuard } from '../../../core/guards/permiso.guard';

export const CUENTAS_POR_PAGAR_ROUTES: Routes = [
  {
    path: 'cuentas-por-pagar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Finanzas', accion: 'Ver' },
    loadComponent: () => import('./cuentas-por-pagar.component').then(m => m.CuentasPorPagarComponent)
  }
];
