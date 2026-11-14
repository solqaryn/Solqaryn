import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { FormBuilder } from '@angular/forms';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { environment } from '../../../environments/environment';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { SecuenciaDocumentoCardComponent } from './secuencia-documento-card.component';

describe('N6.6.E Numeraciones frontend UX', () => {
  let httpMock: HttpTestingController;
  const empresaIdVerificada = signal<number | null>(7);
  const tenant = { empresaIdVerificada };
  const permisos = { puede: vi.fn(() => true) };
  const snackBar = { open: vi.fn() };

  beforeEach(() => {
    vi.clearAllMocks();
    empresaIdVerificada.set(7);
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        FormBuilder,
        { provide: TenantContextService, useValue: tenant },
        { provide: PermisosRuntimeService, useValue: permisos },
        { provide: MatSnackBar, useValue: snackBar }
      ]
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  function createComponent(): SecuenciaDocumentoCardComponent {
    return TestBed.runInInjectionContext(() => new SecuenciaDocumentoCardComponent());
  }

  it('queries the sequence using the verified tenant and selected document scope', () => {
    const component = createComponent();
    component.ngOnInit();
    component.form.setValue({ tipoDocumento: 'FACTURA', sucursalId: 3 });
    component.consultar();

    const req = httpMock.expectOne(request => request.url === `${environment.apiUrl}/secuencias-documento`);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('empresaId')).toBe('7');
    expect(req.request.params.get('sucursalId')).toBe('3');
    expect(req.request.params.get('tipoDocumento')).toBe('FACTURA');
    req.flush({
      success: true,
      data: {
        empresaId: 7,
        sucursalId: 3,
        tipoDocumento: 'FACTURA',
        ultimoValor: 12,
        prefijo: 'FAC-',
        longitudNumero: 8,
        activa: true,
        ultimoNumeroFormateado: 'FAC-00000012'
      },
      message: '',
      errors: []
    });

    expect(component.loading()).toBe(false);
    expect(component.secuencia()?.empresaId).toBe(7);
    expect(component.secuencia()?.ultimoNumeroFormateado).toBe('FAC-00000012');
  });

  it('fails closed without a verified tenant and sends no request', () => {
    empresaIdVerificada.set(null);
    const component = createComponent();
    component.consultar();

    expect(component.secuencia()).toBeNull();
    httpMock.expectNone(`${environment.apiUrl}/secuencias-documento`);
  });

  it('reserves the next number inside the same verified scope and refreshes the query', () => {
    const component = createComponent();
    component.ngOnInit();
    component.secuencia.set({
      empresaId: 7,
      sucursalId: null,
      tipoDocumento: 'FACTURA',
      ultimoValor: 12,
      prefijo: 'FAC-',
      longitudNumero: 8,
      activa: true,
      ultimoNumeroFormateado: 'FAC-00000012'
    });

    component.reservarSiguiente();

    const reserve = httpMock.expectOne(`${environment.apiUrl}/secuencias-documento/siguiente`);
    expect(reserve.request.method).toBe('POST');
    expect(reserve.request.body).toEqual({ empresaId: 7, sucursalId: null, tipoDocumento: 'FACTURA' });
    reserve.flush({
      success: true,
      data: { empresaId: 7, sucursalId: null, tipoDocumento: 'FACTURA', valor: 13, numero: 'FAC-00000013' },
      message: 'Número reservado correctamente.',
      errors: []
    });

    expect(snackBar.open).toHaveBeenCalledWith('Número reservado: FAC-00000013', 'Cerrar', { duration: 5000 });

    const refresh = httpMock.expectOne(request => request.url === `${environment.apiUrl}/secuencias-documento`);
    expect(refresh.request.params.get('empresaId')).toBe('7');
    expect(refresh.request.params.get('tipoDocumento')).toBe('FACTURA');
    refresh.flush({
      success: true,
      data: {
        empresaId: 7,
        sucursalId: null,
        tipoDocumento: 'FACTURA',
        ultimoValor: 13,
        prefijo: 'FAC-',
        longitudNumero: 8,
        activa: true,
        ultimoNumeroFormateado: 'FAC-00000013'
      },
      message: '',
      errors: []
    });

    expect(component.secuencia()?.ultimoValor).toBe(13);
  });
});
