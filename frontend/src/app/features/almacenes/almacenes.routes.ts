import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { permisoGuard } from '../../core/guards/permiso.guard';

export const ALMACENES_ROUTES: Routes = [
  {
    path: 'almacenes',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Almacenes', accion: 'Ver' },
    loadComponent: () => import('./almacenes-list.component').then(m => m.AlmacenesListComponent)
  },
  {
    path: 'almacenes/nuevo',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Almacenes', accion: 'Crear' },
    loadComponent: () => import('./almacen-form.component').then(m => m.AlmacenFormComponent)
  },
  {
    path: 'almacenes/:id/editar',
    canActivate: [authGuard, permisoGuard],
    data: { modulo: 'Almacenes', accion: 'Editar' },
    loadComponent: () => import('./almacen-form.component').then(m => m.AlmacenFormComponent)
  }
];
