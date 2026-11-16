import { signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { EMPTY, of, Subject, throwError, type Observable } from 'rxjs';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { routes } from '../../app.routes';
import { ObservabilidadPrivadaV5Component } from './observabilidad-privada-v5.component';

describe('C11-04.M04.01 - observabilidad privada', () => {
  const admin: PlataformaSesion = {
    nombreUsuario: 'Plataforma', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true
  };
  async function montar(value: PlataformaSesion | null,
    obtener: () => Observable<PlataformaSesion> = () => of(admin)) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(value);
    await TestBed.configureTestingModule({
      imports: [ObservabilidadPrivadaV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion, obtener } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(ObservabilidadPrivadaV5Component);
    fixture.detectChanges();
    return { fixture, sesion };
  }
  it('ruta única lazy con guard de plataforma', async () => {
    const matches = routes.filter(x => x.path === 'plataforma/observabilidad-v5');
    expect(matches).toHaveLength(1);
    expect(matches[0].canActivate).toEqual([plataformaGuard]);
    expect(await matches[0].loadComponent?.()).toBe(ObservabilidadPrivadaV5Component);
  });
  it('anónimo no muestra detalles privados', async () => {
    const { fixture } = await montar(null);
    expect(fixture.componentInstance.verificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.obs-points li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).toContain('Observabilidad privada oculta');
  });
  it('plataforma autorizada muestra cuatro frentes sin acciones operativas', async () => {
    const { fixture } = await montar(admin);
    expect(fixture.componentInstance.verificado()).toBe(true);
    expect(fixture.nativeElement.querySelectorAll('.obs-points li')).toHaveLength(4);
    expect(fixture.nativeElement.querySelectorAll('form, input, textarea')).toHaveLength(0);
    expect(fixture.nativeElement.querySelectorAll('button')).toHaveLength(1);
    expect(fixture.nativeElement.textContent).toContain('NO_MEDIDO');
  });
  it('sesión empresarial forjada no habilita observabilidad', async () => {
    const { fixture } = await montar({ ...admin, ambito: 'Empresa' } as unknown as PlataformaSesion);
    expect(fixture.componentInstance.verificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.obs-points li')).toHaveLength(0);
  });
  it('revocación oculta los cuatro frentes inmediatamente', async () => {
    const { fixture, sesion } = await montar(admin);
    expect(fixture.nativeElement.querySelectorAll('.obs-points li')).toHaveLength(4);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.componentInstance.verificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.obs-points li')).toHaveLength(0);
  });
});


describe('C11-04.M04.02 - estados seguros de observabilidad', () => {
  const admin: PlataformaSesion = { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true };
  async function montar(obtener: () => Observable<PlataformaSesion>) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(admin);
    await TestBed.configureTestingModule({
      imports: [ObservabilidadPrivadaV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion, obtener } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(ObservabilidadPrivadaV5Component);
    fixture.detectChanges();
    return { fixture, sesion, component: fixture.componentInstance };
  }
  it('muestra vacio sin simular metricas conectadas', async () => {
    const { fixture, component } = await montar(() => of(admin));
    expect(component.estadoVisible()).toBe('vacio');
    expect(fixture.nativeElement.textContent).toContain('No hay métricas ni diagnósticos en vivo');
  });
  it('cargando oculta contenido y evita reintentos simultaneos', async () => {
    const pending = new Subject<PlataformaSesion>();
    let llamadas = 0;
    const { fixture, component } = await montar(() => { llamadas++; return pending.asObservable(); });
    component.revalidar();
    component.revalidar();
    fixture.detectChanges();
    expect(llamadas).toBe(1);
    expect(component.estadoVisible()).toBe('cargando');
    expect(fixture.nativeElement.querySelectorAll('.obs-points li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('button')?.disabled).toBe(true);
    pending.next(admin); pending.complete(); fixture.detectChanges();
    expect(component.estadoVisible()).toBe('exito');
    expect(fixture.nativeElement.querySelectorAll('.obs-points li')).toHaveLength(4);
  });
  it('respuesta 403 oculta frentes y mantiene permiso denegado', async () => {
    const { fixture, component } = await montar(() => throwError(() => new HttpErrorResponse({ status: 403 })));
    component.revalidar(); fixture.detectChanges();
    expect(component.estadoVisible()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.obs-points li')).toHaveLength(0);
  });
  it('fallo 503 es generico y no divulga detalle de proveedor', async () => {
    const { fixture, component } = await montar(() => throwError(() =>
      new HttpErrorResponse({ status: 503, error: { secret: 'DO_NOT_DISPLAY' } })));
    component.revalidar(); fixture.detectChanges();
    expect(component.estadoVisible()).toBe('error');
    expect(fixture.nativeElement.textContent).not.toContain('DO_NOT_DISPLAY');
    expect(fixture.nativeElement.querySelectorAll('.obs-points li')).toHaveLength(0);
  });
  it('respuesta vacia o throw sincrono no se interpreta como permiso', async () => {
    const a = await montar(() => EMPTY);
    a.component.revalidar(); a.fixture.detectChanges();
    expect(a.component.estadoVisible()).toBe('error');
    const b = await montar(() => { throw new Error('SENSITIVE'); });
    b.component.revalidar(); b.fixture.detectChanges();
    expect(b.component.estadoVisible()).toBe('error');
    expect(b.fixture.nativeElement.textContent).not.toContain('SENSITIVE');
  });
  it('suplantacion de ambito empresarial y revocacion inmediata ocultan datos', async () => {
    const { fixture, component, sesion } = await montar(() =>
      of({ ...admin, ambito: 'Empresa' } as unknown as PlataformaSesion));
    component.revalidar(); fixture.detectChanges();
    expect(component.estadoVisible()).toBe('sin_permiso');
    sesion.set(admin); component.estado.set('exito'); fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.obs-points li')).toHaveLength(4);
    sesion.set(null); fixture.detectChanges();
    expect(component.verificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.obs-points li')).toHaveLength(0);
  });
});


describe('C11-04.M04.03 - branding visual tipado de observabilidad', () => {
  const admin: PlataformaSesion = { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true };
  async function montar(inicial: PlataformaSesion | null) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(inicial);
    let llamadas = 0;
    await TestBed.configureTestingModule({
      imports: [ObservabilidadPrivadaV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: {
        sesion, obtener: () => { llamadas++; return of(admin); }
      }}]
    }).compileComponents();
    const fixture = TestBed.createComponent(ObservabilidadPrivadaV5Component);
    fixture.detectChanges();
    return { fixture, sesion, llamadas: () => llamadas };
  }
  it('muestra marca institucional sin métricas ni acciones conectadas', async () => {
    const { fixture, llamadas } = await montar(admin);
    const region: HTMLElement = fixture.nativeElement.querySelector('[aria-label="Identidad visual de observabilidad"]');
    expect(region).not.toBeNull();
    expect(region.textContent).toContain('SOLQARYN · Observabilidad');
    expect(region.textContent).toContain('sin métricas conectadas');
    expect(llamadas()).toBe(0);
    expect(fixture.nativeElement.querySelectorAll('.obs-points li')).toHaveLength(4);
  });
  it('opción neutra no muestra identidad institucional ni ejecuta requests', async () => {
    const { fixture, llamadas } = await montar(admin);
    fixture.componentRef.setInput('modoIdentidad', 'neutra');
    fixture.detectChanges();
    const region: HTMLElement = fixture.nativeElement.querySelector('[aria-label="Identidad visual de observabilidad"]');
    expect(region.classList.contains('obs-branding--neutral')).toBe(true);
    expect(region.textContent).toContain('Identidad visual neutra');
    expect(region.textContent).not.toContain('SOLQARYN · Observabilidad');
    expect(llamadas()).toBe(0);
  });
  it('valor dinámico no permitido falla a neutro sin inyectar texto arbitrario', async () => {
    const { fixture } = await montar(admin);
    fixture.componentRef.setInput('modoIdentidad', '<script>no permitido</script>');
    fixture.detectChanges();
    const region: HTMLElement = fixture.nativeElement.querySelector('[aria-label="Identidad visual de observabilidad"]');
    expect(region.classList.contains('obs-branding--neutral')).toBe(true);
    expect(region.textContent).toContain('Identidad visual neutra');
    expect(region.textContent).not.toContain('<script>');
  });
  it('anónimo y ámbito Empresa mantienen oculta la región tipada', async () => {
    const anon = await montar(null);
    expect(anon.fixture.nativeElement.querySelector('.obs-branding')).toBeNull();
    const empresa = await montar({ ...admin, ambito: 'Empresa' } as unknown as PlataformaSesion);
    expect(empresa.fixture.nativeElement.querySelector('.obs-branding')).toBeNull();
  });
  it('revocación de plataforma oculta inmediatamente la identidad', async () => {
    const { fixture, sesion } = await montar(admin);
    expect(fixture.nativeElement.querySelector('.obs-branding')).not.toBeNull();
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.obs-branding')).toBeNull();
  });
});


describe('C11-04.M04.04 - navegación y acciones sin duplicados', () => {
  const admin: PlataformaSesion = { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true };
  async function montar(obtener: () => Observable<PlataformaSesion>) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(admin);
    await TestBed.configureTestingModule({
      imports: [ObservabilidadPrivadaV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion, obtener } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(ObservabilidadPrivadaV5Component);
    fixture.detectChanges();
    return { fixture, component: fixture.componentInstance };
  }
  it('navegación semántica, sin formulario operativo y estado accesible', async () => {
    const { fixture } = await montar(() => of(admin));
    const nav = fixture.nativeElement.querySelector('nav[aria-label="Acciones de observabilidad"]');
    const link: HTMLAnchorElement = nav.querySelector('a[aria-label="Regresar a plataforma"]');
    const button: HTMLButtonElement = nav.querySelector('button');
    expect(link.getAttribute('href')).toBe('/plataforma');
    expect(button.type).toBe('button');
    expect(button.getAttribute('aria-describedby')).toBe('obs-status');
    expect(fixture.nativeElement.querySelector('#obs-status[aria-live="polite"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('form, input, textarea')).toHaveLength(0);
  });
  it('dos revalidaciones solapadas ejecutan una sola solicitud y dan mensaje seguro', async () => {
    let calls = 0;
    const deferred = new Subject<PlataformaSesion>();
    const { fixture, component } = await montar(() => { calls++; return deferred.asObservable(); });
    component.revalidar();
    component.revalidar();
    fixture.detectChanges();
    expect(calls).toBe(1);
    expect(fixture.nativeElement.querySelector('button').disabled).toBe(true);
    expect(fixture.nativeElement.querySelector('#obs-status').getAttribute('aria-busy')).toBe('true');
    expect(fixture.nativeElement.querySelectorAll('.obs-points li')).toHaveLength(0);
    deferred.error(new HttpErrorResponse({ status: 503, error: { sensitive: 'PRIVATE_TELEMETRY_SECRET' } }));
    fixture.detectChanges();
    expect(component.estadoVisible()).toBe('error');
    expect(fixture.nativeElement.textContent).not.toContain('PRIVATE_TELEMETRY_SECRET');
    expect(fixture.nativeElement.querySelector('button').disabled).toBe(false);
    expect(fixture.nativeElement.querySelector('#obs-status').getAttribute('aria-busy')).toBe('false');
  });
});
