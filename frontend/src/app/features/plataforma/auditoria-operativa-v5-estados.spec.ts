import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { AuditoriaOperativaV5Component } from './auditoria-operativa-v5.component';

describe('C15-05.M04.02 estados privados de auditoría operativa', () => {
  const admin: PlataformaSesion = { nombreUsuario: 'Admin sintético', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true };
  async function montar(permitir = true) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(permitir ? admin : null);
    const pendiente = new Subject<PlataformaSesion>();
    const obtener = vi.fn().mockImplementation(() => {
      sesion.set(null);
      return pendiente.asObservable();
    });
    await TestBed.configureTestingModule({
      imports: [AuditoriaOperativaV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion, obtener } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(AuditoriaOperativaV5Component);
    fixture.detectChanges();
    return { fixture, sesion, pendiente, obtener, root: fixture.nativeElement as HTMLElement };
  }
  it('inicia vacío autorizado sin inventar datos institucionales', async () => {
    const { fixture, root } = await montar();
    expect(fixture.componentInstance.estado()).toBe('vacio');
    expect(root.querySelectorAll('.controls li')).toHaveLength(4);
    expect(root.querySelector('.audit-state')?.textContent).toContain('No hay datos institucionales conectados');
  });
  it('carga oculta contenido y evita peticiones simultáneas', async () => {
    const { fixture, obtener, root } = await montar();
    const boton = root.querySelector('button.refresh-button') as HTMLButtonElement;
    boton.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('cargando');
    expect(boton.disabled).toBe(true);
    expect(root.querySelector('.controls')).toBeNull();
    fixture.componentInstance.revalidar();
    expect(obtener).toHaveBeenCalledTimes(1);
  });
  it('éxito confirmado restaura guía sin conceder acciones', async () => {
    const { fixture, sesion, pendiente, root } = await montar();
    fixture.componentInstance.revalidar();
    sesion.set(admin);
    pendiente.next(admin);
    pendiente.complete();
    fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('exito');
    expect(root.querySelectorAll('.controls li')).toHaveLength(4);
    expect(root.querySelector('.audit-state')?.textContent).toContain('revalidada correctamente');
    expect(root.querySelectorAll('form, input')).toHaveLength(0);
  });
  it.each([401, 403])('HTTP %i deniega sin exponer controles', async status => {
    const { fixture, pendiente, root } = await montar();
    fixture.componentInstance.revalidar();
    pendiente.error(new HttpErrorResponse({ status }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('sin_permiso');
    expect(root.querySelector('.controls')).toBeNull();
    expect(root.querySelector('.audit-state [role=alert]')?.textContent).toContain('Sin permiso');
  });
  it('HTTP 500 o error de red preserva acceso restringido', async () => {
    const { fixture, pendiente, root } = await montar();
    fixture.componentInstance.revalidar();
    pendiente.error(new HttpErrorResponse({ status: 500 }));
    fixture.detectChanges();
    expect(fixture.componentInstance.estado()).toBe('error');
    expect(root.querySelector('.controls')).toBeNull();
    expect(root.querySelector('.audit-state [role=alert]')?.textContent).toContain('No fue posible');
  });
  it('sin sesión inicial nunca muestra controles privados', async () => {
    const { fixture, root } = await montar(false);
    expect(fixture.componentInstance.estado()).toBe('sin_permiso');
    expect(root.querySelector('.controls')).toBeNull();
  });
});
