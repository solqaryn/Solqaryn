import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../environments/environment';
import { ReporteAdministrativoService } from './reporte-administrativo.service';

describe('N5.1.E.2 ReporteAdministrativoService regression coverage', () => {
  let service: ReporteAdministrativoService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ReporteAdministrativoService]
    });
    service = TestBed.inject(ReporteAdministrativoService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('composes resumen endpoint and optional date parameters', () => {
    service.getResumen('2026-01-01', '2026-01-31').subscribe();

    const req = httpMock.expectOne(request =>
      request.url === `${environment.apiUrl}/reportes-administrativos/resumen` &&
      request.params.get('desde') === '2026-01-01' &&
      request.params.get('hasta') === '2026-01-31'
    );
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: {} });
  });

  it('omits absent date parameters from resumen', () => {
    service.getResumen().subscribe();

    const req = httpMock.expectOne(`${environment.apiUrl}/reportes-administrativos/resumen`);
    expect(req.request.params.has('desde')).toBe(false);
    expect(req.request.params.has('hasta')).toBe(false);
    req.flush({ success: true, data: {} });
  });

  it('calls users and roles endpoints without inventing parameters', () => {
    service.getUsuariosAccesos().subscribe();
    const usuarios = httpMock.expectOne(`${environment.apiUrl}/reportes-administrativos/usuarios-accesos`);
    expect(usuarios.request.method).toBe('GET');
    usuarios.flush({ success: true, data: [] });

    service.getRolesPermisos().subscribe();
    const roles = httpMock.expectOne(`${environment.apiUrl}/reportes-administrativos/roles-permisos`);
    expect(roles.request.method).toBe('GET');
    roles.flush({ success: true, data: [] });
  });

  it('composes auditoria endpoint with optional period', () => {
    service.getAuditoriaResumen('2026-02-01', '2026-02-28').subscribe();

    const req = httpMock.expectOne(request =>
      request.url === `${environment.apiUrl}/reportes-administrativos/auditoria-resumen` &&
      request.params.get('desde') === '2026-02-01' &&
      request.params.get('hasta') === '2026-02-28'
    );
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: {} });
  });

  it('exports with format and blob response while preserving optional dates', () => {
    service.exportar('usuarios', 'csv', '2026-03-01', '2026-03-31').subscribe(response => {
      expect(response.body).toBeInstanceOf(Blob);
    });

    const req = httpMock.expectOne(request =>
      request.url === `${environment.apiUrl}/reportes-administrativos/exportar/usuarios` &&
      request.params.get('formato') === 'csv' &&
      request.params.get('desde') === '2026-03-01' &&
      request.params.get('hasta') === '2026-03-31'
    );
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(new Blob(['ok'], { type: 'text/csv' }));
  });

  it('exports to pdf format and handles HttpResponse correctly', () => {
    service.exportar('roles', 'pdf', '2026-04-01', '2026-04-30').subscribe(response => {
      expect(response.body).toBeInstanceOf(Blob);
      expect(response.headers.get('Content-Disposition')).toBe('attachment; filename=roles-export.pdf');
    });

    const req = httpMock.expectOne(request =>
      request.url === `${environment.apiUrl}/reportes-administrativos/exportar/roles` &&
      request.params.get('formato') === 'pdf' &&
      request.params.get('desde') === '2026-04-01' &&
      request.params.get('hasta') === '2026-04-30'
    );
    expect(req.request.method).toBe('GET');
    expect(req.request.responseType).toBe('blob');
    req.flush(new Blob(['pdf content'], { type: 'application/pdf' }), {
      headers: { 'Content-Disposition': 'attachment; filename=roles-export.pdf' }
    });
  });
});
