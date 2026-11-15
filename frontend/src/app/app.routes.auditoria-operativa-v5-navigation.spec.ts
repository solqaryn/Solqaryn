import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService } from './core/auth/plataforma-sesion.service';
import { AuditoriaOperativaV5Component } from './features/plataforma/auditoria-operativa-v5.component';
import { routes } from './app.routes';

/** C15-05.M04.04: pruebas reales de navegación, mensajes y ausencia de doble acción. */
describe('C15-05.M04.04 - consulta de auditoría sin acciones duplicables', () => {
  async function render(admin: boolean) {
    TestBed.resetTestingModule();
    const sesion = signal({
      ambito: 'Plataforma',
      esAdministrador: admin,
      rolNombre: 'Plataforma',
      nombreUsuario: 'prueba-sintetica'
    });
    await TestBed.configureTestingModule({
      imports: [AuditoriaOperativaV5Component],
      providers: [
        provideRouter([]),
        { provide: PlataformaSesionService, useValue: { sesion } }
      ]
    }).compileComponents();
    const fixture = TestBed.createComponent(AuditoriaOperativaV5Component);
    fixture.detectChanges();
    return fixture;
  }

  it('mantiene la ruta privada con guard sin operaciones de negocio', () => {
    const route = routes.find(r => r.path === 'plataforma/auditoria-operativa-v5');
    expect(route).toBeDefined();
    expect(route?.canActivate).toHaveLength(1);
    expect(route?.data?.['accion']).toBeUndefined();
    expect(route?.redirectTo).toBeUndefined();
  });

  it('presenta estado honesto sin telemetría ni resultado vivo', async () => {
    const fixture = await render(true);
    const root: HTMLElement = fixture.nativeElement;
    expect(root.textContent).toContain('Guía estática');
    expect(root.textContent).toContain('Sin resultados operativos conectados');
    expect(root.textContent).toContain('no sustituye PLAN, COLA, DAG, BITACORA ni CI');
    expect(root.querySelectorAll('ol.controls > li')).toHaveLength(4);
    fixture.destroy();
  });

  it('carece de formularios, botones y doble envío: solo navegación de lectura', async () => {
    const fixture = await render(true);
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelectorAll('form, input, select, textarea, button, [role="button"]')).toHaveLength(0);
    expect(root.querySelectorAll('a')).toHaveLength(1);
    const returnLink = root.querySelector('a');
    expect(returnLink?.getAttribute('href')).toBe('/plataforma');
    expect(returnLink?.getAttribute('aria-label')).toBe('Regresar a administración de plataforma');
    expect(root.textContent).not.toContain('Aprobar microtarea');
    fixture.destroy();
  });

  it('deniega controles a no-admin y no ofrece acciones de reintento o aprobación', async () => {
    const fixture = await render(false);
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[role="alert"]')?.textContent).toContain('Acceso de plataforma no confirmado');
    expect(root.querySelectorAll('ol.controls > li')).toHaveLength(0);
    expect(root.querySelectorAll('form, button, input, [role="button"]')).toHaveLength(0);
    expect(root.textContent).not.toContain('Propiedad exclusiva');
    fixture.destroy();
  });
});
