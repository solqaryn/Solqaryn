import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

@Component({
  selector: 'app-navigation-menu',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    @if (permisosRuntime.puede('Dashboard', 'Ver')) {
      <section class="nav-group" aria-labelledby="nav-inicio">
        <h2 id="nav-inicio" class="nav-group__title">Inicio</h2>
        <a routerLink="/dashboard" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">dashboard</span> Dashboard</a>
      </section>
    }

    @if (tieneCatalogo()) {
      <section class="nav-group" aria-labelledby="nav-catalogo">
        <h2 id="nav-catalogo" class="nav-group__title">Catálogo</h2>
        @if (permisosRuntime.puede('Productos', 'Ver')) {
          <a routerLink="/productos" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">widgets</span> Productos</a>
        }
        @if (permisosRuntime.puede('Categorias', 'Ver')) {
          <a routerLink="/categorias" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">category</span> Categorías</a>
        }
        @if (permisosRuntime.puede('Colores', 'Ver')) {
          <a routerLink="/colores" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">palette</span> Colores</a>
        }
        @if (permisosRuntime.puede('Tallas', 'Ver')) {
          <a routerLink="/tallas" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">straighten</span> Tallas</a>
        }
        @if (permisosRuntime.puede('Marcas', 'Ver')) {
          <a routerLink="/marcas" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">branding_watermark</span> Marcas</a>
        }
        @if (permisosRuntime.puede('Modelos', 'Ver')) {
          <a routerLink="/modelos" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">devices</span> Modelos</a>
        }
        @if (permisosRuntime.puede('MetodosPago', 'Ver')) {
          <a routerLink="/metodos-pago" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">payments</span> Métodos de pago</a>
        }
        @if (permisosRuntime.puede('Descuentos', 'Ver')) {
          <a routerLink="/descuentos" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">sell</span> Descuentos</a>
        }
        @if (permisosRuntime.puede('Impuestos', 'Ver')) {
          <a routerLink="/impuestos" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">request_quote</span> Impuestos</a>
        }
      </section>
    }

    @if (tieneOperacion()) {
      <section class="nav-group" aria-labelledby="nav-operacion">
        <h2 id="nav-operacion" class="nav-group__title">Operación</h2>
        @if (permisosRuntime.puede('Sucursales', 'Ver')) {
          <a routerLink="/sucursales" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">storefront</span> Sucursales</a>
        }
        @if (permisosRuntime.puede('Almacenes', 'Ver')) {
          <a routerLink="/almacenes" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">warehouse</span> Almacenes</a>
        }
        @if (permisosRuntime.puede('UbicacionesAlmacen', 'Ver')) {
          <a routerLink="/ubicaciones-almacen" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">account_tree</span> Ubicaciones</a>
        }
        @if (permisosRuntime.puede('Proveedores', 'Ver')) {
          <a routerLink="/proveedores" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">local_shipping</span> Proveedores</a>
        }
        @if (permisosRuntime.puede('Clientes', 'Ver')) {
          <a routerLink="/clientes" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">groups</span> Clientes</a>
        }
        @if (permisosRuntime.puede('CargasMasivas', 'Ver')) {
          <a routerLink="/cargas-masivas" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">upload_file</span> Cargas masivas</a>
        }
      </section>
    }

    @if (permisosRuntime.puede('Compras', 'Ver')) {
      <section class="nav-group" aria-labelledby="nav-compras">
        <h2 id="nav-compras" class="nav-group__title">Compras</h2>
        <a routerLink="/solicitudes-compra" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">request_quote</span> Solicitudes</a>
        <a routerLink="/ordenes-compra" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">receipt_long</span> Órdenes</a>
        <a routerLink="/recepciones-compra" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">inventory_2</span> Recepciones</a>
        <a routerLink="/devoluciones-proveedor" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">assignment_return</span> Devoluciones</a>
        <a routerLink="/compras" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">shopping_cart</span> Compras</a>
      </section>
    }

    @if (permisosRuntime.puede('Ventas', 'Ver')) {
      <section class="nav-group" aria-labelledby="nav-ventas">
        <h2 id="nav-ventas" class="nav-group__title">Ventas</h2>
        <a routerLink="/ventas" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">point_of_sale</span> Ventas</a>
        <a routerLink="/pedidos-venta" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">shopping_bag</span> Pedidos</a>
      </section>
    }

    @if (permisosRuntime.puede('MovimientosInventario', 'Ver') || permisosRuntime.puede('Inventario', 'Ver')) {
      <section class="nav-group" aria-labelledby="nav-inventario">
        <h2 id="nav-inventario" class="nav-group__title">Inventario</h2>
        @if (permisosRuntime.puede('MovimientosInventario', 'Ver')) {
          <a routerLink="/inventario/movimientos" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">sync_alt</span> Movimientos</a>
          <a routerLink="/inventario/transferencias" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">swap_horiz</span> Transferencias</a>
          <a routerLink="/inventario/conteos" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">fact_check</span> Conteos físicos</a>
          <a routerLink="/inventario/reservas" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">event_available</span> Reservas</a>
          <a routerLink="/inventario/costeo" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">calculate</span> Costeo</a>
        }
        @if (permisosRuntime.puede('Inventario', 'Ver')) {
          <a routerLink="/inventario/ajustes" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">tune</span> Ajustes</a>
        }
      </section>
    }

    @if (permisosRuntime.puede('Finanzas', 'Ver') || (permisosRuntime.esAdministrador() && permisosRuntime.puede('ReportesAdministrativos', 'Ver'))) {
      <section class="nav-group" aria-labelledby="nav-finanzas">
        <h2 id="nav-finanzas" class="nav-group__title">Finanzas</h2>
        @if (permisosRuntime.puede('Finanzas', 'Ver')) {
          <a routerLink="/finanzas" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">account_balance_wallet</span> Finanzas</a>
          <a routerLink="/plan-cuentas" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">account_tree</span> Plan de cuentas</a>
          <a routerLink="/estados-financieros" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">assessment</span> Estados financieros</a>
        }
        @if (permisosRuntime.esAdministrador() && permisosRuntime.puede('ReportesAdministrativos', 'Ver')) {
          <a routerLink="/centro-reportes" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">analytics</span> Centro de reportes</a>
        }
      </section>
    }

    @if (tieneAdministracion()) {
      <section class="nav-group" aria-labelledby="nav-administracion">
        <h2 id="nav-administracion" class="nav-group__title">Administración</h2>
        @if (permisosRuntime.puede('Usuarios', 'Ver')) {
          <a routerLink="/usuarios" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">manage_accounts</span> Usuarios</a>
        }
        @if (permisosRuntime.puede('Roles', 'Ver')) {
          <a routerLink="/roles" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">admin_panel_settings</span> Roles</a>
        }
        @if (permisosRuntime.puede('Permisos', 'Administrar')) {
          <a routerLink="/permisos" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">lock_outline</span> Permisos</a>
        }
        @if (permisosRuntime.esAdministrador() && permisosRuntime.puede('Auditoria', 'Ver')) {
          <a routerLink="/auditoria" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">manage_search</span> Auditoría</a>
        }
        @if (permisosRuntime.puede('Configuracion', 'Ver')) {
          <a routerLink="/configuracion" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">settings</span> Configuración</a>
          <a routerLink="/periodos-contables" routerLinkActive="active" ariaCurrentWhenActive="page"><span class="material-icons" aria-hidden="true">calendar_month</span> Periodos contables</a>
        }
      </section>
    }
  `,
  styles: [`
    :host { display: flex; flex-direction: column; gap: 10px; }
    .nav-group { display: grid; gap: 4px; }
    .nav-group__title {
      margin: 8px 12px 2px;
      color: color-mix(in srgb, var(--color-on-sidebar) 68%, transparent);
      font-size: 10px;
      font-weight: 800;
      letter-spacing: .08em;
      line-height: 1.2;
      text-transform: uppercase;
    }
    a {
      min-width: 0;
      min-height: 46px;
      display: flex;
      align-items: center;
      gap: 12px;
      padding: 11px 16px;
      border-radius: 9px;
      color: var(--color-on-sidebar);
      font-size: 14px;
      line-height: 1.3;
      text-decoration: none;
      overflow-wrap: anywhere;
      transition: background var(--motion-fast) var(--ease-standard), color var(--motion-fast) var(--ease-standard), transform var(--motion-fast) var(--ease-standard);
    }
    a .material-icons { flex: 0 0 24px; width: 24px; font-size: 24px; line-height: 1; }
    a:hover { background: color-mix(in srgb, var(--color-on-sidebar) 10%, transparent); color: var(--color-on-sidebar); }
    a:focus-visible { outline-color: var(--color-on-sidebar); box-shadow: 0 0 0 3px color-mix(in srgb, var(--color-on-sidebar) 28%, transparent); }
    a.active { background: var(--color-button); color: var(--color-on-primary); box-shadow: 0 8px 18px rgba(0, 0, 0, .16); }
  `]
})
export class AppNavigationMenuComponent {
  constructor(public permisosRuntime: PermisosRuntimeService) {}

  tieneCatalogo(): boolean {
    return [
      'Productos', 'Categorias', 'Colores', 'Tallas', 'Marcas', 'Modelos', 'MetodosPago', 'Descuentos', 'Impuestos'
    ].some(modulo => this.permisosRuntime.puede(modulo, 'Ver'));
  }

  tieneOperacion(): boolean {
    return ['Sucursales', 'Almacenes', 'UbicacionesAlmacen', 'Proveedores', 'Clientes', 'CargasMasivas']
      .some(modulo => this.permisosRuntime.puede(modulo, 'Ver'));
  }

  tieneAdministracion(): boolean {
    return this.permisosRuntime.puede('Usuarios', 'Ver')
      || this.permisosRuntime.puede('Roles', 'Ver')
      || this.permisosRuntime.puede('Permisos', 'Administrar')
      || (this.permisosRuntime.esAdministrador() && this.permisosRuntime.puede('Auditoria', 'Ver'))
      || this.permisosRuntime.puede('Configuracion', 'Ver');
  }
}
