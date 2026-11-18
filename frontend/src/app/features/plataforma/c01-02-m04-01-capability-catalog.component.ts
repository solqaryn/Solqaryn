import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

/** Snapshot de rutas ERP protegidas verificadas en dev. No es un motor RBAC. */
export const C0102_CATALOG = [
  {
    "ruta": "/dashboard",
    "modulo": "Dashboard",
    "accion": "Ver"
  },
  {
    "ruta": "/productos",
    "modulo": "Productos",
    "accion": "Ver"
  },
  {
    "ruta": "/productos/nuevo",
    "modulo": "Productos",
    "accion": "Crear"
  },
  {
    "ruta": "/productos/:id/editar",
    "modulo": "Productos",
    "accion": "Editar"
  },
  {
    "ruta": "/productos/:id/variantes",
    "modulo": "Productos",
    "accion": "Editar"
  },
  {
    "ruta": "/productos/:id",
    "modulo": "Productos",
    "accion": "Ver"
  },
  {
    "ruta": "/categorias",
    "modulo": "Categorias",
    "accion": "Ver"
  },
  {
    "ruta": "/categorias/nueva",
    "modulo": "Categorias",
    "accion": "Crear"
  },
  {
    "ruta": "/categorias/:id/editar",
    "modulo": "Categorias",
    "accion": "Editar"
  },
  {
    "ruta": "/sucursales",
    "modulo": "Sucursales",
    "accion": "Ver"
  },
  {
    "ruta": "/sucursales/nueva",
    "modulo": "Sucursales",
    "accion": "Crear"
  },
  {
    "ruta": "/sucursales/:id/editar",
    "modulo": "Sucursales",
    "accion": "Editar"
  },
  {
    "ruta": "/colores",
    "modulo": "Colores",
    "accion": "Ver"
  },
  {
    "ruta": "/tallas",
    "modulo": "Tallas",
    "accion": "Ver"
  },
  {
    "ruta": "/marcas",
    "modulo": "Marcas",
    "accion": "Ver"
  },
  {
    "ruta": "/modelos",
    "modulo": "Modelos",
    "accion": "Ver"
  },
  {
    "ruta": "/metodos-pago",
    "modulo": "MetodosPago",
    "accion": "Ver"
  },
  {
    "ruta": "/proveedores",
    "modulo": "Proveedores",
    "accion": "Ver"
  },
  {
    "ruta": "/proveedores/nuevo",
    "modulo": "Proveedores",
    "accion": "Crear"
  },
  {
    "ruta": "/proveedores/:id/editar",
    "modulo": "Proveedores",
    "accion": "Editar"
  },
  {
    "ruta": "/clientes",
    "modulo": "Clientes",
    "accion": "Ver"
  },
  {
    "ruta": "/clientes/nuevo",
    "modulo": "Clientes",
    "accion": "Crear"
  },
  {
    "ruta": "/clientes/:id/editar",
    "modulo": "Clientes",
    "accion": "Editar"
  },
  {
    "ruta": "/tipo-clientes",
    "modulo": "TiposClientes",
    "accion": "Ver"
  },
  {
    "ruta": "/tipo-clientes/nuevo",
    "modulo": "TiposClientes",
    "accion": "Crear"
  },
  {
    "ruta": "/tipo-clientes/:id/editar",
    "modulo": "TiposClientes",
    "accion": "Editar"
  },
  {
    "ruta": "/usuarios",
    "modulo": "Usuarios",
    "accion": "Ver"
  },
  {
    "ruta": "/usuarios/:id/editar",
    "modulo": "Usuarios",
    "accion": "Editar"
  },
  {
    "ruta": "/usuarios/:id",
    "modulo": "Usuarios",
    "accion": "Ver"
  },
  {
    "ruta": "/roles",
    "modulo": "Roles",
    "accion": "Ver"
  },
  {
    "ruta": "/roles/nuevo",
    "modulo": "Roles",
    "accion": "Crear"
  },
  {
    "ruta": "/roles/:id/editar",
    "modulo": "Roles",
    "accion": "Editar"
  },
  {
    "ruta": "/permisos",
    "modulo": "Permisos",
    "accion": "Administrar"
  },
  {
    "ruta": "/descuentos",
    "modulo": "Descuentos",
    "accion": "Ver"
  },
  {
    "ruta": "/descuentos/nuevo",
    "modulo": "Descuentos",
    "accion": "Crear"
  },
  {
    "ruta": "/descuentos/:id/editar",
    "modulo": "Descuentos",
    "accion": "Editar"
  },
  {
    "ruta": "/impuestos",
    "modulo": "Impuestos",
    "accion": "Ver"
  },
  {
    "ruta": "/impuestos/nuevo",
    "modulo": "Impuestos",
    "accion": "Crear"
  },
  {
    "ruta": "/impuestos/:id/editar",
    "modulo": "Impuestos",
    "accion": "Editar"
  },
  {
    "ruta": "/costos-envio",
    "modulo": "Facturacion",
    "accion": "Administrar"
  },
  {
    "ruta": "/cargas-masivas",
    "modulo": "CargasMasivas",
    "accion": "Ver"
  },
  {
    "ruta": "/auditoria",
    "modulo": "Auditoria",
    "accion": "Ver"
  },
  {
    "ruta": "/solicitudes-compra",
    "modulo": "Compras",
    "accion": "Ver"
  },
  {
    "ruta": "/ordenes-compra",
    "modulo": "Compras",
    "accion": "Ver"
  },
  {
    "ruta": "/ordenes-compra/nueva",
    "modulo": "Compras",
    "accion": "Crear"
  },
  {
    "ruta": "/ordenes-compra/:id/editar",
    "modulo": "Compras",
    "accion": "Editar"
  },
  {
    "ruta": "/compras",
    "modulo": "Compras",
    "accion": "Ver"
  },
  {
    "ruta": "/compras/nueva",
    "modulo": "Compras",
    "accion": "Crear"
  },
  {
    "ruta": "/compras/:id/editar",
    "modulo": "Compras",
    "accion": "Editar"
  },
  {
    "ruta": "/compras/:id",
    "modulo": "Compras",
    "accion": "Ver"
  },
  {
    "ruta": "/ventas",
    "modulo": "Ventas",
    "accion": "Ver"
  },
  {
    "ruta": "/ventas/nueva",
    "modulo": "Ventas",
    "accion": "Crear"
  },
  {
    "ruta": "/ventas/:id/editar",
    "modulo": "Ventas",
    "accion": "Editar"
  },
  {
    "ruta": "/ventas/:id",
    "modulo": "Ventas",
    "accion": "Ver"
  },
  {
    "ruta": "/facturas/:id/pagos",
    "modulo": "Facturacion",
    "accion": "Aplicar"
  },
  {
    "ruta": "/facturas/:id",
    "modulo": "Facturacion",
    "accion": "Ver"
  },
  {
    "ruta": "/cuentas-por-cobrar",
    "modulo": "Facturacion",
    "accion": "Ver"
  },
  {
    "ruta": "/asientos-contables",
    "modulo": "Finanzas",
    "accion": "Ver"
  },
  {
    "ruta": "/finanzas",
    "modulo": "Finanzas",
    "accion": "Ver"
  },
  {
    "ruta": "/estados-financieros",
    "modulo": "Finanzas",
    "accion": "Ver"
  },
  {
    "ruta": "/plan-cuentas",
    "modulo": "Finanzas",
    "accion": "Ver"
  },
  {
    "ruta": "/cuentas-bancarias",
    "modulo": "Finanzas",
    "accion": "Ver"
  },
  {
    "ruta": "/inventario/movimientos",
    "modulo": "MovimientosInventario",
    "accion": "Ver"
  },
  {
    "ruta": "/inventario/costeo",
    "modulo": "MovimientosInventario",
    "accion": "Ver"
  },
  {
    "ruta": "/configuracion",
    "modulo": "Configuracion",
    "accion": "Ver"
  },
  {
    "ruta": "/periodos-contables",
    "modulo": "Configuracion",
    "accion": "Ver"
  }
] as const;

@Component({
  selector: 'app-c0102-capability-catalog',
  standalone: true,
  imports: [RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <main class="catalogo" aria-labelledby="c0102-title">
      <a routerLink="/plataforma">Volver a plataforma</a>
      <h1 id="c0102-title">Catálogo global de módulos y capacidades</h1>
      <p role="note">Información de rutas protegidas, no fuente de permisos. La autorización efectiva se resuelve exclusivamente en el servidor.</p>
      @if (!autorizado()) {
        <p role="alert" data-testid="c0102-oculto">No se verificó autoridad de plataforma. Catálogo oculto.</p>
      } @else {
        <ol data-testid="c0102-listado">
          @for (fila of filas(); track fila.ruta) {
            <li>
              <h2>{{ fila.modulo }} · {{ fila.accion }}</h2>
              <p>Ruta: {{ fila.ruta }}</p>
              <p [attr.data-evidence-state]="fila.estado">{{ fila.estado }} — {{ fila.causa }}</p>
            </li>
          }
        </ol>
      }
    </main>
  `,
  styles: [`
    .catalogo { max-width: 1120px; padding: clamp(16px, 3vw, 32px); margin: auto; }
    ol { list-style: none; display: grid; gap: 16px; padding: 0; grid-template-columns: repeat(auto-fit,minmax(min(100%,260px),1fr)); }
    li { border: 1px solid var(--color-border, #8193a5); border-radius: 12px; padding: 14px; overflow-wrap: anywhere; }
    h2 { font-size: 1rem; } a:focus-visible { outline: 3px solid var(--color-primary,#0868ad); outline-offset: 4px; }
  `]
})
export class C0102CapabilityCatalogComponent {
  private readonly plataforma = inject(PlataformaSesionService);
  private readonly tenant = inject(TenantContextService);
  private readonly permisos = inject(PermisosRuntimeService);
  readonly autorizado = computed(() => {
    const sesion = this.plataforma.sesion();
    return sesion?.ambito === 'Plataforma' && sesion.esAdministrador === true;
  });
  readonly filas = computed(() => {
    if (!this.autorizado()) return [];
    return C0102_CATALOG.map(ruta => {
      const base = { ...ruta };
      if (!this.tenant.tieneContextoVerificado())
        return { ...base, estado: 'SIN_TENANT_VERIFICADO', causa: 'No hay contexto y membresía empresarial verificados.' };
      if (!this.permisos.cargado())
        return { ...base, estado: 'PERMISOS_NO_CONFIRMADOS', causa: 'La API no confirmó permisos del contexto actual.' };
      if (!this.permisos.puede(ruta.modulo, ruta.accion))
        return { ...base, estado: 'PERMISO_O_CAPACIDAD_NO_CONFIRMADA', causa: 'No se confirmó simultáneamente permiso y capacidad; plan/rol/rubro no identificables desde esta señal.' };
      if (!this.permisos.rutaHabilitada(ruta.ruta))
        return { ...base, estado: 'RUTA_RESTRINGIDA', causa: 'No se confirmó la ruta habilitada para este contexto.' };
      return { ...base, estado: 'SENAL_CLIENTE_NO_AUTORITATIVA', causa: 'Coinciden señales UI; cada solicitud todavía exige autorización de servidor.' };
    });
  });
}
