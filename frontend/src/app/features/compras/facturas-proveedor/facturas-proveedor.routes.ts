import { Routes } from '@angular/router';
import { authGuard } from '../../../core/guards/auth.guard';
import { permisoGuard } from '../../../core/guards/permiso.guard';
import { CUENTAS_POR_COBRAR_ROUTES } from '../../finanzas/cuentas-por-cobrar/cuentas-por-cobrar.routes';
import { CUENTAS_POR_PAGAR_ROUTES } from '../../finanzas/cuentas-por-pagar/cuentas-por-pagar.routes';

export const FACTURAS_PROVEEDOR_ROUTES: Routes = [
  {
    path: 'compras/:id/three-way-match',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Ver' },
    loadComponent: () => import('../three-way-match/three-way-match.component').then(m => m.ThreeWayMatchComponent)
  },
  {
    path: 'facturas-proveedor/nueva',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Crear' },
    loadComponent: () => import('./facturas-proveedor.components').then(m => m.FacturaProveedorFormComponent)
  },
  {
    path: 'facturas-proveedor/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Editar' },
    loadComponent: () => import('./facturas-proveedor.components').then(m => m.FacturaProveedorFormComponent)
  },
  {
    path: 'facturas-proveedor/:id',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Ver' },
    loadComponent: () => import('./facturas-proveedor.components').then(m => m.FacturaProveedorDetailComponent)
  },
  {
    path: 'facturas-proveedor',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Ver' },
    loadComponent: () => import('./facturas-proveedor.components').then(m => m.FacturasProveedorListComponent)
  },
  {
    path: 'devoluciones-proveedor/nueva',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Crear' },
    loadComponent: () => import('../devoluciones-proveedor/devoluciones-proveedor.components').then(m => m.DevolucionProveedorFormComponent)
  },
  {
    path: 'devoluciones-proveedor/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Editar' },
    loadComponent: () => import('../devoluciones-proveedor/devoluciones-proveedor.components').then(m => m.DevolucionProveedorFormComponent)
  },
  {
    path: 'devoluciones-proveedor/:id',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Ver' },
    loadComponent: () => import('../devoluciones-proveedor/devoluciones-proveedor.components').then(m => m.DevolucionProveedorDetailComponent)
  },
  {
    path: 'devoluciones-proveedor',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Ver' },
    loadComponent: () => import('../devoluciones-proveedor/devoluciones-proveedor.components').then(m => m.DevolucionesProveedorListComponent)
  },
  {
    path: 'notas-credito-proveedor/nueva',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Crear' },
    loadComponent: () => import('../notas-credito-proveedor/notas-credito-proveedor.components').then(m => m.NotaCreditoProveedorFormComponent)
  },
  {
    path: 'notas-credito-proveedor/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Editar' },
    loadComponent: () => import('../notas-credito-proveedor/notas-credito-proveedor.components').then(m => m.NotaCreditoProveedorFormComponent)
  },
  {
    path: 'notas-credito-proveedor/:id',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Ver' },
    loadComponent: () => import('../notas-credito-proveedor/notas-credito-proveedor.components').then(m => m.NotaCreditoProveedorDetailComponent)
  },
  {
    path: 'notas-credito-proveedor',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Ver' },
    loadComponent: () => import('../notas-credito-proveedor/notas-credito-proveedor.components').then(m => m.NotasCreditoProveedorListComponent)
  },
  {
    path: 'evaluaciones-proveedor',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Ver' },
    loadComponent: () => import('../evaluaciones-proveedor/evaluaciones-proveedor.component').then(m => m.EvaluacionesProveedorComponent)
  },
  ...CUENTAS_POR_PAGAR_ROUTES,
  ...CUENTAS_POR_COBRAR_ROUTES
];
