import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { environment } from '../../../environments/environment';
import { TenantContextService } from './tenant-context.service';

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


});
