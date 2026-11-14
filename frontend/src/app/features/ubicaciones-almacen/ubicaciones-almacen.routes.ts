import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { permisoGuard } from '../../core/guards/permiso.guard';

export const UBICACIONES_ALMACEN_ROUTES: Routes = [
  {
    path: 'ubicaciones-almacen',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'UbicacionesAlmacen', accion: 'Ver' },
    loadComponent: () => import('./ubicaciones-almacen-list.component').then(m => m.UbicacionesAlmacenListComponent)
  },
  {
    path: 'ubicaciones-almacen/nueva',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'UbicacionesAlmacen', accion: 'Crear' },
    loadComponent: () => import('./ubicacion-almacen-form.component').then(m => m.UbicacionAlmacenFormComponent)
  },
  {
    path: 'ubicaciones-almacen/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'UbicacionesAlmacen', accion: 'Editar' },
    loadComponent: () => import('./ubicacion-almacen-form.component').then(m => m.UbicacionAlmacenFormComponent)
  }
];
