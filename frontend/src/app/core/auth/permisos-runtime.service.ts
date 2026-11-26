import { Injectable, signal } from '@angular/core';
import { catchError, map, of } from 'rxjs';
import { PermisoService } from '../../services/permiso.service';
import { TenantContextService } from './tenant-context.service';

const RUTAS_PROTEGIDAS = [
  { ruta: '/dashboard', modulo: 'Dashboard', accion: 'Ver' },
  { ruta: '/productos', modulo: 'Productos', accion: 'Ver' },
  { ruta: '/categorias', modulo: 'Categorias', accion: 'Ver' },
  { ruta: '/compras', modulo: 'Compras', accion: 'Ver' },
  { ruta: '/facturas-proveedor', modulo: 'Compras', accion: 'Ver' },
  { ruta: '/proveedores', modulo: 'Proveedores', accion: 'Ver' },
  { ruta: '/ventas', modulo: 'Ventas', accion: 'Ver' },
  { ruta: '/clientes', modulo: 'Clientes', accion: 'Ver' },
  { ruta: '/costos-envio', modulo: 'Facturacion', accion: 'Administrar' },
  { ruta: '/finanzas', modulo: 'Finanzas', accion: 'Ver' },
  { ruta: '/plan-cuentas', modulo: 'Finanzas', accion: 'Ver' },
  { ruta: '/inventario/movimientos', modulo: 'MovimientosInventario', accion: 'Ver' },
  { ruta: '/inventario/transferencias', modulo: 'MovimientosInventario', accion: 'Ver' },
  { ruta: '/inventario/ajustes', modulo: 'Inventario', accion: 'Ver' },
  { ruta: '/cargas-masivas', modulo: 'CargasMasivas', accion: 'Ver' },
  { ruta: '/usuarios', modulo: 'Usuarios', accion: 'Ver' },
  { ruta: '/roles', modulo: 'Roles', accion: 'Ver' },
  { ruta: '/descuentos', modulo: 'Descuentos', accion: 'Ver' },
  { ruta: '/impuestos', modulo: 'Impuestos', accion: 'Ver' },
  { ruta: '/permisos', modulo: 'Permisos', accion: 'Administrar' },
  { ruta: '/auditoria', modulo: 'Auditoria', accion: 'Ver', soloAdministrador: true },
  { ruta: '/configuracion', modulo: 'Configuracion', accion: 'Ver' }
] as const;

@Injectable({ providedIn: 'root' })
export class PermisosRuntimeService {
  private readonly _permisos = signal<Set<string>>(new Set());
  private readonly _esAdministrador = signal(false);
  private readonly _cargado = signal(false);

  readonly esAdministrador = this._esAdministrador.asReadonly();
  readonly cargado = this._cargado.asReadonly();

  constructor(
    private permisoService: PermisoService,
    private tenantContext: TenantContextService
  ) {}

  cargar() {
    const empresaId = this.tenantContext.empresaIdVerificada();
    if (!empresaId) {
      this.limpiar();
      return of(false);
    }

    return this.permisoService.getMisPermisos(empresaId).pipe(
      map((res) => {
        // No aceptar una respuesta si el tenant cambió mientras la petición estaba en vuelo.
        if (this.tenantContext.empresaIdVerificada() !== empresaId) {
          this.limpiar();
          return false;
        }

        this._permisos.set(new Set(res.data.permisos));
        this._esAdministrador.set(res.data.esAdministrador);
        this._cargado.set(true);
        return true;
      }),
      catchError(() => {
        this._permisos.set(new Set());
        this._esAdministrador.set(false);
        this._cargado.set(true);
        return of(false);
      })
    );
  }

  limpiar(): void {
    this._permisos.set(new Set());
    this._esAdministrador.set(false);
    this._cargado.set(false);
  }

  puede(modulo: string, accion: string): boolean {
    if (!this.tenantContext.tieneContextoVerificado()) return false;
    if (this._esAdministrador()) return true;
    return this._permisos().has(`${modulo}:${accion}`);
  }

  rutaInicialPermitida(): string | null {
    if (!this.tenantContext.tieneContextoVerificado()) return null;
    if (this._esAdministrador()) return '/dashboard';
    return RUTAS_PROTEGIDAS.find(item =>
      !('soloAdministrador' in item) && this.puede(item.modulo, item.accion)
    )?.ruta ?? null;
  }
}
