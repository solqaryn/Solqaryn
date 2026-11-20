import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { C1103OutboxReadonlyComponent } from './c11-03-m04-01-outbox-readonly.component';

describe('C11-03.M04.02 - estados accesibles UI desconectada', () => {
  const admin: PlataformaSesion = {nombreUsuario:'QA',rolNombre:'Plataforma',ambito:'Plataforma',esAdministrador:true};
  async function montar(inicial: PlataformaSesion | null = admin) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(inicial);
    await TestBed.configureTestingModule({
      imports:[C1103OutboxReadonlyComponent],
      providers:[provideRouter([]),{provide:PlataformaSesionService,useValue:{sesion}}]
    }).compileComponents();
    const fixture = TestBed.createComponent(C1103OutboxReadonlyComponent);
    fixture.detectChanges();
    return {fixture,sesion,root:fixture.nativeElement as HTMLElement};
  }
  it('vacio conserva referencias sin inventar eventos',async()=>{
    const {root}=await montar();
    expect(root.querySelector('[data-testid="c1103-state-empty"]')?.getAttribute('role')).toBe('status');
    expect(root.querySelectorAll('[data-testid^="c1103-outbox-step-"]')).toHaveLength(3);
    expect(root.textContent).toContain('SIN_FUENTE_CONECTADA');
    expect(root.querySelectorAll('form,button,input,iframe')).toHaveLength(0);
  });
  it('cargando anuncia estado accesible sin relay',async()=>{
    const {root,fixture}=await montar();
    fixture.componentRef.setInput('estadoVista','cargando');fixture.detectChanges();
    expect(root.querySelector('[data-testid="c1103-outbox-private"]')?.getAttribute('aria-busy')).toBe('true');
    expect(root.querySelector('[data-testid="c1103-state-loading"]')?.getAttribute('aria-live')).toBe('polite');
    expect(root.textContent).toContain('no se consulta un relay real');
    expect(root.querySelector('[data-testid="c1103-state-ready"]')).toBeNull();
  });
  it('error no refleja detalles sensibles de servidor',async()=>{
    const {root,fixture}=await montar();
    fixture.componentRef.setInput('estadoVista','error');fixture.detectChanges();
    expect(root.querySelector('[data-testid="c1103-state-error"]')?.getAttribute('role')).toBe('alert');
    expect(root.textContent).toContain('Ningún evento fue transmitido');
    expect(root.querySelector('img,script,iframe')).toBeNull();
  });
  it('listo no certifica una entrega',async()=>{
    const {root,fixture}=await montar();
    fixture.componentRef.setInput('estadoVista','listo');fixture.detectChanges();
    expect(root.querySelector('[data-testid="c1103-state-ready"]')?.getAttribute('role')).toBe('status');
    expect(root.querySelector('[data-testid="c1103-outbox-private"]')?.getAttribute('aria-busy')).toBe('false');
    expect(root.textContent).toContain('no confirma entregas');
    expect(root.querySelectorAll('form,button,input')).toHaveLength(0);
  });
  it('sin rol o tras revocar no expone estado privado',async()=>{
    const {root,fixture,sesion}=await montar(null);
    fixture.componentRef.setInput('estadoVista','listo');fixture.detectChanges();
    expect(root.querySelector('[data-testid="c1103-outbox-private"]')).toBeNull();
    sesion.set({...admin,ambito:'Empresa'} as unknown as PlataformaSesion);fixture.detectChanges();
    expect(root.querySelector('[data-testid="c1103-state-ready"]')).toBeNull();
    sesion.set(admin);fixture.detectChanges();
    expect(root.querySelector('[data-testid="c1103-state-ready"]')).not.toBeNull();
    sesion.set(null);fixture.detectChanges();
    expect(root.querySelector('[data-testid="c1103-outbox-private"]')).toBeNull();
  });
  it('estado hostil desconocido falla cerrado',async()=>{
    const {root,fixture}=await montar();
    fixture.componentRef.setInput('estadoVista','<img src=x onerror=alert(1)>');fixture.detectChanges();
    expect(root.querySelector('[data-testid="c1103-state-invalid"]')?.getAttribute('role')).toBe('alert');
    expect(root.querySelector('img')).toBeNull();
    expect(root.innerHTML).not.toContain('onerror');
  });
});
