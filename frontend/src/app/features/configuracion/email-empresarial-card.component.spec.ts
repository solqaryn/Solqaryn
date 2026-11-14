import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { EmailEmpresarialCardComponent } from './email-empresarial-card.component';

describe('N7.7.E correo empresarial UX', () => {
  let httpMock: HttpTestingController;
  const empresaVerificada = signal<number | null>(7);
  const permisoVer = signal(true);
  const permisoCrear = signal(true);
  const tenantContext = { empresaIdVerificada: empresaVerificada };
  const permisos = {
    puede: vi.fn((_modulo: string, accion: string) => accion === 'Ver' ? permisoVer() : permisoCrear())
  };

  beforeEach(() => {
    vi.clearAllMocks();
    empresaVerificada.set(7);
    permisoVer.set(true);
    permisoCrear.set(true);
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        { provide: TenantContextService, useValue: tenantContext },
        { provide: PermisosRuntimeService, useValue: permisos }
      ]
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function createComponent(): EmailEmpresarialCardComponent {
    return TestBed.runInInjectionContext(() => new EmailEmpresarialCardComponent());
  }

  it('queries only the verified tenant and ignores a requested tenant from localStorage', () => {
    localStorage.setItem('solqaryn_empresa_solicitada_id', '999');
    const component = createComponent();

    component.cargar();

    const req = httpMock.expectOne(request =>
      request.url === `${environment.apiUrl}/email-empresarial/tenants/7` &&
      request.params.get('pagina') === '1' &&
      request.params.get('tamano') === '25');
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, message: '', errors: [], data: { items: [], pagina: 1, tamano: 25, total: 0 } });

    expect(component.empresaId()).toBe(7);
    expect(component.total()).toBe(0);
    localStorage.removeItem('solqaryn_empresa_solicitada_id');
  });

  it('sends Idempotency-Key and preserves it after a failed retryable request', () => {
    const component = createComponent();
    component.destinatario = 'cliente@example.com';
    component.asunto = 'Factura lista';
    component.cuerpoHtml = '<p>Su factura está disponible.</p>';
    component.cuerpoTexto = 'Su factura está disponible.';
    const originalKey = component.claveIdempotencia();

    component.registrar();

    const req = httpMock.expectOne(`${environment.apiUrl}/email-empresarial/tenants/7`);
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.get('Idempotency-Key')).toBe(originalKey);
    expect(req.request.body).toMatchObject({
      destinatario: 'cliente@example.com',
      asunto: 'Factura lista',
      cuerpoTexto: 'Su factura está disponible.'
    });
    req.flush(
      { title: 'Servicio no disponible', detail: 'Proveedor temporalmente no disponible.' },
      { status: 503, statusText: 'Service Unavailable' });

    expect(component.claveIdempotencia()).toBe(originalKey);
    expect(component.error()).toBe('Proveedor temporalmente no disponible.');
  });

  it('rotates Idempotency-Key only after a successful durable registration', () => {
    const component = createComponent();
    component.destinatario = 'cliente@example.com';
    component.asunto = 'Factura lista';
    component.cuerpoHtml = '<p>Lista</p>';
    const originalKey = component.claveIdempotencia();

    component.registrar();

    const post = httpMock.expectOne(`${environment.apiUrl}/email-empresarial/tenants/7`);
    post.flush({
      success: true,
      message: '',
      errors: [],
      data: {
        mensajeId: '11111111-1111-1111-1111-111111111111',
        empresaId: 7,
        destinatario: 'cliente@example.com',
        asunto: 'Factura lista',
        plantillaCodigo: null,
        plantillaVersion: null,
        estado: 0,
        intentos: 0,
        correlationId: null,
        providerMessageId: null,
        creadoEnUtc: '2026-09-14T17:00:00Z',
        disponibleDesdeUtc: '2026-09-14T17:00:00Z',
        entregadoEnUtc: null,
        rebotadoEnUtc: null
      }
    });

    const refresh = httpMock.expectOne(request => request.url === `${environment.apiUrl}/email-empresarial/tenants/7`);
    refresh.flush({ success: true, message: '', errors: [], data: { items: [], pagina: 1, tamano: 25, total: 0 } });

    expect(component.claveIdempotencia()).not.toBe(originalKey);
    expect(component.success()).toContain('11111111-1111-1111-1111-111111111111');
  });

  it('fails closed without a verified tenant and emits no HTTP request', () => {
    empresaVerificada.set(null);
    const component = createComponent();
    component.destinatario = 'cliente@example.com';
    component.asunto = 'Factura';
    component.cuerpoTexto = 'Contenido';

    component.registrar();
    component.cargar();

    httpMock.expectNone(request => request.url.includes('/email-empresarial/'));
    expect(component.items()).toEqual([]);
    expect(component.total()).toBe(0);
    expect(component.error()).toBe('No existe un tenant verificado, permiso suficiente o un borrador válido.');
  });

  it('maps queue state labels and applies bounded filters', () => {
    const component = createComponent();
    component.estadoFiltro = 2;
    component.correlationFiltro = ' corr-77 ';

    component.aplicarFiltros();

    const req = httpMock.expectOne(request =>
      request.url === `${environment.apiUrl}/email-empresarial/tenants/7` &&
      request.params.get('estado') === '2' &&
      request.params.get('correlationId') === 'corr-77');
    req.flush({ success: true, message: '', errors: [], data: { items: [], pagina: 1, tamano: 25, total: 0 } });

    expect(component.etiquetaEstado(2)).toBe('Reintento pendiente');
  });
});
