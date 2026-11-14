import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { permisoGuard } from '../../core/guards/permiso.guard';
import { TRANSFERENCIAS_INVENTARIO_ROUTES } from './transferencias.routes';
import { CONTEOS_INVENTARIO_ROUTES } from './conteos.routes';
import { RESERVAS_INVENTARIO_ROUTES } from './reservas.routes';
import { FACTURAS_PROVEEDOR_ROUTES } from '../compras/facturas-proveedor/facturas-proveedor.routes';

export const AJUSTES_INVENTARIO_ROUTES: Routes = [
  {
    path: 'inventario/existencias',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Inventario', accion: 'Ver' },
    loadComponent: () => import('./existencias-variante-list.component').then(m => m.ExistenciasVarianteListComponent)
  },
  {
    path: 'inventario/existencias/nueva',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Inventario', accion: 'Crear' },
    loadComponent: () => import('./existencia-variante-form.component').then(m => m.ExistenciaVarianteFormComponent)
  },
  {
    path: 'inventario/existencias/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Inventario', accion: 'Editar' },
    loadComponent: () => import('./existencia-variante-form.component').then(m => m.ExistenciaVarianteFormComponent)
  },
  {
    path: 'inventario/trazabilidad/:varianteId',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Inventario', accion: 'Editar' },
    loadComponent: () => import('./trazabilidad-variante-page.component').then(m => m.TrazabilidadVariantePageComponent)
  },
  {
    path: 'inventario/ajustes',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Inventario', accion: 'Ver' },
    loadComponent: () => import('./ajustes-list.component').then(m => m.AjustesListComponent)
  },
  {
    path: 'inventario/ajustes/nuevo',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Inventario', accion: 'Crear' },
    loadComponent: () => import('./ajuste-form.component').then(m => m.AjusteFormComponent)
  },
  {
    path: 'inventario/ajustes/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Inventario', accion: 'Editar' },
    loadComponent: () => import('./ajuste-form.component').then(m => m.AjusteFormComponent)
  },
  {
    path: 'inventario/ajustes/:id',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Inventario', accion: 'Ver' },
    loadComponent: () => import('./ajuste-detail.component').then(m => m.AjusteDetailComponent)
  },
  {
    path: 'recepciones-compra/nueva',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Crear' },
    loadComponent: () => import('../recepciones-compra/recepcion-compra-form.component').then(m => m.RecepcionCompraFormComponent)
  },
  {
    path: 'recepciones-compra/:id',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Ver' },
    loadComponent: () => import('../recepciones-compra/recepcion-compra-detail.component').then(m => m.RecepcionCompraDetailComponent)
  },
  {
    path: 'recepciones-compra',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Compras', accion: 'Ver' },
    loadComponent: () => import('../recepciones-compra/recepciones-compra-shell.component').then(m => m.RecepcionesCompraShellComponent)
  },
  ...FACTURAS_PROVEEDOR_ROUTES,
  ...TRANSFERENCIAS_INVENTARIO_ROUTES,
  ...CONTEOS_INVENTARIO_ROUTES,
  ...RESERVAS_INVENTARIO_ROUTES
];
