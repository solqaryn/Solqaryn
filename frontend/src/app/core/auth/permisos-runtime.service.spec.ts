import { computed, signal } from '@angular/core';
import { of, Subject } from 'rxjs';
import { PermisoService } from '../../services/permiso.service';
import { TenantContextService } from './tenant-context.service';
import { PermisosRuntimeService } from './permisos-runtime.service';

describe('MF3 - permisos vs capacidades empresariales', () => {
  const empresa = signal<number | null>(11);
  const actor = signal(1);
  const rolId = signal(4);
  const sesionVigente = signal(true);
  const contexto = computed(() => {
    const e = empresa();
    return e !== null && sesionVigente() ? { usuarioId: actor(), empresaId: e, rolId: rolId(), rolNombre: 'Operador', esAdministrador: false } : null;
  });
  let response: any;
  let runtime: PermisosRuntimeService;

  function reply(data: Record<string, unknown>) {
    return { success: true, data, message: '', errors: [] };
  }

  beforeEach(() => {
    empresa.set(11);
    actor.set(1);
    rolId.set(4);
    sesionVigente.set(true);
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
      tieneContextoVerificado: () => empresa() !== null,
      contextoParaPermisos: () => contexto()
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
    expect(runtime.rutaHabilitada('centro-reportes/ventas')).toBe(false);
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
  it('comparte consulta concurrente del mismo tenant sin denegar al primer guard', () => {
    const pendiente = new Subject<any>();
    let llamadas = 0;
    const permisoService = {
      getMisPermisos: (_id: number) => {
        llamadas++;
        return pendiente.asObservable();
      }
    } as unknown as PermisoService;
    const tenantContext = {
      empresaIdVerificada: () => empresa(),
      tieneContextoVerificado: () => empresa() !== null,
      contextoParaPermisos: () => contexto()
    } as unknown as TenantContextService;
    runtime = new PermisosRuntimeService(permisoService, tenantContext);

    const resultadosA: boolean[] = [];
    const resultadosB: boolean[] = [];
    runtime.cargar().subscribe(ok => resultadosA.push(ok));
    runtime.cargar().subscribe(ok => resultadosB.push(ok));

    expect(llamadas).toBe(1);
    pendiente.next(response);
    pendiente.complete();

    expect(resultadosA).toEqual([true]);
    expect(resultadosB).toEqual([true]);
    expect(runtime.puede('Productos', 'Ver')).toBe(true);
    expect(runtime.cargado()).toBe(true);
  });

  it('oculta inmediatamente permisos, rutas y rol administrador al pedir otra empresa', () => {
    runtime.cargar().subscribe();
    expect(runtime.esAdministrador()).toBe(true);
    expect(runtime.cargado()).toBe(true);

    empresa.set(22);
    expect(runtime.cargado()).toBe(false);
    expect(runtime.esAdministrador()).toBe(false);
    expect(runtime.puede('Productos', 'Ver')).toBe(false);
    expect(runtime.rutaHabilitada('productos')).toBe(false);
  });

  it('la respuesta tardía de A no sobrescribe los permisos ya cargados de B', () => {
    const pendientes = new Map<number, Subject<any>>();
    const permisoService = {
      getMisPermisos: (id: number) => {
        const solicitud = new Subject<any>();
        pendientes.set(id, solicitud);
        return solicitud.asObservable();
      }
    } as unknown as PermisoService;
    const tenantContext = {
      empresaIdVerificada: () => empresa(),
      tieneContextoVerificado: () => empresa() !== null,
      contextoParaPermisos: () => contexto()
    } as unknown as TenantContextService;
    runtime = new PermisosRuntimeService(permisoService, tenantContext);

    runtime.cargar().subscribe();
    empresa.set(22);
    runtime.cargar().subscribe();
    pendientes.get(22)!.next(reply({
      esAdministrador: false, permisos: ['Clientes:Ver'],
      operacionMultiEmpresaRestringida: true,
      capacidadesHabilitadas: ['Clientes:Ver'],
      rutasHabilitadas: ['clientes']
    }));
    pendientes.get(22)!.complete();

    expect(runtime.puede('Clientes', 'Ver')).toBe(true);
    pendientes.get(11)!.next(reply({
      esAdministrador: true, permisos: ['Productos:Ver'],
      operacionMultiEmpresaRestringida: false,
      capacidadesHabilitadas: [], rutasHabilitadas: []
    }));
    pendientes.get(11)!.complete();

    expect(runtime.puede('Clientes', 'Ver')).toBe(true);
    expect(runtime.puede('Productos', 'Ver')).toBe(false);
    expect(runtime.rutaHabilitada('clientes')).toBe(true);
    expect(runtime.esAdministrador()).toBe(false);
  });

  it('el error tardío de A no limpia los permisos validados de B', () => {
    const pendientes = new Map<number, Subject<any>>();
    const permisoService = {
      getMisPermisos: (id: number) => {
        const solicitud = new Subject<any>();
        pendientes.set(id, solicitud);
        return solicitud.asObservable();
      }
    } as unknown as PermisoService;
    const tenantContext = {
      empresaIdVerificada: () => empresa(),
      tieneContextoVerificado: () => empresa() !== null,
      contextoParaPermisos: () => contexto()
    } as unknown as TenantContextService;
    runtime = new PermisosRuntimeService(permisoService, tenantContext);

    runtime.cargar().subscribe();
    empresa.set(22);
    runtime.cargar().subscribe();
    pendientes.get(22)!.next(reply({
      esAdministrador: false, permisos: ['Clientes:Ver'],
      operacionMultiEmpresaRestringida: true,
      capacidadesHabilitadas: ['Clientes:Ver'],
      rutasHabilitadas: ['clientes']
    }));
    pendientes.get(22)!.complete();
    pendientes.get(11)!.error(new Error('Respuesta antigua de A: 403'));

    expect(runtime.cargado()).toBe(true);
    expect(runtime.puede('Clientes', 'Ver')).toBe(true);
    expect(runtime.rutaHabilitada('clientes')).toBe(true);
  });


  it('revoca la caché al cambiar actor o rol en la misma empresa', () => {
    runtime.cargar().subscribe();
    expect(runtime.puede('Productos', 'Ver')).toBe(true);
    actor.set(2);
    expect(runtime.cargado()).toBe(false);
    expect(runtime.esAdministrador()).toBe(false);
    expect(runtime.puede('Productos', 'Ver')).toBe(false);
    expect(runtime.rutaHabilitada('productos')).toBe(false);
    runtime.cargar().subscribe();
    expect(runtime.puede('Productos', 'Ver')).toBe(true);
    rolId.set(8);
    expect(runtime.puede('Productos', 'Ver')).toBe(false);
  });

  it('no comparte ni aplica respuestas del actor anterior en la misma empresa', () => {
    const a = new Subject<any>();
    const b = new Subject<any>();
    let solicitudes = 0;
    const permisoService = { getMisPermisos: () => (++solicitudes === 1 ? a : b) } as unknown as PermisoService;
    runtime = new PermisosRuntimeService(permisoService, {
      contextoParaPermisos: () => contexto()
    } as unknown as TenantContextService);
    const resultados: boolean[] = [];
    runtime.cargar().subscribe(ok => resultados.push(ok));
    actor.set(2);
    runtime.cargar().subscribe(ok => resultados.push(ok));
    expect(solicitudes).toBe(2);
    b.next(reply({ esAdministrador: false, permisos: ['Clientes:Ver'], operacionMultiEmpresaRestringida: false, capacidadesHabilitadas: [], rutasHabilitadas: [] }));
    a.next(response);
    expect(resultados).toEqual([true, false]);
    expect(runtime.puede('Clientes', 'Ver')).toBe(true);
    expect(runtime.puede('Productos', 'Ver')).toBe(false);
    expect(runtime.esAdministrador()).toBe(false);
    a.complete();
    b.complete();
  });

  it('deniega permisos y carga si la sesión del contexto dejó de estar vigente', () => {
    runtime.cargar().subscribe();
    expect(runtime.cargado()).toBe(true);
    sesionVigente.set(false);
    expect(runtime.cargado()).toBe(false);
    expect(runtime.rutaInicialPermitida()).toBeNull();
    expect(runtime.puede('Productos', 'Ver')).toBe(false);
    runtime.cargar().subscribe(ok => expect(ok).toBe(false));
  });
});
