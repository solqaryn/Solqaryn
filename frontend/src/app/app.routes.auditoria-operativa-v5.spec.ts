import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService } from './core/auth/plataforma-sesion.service';
import { plataformaGuard } from './core/guards/plataforma.guard';
import { AuditoriaOperativaV5Component } from './features/plataforma/auditoria-operativa-v5.component';
import { routes } from './app.routes';

describe('C15-05.M04.01 - ruta y pantalla de auditoría operativa privada', () => {
  const path = 'plataforma/auditoria-operativa-v5';
  const ruta = routes.find(item => item.path === path);

  it('expone una sola ruta con guard de sesión plataforma y sin redirección pública', () => {
    expect(routes.filter(item => item.path === path)).toHaveLength(1);
    expect(ruta?.canActivate).toEqual([plataformaGuard]);
    expect(ruta?.redirectTo).toBeUndefined();
    expect(ruta?.children).toBeUndefined();
    expect(ruta?.data?.['modulo']).toBeUndefined();
  });

  it('carga perezosamente el componente real', async () => {
    expect(ruta?.loadComponent).toBeDefined();
    const resultado = await ruta!.loadComponent!();
    expect(resultado).toBe(AuditoriaOperativaV5Component);
  });

  async function crear(autorizado: boolean) {
    TestBed.resetTestingModule();
    const sesion = signal(autorizado
      ? { ambito: 'Plataforma', esAdministrador: true, nombreUsuario: 'prueba', rolNombre: 'Plataforma' }
      : null);
    await TestBed.configureTestingModule({
      imports: [AuditoriaOperativaV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(AuditoriaOperativaV5Component);
    fixture.detectChanges();
    return fixture;
  }

  it('muestra cuatro controles estáticos solo con sesión de plataforma, sin formularios ni aprobaciones', async () => {
    const fixture = await crear(true);
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('main[aria-labelledby="audit-view-title"]')).not.toBeNull();
    expect(root.querySelectorAll('ol.controls > li')).toHaveLength(4);
    expect(root.textContent).toContain('Guía estática');
    expect(root.textContent).toContain('Sin resultados operativos conectados');
    expect(root.querySelectorAll('form, button, input')).toHaveLength(0);
    expect(root.querySelector('a[aria-label="Regresar a administración de plataforma"]')?.getAttribute('href')).toBe('/plataforma');
    fixture.destroy();
  });

  it('deniega contenido protegido cuando falta sesión y no inventa datos', async () => {
    const fixture = await crear(false);
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[role="alert"]')?.textContent).toContain('Acceso de plataforma no confirmado');
    expect(root.querySelectorAll('ol.controls > li')).toHaveLength(0);
    expect(root.textContent).not.toContain('Propiedad exclusiva');
    expect(root.querySelectorAll('form, button')).toHaveLength(0);
    fixture.destroy();
  });
});
