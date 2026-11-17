import { signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Subject, tap } from 'rxjs';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { routes } from '../../app.routes';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { C1304PresentacionV5Component, normalizarPresentacionC1304 } from './c13-04-presentacion-v5.component';

describe('C13-04.M04.03 visual allowlist', () => {
  const admin: PlataformaSesion = { nombreUsuario:'Synthetic', rolNombre:'Global', ambito:'Plataforma', esAdministrador:true };
  async function montar(autorizado=true) {
    TestBed.resetTestingModule();
    const sesion=signal<PlataformaSesion|null>(autorizado ? admin : null);
    const pendiente = new Subject<PlataformaSesion>();
    let consultas = 0;
    await TestBed.configureTestingModule({imports:[C1304PresentacionV5Component],providers:[
      provideRouter([]), {provide:PlataformaSesionService,useValue:{
        sesion, obtener: () => {
          consultas++;
          sesion.set(null);
          return pendiente.pipe(tap(value => sesion.set(value)));
        }
      }}
    ]}).compileComponents();
    const fixture=TestBed.createComponent(C1304PresentacionV5Component);
    fixture.detectChanges();
    return {fixture,sesion,pendiente,consultas:()=>consultas,root:fixture.nativeElement as HTMLElement};
  }
  it('ruta privada única usa plataformaGuard',()=>{
    const m=routes.filter(x=>x.path==='plataforma/experiencia-visual-v5');
    expect(m).toHaveLength(1);
    expect(m[0].canActivate).toEqual([plataformaGuard]);
  });
  it('presets permitidos cambian estado local sin formularios',async()=>{
    const {fixture,root}=await montar();
    expect(root.querySelectorAll('.visual-actions button')).toHaveLength(4);
    expect(root.querySelectorAll('form,input,textarea')).toHaveLength(0);
    expect(fixture.componentInstance.seleccionar({marca:'contraste',densidad:'compacta'})).toBe(true);
    fixture.detectChanges();
    expect(root.querySelector('.visual-shell')?.classList.contains('visual-contrast')).toBe(true);
    expect(root.querySelector('.visual-shell')?.classList.contains('visual-compact')).toBe(true);
    expect(root.textContent).toContain('NO_MEDIDO');
    expect(fixture.componentInstance.seleccionar({marca:'contraste',densidad:'compacta'})).toBe(false);
  });
  it('HTML y CSS no permitidos nunca se interpolan',async()=>{
    const {fixture,root}=await montar();
    fixture.componentInstance.seleccionar({marca:'<img src=x onerror=alert(1)>',densidad:'compacta'});
    fixture.detectChanges();
    expect(root.querySelector('img')).toBeNull();
    expect(root.querySelector('.visual-shell')?.getAttribute('style')).toBeNull();
    expect(fixture.componentInstance.presentacion().marca).toBe('institucional');
  });
  it('un getter mutable se lee exactamente una vez',()=>{
    let reads=0;
    const obj={get marca(){reads++;return reads===1?'contraste':'<script>';},densidad:'compacta'};
    expect(normalizarPresentacionC1304(obj)).toEqual({marca:'contraste',densidad:'compacta'});
    expect(reads).toBe(1);
  });
  it('getters que fallan y Proxy revocado usan default congelado',()=>{
    const obj={get marca():never{throw new Error('PRIVATE_DSN');},densidad:'compacta'};
    const r=normalizarPresentacionC1304(obj);
    expect(r).toEqual({marca:'institucional',densidad:'comoda'});
    expect(Object.isFrozen(r)).toBe(true);
    const p=Proxy.revocable({},{});
    p.revoke();
    expect(normalizarPresentacionC1304(p.proxy)).toEqual(r);
  });
  it('revocación y rol ERP no revelan controles',async()=>{
    const {fixture,sesion,root}=await montar();
    sesion.set({...admin,ambito:'Empresa'} as unknown as PlataformaSesion);
    fixture.detectChanges();
    expect(root.querySelectorAll('.visual-actions button')).toHaveLength(0);
    expect(fixture.componentInstance.seleccionar({marca:'contraste',densidad:'compacta'})).toBe(false);
    sesion.set(null);
    fixture.detectChanges();
    expect(root.querySelector('[role=alert]')?.textContent).toContain('Sin permiso');
  });

  it('M04.02 inicializa vacío/SIN_VERIFICAR y revalida sesión sin confundirla con catálogo', async () => {
    const {fixture,root,pendiente,consultas}=await montar();
    const vm=fixture.componentInstance;
    expect(vm.estadoInventario()).toBe('vacio');
    expect(root.textContent).toContain('SIN_VERIFICAR');
    expect(consultas()).toBe(0);
    vm.revalidarEstadoGlobal();
    vm.revalidarEstadoGlobal();
    fixture.detectChanges();
    expect(consultas()).toBe(1);
    expect(vm.estadoInventario()).toBe('cargando');
    expect(root.querySelectorAll('.visual-actions button')).toHaveLength(0);
    pendiente.next(admin);
    pendiente.complete();
    fixture.detectChanges();
    expect(vm.estadoInventario()).toBe('exito');
    expect(root.querySelector('[data-testid=c1304-m0402-state]')?.textContent).toContain('sesión global');
    expect(root.textContent).toContain('SIN_VERIFICAR');
    expect(root.textContent).not.toContain('QA_AUTOMATICO_VERIFICADO');
  });

  it('M04.02 revocación de sesión y 403 ocultan todos los controles privados', async () => {
    const {fixture,root,pendiente,consultas}=await montar();
    fixture.componentInstance.revalidarEstadoGlobal();
    pendiente.error(new HttpErrorResponse({status:403, statusText:'PRIVATE_TENANT'}));
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoInventario()).toBe('sin_permiso');
    expect(root.querySelectorAll('.visual-actions button')).toHaveLength(0);
    expect(root.querySelector('[data-testid=c1304-m0402-revalidate]')).toBeNull();
    fixture.componentInstance.revalidarEstadoGlobal();
    expect(consultas()).toBe(1);
    expect(root.textContent).not.toContain('PRIVATE_TENANT');
  });

  it('M04.02 error 503 y fuente vacía no son PASS; mensajes siempre opacos', async () => {
    const {fixture,root,pendiente}=await montar();
    fixture.componentInstance.revalidarEstadoGlobal();
    pendiente.error(new HttpErrorResponse({status:503, statusText:'PRIVATE_DSN'}));
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoInventario()).toBe('error');
    expect(root.textContent).not.toContain('PRIVATE_DSN');
    expect(root.textContent).not.toContain('LISTO_TECNICO certificado');
    const second=await montar();
    second.fixture.componentInstance.revalidarEstadoGlobal();
    second.pendiente.complete();
    second.fixture.detectChanges();
    expect(second.fixture.componentInstance.estadoInventario()).toBe('error');
  });

  it('M04.02 no permite intentar revisión sin plataforma', async () => {
    const {fixture,root,consultas}=await montar(false);
    fixture.componentInstance.revalidarEstadoGlobal();
    fixture.detectChanges();
    expect(consultas()).toBe(0);
    expect(fixture.componentInstance.estadoInventario()).toBe('sin_permiso');
    expect(root.querySelector('[data-testid=c1304-m0402-revalidate]')).toBeNull();
  });
});
