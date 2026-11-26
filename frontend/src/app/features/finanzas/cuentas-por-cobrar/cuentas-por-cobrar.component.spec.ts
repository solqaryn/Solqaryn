import { provideHttpClient, withXhr } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../../environments/environment';
import { CuentasPorCobrarComponent } from './cuentas-por-cobrar.component';

describe('CuentasPorCobrarComponent - cuarentena empresarial', () => {
  let http: HttpTestingController;
  const url = environment.apiUrl + '/cuentas-por-cobrar';

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CuentasPorCobrarComponent],
      providers: [provideHttpClient(withXhr()), provideHttpClientTesting()]
    }).compileComponents();
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('distingue la cuarentena 503, sin mostrar datos ni un vacío falso, y permite reintentar', () => {
    const fixture = TestBed.createComponent(CuentasPorCobrarComponent);
    fixture.detectChanges();
    http.expectOne(url).flush({ success: false, code: 'TENANT_DATA_ISOLATION_REQUIRED', message: 'detalle no confiable' },
      { status: 503, statusText: 'Service Unavailable' });
    fixture.detectChanges();
    expect(fixture.componentInstance.items()).toEqual([]);
    expect(fixture.componentInstance.error()).toBe('La consulta está temporalmente bloqueada para proteger los datos empresariales.');
    const alert = fixture.nativeElement.querySelector('.state.error[role="alert"]');
    expect(alert?.textContent).toContain('proteger los datos empresariales');
    expect(alert?.textContent).not.toContain('detalle no confiable');
    expect(fixture.nativeElement.querySelector('.state.empty, .table-wrap')).toBeNull();
    fixture.componentInstance.cargar();
    http.expectOne(url).flush({ success: true, data: [], message: '', errors: [] });
    fixture.detectChanges();
    expect(fixture.componentInstance.error()).toBe('');
    expect(fixture.nativeElement.querySelector('.state.empty')).not.toBeNull();
  });

  it('conserva el mensaje genérico para otros 503', () => {
    const fixture = TestBed.createComponent(CuentasPorCobrarComponent);
    fixture.detectChanges();
    http.expectOne(url).flush({ success: false, code: 'OTHER_FAILURE' },
      { status: 503, statusText: 'Service Unavailable' });
    fixture.detectChanges();
    expect(fixture.componentInstance.error()).toBe('Verifica tu conexión o permisos e inténtalo nuevamente.');
    expect(fixture.componentInstance.items()).toEqual([]);
  });
});
