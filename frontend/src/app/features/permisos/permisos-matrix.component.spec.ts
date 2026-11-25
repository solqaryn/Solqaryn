import { of, Subject } from 'rxjs';
import { MatSnackBar } from '@angular/material/snack-bar';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { ApiResponse } from '../../core/models/api-response.model';
import { PermisoCatalogo, PermisoMatrizItem } from '../../core/models/permiso.model';
import { Rol } from '../../core/models/rol.model';
import { PermisoService } from '../../services/permiso.service';
import { RolService } from '../../services/rol.service';
import { PermisosMatrixComponent } from './permisos-matrix.component';

const ok = <T>(data: T): ApiResponse<T> => ({ success: true, data, message: '', errors: [] });
const admin: Rol = { id: 1, nombre: 'Administrador', esSistema: true, esAdministrador: true, activo: true, cantidadUsuarios: 1, cantidadPermisos: 1, fechaCreacion: '' };
const vendedor: Rol = { ...admin, id: 2, nombre: 'Vendedor', esSistema: false, esAdministrador: false, cantidadUsuarios: 0 };
const permiso: PermisoCatalogo = { id: 1, codigo: 'Productos.Ver', nombre: 'Ver productos', modulo: 'Productos', accion: 'Ver', esSistema: true, activo: true, cantidadRolesAsignados: 1, fechaCreacion: '' };

describe('Catálogo y matriz de permisos', () => {
  function preparar(roles: Rol[], puedeVer = true) {
    const primera = new Subject<ApiResponse<PermisoMatrizItem[]>>();
    const segunda = new Subject<ApiResponse<PermisoMatrizItem[]>>();
    const getCatalogo = vi.fn(() => of(ok([permiso])));
    const getMatriz = vi.fn((id: number) => id === 2 ? primera : segunda);
    const updateMatriz = vi.fn(() => of(ok([])));
    const componente = new PermisosMatrixComponent(
      { getCatalogo, getMatriz, updateMatriz } as unknown as PermisoService,
      { getAll: () => of(ok(roles)) } as unknown as RolService,
      { open: vi.fn() } as unknown as MatSnackBar,
      { puede: () => puedeVer } as unknown as PermisosRuntimeService
    );
    return { componente, primera, segunda, getCatalogo, getMatriz, updateMatriz };
  }

  it('consulta el catálogo incluso si solamente existe el administrador protegido', () => {
    const { componente, getCatalogo, getMatriz, updateMatriz } = preparar([admin]);
    componente.ngOnInit();
    expect(getCatalogo).toHaveBeenCalledOnce();
    expect(componente.catalogo()).toEqual([permiso]);
    expect(componente.roles()).toEqual([]);
    expect(getMatriz).not.toHaveBeenCalled();
    componente.guardar();
    expect(updateMatriz).not.toHaveBeenCalled();
    componente.filtroCatalogo.set('PRODUCTOS');
    expect(componente.catalogoFiltrado()).toHaveLength(1);
  });

  it('no consulta el catálogo si falta Permisos.Ver', () => {
    const { componente, getCatalogo } = preparar([admin], false);
    componente.ngOnInit();
    expect(getCatalogo).not.toHaveBeenCalled();
    expect(componente.errorCatalogo()).toContain('autorización');
  });

  it('descarta respuestas del rol anterior y no guarda matrices durante carga', () => {
    const { componente, primera, segunda, updateMatriz } = preparar([admin, vendedor, { ...vendedor, id: 3, nombre: 'Cajero' }]);
    componente.ngOnInit();
    componente.seleccionarRol(3);
    primera.next(ok([{ rol: 'Vendedor', modulo: 'Productos', accion: 'Ver', permitido: true }]));
    expect(componente.items()).toEqual([]);
    componente.guardar();
    expect(updateMatriz).not.toHaveBeenCalled();
    segunda.next(ok([{ rol: 'Cajero', modulo: 'Productos', accion: 'Ver', permitido: false }]));
    expect(componente.items()[0].rol).toBe('Cajero');
    expect(componente.permiteGuardar()).toBe(true);
  });
});
