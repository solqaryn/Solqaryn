import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { C1103OutboxReadonlyComponent } from './c11-03-m04-01-outbox-readonly.component';

describe('C11-03.M04.03 - branding privado fail closed', () => {
  const admin: PlataformaSesion = {nombreUsuario:'QA',rolNombre:'Plataforma',ambito:'Plataforma',esAdministrador:true};
  async function montar(initial: PlataformaSesion | null = admin) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(initial);
    await TestBed.configureTestingModule({
      imports:[C1103OutboxReadonlyComponent],
      providers:[provideRouter([]),{provide:PlataformaSesionService,useValue:{sesion}}]
    }).compileComponents();
    const fixture = TestBed.createComponent(C1103OutboxReadonlyComponent);
    fixture.detectChanges();
    return {fixture,sesion,root:fixture.nativeElement as HTMLElement};
  }
  it('base predeterminado sin formularios, imagenes o PSP',async()=>{
    const {root}=await montar();
    expect(root.querySelector('[data-testid="c1103-outbox-view"]')?.getAttribute('data-tema')).toBe('base');
    expect(root.querySelector('[data-testid="c1103-branding-preset"]')?.textContent).toContain('SOLQARYN');
    expect(root.querySelectorAll('form,button,input,iframe,img')).toHaveLength(0);
  });
  it('contraste exacto actualiza Signal Input y vista despues de detectChanges',async()=>{
    const {root,fixture}=await montar();
    fixture.componentRef.setInput('temaSolicitado','contraste');
    fixture.detectChanges();await fixture.whenStable();fixture.detectChanges();
    expect(root.querySelector('[data-testid="c1103-outbox-view"]')?.getAttribute('data-tema')).toBe('contraste');
    expect(root.querySelector('[data-testid="c1103-branding-preset"]')?.textContent).toContain('alto contraste');
  });
  it('rechaza HTML/CSS y presets no autorizados sin reflejarlos',async()=>{
    const {root,fixture}=await montar();
    for(const valor of ['CONTRASTE','contraste ','<img src=x onerror=alert(1)>','url(javascript:alert(1))']){
      fixture.componentRef.setInput('temaSolicitado',valor);
      fixture.detectChanges();await fixture.whenStable();fixture.detectChanges();
      expect(root.querySelector('[data-testid="c1103-outbox-view"]')?.getAttribute('data-tema')).toBe('base');
      expect(root.innerHTML).not.toContain('onerror');
      expect(root.querySelectorAll('img,iframe,script')).toHaveLength(0);
    }
  });
  it('revocacion y ambito Empresa ocultan branding privado',async()=>{
    const {root,fixture,sesion}=await montar();
    expect(root.querySelector('[data-testid="c1103-branding-preset"]')).not.toBeNull();
    sesion.set({...admin,ambito:'Empresa'} as unknown as PlataformaSesion);
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="c1103-branding-preset"]')).toBeNull();
    sesion.set(null);fixture.detectChanges();
    expect(root.querySelector('[data-testid="c1103-outbox-private"]')).toBeNull();
  });
});
