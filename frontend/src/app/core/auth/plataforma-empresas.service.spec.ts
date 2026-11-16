import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { PlataformaEmpresasService } from './plataforma-empresas.service';

describe('Microfase 4 — directorio global de empresas', () => {
  let http: HttpTestingController;
  let servicio: PlataformaEmpresasService;
  const endpoint = environment.apiUrl + '/plataforma/empresas';

  beforeEach(() => {
    TestBed.configureTestingModule({ imports: [HttpClientTestingModule] });
    http = TestBed.inject(HttpTestingController);
    servicio = TestBed.inject(PlataformaEmpresasService);
  });
  afterEach(() => http.verify());

  it('admite EMPRESA_A_INACTIVA inactiva como metadato sin permisos de operación', () => {
    let activa: boolean | undefined;
    let habilitada: boolean | undefined;
    servicio.listar().subscribe(d => {
      activa = d.items[0].activa;
      habilitada = d.puedeCrear;
    });
    const request = http.expectOne(r => r.url === endpoint);
    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('pagina')).toBe('1');
    expect(request.request.params.get('tamano')).toBe('25');
    request.flush({ success: true, data: {
      items: [{ id: 1, nombre: 'Empresa A inactiva', activa: false, usuariosActivos: 0 }],
      pagina: 1, tamano: 25, total: 1, puedeCrear: false
    } });
    expect(activa).toBe(false);
    expect(habilitada).toBe(false);
  });

  it('rechaza respuestas empresariales no tipadas y vacía su contenido', () => {
    let rechazo = false;
    servicio.listar().subscribe({ error: () => { rechazo = true; } });
    http.expectOne(r => r.url === endpoint).flush({
      success: true, data: {
        items: [{ id: 1, nombre: 'B', activa: 'true', usuariosActivos: 0 }],
        pagina: 1, tamano: 25, total: 1, puedeCrear: true
      }
    });
    expect(rechazo).toBe(true);
  });

  it('no transforma 403 en listado ni privilegio global', () => {
    let autorizado = false;
    let rechazado = false;
    servicio.listar().subscribe({ next: () => { autorizado = true; },
      error: () => { rechazado = true; } });
    http.expectOne(r => r.url === endpoint).flush({}, {status: 403, statusText: 'Forbidden'});
    expect(autorizado).toBe(false);
    expect(rechazado).toBe(true);
  });

  it('C05-04.M05.02: respeta paginacion valida en frontera 50 y no cambia permisos de crear', () => {
    let result: unknown;
    servicio.listar(2, 50).subscribe(data => { result = data; });
    const req = http.expectOne(r => r.url === endpoint);
    expect(req.request.method).toBe('GET');
    expect(req.request.params.get('pagina')).toBe('2');
    expect(req.request.params.get('tamano')).toBe('50');
    const value = { items: [{ id: 71, nombre: 'Empresa B', activa: false, usuariosActivos: 0 }],
      pagina: 2, tamano: 50, total: 51, puedeCrear: false };
    req.flush({ success: true, data: value });
    expect(result).toEqual(value);
  });

  it('C05-04.M05.02: vacio real 200 es valido pero no concede creacion', () => {
    let result: unknown;
    servicio.listar().subscribe(data => { result = data; });
    http.expectOne(r => r.url === endpoint).flush({
      success: true, data: { items: [], pagina: 1, tamano: 25, total: 0, puedeCrear: false }
    });
    expect(result).toEqual({ items: [], pagina: 1, tamano: 25, total: 0, puedeCrear: false });
  });

  it('C05-04.M05.02: rechaza DTOs malformados de pagina, total, duplicados y metadatos', () => {
    const item = { id: 7, nombre: 'Empresa A', activa: true, usuariosActivos: 1 };
    const valid = { items: [item], pagina: 1, tamano: 25, total: 1, puedeCrear: false };
    const invalid = [
      { ...valid, pagina: 0 },
      { ...valid, pagina: 1.5 },
      { ...valid, tamano: 0 },
      { ...valid, tamano: 51 },
      { ...valid, tamano: 1, items: [item, { ...item, id: 8 }] },
      { ...valid, total: 0 },
      { ...valid, items: [item, { ...item }] },
      { ...valid, items: [{ ...item, nombre: '   ' }] },
      { ...valid, items: [{ ...item, usuariosActivos: -1 }] },
      { ...valid, items: [{ ...item, activa: 'true' }] }
    ];
    for (const data of invalid) {
      let completed = false;
      let rejected = false;
      servicio.listar().subscribe({ next: () => { completed = true; }, error: () => { rejected = true; } });
      http.expectOne(r => r.url === endpoint).flush({ success: true, data });
      expect(completed).toBe(false);
      expect(rejected).toBe(true);
    }
  });

  it('C05-04.M05.02: HTTP 403 y 503 nunca producen directorio ni grant positivo', () => {
    for (const status of [403, 503]) {
      let leaked = false;
      let rejected = false;
      servicio.listar().subscribe({ next: () => { leaked = true; }, error: () => { rejected = true; } });
      http.expectOne(r => r.url === endpoint).flush(
        { success: false, message: 'PRIVATE_PROVIDER_ERROR' },
        { status, statusText: status === 403 ? 'Forbidden' : 'Unavailable' }
      );
      expect(leaked).toBe(false);
      expect(rejected).toBe(true);
    }
  });

});
