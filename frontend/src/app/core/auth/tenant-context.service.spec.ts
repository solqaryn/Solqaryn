import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { firstValueFrom, isObservable, of } from 'rxjs';
import { AuthService } from './auth.service';
import { SessionActivityService } from './session-activity.service';
import { PermisosRuntimeService } from './permisos-runtime.service';
import { authGuard } from '../guards/auth.guard';
import { permisoGuard } from '../guards/permiso.guard';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { environment } from '../../../environments/environment';
import { TenantContextService, CREDENCIAL_SUCURSAL } from './tenant-context.service';

describe('TenantContextService', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule]
    });

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('consulta una sucursal con credencial contextual efímera sin cambiar el JWT base', () => {
    localStorage.setItem('solqaryn_token', 'base-A');
    const service = TestBed.inject(TenantContextService);
    service.seleccionarEmpresa(1).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/1`).flush(tenant1);
    service.listarSucursalesContextuales().subscribe(lista => expect(lista[0].id).toBe(5));
    const sucursal = { id: 5, empresaId: 1, codigo: 'S1', nombre: 'Sucursal' };
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/sesion/sucursales/1`).flush({ success: true, data: [sucursal] });
    service.consultarSucursalContextual(5).subscribe(r => expect(r).toEqual(sucursal));
    const emision = httpMock.expectOne(`${environment.apiUrl}/tenant-context/sesion`);
    expect(emision.request.body).toEqual({ empresaId: 1, sucursalId: 5 });
    emision.flush({ success: true, data: { token: 'scoped-A', empresaId: 1, sucursalId: 5, rolId: 4, expiraEn: new Date(Date.now() + 60000).toISOString() } });
    const lectura = httpMock.expectOne(`${environment.apiUrl}/tenant-context/sesion/sucursal`);
    expect(lectura.request.context.get(CREDENCIAL_SUCURSAL)).toEqual({ baseToken: 'base-A', token: 'scoped-A', empresaId: 1, sucursalId: 5 });
    lectura.flush({ success: true, data: sucursal });
    expect(localStorage.getItem('solqaryn_token')).toBe('base-A');
  });

  it('no usa una emisión tardía tras sustituir el JWT', () => {
    localStorage.setItem('solqaryn_token', 'base-A');
    const service = TestBed.inject(TenantContextService);
    service.seleccionarEmpresa(1).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/1`).flush(tenant1);
    let rechazado = false;
    service.consultarSucursalContextual(5).subscribe({ error: () => { rechazado = true; } });
    const emision = httpMock.expectOne(`${environment.apiUrl}/tenant-context/sesion`);
    localStorage.setItem('solqaryn_token', 'base-B');
    emision.flush({ success: true, data: { token: 'scoped-A', empresaId: 1, sucursalId: 5, rolId: 4, expiraEn: new Date(Date.now() + 60000).toISOString() } });
    expect(rechazado).toBe(true);
    httpMock.expectNone(`${environment.apiUrl}/tenant-context/sesion/sucursal`);
  });

  it('no restaura autoridad desde localStorage', () => {
    localStorage.setItem('solqaryn_empresa_solicitada_id', '8');
    const service = TestBed.inject(TenantContextService);

    expect(service.empresaSolicitadaId()).toBe(8);
    expect(service.contextoVerificado()).toBeNull();
    expect(service.tieneContextoVerificado()).toBe(false);
  });

  it('materializa contexto solo cuando el backend confirma la misma empresa', () => {
    const service = TestBed.inject(TenantContextService);
    service.seleccionarEmpresa(7).subscribe();

    const request = httpMock.expectOne(`${environment.apiUrl}/tenant-context/7`);
    request.flush({
      success: true,
      message: 'Contexto tenant verificado.',
      data: {
        usuarioId: 3,
        empresaId: 7,
        rolId: 4,
        rolNombre: 'Operador',
        esAdministrador: false
      }
    });

    expect(service.empresaIdVerificada()).toBe(7);
    expect(service.rolVerificado()).toBe('Operador');
    expect(service.tieneContextoVerificado()).toBe(true);
  });

  it('falla cerrado si el backend devuelve otra empresa', () => {
    const service = TestBed.inject(TenantContextService);
    service.seleccionarEmpresa(7).subscribe({ error: () => undefined });

    const request = httpMock.expectOne(`${environment.apiUrl}/tenant-context/7`);
    request.flush({
      success: true,
      message: 'Contexto inválido.',
      data: {
        usuarioId: 3,
        empresaId: 9,
        rolId: 4,
        rolNombre: 'Operador',
        esAdministrador: false
      }
    });

    expect(service.contextoVerificado()).toBeNull();
    expect(service.empresaSolicitadaId()).toBeNull();
  });

  it('revoca selección local ante 403', () => {
    const service = TestBed.inject(TenantContextService);
    service.seleccionarEmpresa(7).subscribe({ error: () => undefined });

    const request = httpMock.expectOne(`${environment.apiUrl}/tenant-context/7`);
    request.flush({}, { status: 403, statusText: 'Forbidden' });

    expect(service.contextoVerificado()).toBeNull();
    expect(service.empresaSolicitadaId()).toBeNull();
    expect(localStorage.getItem('solqaryn_empresa_solicitada_id')).toBeNull();
  });
  const tenant1 = {
    success: true,
    data: { usuarioId: 11, empresaId: 1, rolId: 4, rolNombre: 'Administrador', esAdministrador: true }
  };

  it('no conserva el contexto de A mientras el backend rechaza el cambio a B con 403', () => {
    const service = TestBed.inject(TenantContextService);
    service.seleccionarEmpresa(1).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/1`).flush(tenant1);
    expect(service.empresaIdVerificada()).toBe(1);
    expect(service.rolVerificado()).toBe('Administrador');

    let rechazado = false;
    service.seleccionarEmpresa(2).subscribe({ error: () => { rechazado = true; } });
    expect(service.contextoVerificado()).toBeNull();
    expect(service.rolVerificado()).toBeNull();
    expect(localStorage.getItem('solqaryn_empresa_solicitada_id')).toBe('2');

    httpMock.expectOne(`${environment.apiUrl}/tenant-context/2`).flush(
      {}, { status: 403, statusText: 'Forbidden' }
    );

    expect(rechazado).toBe(true);
    expect(service.contextoVerificado()).toBeNull();
    expect(service.empresaSolicitadaId()).toBeNull();
    expect(service.rolVerificado()).toBeNull();
    expect(localStorage.getItem('solqaryn_empresa_solicitada_id')).toBeNull();
  });

  it('rechaza HTTP 200 de A al solicitar B sin exponer el usuario ni el rol anterior', () => {
    const service = TestBed.inject(TenantContextService);
    service.seleccionarEmpresa(1).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/1`).flush(tenant1);

    let rechazado = false;
    service.seleccionarEmpresa(2).subscribe({ error: () => { rechazado = true; } });
    expect(service.contextoVerificado()).toBeNull();
    expect(service.empresaIdVerificada()).toBeNull();
    expect(service.rolVerificado()).toBeNull();

    httpMock.expectOne(`${environment.apiUrl}/tenant-context/2`).flush(tenant1);

    expect(rechazado).toBe(true);
    expect(service.contextoVerificado()).toBeNull();
    expect(service.empresaSolicitadaId()).toBeNull();
    expect(service.rolVerificado()).toBeNull();
    expect(localStorage.getItem('solqaryn_empresa_solicitada_id')).toBeNull();
  });

  it('comparte la única petición cuando selector y guards verifican la misma empresa a la vez', () => {
    const service = TestBed.inject(TenantContextService);
    let resueltoA = false;
    let resueltoB = false;
    service.seleccionarEmpresa(1).subscribe({ next: () => { resueltoA = true; } });
    service.seleccionarEmpresa(1).subscribe({ next: () => { resueltoB = true; } });

    const solicitudes = httpMock.match(`${environment.apiUrl}/tenant-context/1`);
    expect(solicitudes.length).toBe(1);
    solicitudes[0].flush(tenant1);

    expect(resueltoA).toBe(true);
    expect(resueltoB).toBe(true);
    expect(service.empresaIdVerificada()).toBe(1);
    expect(localStorage.getItem('solqaryn_empresa_solicitada_id')).toBe('1');
  });

  it('una respuesta antigua no anula la segunda empresa mientras cambia el tenant', () => {
    const service = TestBed.inject(TenantContextService);
    const errores: unknown[] = [];
    service.seleccionarEmpresa(1).subscribe({ error: err => errores.push(err) });
    service.seleccionarEmpresa(2).subscribe();

    const vieja = httpMock.expectOne(`${environment.apiUrl}/tenant-context/1`);
    const nueva = httpMock.expectOne(`${environment.apiUrl}/tenant-context/2`);
    vieja.flush(tenant1);
    nueva.flush({
      success: true,
      data: { usuarioId: 11, empresaId: 2, rolId: 7, rolNombre: 'Operador', esAdministrador: false }
    });

    expect(errores.length).toBe(1);
    expect(service.empresaIdVerificada()).toBe(2);
    expect(service.empresaSolicitadaId()).toBe(2);
    expect(localStorage.getItem('solqaryn_empresa_solicitada_id')).toBe('2');
  });

  it('rol del tenant se muestra solo mientras el backend confirmó la empresa activa', () => {
    const service = TestBed.inject(TenantContextService);
    expect(service.rolVerificado()).toBeNull();

    service.seleccionarEmpresa(2).subscribe();
    const requestA = httpMock.expectOne(`${environment.apiUrl}/tenant-context/2`);
    requestA.flush({
      success: true,
      data: { usuarioId: 11, empresaId: 2, rolId: 4, rolNombre: 'Administrador', esAdministrador: true }
    });
    expect(service.rolVerificado()).toBe('Administrador');

    service.seleccionarEmpresa(3).subscribe();
    expect(service.rolVerificado()).toBeNull();
    const requestB = httpMock.expectOne(`${environment.apiUrl}/tenant-context/3`);
    requestB.flush({
      success: true,
      data: { usuarioId: 11, empresaId: 3, rolId: 8, rolNombre: 'Operador', esAdministrador: false }
    });
    expect(service.rolVerificado()).toBe('Operador');

    service.limpiar();
    expect(service.rolVerificado()).toBeNull();
    expect(service.empresaIdVerificada()).toBeNull();
  });

  it('cerrar el contexto revoca una respuesta pendiente incluso si devuelve HTTP 200', () => {
    const service = TestBed.inject(TenantContextService);
    let denegada = false;
    service.seleccionarEmpresa(1).subscribe({ error: () => { denegada = true; } });
    const pendiente = httpMock.expectOne(`${environment.apiUrl}/tenant-context/1`);

    service.limpiar();
    pendiente.flush(tenant1);
    expect(denegada).toBe(true);
    expect(service.tieneContextoVerificado()).toBe(false);
    expect(service.empresaSolicitadaId()).toBeNull();
  });



  it('lista solo empresas entregadas por el backend sin conceder contexto por localStorage', () => {
    localStorage.setItem('solqaryn_empresa_solicitada_id', '999');
    const service = TestBed.inject(TenantContextService);
    let empresas: { empresaId: number; nombre: string }[] = [];
    service.listarEmpresasSeleccionables().subscribe(resultado => { empresas = resultado; });

    const request = httpMock.expectOne(`${environment.apiUrl}/tenant-context/empresas`);
    request.flush({
      success: true,
      data: [{ empresaId: 7, nombre: 'Empresa A' }]
    });
    expect(empresas).toEqual([{ empresaId: 7, nombre: 'Empresa A' }]);
    expect(service.contextoVerificado()).toBeNull();
    expect(service.empresaSolicitadaId()).toBe(999);
    expect(service.tieneContextoVerificado()).toBe(false);
  });

  it('falla cerrado si el listado de empresas está mal formado', () => {
    const service = TestBed.inject(TenantContextService);
    let rechazado = false;
    service.listarEmpresasSeleccionables().subscribe({
      error: () => { rechazado = true; }
    });
    const request = httpMock.expectOne(`${environment.apiUrl}/tenant-context/empresas`);
    request.flush({
      success: true,
      data: [{ empresaId: -1, nombre: 'Empresa inválida' }]
    });
    expect(rechazado).toBe(true);
    expect(service.contextoVerificado()).toBeNull();
  });

  it('no obtiene empresas si el servidor rechaza el JWT', () => {
    const service = TestBed.inject(TenantContextService);
    let rechazado = false;
    service.listarEmpresasSeleccionables().subscribe({
      error: () => { rechazado = true; }
    });
    const request = httpMock.expectOne(`${environment.apiUrl}/tenant-context/empresas`);
    request.flush({}, { status: 401, statusText: 'Unauthorized' });
    expect(rechazado).toBe(true);
    expect(service.tieneContextoVerificado()).toBe(false);
  });

  it('solo habilita el cambio con dos empresas reales e incluye la verificada', () => {
    const service = TestBed.inject(TenantContextService);
    service.seleccionarEmpresa(7).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/7`).flush({
      success: true, data: { usuarioId: 3, empresaId: 7, rolId: 4,
        rolNombre: 'Operador', esAdministrador: false }
    });
    expect(service.puedeCambiarEmpresa()).toBe(false);
    service.listarEmpresasSeleccionables().subscribe();
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/empresas`).flush({
      success: true, data: [{ empresaId: 7, nombre: 'A' }]
    });
    expect(service.puedeCambiarEmpresa()).toBe(false);
    service.listarEmpresasSeleccionables().subscribe();
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/empresas`).flush({
      success: true, data: [{ empresaId: 7, nombre: 'A' },
        { empresaId: 8, nombre: 'B' }]
    });
    expect(service.puedeCambiarEmpresa()).toBe(true);
    service.limpiar();
    expect(service.puedeCambiarEmpresa()).toBe(false);
  });

  it('rechaza listas duplicadas, ajenas y errores del backend para el switch', () => {
    const service = TestBed.inject(TenantContextService);
    service.seleccionarEmpresa(7).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/7`).flush({
      success: true, data: { usuarioId: 3, empresaId: 7, rolId: 4,
        rolNombre: 'Operador', esAdministrador: false }
    });
    service.listarEmpresasSeleccionables().subscribe();
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/empresas`).flush({
      success: true, data: [{ empresaId: 8, nombre: 'B' },
        { empresaId: 9, nombre: 'C' }]
    });
    expect(service.puedeCambiarEmpresa()).toBe(false);
    let rechazo = false;
    service.listarEmpresasSeleccionables().subscribe({ error: () => { rechazo = true; } });
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/empresas`).flush({
      success: true, data: [{ empresaId: 7, nombre: 'A' },
        { empresaId: 7, nombre: 'A duplicada' }]
    });
    expect(rechazo).toBe(true);
    expect(service.puedeCambiarEmpresa()).toBe(false);
    service.listarEmpresasSeleccionables().subscribe({ error: () => undefined });
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/empresas`)
      .flush({}, { status: 403, statusText: 'Forbidden' });
    expect(service.puedeCambiarEmpresa()).toBe(false);
  });

  it('descarta la lista HTTP tardía después de revocar el contexto', () => {
    const service = TestBed.inject(TenantContextService);
    service.seleccionarEmpresa(7).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/7`).flush({
      success: true, data: { usuarioId: 3, empresaId: 7, rolId: 4,
        rolNombre: 'Operador', esAdministrador: false }
    });
    service.listarEmpresasSeleccionables().subscribe();
    const request = httpMock.expectOne(`${environment.apiUrl}/tenant-context/empresas`);
    service.limpiar();
    request.flush({ success: true, data: [
      { empresaId: 7, nombre: 'A' }, { empresaId: 8, nombre: 'B' }
    ] });
    expect(service.puedeCambiarEmpresa()).toBe(false);
  });


  it('descarta membresias del tenant A al confirmar B hasta un nuevo listado autenticado', () => {
    const service = TestBed.inject(TenantContextService);
    service.seleccionarEmpresa(7).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/7`).flush({
      success: true, data: { usuarioId: 3, empresaId: 7,
        rolId: 4, rolNombre: 'Operador', esAdministrador: false }
    });
    service.listarEmpresasSeleccionables().subscribe();
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/empresas`).flush({
      success: true,
      data: [{ empresaId: 7, nombre: 'A' }, { empresaId: 8, nombre: 'B' }]
    });
    expect(service.puedeCambiarEmpresa()).toBe(true);

    service.seleccionarEmpresa(8).subscribe();
    expect(service.puedeCambiarEmpresa()).toBe(false);
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/8`).flush({
      success: true, data: { usuarioId: 3, empresaId: 8,
        rolId: 4, rolNombre: 'Operador', esAdministrador: false }
    });
    // La lista previa [7,8] no debe reactivar el menu al confirmar el contexto B.
    expect(service.empresaIdVerificada()).toBe(8);
    expect(service.puedeCambiarEmpresa()).toBe(false);

    service.listarEmpresasSeleccionables().subscribe();
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/empresas`).flush({
      success: true, data: [{ empresaId: 8, nombre: 'B' }]
    });
    expect(service.puedeCambiarEmpresa()).toBe(false);
    service.listarEmpresasSeleccionables().subscribe();
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/empresas`).flush({
      success: true, data: [{ empresaId: 7, nombre: 'A' },
        { empresaId: 8, nombre: 'B' }]
    });
    expect(service.puedeCambiarEmpresa()).toBe(true);
    service.limpiar();
    expect(service.puedeCambiarEmpresa()).toBe(false);
  });

  it('no acepta el HTTP de A cuando B reemplaza su JWT en otra pestaña', () => {
    localStorage.setItem('solqaryn_token', 'jwt-A');
    const service = TestBed.inject(TenantContextService);
    let failed = false;
    service.seleccionarEmpresa(7).subscribe({ error: () => { failed = true; } });
    const old = httpMock.expectOne(`${environment.apiUrl}/tenant-context/7`);
    localStorage.setItem('solqaryn_token', 'jwt-B');
    old.flush({ success: true, data: {
      usuarioId: 3, empresaId: 7, rolId: 4, rolNombre: 'Operador A', esAdministrador: false
    } });
    expect(failed).toBe(true);
    expect(service.contextoVerificado()).toBeNull();
    expect(service.empresaSolicitadaId()).toBeNull();
    expect(localStorage.getItem('solqaryn_token')).toBe('jwt-B');
  });

  it('no comparte la petición del actor A con el actor B aunque el tenant coincida', () => {
    localStorage.setItem('solqaryn_token', 'jwt-A');
    const service = TestBed.inject(TenantContextService);
    let failedOld = false;
    service.seleccionarEmpresa(7).subscribe({ error: () => { failedOld = true; } });
    const old = httpMock.expectOne(`${environment.apiUrl}/tenant-context/7`);
    localStorage.setItem('solqaryn_token', 'jwt-B');
    let confirmedNew = false;
    service.seleccionarEmpresa(7).subscribe({ next: () => { confirmedNew = true; } });
    const latest = httpMock.expectOne(`${environment.apiUrl}/tenant-context/7`);
    latest.flush({ success: true, data: {
      usuarioId: 8, empresaId: 7, rolId: 9, rolNombre: 'Operador B', esAdministrador: false
    } });
    old.flush({ success: true, data: {
      usuarioId: 3, empresaId: 7, rolId: 4, rolNombre: 'Operador A', esAdministrador: false
    } });
    expect(confirmedNew).toBe(true);
    expect(failedOld).toBe(true);
    expect(service.contextoVerificado()?.usuarioId).toBe(8);
    expect(localStorage.getItem('solqaryn_token')).toBe('jwt-B');
  });

  it('descarta la lista tardía de A sin borrar el listado vigente de B', () => {
    localStorage.setItem('solqaryn_token', 'jwt-A');
    const service = TestBed.inject(TenantContextService);
    let failedOld = false;
    service.listarEmpresasSeleccionables().subscribe({ error: () => { failedOld = true; } });
    const old = httpMock.expectOne(`${environment.apiUrl}/tenant-context/empresas`);
    localStorage.setItem('solqaryn_token', 'jwt-B');
    let idsFromB: number[] = [];
    service.listarEmpresasSeleccionables().subscribe({
      next: empresas => { idsFromB = empresas.map(empresa => empresa.empresaId); }
    });
    const latest = httpMock.expectOne(`${environment.apiUrl}/tenant-context/empresas`);
    latest.flush({ success: true, data: [
      { empresaId: 8, nombre: 'Empresa B' }, { empresaId: 9, nombre: 'Otra B' }
    ] });
    old.flush({ success: true, data: [{ empresaId: 7, nombre: 'Empresa A' }] });
    expect(failedOld).toBe(true);
    expect(idsFromB).toEqual([8, 9]);
  });


  it('vincula el snapshot de permisos al JWT verificado y deniega sustitución o logout', () => {
    localStorage.setItem('solqaryn_token', 'jwt-A');
    const service = TestBed.inject(TenantContextService);
    service.seleccionarEmpresa(7).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/7`).flush({
      success: true, data: { usuarioId: 11, empresaId: 7, rolId: 4, rolNombre: 'A', esAdministrador: true }
    });
    expect(service.contextoParaPermisos()?.usuarioId).toBe(11);
    localStorage.setItem('solqaryn_token', 'jwt-B');
    expect(service.contextoParaPermisos()).toBeNull();
    service.seleccionarEmpresa(7).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/7`).flush({
      success: true, data: { usuarioId: 22, empresaId: 7, rolId: 8, rolNombre: 'B', esAdministrador: false }
    });
    expect(service.contextoParaPermisos()?.usuarioId).toBe(22);
    expect(service.contextoParaPermisos()?.rolId).toBe(8);
    localStorage.removeItem('solqaryn_token');
    expect(service.contextoParaPermisos()).toBeNull();
  });

  it('revoca el snapshot de permisos ante cambio de empresa con el mismo JWT y no lo resucita al restaurar el hint', () => {
    localStorage.setItem('solqaryn_token', 'jwt-A');
    const service = TestBed.inject(TenantContextService);
    const data = { usuarioId: 11, empresaId: 7, rolId: 4, rolNombre: 'A', esAdministrador: true };
    service.seleccionarEmpresa(7).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/7`).flush({ success: true, data });
    expect(service.contextoParaPermisos()).toEqual(data);
    localStorage.setItem('solqaryn_empresa_solicitada_id', '8');
    expect(service.contextoParaPermisos()).toBeNull();
    let rejected = false;
    service.listarSucursalesContextuales().subscribe({ error: () => { rejected = true; } });
    expect(rejected).toBe(true);
    httpMock.expectNone(`${environment.apiUrl}/tenant-context/sesion/sucursales/7`);
    localStorage.setItem('solqaryn_empresa_solicitada_id', '7');
    expect(service.contextoParaPermisos()).toBeNull();
    service.seleccionarEmpresa(7).subscribe();
    httpMock.expectOne(`${environment.apiUrl}/tenant-context/7`).flush({ success: true, data });
    expect(service.contextoParaPermisos()).toEqual(data);
    expect(localStorage.getItem('solqaryn_token')).toBe('jwt-A');
  });

  it('una selección tardía no instala autoridad ni borra el hint de otra pestaña con el mismo JWT', () => {
    localStorage.setItem('solqaryn_token', 'jwt-A');
    const service = TestBed.inject(TenantContextService);
    let rejected = false;
    service.seleccionarEmpresa(7).subscribe({ error: () => { rejected = true; } });
    const pending = httpMock.expectOne(`${environment.apiUrl}/tenant-context/7`);
    localStorage.setItem('solqaryn_empresa_solicitada_id', '8');
    pending.flush({ success: true, data: { usuarioId: 11, empresaId: 7, rolId: 4, rolNombre: 'A', esAdministrador: true } });
    expect(rejected).toBe(true);
    expect(service.contextoParaPermisos()).toBeNull();
    expect(service.contextoVerificado()).toBeNull();
    expect(localStorage.getItem('solqaryn_empresa_solicitada_id')).toBe('8');
    expect(localStorage.getItem('solqaryn_token')).toBe('jwt-A');
  });
});

describe('Guards ERP con prueba contextual vigente', () => {
  let http: HttpTestingController;
  let tenant: TenantContextService;
  const permisos = {
    cargar: vi.fn(() => of(true)),
    puede: vi.fn(() => true),
    rutaHabilitada: vi.fn(() => true),
    rutaInicialPermitida: vi.fn(() => '/perfil')
  };
  const route = { data: { modulo: 'Ventas', accion: 'Ver' }, pathFromRoot: [] } as unknown as ActivatedRouteSnapshot;
  const state = {} as RouterStateSnapshot;

  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
    localStorage.setItem('solqaryn_token', 'jwt-A');
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        provideRouter([]),
        { provide: AuthService, useValue: { isAuthenticated: () => true } },
        { provide: SessionActivityService, useValue: {} },
        { provide: PermisosRuntimeService, useValue: permisos }
      ]
    });
    http = TestBed.inject(HttpTestingController);
    tenant = TestBed.inject(TenantContextService);
    tenant.seleccionarEmpresa(7).subscribe();
    http.expectOne(`${environment.apiUrl}/tenant-context/7`).flush({
      success: true, data: { usuarioId: 11, empresaId: 7, rolId: 4, rolNombre: 'A', esAdministrador: true }
    });
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('permite navegar con prueba HTTP vigente del mismo actor y tenant', async () => {
    expect(TestBed.runInInjectionContext(() => authGuard(route, state))).toBe(true);
    const decision = TestBed.runInInjectionContext(() => permisoGuard(route, state));
    if (!isObservable(decision)) throw new Error('Se esperaba la consulta de permisos.');
    expect(await firstValueFrom(decision)).toBe(true);
    expect(permisos.cargar).toHaveBeenCalledTimes(1);
  });

  it.each([
    ['solqaryn_empresa_solicitada_id', '8'],
    ['solqaryn_token', 'jwt-B']
  ])('deniega ambos guards al sustituir %s sin reescribir la selección compartida', (key, replacement) => {
    localStorage.setItem(key, replacement);
    // The cached context still exists, but its proof has expired.
    expect(tenant.tieneContextoVerificado()).toBe(true);
    const authDecision = TestBed.runInInjectionContext(() => authGuard(route, state));
    const permisoDecision = TestBed.runInInjectionContext(() => permisoGuard(route, state));
    expect(authDecision).toBeInstanceOf(UrlTree);
    expect(String(authDecision)).toBe('/login');
    expect(permisoDecision).toBeInstanceOf(UrlTree);
    expect(String(permisoDecision)).toBe('/login');
    expect(permisos.cargar).not.toHaveBeenCalled();
    expect(localStorage.getItem(key)).toBe(replacement);
    http.expectNone(`${environment.apiUrl}/tenant-context/7`);
  });
});
