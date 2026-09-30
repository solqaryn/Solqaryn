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
});
