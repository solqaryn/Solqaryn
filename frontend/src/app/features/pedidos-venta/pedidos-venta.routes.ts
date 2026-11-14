import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { permisoGuard } from '../../core/guards/permiso.guard';

export const PEDIDOS_VENTA_ROUTES:Routes=[
 {path:'pedidos-venta',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Ver'},loadComponent:()=>import('./pedidos-venta.components').then(m=>m.PedidosVentaListComponent)},
 {path:'pedidos-venta/nuevo',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Crear'},loadComponent:()=>import('./pedidos-venta.components').then(m=>m.PedidoVentaFormComponent)},
 {path:'pedidos-venta/:id/editar',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Editar'},loadComponent:()=>import('./pedidos-venta.components').then(m=>m.PedidoVentaFormComponent)},
 {path:'pedidos-venta/:pedidoVentaId/preparacion',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Ver'},loadComponent:()=>import('../preparaciones-pedido-venta/preparaciones-pedido-venta.component').then(m=>m.PreparacionesPedidoVentaComponent)},
 {path:'preparaciones-pedido-venta',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Ver'},loadComponent:()=>import('../preparaciones-pedido-venta/preparaciones-pedido-venta.component').then(m=>m.PreparacionesPedidoVentaComponent)},
 {path:'pedidos-venta/:id',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Ver'},loadComponent:()=>import('./pedidos-venta.components').then(m=>m.PedidoVentaDetailComponent)},
 {path:'devoluciones-clientes',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Ver'},loadComponent:()=>import('../ventas/devoluciones-cliente/devoluciones-cliente.components').then(m=>m.DevolucionesClienteListComponent)},
 {path:'devoluciones-clientes/nueva',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Crear'},loadComponent:()=>import('../ventas/devoluciones-cliente/devoluciones-cliente.components').then(m=>m.DevolucionClienteFormComponent)},
 {path:'devoluciones-clientes/:id',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Ver'},loadComponent:()=>import('../ventas/devoluciones-cliente/devoluciones-cliente.components').then(m=>m.DevolucionClienteDetailComponent)},
 {path:'notas-credito-cliente',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Ver'},loadComponent:()=>import('../ventas/notas-credito-cliente/notas-credito-cliente.components').then(m=>m.NotasCreditoClienteHomeComponent)},
 {path:'notas-credito-cliente/nueva',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Crear'},loadComponent:()=>import('../ventas/notas-credito-cliente/notas-credito-cliente.components').then(m=>m.NotaCreditoClienteFormComponent)},
 {path:'notas-credito-cliente/:id',canActivate:[authGuard,permisoGuard],data:{modulo:'Ventas',accion:'Ver'},loadComponent:()=>import('../ventas/notas-credito-cliente/notas-credito-cliente.components').then(m=>m.NotaCreditoClienteDetailComponent)}
];
