import { signal } from '@angular/core';
import { of } from 'rxjs';
import { PermisoService } from '../../services/permiso.service';
import { TenantContextService } from './tenant-context.service';
import { PermisosRuntimeService } from './permisos-runtime.service';

describe('MF3 - permisos vs capacidades empresariales', () => {
  const empresa = signal<number | null>(11);
  let response: any;
  let runtime: PermisosRuntimeService;

  function reply(data: Record<string, unknown>) {
    return { success: true, data, message: '', errors: [] };
  }

  beforeEach(() => {
    empresa.set(11);
    response = reply({
      esAdministrador: true,
      rol: 'Administrador',
      permisos: ['Dashboard:Ver', 'Productos:Ver', 'Productos:Crear'],
      operacionMultiEmpresaRestringida: true,
      capacidadesHabilitadas: ['Productos:Ver'],
      rutasHabilitadas: ['productos', 'productos/:id']
    });
    const permisoService = { getMisPermisos: () => of(response) } as unknown as PermisoService;
    const tenantContext = {
      empresaIdVerificada: () => empresa(),
      tieneContextoVerificado: () => empresa() !== null
    } as unknown as TenantContextService;
    runtime = new PermisosRuntimeService(permisoService, tenantContext);
  });

  it('administrador no puede abrir dashboard ni crear sin capacidad habilitada', () => {
    runtime.cargar().subscribe();
    expect(runtime.esAdministrador()).toBe(true);
    expect(runtime.puede('Productos', 'Ver')).toBe(true);
    expect(runtime.puede('Productos', 'Crear')).toBe(false);
    expect(runtime.puede('Dashboard', 'Ver')).toBe(false);
    expect(runtime.rutaHabilitada('productos')).toBe(true);
    expect(runtime.rutaHabilitada('productos/:id')).toBe(true);
    expect(runtime.rutaHabilitada('ordenes-compra')).toBe(false);
    expect(runtime.rutaInicialPermitida()).toBe('/productos');
  });

  it('no confiere permiso aunque exista capacidad si el rol no tiene grant', () => {
    response = reply({
      esAdministrador: true, rol: 'Administrador', permisos: [],
      operacionMultiEmpresaRestringida: true,
      capacidadesHabilitadas: ['Productos:Ver'],
      rutasHabilitadas: ['productos']
    });
    runtime.cargar().subscribe();
    expect(runtime.puede('Productos', 'Ver')).toBe(false);
    expect(runtime.rutaInicialPermitida()).toBe('/perfil');
  });

  it('una empresa mantiene su navegación legacy sin depender de pilotos', () => {
    response = reply({
      esAdministrador: false, rol: 'Vendedor',
      permisos: ['Dashboard:Ver'],
      operacionMultiEmpresaRestringida: false,
      capacidadesHabilitadas: [],
      rutasHabilitadas: []
    });
    runtime.cargar().subscribe();
    expect(runtime.puede('Dashboard', 'Ver')).toBe(true);
    expect(runtime.rutaHabilitada('dashboard')).toBe(true);
    expect(runtime.rutaInicialPermitida()).toBe('/dashboard');
  });

  it('rechaza payload anterior sin proyección de capacidades en modo seguro', () => {
    response = reply({
      esAdministrador: true, rol: 'Administrador',
      permisos: ['Productos:Ver']
    });
    runtime.cargar().subscribe();
    expect(runtime.puede('Productos', 'Ver')).toBe(false);
    expect(runtime.rutaInicialPermitida()).toBe('/perfil');
  });

  it('no permite reutilizar concesiones del tenant A tras seleccionar B', () => {
    runtime.cargar().subscribe();
    empresa.set(22);
    expect(runtime.puede('Productos', 'Ver')).toBe(false);
    expect(runtime.rutaHabilitada('productos')).toBe(false);
    response = reply({
      esAdministrador: true, rol: 'Administrador',
      permisos: ['Clientes:Ver'],
      operacionMultiEmpresaRestringida: true,
      capacidadesHabilitadas: [],
      rutasHabilitadas: []
    });
    runtime.cargar().subscribe();
    expect(runtime.puede('Productos', 'Ver')).toBe(false);
    expect(runtime.rutaHabilitada('productos')).toBe(false);
    expect(runtime.rutaInicialPermitida()).toBe('/perfil');
  });

  it('revoca todas las capacidades al cerrar sesión', () => {
    runtime.cargar().subscribe();
    runtime.limpiar();
    expect(runtime.puede('Productos', 'Ver')).toBe(false);
    expect(runtime.rutaHabilitada('productos')).toBe(false);
  });
});
