import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { permisoGuard } from '../../core/guards/permiso.guard';
export const COTIZACIONES_ROUTES:Routes=[
 {path:'cotizaciones',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Ver'},loadComponent:()=>import('./cotizaciones.components').then(m=>m.CotizacionesListComponent)},
 {path:'cotizaciones/nueva',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Crear'},loadComponent:()=>import('./cotizaciones.components').then(m=>m.CotizacionFormComponent)},
 {path:'cotizaciones/:id/editar',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Editar'},loadComponent:()=>import('./cotizaciones.components').then(m=>m.CotizacionFormComponent)},
 {path:'cotizaciones/:id',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Ver'},loadComponent:()=>import('./cotizaciones.components').then(m=>m.CotizacionDetailComponent)}
];
