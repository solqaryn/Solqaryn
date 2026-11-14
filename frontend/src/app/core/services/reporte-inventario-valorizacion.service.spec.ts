import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { ReporteInventarioValorizacionService } from './reporte-inventario-valorizacion.service';

describe('ReporteInventarioValorizacionService', () => {
  let service: ReporteInventarioValorizacionService;
  let http: HttpTestingController;
  beforeEach(() => { TestBed.configureTestingModule({ imports: [HttpClientTestingModule] }); service = TestBed.inject(ReporteInventarioValorizacionService); http = TestBed.inject(HttpTestingController); });
  afterEach(() => http.verify());
  it('consume el endpoint canónico de valorización', () => {
    service.getResumen().subscribe(res => expect(res.success).toBe(true));
    const req = http.expectOne(`${environment.apiUrl}/inventario/reportes/valorizacion/resumen`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, message: 'ok', errors: [], data: { valorInventarioCosto: 10, valorInventarioCostoMercaderia: 8, valorInventarioCostoInsumosAdministrativos: 2, valorPotencialVentaMercaderia: 12 } });
  });
});
