import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { Subject, tap } from 'rxjs';
import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { routes } from '../../app.routes';
import { PlacementReviewV5Component } from './placement-review-v5.component';

describe('C02-02.M04.01 - placement privado no operativo', () => {
  const verified: PlataformaSesion = {
    nombreUsuario: 'Plataforma', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true
  };
  async function render(value: PlataformaSesion | null) {
    TestBed.resetTestingModule();
    const sesion = signal(value);
    const respuesta = new Subject<PlataformaSesion>();
    const obtener = vi.fn(() => {
      sesion.set(null); // production service invalidates old grant before GET.
      return respuesta.asObservable().pipe(tap(grant => sesion.set(grant)));
    });
    await TestBed.configureTestingModule({
      imports: [PlacementReviewV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion, obtener } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(PlacementReviewV5Component);
    fixture.detectChanges();
    return { fixture, sesion, respuesta, obtener };
  }
  it('registra una sola ruta lazy protegida exclusivamente por plataformaGuard', async () => {
    const match = routes.filter(r => r.path === 'plataforma/placement-v5');
    expect(match).toHaveLength(1);
    expect(match[0].canActivate).toEqual([plataformaGuard]);
    expect(await match[0].loadComponent?.()).toBe(PlacementReviewV5Component);
  });
  it('sin sesión no muestra modalidades ni operación', async () => {
    const { fixture } = await render(null);
    expect(fixture.componentInstance.accesoVerificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).toContain('Placement privado oculto');
  });
  it('acceso plataforma muestra Shared/Dedicated solo lectura y NO_EJECUTADO', async () => {
    const { fixture } = await render(verified);
    expect(fixture.componentInstance.accesoVerificado()).toBe(true);
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(2);
    expect(fixture.nativeElement.textContent).toContain('NO_EJECUTADO');
    expect(fixture.nativeElement.querySelectorAll('form, input, textarea')).toHaveLength(0);
    expect(fixture.nativeElement.querySelectorAll('button')).toHaveLength(1);
    expect(fixture.nativeElement.querySelector('button')?.textContent).toContain('Revalidar sesión');
    expect(fixture.nativeElement.textContent).not.toContain('mysql://');
  });
  it('rol empresarial simulado no concede privilegios de plataforma', async () => {
    const fake = { ...verified, ambito: 'Empresa' } as unknown as PlataformaSesion;
    const { fixture } = await render(fake);
    expect(fixture.componentInstance.accesoVerificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(0);
  });
  it('revocación de sesión reactiva es fail-closed', async () => {
    const { fixture, sesion } = await render(verified);
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(2);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(0);
  });

  it('C02-02.M04.02 - carga accesible oculta modalidades y bloquea doble click', async () => {
    const { fixture, obtener } = await render(verified);
    fixture.componentInstance.revalidar();
    fixture.componentInstance.revalidar();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('cargando');
    expect(fixture.nativeElement.querySelector('.estado')?.getAttribute('aria-busy')).toBe('true');
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('button')?.disabled).toBe(true);
    expect(obtener).toHaveBeenCalledOnce();
  });

  it('C02-02.M04.02 - 403 niega contenido y no filtra mensaje de servidor', async () => {
    const { fixture, respuesta } = await render(verified);
    fixture.componentInstance.revalidar();
    respuesta.error(new HttpErrorResponse({ status: 403, error: { message: 'DSN_PRIVADO' } }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).not.toContain('DSN_PRIVADO');
  });

  it('C02-02.M04.02 - 503 muestra error sin revelar payload ni modos privados', async () => {
    const { fixture, respuesta } = await render(verified);
    fixture.componentInstance.revalidar();
    respuesta.error(new HttpErrorResponse({ status: 503, error: { message: 'TOKEN_SECRETO' } }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('error');
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).not.toContain('TOKEN_SECRETO');
  });

  it('C02-02.M04.02 - completar sin emitir grant no autoriza placement', async () => {
    const { fixture, respuesta } = await render(verified);
    fixture.componentInstance.revalidar();
    respuesta.complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('error');
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(0);
  });

  it('C02-02.M04.02 - grant actual restituye exclusivamente lectura sin mutaciones', async () => {
    const { fixture, respuesta } = await render(verified);
    fixture.componentInstance.revalidar();
    expect(fixture.componentInstance.accesoVerificado()).toBe(false);
    respuesta.next(verified);
    respuesta.complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('exito');
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(2);
    expect(fixture.nativeElement.querySelectorAll('form, input, textarea')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).toContain('sin migraciones');
  });

  it('C02-02.M04.02 - resultado empresa nunca concede plataforma', async () => {
    const { fixture, respuesta } = await render(verified);
    fixture.componentInstance.revalidar();
    const empresa = { ...verified, ambito: 'Empresa' } as unknown as PlataformaSesion;
    respuesta.next(empresa);
    respuesta.complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(0);
  });

  it('C02-02.M04.02 - revocación después de revalidar oculta modos inmediatamente', async () => {
    const { fixture, respuesta, sesion } = await render(verified);
    fixture.componentInstance.revalidar();
    respuesta.next(verified);
    respuesta.complete();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(2);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.modalidades article')).toHaveLength(0);
  });

});
