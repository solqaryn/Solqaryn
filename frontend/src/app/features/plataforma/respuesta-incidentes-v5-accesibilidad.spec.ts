import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { RespuestaIncidentesV5Component } from './respuesta-incidentes-v5.component';

describe('C03-04.M04.05 accesibilidad incidentes', () => {
  const admin: PlataformaSesion = { nombreUsuario:'Control', rolNombre:'Admin', ambito:'Plataforma', esAdministrador:true };
  async function montar(sesionInicial: PlataformaSesion | null) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(sesionInicial);
    await TestBed.configureTestingModule({
      imports:[RespuestaIncidentesV5Component],
      providers:[provideRouter([]), {provide:PlataformaSesionService, useValue:{sesion}}]
    }).compileComponents();
    const fixture=TestBed.createComponent(RespuestaIncidentesV5Component);
    fixture.detectChanges();
    await fixture.whenStable();
    return {fixture, sesion};
  }
  it('ancla de teclado apunta a título visible y enfocable sin operación', async () => {
    const {fixture}=await montar(admin);
    const salto=(fixture.nativeElement as HTMLElement).querySelector<HTMLAnchorElement>('.skip-link');
    const titulo=(fixture.nativeElement as HTMLElement).querySelector<HTMLHeadingElement>('#incident-title');
    expect(salto?.getAttribute('href')).toBe('#incident-title');
    expect(titulo?.tabIndex).toBe(-1);
    expect((fixture.nativeElement as HTMLElement).querySelector('a[aria-label="Regresar a plataforma"]')).toBeTruthy();
    expect(fixture.nativeElement.querySelectorAll('.incident-review li')).toHaveLength(4);
    // M04.02 añade exclusivamente GET de revalidación de sesión: no acciones operativas.
    expect(fixture.nativeElement.querySelectorAll('form, input')).toHaveLength(0);
    const botones = fixture.nativeElement.querySelectorAll('button');
    expect(botones).toHaveLength(1);
    expect(botones[0].getAttribute('type')).toBe('button');
    expect(botones[0].textContent).toContain('Revalidar sesión de plataforma');
  });
  it('rechazo anónimo nunca presenta decisiones protegidas', async () => {
    const {fixture}=await montar(null);
    expect(fixture.nativeElement.querySelectorAll('.incident-review li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('[role=alert]')?.textContent).toContain('Guía protegida oculta');
  });
  it('rechaza sesión de empresa y preserva mensaje sin identificadores', async () => {
    const {fixture}=await montar({...admin,ambito:'Empresa'} as unknown as PlataformaSesion);
    expect(fixture.nativeElement.querySelectorAll('.incident-review li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).not.toContain('Contención y pertenencia');
  });
  it('revocación reactiva retira todo el contenido sin navegación operativa', async () => {
    const {fixture,sesion}=await montar(admin);
    expect(fixture.nativeElement.querySelectorAll('.incident-review li')).toHaveLength(4);
    sesion.set(null);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(fixture.nativeElement.querySelectorAll('.incident-review li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('[role=alert]')).toBeTruthy();
  });
});
