import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import { C1204CutoverReadonlyComponent } from './c12-04-m04-01-cutover-readonly.component';

/** C12-04.M05.02: Angular real y HTTP simulado; no acredita MySQL o JWT backend. */
describe('C12-04.M05.02 contrato cutover HTTP/DTO', () => {
  const url = environment.apiUrl + '/plataforma/sesion';
  const valido = {nombreUsuario:'Global',rolNombre:'Plataforma',ambito:'Plataforma',esAdministrador:true};
  let http:HttpTestingController, sesion:PlataformaSesionService;
  beforeEach(async()=>{
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports:[C1204CutoverReadonlyComponent],
      providers:[provideRouter([]),provideHttpClient(),provideHttpClientTesting()]
    }).compileComponents();
    http=TestBed.inject(HttpTestingController);
    sesion=TestBed.inject(PlataformaSesionService);
  });
  afterEach(()=>http.verify());
  function autorizar(){
    let ok=false;
    sesion.obtener().subscribe({next:()=>{ok=true;}});
    const req=http.expectOne(url);
    expect(req.request.method).toBe('GET');
    req.flush({success:true,data:valido});
    expect(ok).toBe(true);
    expect(sesion.sesion()).not.toBeNull();
  }
  function montar(){
    const fixture=TestBed.createComponent(C1204CutoverReadonlyComponent);
    fixture.detectChanges();
    return {fixture,root:fixture.nativeElement as HTMLElement};
  }
  function privadoAusente(root:HTMLElement){
    expect(root.querySelector('[data-testid=c1204-cutover-private]')).toBeNull();
    expect(root.querySelectorAll('.controles li')).toHaveLength(0);
    expect(root.textContent).not.toContain('PRIVATE_DSN_DO_NOT_LEAK');
    expect(sesion.sesion()).toBeNull();
  }
  it('200 global: GET sin body, carga oculta y resultado SIN_VERIFICAR',()=>{
    autorizar();
    const {fixture,root}=montar();
    expect(root.querySelectorAll('.controles li')).toHaveLength(4);
    fixture.componentInstance.revalidar();
    const req=http.expectOne(url);
    expect(req.request.method).toBe('GET');
    expect(req.request.body).toBeNull();
    fixture.detectChanges();
    expect(root.querySelectorAll('.controles li')).toHaveLength(0);
    expect(root.querySelector('[data-testid=c1204-m0402-panel]')?.getAttribute('aria-busy')).toBe('true');
    req.flush({success:true,data:valido});
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('exito');
    expect(root.querySelectorAll('.controles li')).toHaveLength(4);
    expect(root.textContent).toContain('cutover sigue SIN_VERIFICAR');
  });
  it('401 revoca grant y no muestra datos privados',()=>{
    autorizar();const {fixture,root}=montar();
    fixture.componentInstance.revalidar();
    http.expectOne(url).flush({message:'PRIVATE_DSN_DO_NOT_LEAK'}, {status:401,statusText:'Unauthorized'});
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
    privadoAusente(root);
  });
  it('403 deniega sin cache del tenant anterior',()=>{
    autorizar();const {fixture,root}=montar();
    fixture.componentInstance.revalidar();
    http.expectOne(url).flush({message:'PRIVATE_DSN_DO_NOT_LEAK'}, {status:403,statusText:'Forbidden'});
    fixture.detectChanges();expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
    privadoAusente(root);
  });
  it('503 es opaco y no reintenta GET de forma automatica',()=>{
    autorizar();const {fixture,root}=montar();
    fixture.componentInstance.revalidar();
    http.expectOne(url).flush({message:'PRIVATE_DSN_DO_NOT_LEAK'}, {status:503,statusText:'Service Unavailable'});
    fixture.detectChanges();expect(fixture.componentInstance.estadoVisible()).toBe('error');
    privadoAusente(root);
    expect(http.match(()=>true)).toHaveLength(0);
  });
  it('200 de empresa no confiere autoridad de plataforma',()=>{
    autorizar();const {fixture,root}=montar();
    fixture.componentInstance.revalidar();
    http.expectOne(url).flush({success:true,data:{...valido,ambito:'Empresa'}});
    fixture.detectChanges();privadoAusente(root);
  });
  it('200 success=false no concede acceso aunque el DTO aparente ser global',()=>{
    autorizar();const {fixture,root}=montar();
    fixture.componentInstance.revalidar();
    http.expectOne(url).flush({success:false,data:valido});
    fixture.detectChanges();privadoAusente(root);
  });
  it('sin sesion inicial no se generan GET ni controles privados',()=>{
    const {root}=montar();privadoAusente(root);
    expect(http.match(()=>true)).toHaveLength(0);
  });
});
