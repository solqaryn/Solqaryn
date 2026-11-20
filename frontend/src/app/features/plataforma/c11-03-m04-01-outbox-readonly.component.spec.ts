import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { routes } from '../../app.routes';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { C1103OutboxReadonlyComponent } from './c11-03-m04-01-outbox-readonly.component';

describe('C11-03.M04.01 - ruta protegida y superficie outbox no autoritativa', () => {
  const admin: PlataformaSesion = {
    nombreUsuario: 'QA', rolNombre: 'Plataforma',
    ambito: 'Plataforma', esAdministrador: true
  };
  async function montar(initial: PlataformaSesion | null = admin) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(initial);
    await TestBed.configureTestingModule({
      imports: [C1103OutboxReadonlyComponent],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(C1103OutboxReadonlyComponent);
    fixture.detectChanges();
    return { fixture, sesion, root: fixture.nativeElement as HTMLElement };
  }

  it('registra una sola ruta lazy y requiere guard de plataforma', async () => {
    const matches = routes.filter(r => r.path === 'plataforma/eventos-outbox-v22');
    expect(matches).toHaveLength(1);
    expect(matches[0].canActivate).toEqual([plataformaGuard]);
    expect(matches[0].data).toBeUndefined();
    expect(await matches[0].loadComponent?.()).toBe(C1103OutboxReadonlyComponent);
  });

  it('muestra tres etapas pero ninguna evidencia de eventos en vivo', async () => {
    const {root} = await montar();
    expect(root.querySelectorAll('[data-testid^="c1103-outbox-step-"]')).toHaveLength(3);
    expect(root.textContent).toContain('SIN_FUENTE_CONECTADA');
    expect(root.textContent).toContain('no confirma JWT del servidor');
    expect(root.querySelectorAll('form,input,button,select,textarea,iframe')).toHaveLength(0);
    expect(root.querySelector('a[aria-label="Volver a plataforma"]')).not.toBeNull();
  });

  it('falla cerrado con sesión nula o de empresa y al revocar sesión', async () => {
    const {root,fixture,sesion} = await montar(null);
    expect(root.querySelector('[data-testid="c1103-outbox-private"]')).toBeNull();
    expect(root.querySelector('[role="alert"]')).not.toBeNull();
    sesion.set({ ...admin, ambito: 'Empresa' } as PlataformaSesion);
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="c1103-outbox-private"]')).toBeNull();
    sesion.set(admin);
    fixture.detectChanges();
    expect(root.querySelectorAll('[data-testid^="c1103-outbox-step-"]')).toHaveLength(3);
    sesion.set(null);
    fixture.detectChanges();
    expect(root.querySelectorAll('[data-testid^="c1103-outbox-step-"]')).toHaveLength(0);
  });

  it('no otorga privilegios si esAdministrador es falso y no refleja cadenas hostiles', async () => {
    const {root} = await montar({ ...admin, esAdministrador: false, nombreUsuario: '<img src=x onerror=alert(1)>' });
    expect(root.querySelector('[data-testid="c1103-outbox-private"]')).toBeNull();
    expect(root.querySelector('img')).toBeNull();
    expect(root.innerHTML).not.toContain('onerror');
  });
});
