import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { routes } from '../../app.routes';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { C1204CutoverReadonlyComponent } from './c12-04-m04-01-cutover-readonly.component';

describe('C12-04.M04.01 superficie privada de preparación MySQL', () => {
  const admin: PlataformaSesion = {
    nombreUsuario: 'Plataforma', rolNombre: 'Administrador',
    ambito: 'Plataforma', esAdministrador: true
  };

  async function montar(inicial: PlataformaSesion | null = admin) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(inicial);
    const obtener = vi.fn();
    await TestBed.configureTestingModule({
      imports: [C1204CutoverReadonlyComponent],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion, obtener } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(C1204CutoverReadonlyComponent);
    fixture.detectChanges();
    return { fixture, sesion, obtener, root: fixture.nativeElement as HTMLElement };
  }

  it('la ruta privada es única, lazy y exige plataformaGuard', async () => {
    const matching = routes.filter(r => r.path === 'plataforma/mysql-cutover-readonly-v5');
    expect(matching).toHaveLength(1);
    expect(matching[0].canActivate).toEqual([plataformaGuard]);
    expect(await matching[0].loadComponent?.()).toBe(C1204CutoverReadonlyComponent);
  });

  it('sin grant de plataforma no expone controles privados', async () => {
    const { root } = await montar(null);
    expect(root.querySelector('[data-testid=c1204-cutover-private]')).toBeNull();
    expect(root.querySelector('[role=alert]')?.textContent).toContain('no confirmado');
    expect(root.querySelectorAll('form,input,button')).toHaveLength(0);
  });

  it('muestra solo cuatro precondiciones sin afirmar evidencia de restauración', async () => {
    const { root, obtener } = await montar();
    expect(root.querySelectorAll('.controles li')).toHaveLength(4);
    expect(root.textContent).toContain('NO OPERATIVO');
    expect(root.textContent).toContain('NO_MEDIDO');
    expect(root.textContent).toContain('NO_AUTORIZADO');
    expect(root.querySelectorAll('form,input,button')).toHaveLength(0);
    expect(obtener).not.toHaveBeenCalled();
  });

  it('revocación dinámica oculta toda información privada', async () => {
    const { fixture, sesion, root } = await montar();
    sesion.set(null);
    fixture.detectChanges();
    expect(root.querySelector('[data-testid=c1204-cutover-private]')).toBeNull();
    expect(root.querySelector('[role=alert]')).not.toBeNull();
  });

  it('un tenant empresarial no equivale a un grant global', async () => {
    const { root } = await montar({ ...admin, ambito:'Empresa', esAdministrador:true } as unknown as PlataformaSesion);
    expect(root.querySelectorAll('.controles li')).toHaveLength(0);
  });

  it('un usuario sin administrador se deniega aun cuando el ámbito declare plataforma', async () => {
    const { root } = await montar({ ...admin, esAdministrador:false } as unknown as PlataformaSesion);
    expect(root.querySelector('[data-testid=c1204-cutover-private]')).toBeNull();
  });

  it('nombre de sesión adversarial no se interpola en el DOM ni crea acciones', async () => {
    const { root } = await montar({ ...admin, nombreUsuario:'<img src=x onerror=alert(1)>' });
    expect(root.querySelector('img')).toBeNull();
    expect(root.innerHTML).not.toContain('onerror');
    expect(root.querySelectorAll('button')).toHaveLength(0);
  });
});
