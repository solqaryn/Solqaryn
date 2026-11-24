import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { Router } from '@angular/router';
import { of, Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthService } from './auth.service';
import { PermisosRuntimeService } from './permisos-runtime.service';
import { TenantContextService } from './tenant-context.service';
import { SessionActivityService } from './session-activity.service';

describe('C02-01.M05.04: renovación JWT y contexto empresarial real', () => {
  let service: SessionActivityService;
  let tenant: TenantContextService;
  let http: HttpTestingController;
  let renovarSesion: ReturnType<typeof vi.fn>;
  let limpiarPermisos: ReturnType<typeof vi.fn>;
  let cargarPermisos: ReturnType<typeof vi.fn>;
  let navegar: ReturnType<typeof vi.fn>;
  let cerrarToken: ReturnType<typeof vi.fn>;
  let token: string;

  const contexto = (empresaId: number) => ({
    success: true,
    data: {
      usuarioId: 12,
      empresaId,
      rolId: empresaId === 7 ? 3 : 4,
      rolNombre: empresaId === 7 ? 'Operador' : 'Supervisor',
      esAdministrador: false
    }
  });

  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    token = 'jwt-firmado-fixture';
    localStorage.setItem('solqaryn_token', token);
    renovarSesion = vi.fn(() => of({ success: true, data: { token: 'jwt-renovado' } }));
    limpiarPermisos = vi.fn();
    cargarPermisos = vi.fn(() => of(true));
    navegar = vi.fn().mockResolvedValue(true);
    cerrarToken = vi.fn();

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        { provide: AuthService, useValue: {
          getToken: () => token,
          renovarSesion,
          logout: cerrarToken
        } },
        { provide: PermisosRuntimeService, useValue: {
          limpiar: limpiarPermisos,
          cargar: cargarPermisos
        } },
        { provide: Router, useValue: { navigate: navegar, events: of() } }
      ]
    });
    tenant = TestBed.inject(TenantContextService);
    service = TestBed.inject(SessionActivityService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
    sessionStorage.clear();
  });

  const renovar = () =>
    (service as unknown as { renovar(): void }).renovar();

  function validarEmpresa(empresaId: number): void {
    tenant.seleccionarEmpresa(empresaId).subscribe();
    http.expectOne(`${environment.apiUrl}/tenant-context/${empresaId}`)
      .flush(contexto(empresaId));
    expect(tenant.empresaIdVerificada()).toBe(empresaId);
  }

  it('revoca los permisos en memoria y exige un GET SQL/HTTP de la misma empresa tras JWT nuevo', () => {
    validarEmpresa(7);
    renovar();
    expect(renovarSesion).toHaveBeenCalledTimes(1);
    expect(limpiarPermisos).toHaveBeenCalledTimes(1);
    expect(tenant.contextoVerificado()).toBeNull();
    expect(cargarPermisos).not.toHaveBeenCalled();

    http.expectOne(`${environment.apiUrl}/tenant-context/7`)
      .flush(contexto(7));
    expect(tenant.empresaIdVerificada()).toBe(7);
    expect(cargarPermisos).toHaveBeenCalledTimes(1);
    expect(navegar).not.toHaveBeenCalled();
    expect(Number(localStorage.getItem('solqaryn_last_renew'))).toBeGreaterThan(0);
  });

  it('membresía revocada 403 tras JWT renovado borra A y vuelve al selector, no al panel', () => {
    validarEmpresa(7);
    renovar();
    http.expectOne(`${environment.apiUrl}/tenant-context/7`)
      .flush({}, { status: 403, statusText: 'Forbidden' });

    expect(tenant.contextoVerificado()).toBeNull();
    expect(tenant.empresaSolicitadaId()).toBeNull();
    expect(cargarPermisos).not.toHaveBeenCalled();
    expect(navegar).toHaveBeenCalledWith(['/login'], { replaceUrl: true });
    expect(sessionStorage.getItem('solqaryn_session_message')).toContain('revalidar');
    expect(localStorage.getItem('solqaryn_last_renew')).toBeNull();
  });

  it('si falla la recarga de permisos, no reanuda acceso ERP aunque la membresía fuera positiva', () => {
    validarEmpresa(7);
    cargarPermisos.mockReturnValueOnce(of(false));
    renovar();
    http.expectOne(`${environment.apiUrl}/tenant-context/7`)
      .flush(contexto(7));
    expect(tenant.contextoVerificado()).toBeNull();
    expect(navegar).toHaveBeenCalledWith(['/login'], { replaceUrl: true });
  });

  it('la renovación tardía de A no borra B seleccionada mientras se esperaba el POST', () => {
    validarEmpresa(7);
    const pendiente = new Subject<{ success: boolean; data: { token: string } }>();
    renovarSesion.mockReturnValueOnce(pendiente.asObservable());
    renovar();

    validarEmpresa(8);
    pendiente.next({ success: true, data: { token: 'jwt-renovado' } });
    pendiente.complete();

    http.expectNone(`${environment.apiUrl}/tenant-context/7`);
    expect(tenant.empresaIdVerificada()).toBe(8);
    expect(navegar).not.toHaveBeenCalled();
  });

  it('respuesta de renovación sin éxito niega la sesión y borra contexto tenant anterior', () => {
    validarEmpresa(7);
    renovarSesion.mockReturnValueOnce(of({ success: false, data: { token: '' } }));
    renovar();
    expect(cerrarToken).toHaveBeenCalledTimes(1);
    expect(tenant.contextoVerificado()).toBeNull();
    expect(tenant.empresaSolicitadaId()).toBeNull();
    expect(navegar).toHaveBeenCalledWith(['/login']);
  });

  it('401 tardío de renovación A no elimina sesión B con la misma empresa', () => {
    validarEmpresa(7);
    const pendiente = new Subject<{ success: boolean; data: { token: string } }>();
    renovarSesion.mockReturnValueOnce(pendiente.asObservable());
    renovar();
    token = 'jwt-nueva-sesion';
    localStorage.setItem('solqaryn_token', token);
    validarEmpresa(7);
    pendiente.error({ status: 401 });
    expect(cerrarToken).not.toHaveBeenCalled();
    expect(tenant.empresaIdVerificada()).toBe(7);
    expect(navegar).not.toHaveBeenCalled();
  });

  it('401 de renovación vigente sigue cerrando la sesión', () => {
    validarEmpresa(7);
    const pendiente = new Subject<{ success: boolean; data: { token: string } }>();
    renovarSesion.mockReturnValueOnce(pendiente.asObservable());
    renovar();
    pendiente.error({ status: 401 });
    expect(cerrarToken).toHaveBeenCalledTimes(1);
    expect(tenant.contextoVerificado()).toBeNull();
    expect(navegar).toHaveBeenCalledWith(['/login']);
  });
});
