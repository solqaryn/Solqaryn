import { Injectable, computed, signal } from '@angular/core';
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
  private readonly _empresaPermisosId = signal<number | null>(null);
  private readonly _operacionRestringida = signal(true);
  private readonly _capacidades = signal<Set<string>>(new Set());
  private readonly _rutasHabilitadas = signal<Set<string>>(new Set());
  // Una petición de permisos anterior nunca puede reemplazar ni revocar la
  // autorización de la empresa seleccionada posteriormente.
  private versionCarga = 0;

  readonly esAdministrador = computed(() =>
    this._cargado() &&
    this._esAdministrador() &&
    this.tenantContext.tieneContextoVerificado() &&
    this._empresaPermisosId() === this.tenantContext.empresaIdVerificada()
  );
  readonly cargado = computed(() =>
    this._cargado() &&
    this.tenantContext.tieneContextoVerificado() &&
    this._empresaPermisosId() === this.tenantContext.empresaIdVerificada()
  );

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

    // Revocar caches y rutas en memoria ANTES de volver a pedir permisos.
    // La revisión separa peticiones solapadas, incluso del mismo tenant.
    const version = ++this.versionCarga;
    this.limpiarEstado();

    return this.permisoService.getMisPermisos(empresaId).pipe(
      map((res) => {
        // Una respuesta antigua no puede borrar los permisos del nuevo tenant.
        if (version !== this.versionCarga ||
            this.tenantContext.empresaIdVerificada() !== empresaId ||
            !this.tenantContext.tieneContextoVerificado()) {
          return false;
        }

        // La API es la única fuente de capacidades; respuestas antiguas, parciales
        // o de otra empresa no autorizan ningún módulo.
        const data = res?.success === true ? res.data : null;
        if (!data ||
            typeof data.operacionMultiEmpresaRestringida !== 'boolean' ||
            !Array.isArray(data.permisos) ||
            !Array.isArray(data.capacidadesHabilitadas) ||
            !Array.isArray(data.rutasHabilitadas)) {
          throw new Error('La API no confirmó permisos y capacidades empresariales.');
        }
        this._empresaPermisosId.set(empresaId);
        this._permisos.set(new Set(data.permisos));
        this._esAdministrador.set(data.esAdministrador === true);
        this._operacionRestringida.set(data.operacionMultiEmpresaRestringida);
        this._capacidades.set(new Set(data.capacidadesHabilitadas));
        this._rutasHabilitadas.set(new Set(data.rutasHabilitadas));
        this._cargado.set(true);
        return true;
      }),
      catchError(() => {
        // Un error HTTP de A recibido después del éxito de B no puede
        // invalidar los permisos ni la navegación actual de B.
        if (version === this.versionCarga &&
            this.tenantContext.empresaIdVerificada() === empresaId &&
            this.tenantContext.tieneContextoVerificado()) {
          this.limpiarEstado();
          this._cargado.set(true);
        }
        return of(false);
      })
    );
  }

  limpiar(): void {
    this.versionCarga++;
    this.limpiarEstado();
  }

  private limpiarEstado(): void {
    this._empresaPermisosId.set(null);
    this._permisos.set(new Set());
    this._esAdministrador.set(false);
    this._operacionRestringida.set(true);
    this._capacidades.set(new Set());
    this._rutasHabilitadas.set(new Set());
    this._cargado.set(false);
  }

  puede(modulo: string, accion: string): boolean {
    if (!this._cargado() || !this.tenantContext.tieneContextoVerificado() ||
        this._empresaPermisosId() !== this.tenantContext.empresaIdVerificada()) return false;
    const clave = `${modulo}:${accion}`;
    // Administrador también requiere grants explícitos y capacidad operativa.
    return this._permisos().has(clave) &&
      (!this._operacionRestringida() || this._capacidades().has(clave));
  }

  rutaHabilitada(ruta: string | undefined | null): boolean {
    if (!this._cargado() || !this.tenantContext.tieneContextoVerificado() ||
        this._empresaPermisosId() !== this.tenantContext.empresaIdVerificada()) return false;
    if (!this._operacionRestringida()) return true;
    return !!ruta && this._rutasHabilitadas().has(ruta.startsWith('/') ? ruta.slice(1) : ruta);
  }

  rutaInicialPermitida(): string | null {
    if (!this.tenantContext.tieneContextoVerificado()) return null;
    if (this.puede('Dashboard', 'Ver') && this.rutaHabilitada('dashboard'))
      return '/dashboard';
    return RUTAS_PROTEGIDAS.find(item =>
      !('soloAdministrador' in item) &&
      this.puede(item.modulo, item.accion) &&
      this.rutaHabilitada(item.ruta.substring(1))
    )?.ruta ?? '/perfil';
  }
}
