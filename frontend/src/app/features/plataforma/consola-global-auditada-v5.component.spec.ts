import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject, of, throwError, type Observable, tap } from 'rxjs';
import { routes } from '../../app.routes';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { ConsolaGlobalAuditadaV5Component } from './consola-global-auditada-v5.component';

describe('C02-04.M04.01 - consola global protegida sin privilegios nuevos', () => {
  const admin: PlataformaSesion = {
    nombreUsuario: 'Admin global', rolNombre: 'Administrador', ambito: 'Plataforma', esAdministrador: true
  };
  async function montar(inicial: PlataformaSesion | null, falloSincrono = false, fallo503UnaVez = false) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(inicial);
    const pendiente = new Subject<PlataformaSesion>();
    let llamadas = 0;
    const servicio = {
      sesion,
      obtener: (): Observable<PlataformaSesion> => {
        llamadas++;
        sesion.set(null);
        if (falloSincrono) throw new Error('SECRET_INTERNAL_DSN');
        if (fallo503UnaVez && llamadas === 1)
          return throwError(() => new HttpErrorResponse({ status: 503, statusText: 'PRIVATE_DSN' }));
        if (fallo503UnaVez && llamadas === 2)
          return of(admin).pipe(tap(resultado => sesion.set(resultado)));
        return pendiente.asObservable().pipe(tap(resultado => sesion.set(resultado)));
      }
    };
    await TestBed.configureTestingModule({
      imports: [ConsolaGlobalAuditadaV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: servicio }]
    }).compileComponents();
    const fixture = TestBed.createComponent(ConsolaGlobalAuditadaV5Component);
    fixture.detectChanges();
    return { fixture, sesion, pendiente, llamadas: () => llamadas };
  }

  it('declara una ruta lazy única y protegida por el guard real de plataforma', async () => {
    const matches = routes.filter(r => r.path === 'plataforma/consola-global-v5');
    expect(matches).toHaveLength(1);
    expect(matches[0].canActivate).toEqual([plataformaGuard]);
    expect(await matches[0].loadComponent?.()).toBe(ConsolaGlobalAuditadaV5Component);
  });

  it('sin sesión no muestra los cuatro controles privados ni acciones', async () => {
    const { fixture } = await montar(null);
    expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.control-cards li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelectorAll('button')).toHaveLength(0);
  });

  it('rol ERP aun con flag administrador no concede plataforma', async () => {
    const falsa = { ...admin, ambito: 'Empresa' } as unknown as PlataformaSesion;
    const { fixture } = await montar(falsa);
    expect(fixture.nativeElement.querySelectorAll('.control-cards li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).toContain('consola privada permanece oculta');
  });

  it('la sesión global expone solo cuatro frentes informativos', async () => {
    const { fixture, llamadas } = await montar(admin);
    expect(fixture.nativeElement.querySelectorAll('.control-cards li')).toHaveLength(4);
    expect(fixture.nativeElement.querySelectorAll('form, input, textarea')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).toContain('NO_MEDIDO');
    expect(fixture.nativeElement.textContent).not.toContain(admin.nombreUsuario);
    expect(llamadas()).toBe(0);
  });

  it('revocación reactiva oculta la pantalla sin concesiones residuales', async () => {
    const { fixture, sesion } = await montar(admin);
    expect(fixture.nativeElement.querySelectorAll('.control-cards li')).toHaveLength(4);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.control-cards li')).toHaveLength(0);
  });

  it('revalidación en vuelo es única y oculta todo durante carga', async () => {
    const { fixture, pendiente, llamadas } = await montar(admin);
    fixture.componentInstance.revalidarSesion();
    fixture.componentInstance.revalidarSesion();
    fixture.detectChanges();
    expect(llamadas()).toBe(1);
    expect(fixture.componentInstance.estadoVisible()).toBe('cargando');
    expect(fixture.nativeElement.querySelectorAll('.control-cards li')).toHaveLength(0);
    pendiente.next(admin);
    pendiente.complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('exito');
    expect(fixture.nativeElement.querySelectorAll('.control-cards li')).toHaveLength(4);
  });

  it('403 y 401 posteriores mantienen la guía oculta', async () => {
    for (const status of [401, 403]) {
      const { fixture, pendiente } = await montar(admin);
      fixture.componentInstance.revalidarSesion();
      pendiente.error(new HttpErrorResponse({ status, error: { secret: 'PRIVATE_TOKEN' } }));
      fixture.detectChanges();
      expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
      expect(fixture.nativeElement.querySelectorAll('.control-cards li')).toHaveLength(0);
      expect(fixture.nativeElement.textContent).not.toContain('PRIVATE_TOKEN');
    }
  });

  it('503 sanitiza error y no muestra diagnóstico del backend', async () => {
    const { fixture, pendiente } = await montar(admin);
    fixture.componentInstance.revalidarSesion();
    pendiente.error(new HttpErrorResponse({ status: 503, statusText: 'PRIVATE_DSN' }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('error');
    expect(fixture.nativeElement.querySelectorAll('.control-cards li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).not.toContain('PRIVATE_DSN');
  });

  it('observable vacío no es éxito ni mantiene estado cargando', async () => {
    const { fixture, pendiente } = await montar(admin);
    fixture.componentInstance.revalidarSesion();
    pendiente.complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('error');
    expect(fixture.nativeElement.querySelectorAll('.control-cards li')).toHaveLength(0);
  });

  it('excepción síncrona se captura sin exponer secretos', async () => {
    const { fixture } = await montar(admin, true);
    expect(() => fixture.componentInstance.revalidarSesion()).not.toThrow();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('error');
    expect(fixture.nativeElement.textContent).not.toContain('SECRET_INTERNAL_DSN');
  });
  it('C02-04.M04.02 reintenta 503 con GET validado por servidor, sin restaurar cache local', async () => {
    const { fixture, llamadas } = await montar(admin, false, true);
    fixture.componentInstance.revalidarSesion();
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('error');
    expect(fixture.nativeElement.querySelectorAll('.control-cards li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('button')?.textContent).toContain('Reintentar verificación');
    expect(fixture.nativeElement.textContent).not.toContain('PRIVATE_DSN');
    fixture.componentInstance.revalidarSesion();
    fixture.detectChanges();
    expect(llamadas()).toBe(2);
    expect(fixture.componentInstance.estadoVisible()).toBe('exito');
    expect(fixture.nativeElement.querySelectorAll('.control-cards li')).toHaveLength(4);
  });

  it('C02-04.M04.02 deniega reintento de 403 y no envia una nueva consulta', async () => {
    const { fixture, pendiente, llamadas } = await montar(admin);
    fixture.componentInstance.revalidarSesion();
    pendiente.error(new HttpErrorResponse({ status: 403 }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('button')).toHaveLength(0);
    fixture.componentInstance.revalidarSesion();
    expect(llamadas()).toBe(1);
    expect(fixture.nativeElement.querySelectorAll('.control-cards li')).toHaveLength(0);
  });
});
