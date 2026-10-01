import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { WhatsappBusinessCardComponent } from './whatsapp-business-card.component';

describe('N7.6.E WhatsApp Business UX', () => {
  let httpMock: HttpTestingController;
  const empresaVerificada = signal<number | null>(7);
  const permisoEditar = signal(true);
  const tenantContext = { empresaIdVerificada: empresaVerificada };
  const permisos = { puede: vi.fn(() => permisoEditar()) };

  beforeEach(() => {
    vi.clearAllMocks();
    empresaVerificada.set(7);
    permisoEditar.set(true);
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

  function createComponent(): WhatsappBusinessCardComponent {
    return TestBed.runInInjectionContext(() => new WhatsappBusinessCardComponent());
  }

  it('uses only the verified tenant context when consulting WhatsApp', () => {
    localStorage.setItem('solqaryn_empresa_solicitada_id', '999');
    const component = createComponent();

    component.consultarEstado();

    const req = httpMock.expectOne(`${environment.apiUrl}/whatsapp/iniciar-whatsapp`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ empresaId: 7 });
    req.flush({
      status: 'CONFIGURADA_SIN_SESION',
      qr: null,
      numeroTelefonoE164: '+50499999999',
      requiereProveedorSesion: true
    });

    expect(component.empresaId()).toBe(7);
    expect(component.etiquetaProveedor()).toBe('Requerido');
    localStorage.removeItem('solqaryn_empresa_solicitada_id');
  });

  it('fails closed without a verified tenant and sends no request', () => {
    empresaVerificada.set(null);
    const component = createComponent();

    component.consultarEstado();

    httpMock.expectNone(`${environment.apiUrl}/whatsapp/iniciar-whatsapp`);
    expect(component.error()).toBe('No hay una empresa activa válida para consultar.');
    expect(component.estado()).toBeNull();
  });

  it('does not claim the provider is ready before the first verified response', () => {
    const component = createComponent();

    expect(component.etiquetaProveedor()).toBe('Pendiente de consulta');
  });

  it('reacts to permission changes instead of freezing the initial permission value', () => {
    const component = createComponent();
    expect(component.puedeEditar()).toBe(true);

    permisoEditar.set(false);

    expect(component.puedeEditar()).toBe(false);
    component.consultarEstado();
    httpMock.expectNone(`${environment.apiUrl}/whatsapp/iniciar-whatsapp`);
    expect(component.error()).toBe('No tienes permiso para iniciar o consultar la sesión de WhatsApp Business.');
  });
});
