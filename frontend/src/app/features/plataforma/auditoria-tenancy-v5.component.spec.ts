import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { Subject, tap } from 'rxjs';
import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { routes } from '../../app.routes';
import { AUDITORIA_TENANCY_PRESENTACION_V5, AuditoriaTenancyV5Component, normalizarPresentacionAuditoriaTenancyV5 } from './auditoria-tenancy-v5.component';

describe('C15-03.M04.01 - guía privada de seguridad y tenancy', () => {
  const autorizado: PlataformaSesion = {
    nombreUsuario: 'Plataforma', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true
  };
  async function montar(valor: PlataformaSesion | null, configuracionInicial: unknown = { marca: 'solqaryn', densidad: 'normal' }) {
    TestBed.resetTestingModule();
    const sesion = signal(valor);
    const presentacion = signal<unknown>(configuracionInicial);
    const respuesta = new Subject<PlataformaSesion>();
    const obtener = vi.fn(() => {
      sesion.set(null); // el servidor invalida el grant anterior antes de GET.
      return respuesta.asObservable().pipe(tap(grant => sesion.set(grant)));
    });
    await TestBed.configureTestingModule({
      imports: [AuditoriaTenancyV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion, obtener } },
        { provide: AUDITORIA_TENANCY_PRESENTACION_V5, useValue: presentacion }]
    }).compileComponents();
    const fixture = TestBed.createComponent(AuditoriaTenancyV5Component);
    fixture.detectChanges();
    return { fixture, sesion, respuesta, obtener, presentacion };
  }
  it('ruta singular diferida y protegida exclusivamente por plataformaGuard', async () => {
    const rutas = routes.filter(r => r.path === 'plataforma/auditoria-tenancy-v5');
    expect(rutas).toHaveLength(1);
    expect(rutas[0].canActivate).toEqual([plataformaGuard]);
    expect(await rutas[0].loadComponent?.()).toBe(AuditoriaTenancyV5Component);
  });
  it('sin sesión no renderiza guía, controles ni valores de autorización', async () => {
    const { fixture } = await montar(null);
    expect(fixture.componentInstance.accesoVerificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('.notice')).toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Guía protegida oculta');
  });
  it('grant de plataforma muestra solo cuatro frentes estáticos no operativos', async () => {
    const { fixture } = await montar(autorizado);
    expect(fixture.componentInstance.accesoVerificado()).toBe(true);
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(4);
    expect(fixture.nativeElement.querySelectorAll('form, input, textarea')).toHaveLength(0);
    expect(fixture.nativeElement.querySelectorAll('button')).toHaveLength(1);
    expect(fixture.nativeElement.textContent).toContain('NO_MEDIDO');
  });
  it('grant inválido por ambito empresarial se deniega', async () => {
    const falso = { ...autorizado, ambito: 'Empresa' } as unknown as PlataformaSesion;
    const { fixture } = await montar(falso);
    expect(fixture.componentInstance.accesoVerificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(0);
  });
  it('revocación reactiva oculta contenido antes expuesto', async () => {
    const { fixture, sesion } = await montar(autorizado);
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(4);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.componentInstance.accesoVerificado()).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(0);
  });

  it('C15-03.M04.02 - fallo sincrono del proveedor no bloquea reintento ni expone datos', async () => {
    const { fixture, respuesta, obtener } = await montar(autorizado);
    const secreto = 'PRIVATE_SYNCHRONOUS_PROVIDER_SECRET';
    obtener.mockImplementationOnce(() => { throw new Error(secreto); });
    expect(() => fixture.componentInstance.revalidar()).not.toThrow();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('error');
    expect(fixture.nativeElement.querySelector('button')?.disabled).toBe(false);
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).not.toContain(secreto);
    fixture.componentInstance.revalidar();
    expect(obtener).toHaveBeenCalledTimes(2);
    respuesta.next(autorizado);
    respuesta.complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('exito');
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(4);
  });

  it('C15-03.M04.02 - carga impide doble llamada, oculta cuatro fronteras y expone aria-busy', async () => {
    const { fixture, obtener } = await montar(autorizado);
    fixture.componentInstance.revalidar();
    fixture.componentInstance.revalidar();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('cargando');
    expect(fixture.nativeElement.querySelector('.estado')?.getAttribute('aria-busy')).toBe('true');
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('button')?.disabled).toBe(true);
    expect(obtener).toHaveBeenCalledOnce();
  });

  it('C15-03.M04.02 - 403 deniega sin revelar secretos del backend', async () => {
    const { fixture, respuesta } = await montar(autorizado);
    fixture.componentInstance.revalidar();
    respuesta.error(new HttpErrorResponse({ status: 403, error: { message: 'TOKEN_PRIVADO' } }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).not.toContain('TOKEN_PRIVADO');
  });

  it('C15-03.M04.02 - 503 muestra error seguro sin datos internos', async () => {
    const { fixture, respuesta } = await montar(autorizado);
    fixture.componentInstance.revalidar();
    respuesta.error(new HttpErrorResponse({ status: 503, error: { message: 'AUDIT_LOG_PRIVADO' } }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('error');
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).not.toContain('AUDIT_LOG_PRIVADO');
  });

  it('C15-03.M04.02 - complete vacío nunca autoriza revisión', async () => {
    const { fixture, respuesta } = await montar(autorizado);
    fixture.componentInstance.revalidar();
    respuesta.complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('error');
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(0);
  });

  it('C15-03.M04.02 - backend confirma plataforma pero solo guía no operativa', async () => {
    const { fixture, respuesta } = await montar(autorizado);
    fixture.componentInstance.revalidar();
    respuesta.next(autorizado);
    respuesta.complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('exito');
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(4);
    expect(fixture.nativeElement.querySelectorAll('form, input, textarea')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).toContain('NO_MEDIDO');
  });

  it('C15-03.M04.02 - respuesta empresarial no concede privilegio global', async () => {
    const { fixture, respuesta } = await montar(autorizado);
    fixture.componentInstance.revalidar();
    respuesta.next({ ...autorizado, ambito: 'Empresa' } as unknown as PlataformaSesion);
    respuesta.complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(0);
  });

  it('C15-03.M04.02 - revocación posterior al éxito oculta toda la guía', async () => {
    const { fixture, respuesta, sesion } = await montar(autorizado);
    fixture.componentInstance.revalidar();
    respuesta.next(autorizado);
    respuesta.complete();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(4);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(0);
  });

  it('C15-03.M04.03 - branding y densidad tipados reaccionan a Signal sin mutar permisos', async () => {
    const { fixture, presentacion } = await montar(autorizado, { marca: 'institucional', densidad: 'compacta' });
    const seccion = fixture.nativeElement.querySelector('section.tenancy-audit');
    expect(seccion, 'La guia tenancy mantiene region etiquetada').not.toBeNull();
    expect(seccion.getAttribute('data-densidad')).toBe('compacta');
    expect(seccion.querySelector('.eyebrow').textContent).toContain('Plataforma SOLQARYN');
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(4);
    presentacion.set({ marca: 'solqaryn', densidad: 'normal' });
    fixture.detectChanges();
    expect(seccion.getAttribute('data-densidad')).toBe('normal');
    expect(seccion.querySelector('.eyebrow').textContent).toContain('SOLQARYN · Auditoría');
  });

  it('C15-03.M04.03 - rechaza HTML, CSS arbitrario y propiedades que intenten elevar autoridad', async () => {
    const ataque = '<img src=x onerror=alert(1)>';
    const { fixture } = await montar(autorizado, { marca: ataque, densidad: 'compacta' });
    const seccion = fixture.nativeElement.querySelector('section.tenancy-audit');
    expect(seccion, 'La guia tenancy mantiene region etiquetada').not.toBeNull();
    expect(seccion.getAttribute('data-densidad')).toBe('normal');
    expect(seccion.querySelector('.eyebrow').textContent).toContain('SOLQARYN');
    expect(fixture.nativeElement.innerHTML).not.toContain(ataque);
    expect(normalizarPresentacionAuditoriaTenancyV5({ marca: 'institucional', densidad: 'compacta', rol: 'Admin' }))
      .toEqual({ marca: 'solqaryn', densidad: 'normal' });
  });

  it('C15-03.M04.03 - límites nulo, array, prototipo, enumeraciones y cadena extraña fail-closed', () => {
    for (const valor of [null, [], 'institucional', {}, { marca: 'institucional', densidad: 'gigante' },
      { marca: 'tenant', densidad: 'normal' }, Object.create({ marca: 'institucional', densidad: 'compacta' })]) {
      expect(normalizarPresentacionAuditoriaTenancyV5(valor)).toEqual({ marca: 'solqaryn', densidad: 'normal' });
    }
  });

  it('C15-03.M04.03 - marca institucional nunca abre contenido denegado ni sobrevive a revocación', async () => {
    const { fixture, sesion, presentacion } = await montar(null, { marca: 'institucional', densidad: 'compacta' });
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(0);
    presentacion.set({ marca: 'solqaryn', densidad: 'normal' });
    sesion.set(autorizado);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(0);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.fronteras li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelectorAll('form, input, textarea')).toHaveLength(0);
  });

});
