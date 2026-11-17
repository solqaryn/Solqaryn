import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { C0904M0402EstadosComponent } from './c09-04-m04-02-estados.component';

describe('C09-04.M04.02 estados multicanal sin integraciones de proveedor', () => {
  const admin: PlataformaSesion = {
    nombreUsuario: 'Sintetico', rolNombre: 'Administrador',
    ambito: 'Plataforma', esAdministrador: true
  };

  async function montar(inicial: PlataformaSesion | null) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(inicial);
    await TestBed.configureTestingModule({
      imports: [C0904M0402EstadosComponent],
      providers: [{ provide: PlataformaSesionService, useValue: { sesion } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(C0904M0402EstadosComponent);
    fixture.detectChanges();
    return { fixture, sesion, root: fixture.nativeElement as HTMLElement };
  }

  it('anonimo denegado, sin contenido ni acciones que puedan enviar mensajes', async () => {
    const { fixture, root } = await montar(null);
    expect(fixture.componentInstance.estadoVisible()).toBe('sin_permiso');
    expect(root.querySelector('[data-testid="m0402-denegado"]')).not.toBeNull();
    expect(root.querySelectorAll('button,form,input,textarea,a')).toHaveLength(0);
    expect(root.querySelectorAll('[data-testid="m0402-exito"]')).toHaveLength(0);
  });

  it('ambito Empresa y bandera no admin quedan denegados aunque venga rol global', async () => {
    const empresa = { ...admin, ambito: 'Empresa' } as unknown as PlataformaSesion;
    const sinAdmin = { ...admin, esAdministrador: false } as unknown as PlataformaSesion;
    for (const sesion of [empresa, sinAdmin]) {
      const { fixture, root } = await montar(sesion);
      fixture.componentRef.setInput('estado', 'exito');
      fixture.detectChanges();
      expect(root.querySelector('[data-testid="m0402-denegado"]')).not.toBeNull();
      expect(root.querySelector('[data-testid="m0402-exito"]')).toBeNull();
    }
  });

  it('administrador verificado ve vacio, sin inventar entregas', async () => {
    const { fixture, root } = await montar(admin);
    expect(fixture.componentInstance.estadoVisible()).toBe('vacio');
    expect(root.querySelector('[data-testid="m0402-vacio"]')?.textContent).toContain('Sin mensajes');
    expect(root.textContent).not.toContain('ENTREGA_VERIFICADA');
  });

  it('cargando activa aria-busy y cambia a exito declarativo, nunca proveedor real', async () => {
    const { fixture, root } = await montar(admin);
    fixture.componentRef.setInput('estado', 'cargando');
    fixture.detectChanges();
    expect(root.querySelector('section')?.getAttribute('aria-busy')).toBe('true');
    expect(root.querySelector('[data-testid="m0402-cargando"]')).not.toBeNull();
    fixture.componentRef.setInput('estado', 'exito');
    fixture.detectChanges();
    expect(root.querySelector('section')?.getAttribute('aria-busy')).toBe('false');
    expect(root.querySelector('[data-testid="m0402-exito"]')?.textContent).toContain('No acredita envios');
    expect(root.querySelector('[data-testid="m0402-cargando"]')).toBeNull();
  });

  it('error es opaco, sin interpolar mensajes del backend ni enlaces', async () => {
    const { fixture, root } = await montar(admin);
    fixture.componentRef.setInput('estado', 'error');
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="m0402-error"]')?.getAttribute('role')).toBe('alert');
    expect(root.textContent).not.toContain('SECRET_DATABASE_DSN');
    expect(root.querySelectorAll('a,button,form')).toHaveLength(0);
  });

  it('revocacion de sesion elimina exito, incluso despues de renderizarlo', async () => {
    const { fixture, sesion, root } = await montar(admin);
    fixture.componentRef.setInput('estado', 'exito');
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="m0402-exito"]')).not.toBeNull();
    sesion.set(null);
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="m0402-denegado"]')).not.toBeNull();
    expect(root.querySelector('[data-testid="m0402-exito"]')).toBeNull();
  });

  it('un estado inesperado falla cerrado sin HTML hostil ni exito', async () => {
    const { fixture, root } = await montar(admin);
    fixture.componentRef.setInput('estado', '<img src=x onerror=alert(1)>');
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="m0402-error"]')).not.toBeNull();
    expect(root.querySelector('img')).toBeNull();
    expect(root.textContent).not.toContain('onerror');
  });

  it('un servicio de sesion que falla no concede acceso al panel', async () => {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [C0904M0402EstadosComponent],
      providers: [{ provide: PlataformaSesionService, useValue: {
        sesion: () => { throw new Error('PRIVATE_TOKEN'); }
      } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(C0904M0402EstadosComponent);
    fixture.componentRef.setInput('estado', 'exito');
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="m0402-denegado"]')).not.toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('PRIVATE_TOKEN');
  });
});
