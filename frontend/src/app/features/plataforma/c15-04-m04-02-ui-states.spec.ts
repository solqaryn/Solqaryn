import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { type PlataformaSesion, PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import { GobiernoCambiosV5Component } from './gobierno-cambios-v5.component';

// C15-04.M04.02: Estado de UI Angular real; servicio simulado, no JWT HTTP ni tenant real.
const globalSession: PlataformaSesion = {
  nombreUsuario: 'Simulado', rolNombre: 'Global', ambito: 'Plataforma', esAdministrador: true
};

describe('C15-04.M04.02 estados de la vista privada gobierno V5', () => {
  async function montar(autorizada: boolean) {
    TestBed.resetTestingModule();
    const replies: Subject<unknown>[] = [];
    let solicitudes = 0;
    const service = {
      sesion: signal<PlataformaSesion | null>(autorizada ? globalSession : null),
      obtener: () => {
        solicitudes++;
        const r = new Subject<unknown>();
        replies.push(r);
        return r.asObservable();
      }
    };
    await TestBed.configureTestingModule({
      imports: [GobiernoCambiosV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: service }]
    }).compileComponents();
    const fixture = TestBed.createComponent(GobiernoCambiosV5Component);
    fixture.detectChanges();
    return { fixture, replies, calls: () => solicitudes };
  }

  it('sin sesión el estado es denegado, sin pasos ni contenido protegido', async () => {
    const { fixture, calls } = await montar(false);
    expect(fixture.componentInstance.estado()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent).toContain('Sin permiso');
    expect(fixture.nativeElement.querySelectorAll('.steps li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelectorAll('form,input,textarea')).toHaveLength(0);
    expect(calls()).toBe(0);
  });

  it('con sesión verificada se mantiene vacío sin inventar datos institucionales', async () => {
    const { fixture, calls } = await montar(true);
    expect(fixture.componentInstance.estado()).toBe('vacio');
    expect(fixture.nativeElement.querySelector('.state-banner')?.textContent).toContain('Sin datos institucionales');
    expect(fixture.nativeElement.querySelectorAll('.steps li')).toHaveLength(4);
    expect(fixture.nativeElement.querySelectorAll('form,input,textarea')).toHaveLength(0);
    expect(calls()).toBe(0);
  });

  it('cargando bloquea acción duplicada y éxito reabre solo contenido informativo', async () => {
    const { fixture, replies, calls } = await montar(true);
    const component = fixture.componentInstance;
    component.revalidar();
    fixture.detectChanges();
    expect(component.estado()).toBe('cargando');
    expect(fixture.nativeElement.querySelector('.state-banner [role="status"]')?.textContent).toContain('Verificando permisos');
    expect(fixture.nativeElement.querySelectorAll('.steps li')).toHaveLength(0);
    expect((fixture.nativeElement.querySelector('button.return-link') as HTMLButtonElement).disabled).toBe(true);
    component.revalidar();
    expect(calls()).toBe(1);
    replies[0].next(undefined);
    replies[0].complete();
    fixture.detectChanges();
    expect(component.estado()).toBe('exito');
    expect(fixture.nativeElement.querySelectorAll('.steps li')).toHaveLength(4);
    expect(fixture.nativeElement.textContent).toContain('solo lectura');
    expect(calls()).toBe(1);
  });

  it('403 deniega la vista y oculta diagnóstico privado de servidor', async () => {
    const { fixture, replies } = await montar(true);
    fixture.componentInstance.revalidar();
    replies[0].error(new HttpErrorResponse({
      status: 403, error: { message: 'SECRET_TENANT_B_PRIVATE' }
    }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('sin_permiso');
    expect(fixture.nativeElement.querySelectorAll('.steps li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent).toContain('Sin permiso');
    expect(fixture.nativeElement.textContent).not.toContain('SECRET_TENANT_B_PRIVATE');
  });

  it('503 da error opaco y reintento exitoso sin fuga ni autoaprobación', async () => {
    const { fixture, replies, calls } = await montar(true);
    fixture.componentInstance.revalidar();
    replies[0].error(new HttpErrorResponse({
      status: 503, error: { message: 'SECRET_DATABASE_DSN' }
    }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('error');
    expect(fixture.nativeElement.querySelectorAll('.steps li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent).toContain('No fue posible');
    expect(fixture.nativeElement.textContent).not.toContain('SECRET_DATABASE_DSN');
    fixture.componentInstance.revalidar();
    fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('cargando');
    replies[1].next(undefined);
    replies[1].complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('exito');
    expect(fixture.nativeElement.querySelectorAll('.steps li')).toHaveLength(4);
    expect(calls()).toBe(2);
    expect(fixture.nativeElement.textContent).not.toContain('SECRET_DATABASE_DSN');
  });
});
