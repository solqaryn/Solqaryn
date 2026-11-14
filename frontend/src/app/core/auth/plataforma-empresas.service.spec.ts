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
});
