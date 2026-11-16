import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { vi } from 'vitest';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import { ContinuidadOperativaV5Component } from './continuidad-operativa-v5.component';

/** C10-04.M04.04: interacción segura y UI privada; no prueba JWT/HTTP real. */
describe('C10-04.M04.04 - formulario ausente, navegación y revalidación sin dobles acciones', () => {
  const autorizado = {
    nombreUsuario: 'Plataforma', rolNombre: 'Admin',
    ambito: 'Plataforma' as const, esAdministrador: true as const
  };

  async function montar(config: { throwSync?: boolean; sinSesion?: boolean } = {}) {
    TestBed.resetTestingModule();
    const sesion = signal<typeof autorizado | null>(config.sinSesion ? null : autorizado);
    const respuestas: Subject<typeof autorizado>[] = [];
    const obtener = vi.fn(() => {
      if (config.throwSync) throw new Error('TOKEN_PRIVADO_NUNCA_RENDERIZAR');
      const respuesta = new Subject<typeof autorizado>();
      respuestas.push(respuesta);
      return respuesta.asObservable();
    });
    await TestBed.configureTestingModule({
      imports: [ContinuidadOperativaV5Component],
      providers: [
        provideRouter([]),
        { provide: PlataformaSesionService, useValue: { sesion, obtener } }
      ]
    }).compileComponents();
    const fixture = TestBed.createComponent(ContinuidadOperativaV5Component);
    fixture.detectChanges();
    return { fixture, sesion, respuestas, obtener };
  }

  it('con sesión confirmada presenta solo navegación interna y revalidación; no formularios ni restauración', async () => {
    const { fixture } = await montar();
    const view = fixture.nativeElement as HTMLElement;
    expect(view.querySelectorAll('form, input, textarea, select')).toHaveLength(0);
    expect(view.querySelectorAll('button')).toHaveLength(1);
    expect(view.querySelector('a')?.getAttribute('href')).toBe('/plataforma');
    expect(view.querySelectorAll('.controles li')).toHaveLength(4);
    expect(view.textContent).toContain('Solo lectura');
    expect(view.textContent).not.toContain('TOKEN_PRIVADO_NUNCA_RENDERIZAR');
  });

  it('previene doble acción y oculta contenido durante revalidación pendiente', async () => {
    const { fixture, obtener, respuestas } = await montar();
    const view = fixture.nativeElement as HTMLElement;
    const boton = view.querySelector('button')!;
    boton.click();
    fixture.componentInstance.revalidar();
    fixture.detectChanges();
    expect(obtener).toHaveBeenCalledOnce();
    expect(respuestas).toHaveLength(1);
    expect(boton.disabled).toBe(true);
    expect(view.querySelector('.estado')?.getAttribute('aria-busy')).toBe('true');
    expect(view.querySelectorAll('.controles li')).toHaveLength(0);
    respuestas[0].next(autorizado);
    respuestas[0].complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('exito');
    expect(boton.disabled).toBe(false);
    expect(view.querySelectorAll('.controles li')).toHaveLength(4);
  });

  it('error 403 niega permisos, no muestra payload y permite una nueva revalidación', async () => {
    const { fixture, obtener, respuestas } = await montar();
    const view = fixture.nativeElement as HTMLElement;
    fixture.componentInstance.revalidar();
    respuestas[0].error(new HttpErrorResponse({
      status: 403, error: { message: 'DSN_PRIVADO' }
    }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
    expect(view.querySelectorAll('.controles li')).toHaveLength(0);
    expect(view.textContent).not.toContain('DSN_PRIVADO');
    expect((view.querySelector('button') as HTMLButtonElement).disabled).toBe(false);
    fixture.componentInstance.revalidar();
    expect(obtener).toHaveBeenCalledTimes(2);
  });

  it('error 503 nunca concede acceso ni revela datos del servidor', async () => {
    const { fixture, respuestas } = await montar();
    fixture.componentInstance.revalidar();
    respuestas[0].error(new HttpErrorResponse({
      status: 503, error: { message: 'SECRETO_TENANT_B' }
    }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('error');
    expect(fixture.nativeElement.querySelectorAll('.controles li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).not.toContain('SECRETO_TENANT_B');
  });

  it('revocación concurrente niega respuesta positiva anterior', async () => {
    const { fixture, respuestas, sesion } = await montar();
    fixture.componentInstance.revalidar();
    sesion.set(null);
    respuestas[0].next(autorizado);
    respuestas[0].complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.controles li')).toHaveLength(0);
  });

  it('provider que lanza síncronamente no deja botón bloqueado ni filtra error', async () => {
    const { fixture, obtener } = await montar({ throwSync: true });
    const view = fixture.nativeElement as HTMLElement;
    fixture.componentInstance.revalidar();
    fixture.detectChanges();
    expect(obtener).toHaveBeenCalledOnce();
    expect(fixture.componentInstance.estadoVisible()).toBe('error');
    expect((view.querySelector('button') as HTMLButtonElement).disabled).toBe(false);
    expect(view.querySelectorAll('.controles li')).toHaveLength(0);
    expect(view.textContent).not.toContain('TOKEN_PRIVADO_NUNCA_RENDERIZAR');
  });

  it('sin sesión, no presenta guía ni habilita autoridad por UI', async () => {
    const { fixture, obtener } = await montar({ sinSesion: true });
    expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.controles li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelectorAll('form, input')).toHaveLength(0);
    expect(obtener).not.toHaveBeenCalled();
  });
});
