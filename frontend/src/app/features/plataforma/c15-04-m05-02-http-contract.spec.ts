import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

/** C15-04.M05.02 - HttpClient real y backend simulado; NO es JWT/tenant API live. */
describe('C15-04.M05.02 contrato DTO /plataforma/sesion', () => {
  const url = environment.apiUrl + '/plataforma/sesion';
  let http: HttpTestingController;
  let service: PlataformaSesionService;
  const valid = {nombreUsuario:'Plataforma',rolNombre:'Superadministrador',ambito:'Plataforma',esAdministrador:true};
  beforeEach(()=>{
    TestBed.configureTestingModule({providers:[provideHttpClient(),provideHttpClientTesting()]});
    http=TestBed.inject(HttpTestingController);
    service=TestBed.inject(PlataformaSesionService);
  });
  afterEach(()=>http.verify());

  it('DTO positivo requiere GET y grant global completo',()=>{
    let result:unknown;
    service.obtener().subscribe({next:r=>{result=r;}});
    const req=http.expectOne(url);
    expect(req.request.method).toBe('GET');
    expect(req.request.body).toBeNull();
    req.flush({success:true,data:valid});
    expect(result).toEqual(valid);
    expect(service.sesion()).toEqual(valid);
  });

  it('200 de tenant empresarial no equivale a permiso global',()=>{
    let failures=0;
    service.obtener().subscribe({error:()=>{failures++;}});
    http.expectOne(url).flush({success:true,data:{...valid,ambito:'Empresa'}});
    expect(failures).toBe(1);
    expect(service.sesion()).toBeNull();
  });

  it('200 ambiguo, rol vacio o success=false falla cerrado',()=>{
    for(const body of [
      {success:true,data:null},
      {success:true,data:{...valid,rolNombre:'  '}},
      {success:false,data:valid},
      {success:true,data:{...valid,esAdministrador:false}}
    ]){
      let rejected=false;
      service.obtener().subscribe({error:()=>{rejected=true;}});
      http.expectOne(url).flush(body);
      expect(rejected).toBe(true);
      expect(service.sesion()).toBeNull();
    }
  });

  it('401 revocado limpia el grant anterior antes del nuevo GET',()=>{
    service.obtener().subscribe();
    http.expectOne(url).flush({success:true,data:valid});
    expect(service.sesion()).not.toBeNull();
    let denied=false;
    service.obtener().subscribe({error:()=>{denied=true;}});
    expect(service.sesion()).toBeNull();
    http.expectOne(url).flush({message:'PRIVATE_TOKEN'}, {status:401,statusText:'Unauthorized'});
    expect(denied).toBe(true);
    expect(service.sesion()).toBeNull();
  });

  it('403 deniega sin confiar en autoridad local ni dejar sesion',()=>{
    let denied=false;
    service.obtener().subscribe({error:()=>{denied=true;}});
    const req=http.expectOne(url);
    expect(req.request.method).toBe('GET');
    req.flush({message:'PRIVATE_DSN'}, {status:403,statusText:'Forbidden'});
    expect(denied).toBe(true);
    expect(service.sesion()).toBeNull();
  });

  it('503 y fallo de proveedor nunca crean grant ni repiten peticion',()=>{
    let rejected=false;
    service.obtener().subscribe({error:()=>{rejected=true;}});
    const req=http.expectOne(url);
    req.flush({message:'PRIVATE_DSN'}, {status:503,statusText:'Service Unavailable'});
    expect(rejected).toBe(true);
    expect(service.sesion()).toBeNull();
    http.expectNone(url);
  });
});
