import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { C0904MensajeriaPrivadaComponent, CANALES_DECLARADOS_C0904, PRESENTACIONES_C0904 } from './c09-04-m04-01-mensajeria-privada.component';

describe('C09-04.M04.03 branding local con allowlist cerrada R2', () => {
  const admin: PlataformaSesion = {
    nombreUsuario: 'Admin', rolNombre: 'Administrador', ambito: 'Plataforma', esAdministrador: true
  };
  async function montar(estado: PlataformaSesion | null = admin) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(estado);
    await TestBed.configureTestingModule({
      imports: [C0904MensajeriaPrivadaComponent],
      providers: [provideRouter([]), {provide: PlataformaSesionService, useValue: {sesion: sesion.asReadonly()}}]
    }).compileComponents();
    const fixture = TestBed.createComponent(C0904MensajeriaPrivadaComponent);
    fixture.detectChanges();
    return {fixture, root: fixture.nativeElement as HTMLElement, sesion};
  }

  it('administrador puede elegir exactamente tres presentaciones locales sin operaciones HTTP', async () => {
    const {fixture,root}=await montar();
    const select=root.querySelector<HTMLSelectElement>('[data-testid=c0904-tema-selector]');
    expect(select).not.toBeNull();
    expect(Array.from(select!.options).map(o => o.value)).toEqual(['neutro','alto-contraste','azul']);
    select!.value='alto-contraste';
    select!.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(root.querySelector('[data-testid=c0904-branding-config]')?.classList.contains('tema-alto-contraste')).toBe(true);
    expect(root.querySelector('[data-testid=c0904-tema-activo]')?.textContent).toContain('Alto contraste');
    expect(root.querySelectorAll('form,input,textarea,button')).toHaveLength(0);
  });

  it('entradas no permitidas, URL, CSS y contenido HTML caen al preset neutro', async () => {
    const {fixture,root}=await montar();
    for (const candidate of ['javascript:alert(1)','https://tenant-b.local','alto-contraste;position:absolute','<img src=x onerror=alert(1)>','']) {
      fixture.componentInstance.seleccionarTema(candidate);
      fixture.detectChanges();
      expect(fixture.componentInstance.presentacion().id).toBe('neutro');
      expect(root.querySelector('[data-testid=c0904-branding-config]')?.getAttribute('style')).toBeNull();
    }
    expect(PRESENTACIONES_C0904.every(p=>Object.isFrozen(p))).toBe(true);
    expect(CANALES_DECLARADOS_C0904.every(c=>c.estado==='NO_CONECTADO')).toBe(true);
  });

  it('empresa o plataforma no administrador no ve branding ni componentes', async () => {
    for (const estado of [{...admin,ambito:'Empresa'}, {...admin,esAdministrador:false}]) {
      const {fixture,root}=await montar(estado as unknown as PlataformaSesion);
      fixture.componentInstance.seleccionarTema('azul');
      fixture.detectChanges();
      expect(root.querySelector('[data-testid=c0904-branding-config]')).toBeNull();
      expect(root.querySelector('[data-testid=c0904-canales-privados]')).toBeNull();
      expect(fixture.componentInstance.presentacion().id).toBe('neutro');
    }
  });

  it('revocacion elimina interfaz y bloquea cambios de tema', async () => {
    const {fixture,root,sesion}=await montar();
    fixture.componentInstance.seleccionarTema('alto-contraste');
    sesion.set(null); fixture.detectChanges();
    fixture.componentInstance.seleccionarTema('azul'); fixture.detectChanges();
    expect(root.querySelector('[data-testid=c0904-branding-config]')).toBeNull();
    expect(fixture.componentInstance.presentacion().id).toBe('neutro');
  });

  it('elementos permitidos son lectura cerrada, sin URLs ni destinos variables', async () => {
    const {fixture,root}=await montar();
    expect(fixture.componentInstance.elementosPermitidos.map(x=>x.id)).toEqual(['tarjetas','estados','retorno']);
    expect(root.querySelectorAll('[data-testid=c0904-componentes-permitidos] li')).toHaveLength(3);
    expect(root.querySelectorAll('form,textarea,button,input')).toHaveLength(0);
    expect(JSON.stringify(fixture.componentInstance.elementosPermitidos)).not.toContain('http');
  });
});
