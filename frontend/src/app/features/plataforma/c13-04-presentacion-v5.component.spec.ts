import { signal } from '@angular/core';
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
    await TestBed.configureTestingModule({imports:[C1304PresentacionV5Component],providers:[
      provideRouter([]), {provide:PlataformaSesionService,useValue:{sesion}}
    ]}).compileComponents();
    const fixture=TestBed.createComponent(C1304PresentacionV5Component);
    fixture.detectChanges();
    return {fixture,sesion,root:fixture.nativeElement as HTMLElement};
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
    sesion.set({...admin,ambito:'Empresa'} as PlataformaSesion);
    fixture.detectChanges();
    expect(root.querySelectorAll('.visual-actions button')).toHaveLength(0);
    expect(fixture.componentInstance.seleccionar({marca:'contraste',densidad:'compacta'})).toBe(false);
    sesion.set(null);
    fixture.detectChanges();
    expect(root.querySelector('[role=alert]')?.textContent).toContain('Sin permiso');
  });
});
