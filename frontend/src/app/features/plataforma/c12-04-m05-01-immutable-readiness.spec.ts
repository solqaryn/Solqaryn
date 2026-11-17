import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { vi } from 'vitest';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { C1204CutoverReadonlyComponent } from './c12-04-m04-01-cutover-readonly.component';

// C12-04.M05.01: la UI no puede convertir evidencia contractual en restauración real.
describe('C12-04.M05.01 — invariantes inmutables de preparación MySQL (sólo UI)', () => {
  const admin: PlataformaSesion = {
    nombreUsuario: 'Global',
    rolNombre: 'Administrador',
    ambito: 'Plataforma',
    esAdministrador: true
  };

  async function montar(inicial: PlataformaSesion | null = admin) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(inicial);
    const obtener = vi.fn();
    const limpiar = vi.fn(() => sesion.set(null));
    await TestBed.configureTestingModule({
      imports: [C1204CutoverReadonlyComponent],
      providers: [provideRouter([]), {
        provide: PlataformaSesionService,
        useValue: { sesion, obtener, limpiar }
      }]
    }).compileComponents();
    const fixture = TestBed.createComponent(C1204CutoverReadonlyComponent);
    fixture.detectChanges();
    return { fixture, sesion, obtener, root: fixture.nativeElement as HTMLElement };
  }

  it('congela la colección y cada objeto; rechaza falsificar LISTO mediante Object.assign', async () => {
    const { fixture, root } = await montar();
    const controles = fixture.componentInstance.controles;
    expect(Object.isFrozen(controles)).toBe(true);
    expect(controles).toHaveLength(4);
    for (const control of controles) {
      expect(Object.isFrozen(control)).toBe(true);
      expect(() => Object.assign(control, { evidencia: 'LISTO' })).toThrow();
      expect(control.evidencia).not.toBe('LISTO');
    }
    fixture.detectChanges();
    expect(root.textContent).not.toContain('Estado de evidencia: LISTO');
  });

  it('mantiene las cuatro evidencias explícitas sin inventar permisos ni resultados', async () => {
    const { fixture, root } = await montar();
    const controles = fixture.componentInstance.controles;
    expect(controles.map(c => c.id)).toEqual([
      'exportacion', 'restauracion', 'comparacion', 'autorizacion'
    ]);
    expect(controles.map(c => c.evidencia)).toEqual([
      'SIN_VERIFICAR', 'SIN_VERIFICAR', 'NO_MEDIDO', 'NO_AUTORIZADO'
    ]);
    expect(root.querySelectorAll('.controles li')).toHaveLength(4);
    expect(root.textContent).toContain('NO OPERATIVO');
  });

  it('un cambio visual local no modifica evidencia ni dispara proveedor', async () => {
    const { fixture, obtener, root } = await montar();
    const antes = fixture.componentInstance.controles.map(c => c.evidencia);
    expect(fixture.componentInstance.seleccionarTema('alto-contraste')).toBe(true);
    fixture.detectChanges();
    expect(root.querySelector('.cutover')?.classList.contains('contraste-alto')).toBe(true);
    expect(fixture.componentInstance.controles.map(c => c.evidencia)).toEqual(antes);
    expect(obtener).not.toHaveBeenCalled();
  });

  it('revocación oculta todos los controles inmutables sin convertir estados', async () => {
    const { fixture, sesion, root } = await montar();
    sesion.set(null);
    fixture.detectChanges();
    expect(root.querySelector('[data-testid=c1204-cutover-private]')).toBeNull();
    expect(root.querySelectorAll('.controles li')).toHaveLength(0);
    expect(fixture.componentInstance.controles.at(-1)?.evidencia).toBe('NO_AUTORIZADO');
    expect(fixture.componentInstance.seleccionarTema('alto-contraste')).toBe(false);
  });

  it('no admite insertar un quinto control ni redefinir los identificadores', async () => {
    const { fixture } = await montar();
    const controles = fixture.componentInstance.controles;
    const ids = controles.map(c => c.id);
    expect(() => Object.assign(controles[0], { id: 'restaurado' })).toThrow();
    expect(() => Object.assign(controles, { 4: { id: 'aprobado', evidencia: 'LISTO' } })).toThrow();
    expect(controles.map(c => c.id)).toEqual(ids);
    expect(controles).toHaveLength(4);
  });
});
