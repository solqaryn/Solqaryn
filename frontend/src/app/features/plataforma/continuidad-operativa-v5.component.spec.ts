import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { vi } from 'vitest';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { routes } from '../../app.routes';
import { ContinuidadOperativaV5Component } from './continuidad-operativa-v5.component';

describe('C10-04.M04.01 - continuidad privada V5', () => {
  const autorizado = { nombreUsuario: 'Plataforma', rolNombre: 'Admin',
    ambito: 'Plataforma' as const, esAdministrador: true as const };

  async function montar(conSesion: boolean) {
    TestBed.resetTestingModule();
    const respuesta = new Subject<typeof autorizado>();
    const obtener = vi.fn(() => respuesta.asObservable());
    const sesion = signal(conSesion ? autorizado : null);
    await TestBed.configureTestingModule({
      imports: [ContinuidadOperativaV5Component],
      providers: [ provideRouter([]),
        { provide: PlataformaSesionService, useValue: {
          sesion, obtener } } ]
    }).compileComponents();
    const fixture = TestBed.createComponent(ContinuidadOperativaV5Component);
    fixture.detectChanges();
    return { fixture, respuesta, obtener, sesion };
  }
  it('mantiene exactamente una ruta protegida y diferida', async () => {
    const rutas = routes.filter(r => r.path === 'plataforma/continuidad-operativa-v5');
    expect(rutas).toHaveLength(1);
    expect(rutas[0].canActivate).toEqual([plataformaGuard]);
    expect(rutas[0].redirectTo).toBeUndefined();
    expect(await rutas[0].loadComponent?.()).toBe(ContinuidadOperativaV5Component);
  });
  it('sin sesion verificada no muestra controles protegidos', async () => {
    const { fixture } = await montar(false);
    expect(fixture.componentInstance.estado()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.controles li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('.aviso')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Acceso de plataforma no confirmado');
  });
  it('sesion valida muestra solo guia, nunca acciones de restauracion', async () => {
    const { fixture, obtener } = await montar(true);
    expect(fixture.componentInstance.estado()).toBe('vacio');
    expect(fixture.nativeElement.querySelectorAll('.controles li')).toHaveLength(4);
    expect(fixture.nativeElement.querySelectorAll('button')).toHaveLength(1);
    expect(fixture.nativeElement.querySelectorAll('form, input')).toHaveLength(0);
    expect(obtener).not.toHaveBeenCalled();
  });
  it('revalidacion single-flight espera respuesta autentica de sesion', async () => {
    const { fixture, respuesta, obtener } = await montar(true);
    fixture.componentInstance.revalidar();
    fixture.componentInstance.revalidar();
    fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('cargando');
    expect(fixture.nativeElement.querySelectorAll('.controles li')).toHaveLength(0);
    expect(obtener).toHaveBeenCalledOnce();
    respuesta.next(autorizado);
    respuesta.complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('exito');
    expect(fixture.nativeElement.querySelectorAll('.controles li')).toHaveLength(4);
  });
  it('403 oculta contenido y payload', async () => {
    const { fixture, respuesta } = await montar(true);
    fixture.componentInstance.revalidar();
    respuesta.error(new HttpErrorResponse({ status: 403, error: { message: 'TOKEN_PRIVADO' } }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.controles li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).not.toContain('TOKEN_PRIVADO');
  });
  it('503 no concede acceso ni filtra datos del proveedor', async () => {
    const { fixture, respuesta } = await montar(true);
    fixture.componentInstance.revalidar();
    respuesta.error(new HttpErrorResponse({ status: 503, error: { message: 'DSN_PRIVADO' } }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('error');
    expect(fixture.nativeElement.querySelectorAll('.controles li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).not.toContain('DSN_PRIVADO');
  });

  it('C10-04.M04.02 - revocacion de sesion externa oculta guia y permisos inmediatamente', async () => {
    const { fixture, sesion } = await montar(true);
    expect(fixture.nativeElement.querySelectorAll('.controles li')).toHaveLength(4);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.controles li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('.aviso')).toBeNull();
  });

  it('C10-04.M04.02 - carga accesible niega controles, deshabilita reintento y no duplica', async () => {
    const { fixture, obtener } = await montar(true);
    fixture.componentInstance.revalidar();
    fixture.componentInstance.revalidar();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('cargando');
    expect(fixture.nativeElement.querySelector('.estado')?.getAttribute('aria-busy')).toBe('true');
    expect(fixture.nativeElement.querySelectorAll('.controles li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('button')?.disabled).toBe(true);
    expect(obtener).toHaveBeenCalledOnce();
  });

  it('C10-04.M04.02 - respuesta sin grant de plataforma no se representa como exito', async () => {
    const { fixture, respuesta } = await montar(true);
    fixture.componentInstance.revalidar();
    respuesta.next({ ...autorizado, esAdministrador: false } as unknown as typeof autorizado);
    respuesta.complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.controles li')).toHaveLength(0);
  });

  it('C10-04.M04.02 - completa sin respuesta como error, nunca autoriza contenido', async () => {
    const { fixture, respuesta } = await montar(true);
    fixture.componentInstance.revalidar();
    respuesta.complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('error');
    expect(fixture.nativeElement.querySelectorAll('.controles li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('.aviso')).toBeNull();
  });

  it('C10-04.M04.02 - 401 revoca vista y no revela payload del servidor', async () => {
    const { fixture, respuesta } = await montar(true);
    fixture.componentInstance.revalidar();
    respuesta.error(new HttpErrorResponse({ status: 401, error: { message: 'SECRETO_RECHAZADO' } }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.controles li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).not.toContain('SECRETO_RECHAZADO');
  });

  it('C10-04.M04.02 - exito real restaura solo guia documental, no operaciones', async () => {
    const { fixture, respuesta } = await montar(true);
    fixture.componentInstance.revalidar();
    respuesta.next(autorizado);
    respuesta.complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('exito');
    expect(fixture.nativeElement.querySelectorAll('.controles li')).toHaveLength(4);
    expect(fixture.nativeElement.querySelectorAll('form, input')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).toContain('Solo consulta documental');
  });

});
