import { provideRouter, Routes } from '@angular/router';
import { TestBed } from '@angular/core/testing';
import { plataformaGuard } from './core/guards/plataforma.guard';
import { GobiernoCambiosV5Component } from './features/plataforma/gobierno-cambios-v5.component';
import { routes } from './app.routes';

describe('C00-04.M04.01 - gobierno de cambios V5, ruta y superficie protegidas', () => {
  const ruta = routes.find((item) => item.path === 'plataforma/gobierno-cambios-v5');

  it('registra una unica ruta privada con el guard que valida plataforma en servidor', () => {
    expect(ruta).toBeDefined();
    expect(routes.filter((item) => item.path === 'plataforma/gobierno-cambios-v5')).toHaveLength(1);
    expect(ruta?.canActivate).toEqual([plataformaGuard]);
    expect(ruta?.path).not.toBe('tienda/gobierno-cambios-v5');
  });

  it('no convierte un enlace en un permiso, un redireccionamiento o una operacion API', () => {
    expect(ruta?.data?.['modulo']).toBeUndefined();
    expect(ruta?.children).toBeUndefined();
    expect(ruta?.redirectTo).toBeUndefined();
    expect(ruta?.providers).toBeUndefined();
  });

  it('carga perezosamente la pantalla real sin alterar el guard', async () => {
    expect(typeof ruta?.loadComponent).toBe('function');
    const componente = await (ruta?.loadComponent as NonNullable<Routes[number]['loadComponent']>)!();
    expect(componente).toBe(GobiernoCambiosV5Component);
  });

  it('expone titulo accesible y solo un enlace de retorno, sin botones de aprobacion', async () => {
    await TestBed.configureTestingModule({
      imports: [GobiernoCambiosV5Component],
      providers: [provideRouter([])]
    }).compileComponents();
    const fixture = TestBed.createComponent(GobiernoCambiosV5Component);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('main[aria-labelledby="governance-title"]')).not.toBeNull();
    expect(root.querySelector('h1#governance-title')?.textContent).toContain('Decisiones y controles');
    expect(root.querySelectorAll('ol.steps > li')).toHaveLength(4);
    expect(root.querySelectorAll('button, form, input')).toHaveLength(0);
    expect(root.querySelector('a[aria-label="Volver a administración de plataforma"]')?.getAttribute('href')).toBe('/plataforma');
  });
});
