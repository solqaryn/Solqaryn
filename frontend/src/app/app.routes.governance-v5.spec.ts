import { provideRouter, Routes } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { Subject } from 'rxjs';
import { vi } from 'vitest';
import { PlataformaSesionService } from './core/auth/plataforma-sesion.service';
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
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion: signal({ nombreUsuario: 'V5 UI', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true }), obtener: vi.fn() } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(GobiernoCambiosV5Component);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('main[aria-labelledby="governance-title"]')).not.toBeNull();
    expect(root.querySelector('h1#governance-title')?.textContent).toContain('Decisiones y controles');
    expect(root.querySelectorAll('ol.steps > li')).toHaveLength(4);
    expect(root.querySelectorAll('form, input')).toHaveLength(0);
    expect(root.querySelectorAll('button')).toHaveLength(1);
    expect(root.querySelector('button')?.textContent).toContain('Revalidar sesión');
    expect(root.querySelector('a[aria-label="Volver a administración de plataforma"]')?.getAttribute('href')).toBe('/plataforma');
  });
});

describe('C00-04.M04.02 - estados reales de sesión, vacío y denegación', () => {
  const autorizado = {
    nombreUsuario: 'UI test', rolNombre: 'Plataforma',
    ambito: 'Plataforma' as const, esAdministrador: true as const
  };

  async function montar(conSesion: boolean, respuesta: Subject<typeof autorizado>) {
    TestBed.resetTestingModule();
    const sesion = signal<typeof autorizado | null>(conSesion ? autorizado : null);
    const obtener = vi.fn(() => respuesta.asObservable());
    await TestBed.configureTestingModule({
      imports: [GobiernoCambiosV5Component],
      providers: [
        provideRouter([]),
        { provide: PlataformaSesionService, useValue: { sesion, obtener } }
      ]
    }).compileComponents();
    const fixture = TestBed.createComponent(GobiernoCambiosV5Component);
    fixture.detectChanges();
    return { fixture, obtener, respuesta };
  }

  it('inicial autorizado muestra vacío honesto sin atribuir resultados institucionales', async () => {
    const { fixture, obtener } = await montar(true, new Subject<typeof autorizado>());
    expect(fixture.componentInstance.estado()).toBe('vacio');
    expect(fixture.nativeElement.textContent).toContain('Sin datos institucionales conectados');
    expect(fixture.nativeElement.querySelectorAll('ol.steps li')).toHaveLength(4);
    expect(obtener).not.toHaveBeenCalled();
  });

  it('revalidación muestra carga, evita doble petición y solo entonces confirma éxito', async () => {
    const respuesta = new Subject<typeof autorizado>();
    const { fixture, obtener } = await montar(true, respuesta);
    const control = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    control.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('cargando');
    expect(control.disabled).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('Verificando permisos');
    fixture.componentInstance.revalidar();
    expect(obtener).toHaveBeenCalledOnce();
    respuesta.next(autorizado);
    respuesta.complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('exito');
    expect(control.disabled).toBe(false);
    expect(fixture.nativeElement.textContent).toContain('solo lectura');
    expect(fixture.nativeElement.textContent).toContain('Sin datos institucionales conectados');
  });

  it('error HTTP 403 conserva denegación y oculta controles de contenido', async () => {
    const respuesta = new Subject<typeof autorizado>();
    const { fixture } = await montar(true, respuesta);
    fixture.componentInstance.revalidar();
    respuesta.error(new HttpErrorResponse({ status: 403, error: { message: 'TOKEN_PRIVADO' } }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('ol.steps li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).not.toContain('TOKEN_PRIVADO');
    expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent).toContain('Sin permiso');
  });

  it('fallo de red 503 informa error genérico sin filtrar payload ni conceder acceso', async () => {
    const respuesta = new Subject<typeof autorizado>();
    const { fixture } = await montar(true, respuesta);
    fixture.componentInstance.revalidar();
    respuesta.error(new HttpErrorResponse({ status: 503, error: { message: 'SECRETO_INTERNO' } }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('error');
    expect(fixture.nativeElement.textContent).not.toContain('SECRETO_INTERNO');
    expect(fixture.nativeElement.querySelectorAll('ol.steps li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });

  it('sin sesión inicial nunca muestra la secuencia protegida ni inicia I/O', async () => {
    const { fixture, obtener } = await montar(false, new Subject<typeof autorizado>());
    expect(fixture.componentInstance.estado()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('ol.steps li')).toHaveLength(0);
    expect(obtener).not.toHaveBeenCalled();
  });
});
