import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { DesignSystemV5Component } from './design-system-v5.component';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';

describe('C05-01.M04.01 - biblioteca visual privada V5', () => {
  const admin = { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true };
  async function montar(sesionInicial: unknown) {
    TestBed.resetTestingModule();
    const sesion = signal(sesionInicial);
    await TestBed.configureTestingModule({
      imports: [DesignSystemV5Component],
      providers: [{ provide: PlataformaSesionService, useValue: { sesion } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(DesignSystemV5Component);
    fixture.detectChanges();
    return { fixture, sesion };
  }

  it('oculta los cuatro tokens si la sesión no está confirmada', async () => {
    const { fixture } = await montar(null);
    expect(fixture.nativeElement.querySelectorAll('.token-item')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('[role=alert]')?.textContent).toContain('permanece oculto');
  });

  it('deniega explícitamente la sesión de ámbito Empresa', async () => {
    const { fixture } = await montar({ ...admin, ambito: 'Empresa' });
    expect(fixture.nativeElement.querySelectorAll('.token-item')).toHaveLength(0);
    expect(fixture.nativeElement.querySelectorAll('form, input, button')).toHaveLength(0);
  });

  it('presenta cuatro tokens CSS permitidos sin acciones ni formularios', async () => {
    const { fixture } = await montar(admin);
    expect(fixture.nativeElement.querySelectorAll('.token-item')).toHaveLength(4);
    expect(fixture.nativeElement.querySelectorAll('form, input, button')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('h2')?.textContent).toContain('Tokens y componentes');
    expect(fixture.nativeElement.textContent).toContain('--color-primary');
    expect(fixture.nativeElement.textContent).toContain('--color-danger');
    expect(fixture.nativeElement.textContent).toContain('No hay formularios');
  });

  it('oculta inmediatamente la superficie al revocar la sesión', async () => {
    const { fixture, sesion } = await montar(admin);
    expect(fixture.nativeElement.querySelectorAll('.token-item')).toHaveLength(4);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.token-item')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('[role=alert]')).not.toBeNull();
  });

  it('C05-01.M04.02 presenta cinco estados estaticos y accesibles solo al administrador de plataforma', async () => {
    const { fixture } = await montar(admin);
    const estados = [...fixture.nativeElement.querySelectorAll('.state-example')] as HTMLElement[];
    expect(estados).toHaveLength(5);
    expect(estados.map(estado => estado.querySelector('h4')?.textContent?.trim())).toEqual([
      'Cargando', 'Sin resultados', 'Error recuperable', 'Operacion completada', 'Permiso insuficiente'
    ]);
    expect(fixture.nativeElement.querySelector('.state-catalog[aria-labelledby="design-states-title"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('.state-catalog form, .state-catalog input, .state-catalog button')).toHaveLength(0);
  });

  it('C05-01.M04.02 nunca muestra estados privados a empresa y los retira al revocar', async () => {
    const empresa = await montar({ ...admin, ambito: 'Empresa' });
    expect(empresa.fixture.nativeElement.querySelectorAll('.state-example')).toHaveLength(0);
    const { fixture, sesion } = await montar(admin);
    expect(fixture.nativeElement.querySelectorAll('.state-example')).toHaveLength(5);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.state-example')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('[role=alert]')?.textContent).toContain('permanece oculto');
  });
});
