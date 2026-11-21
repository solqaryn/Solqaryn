import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { routes } from '../../app.routes';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { C0401PlanesReadonlyComponent } from './c04-01-m04-01-planes-readonly.component';

describe('C04-01.M04.01 - catálogo de planes privado no comercial', () => {
  const admin: PlataformaSesion = { nombreUsuario: 'QA', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true };
  async function montar(inicial: PlataformaSesion | null) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(inicial);
    await TestBed.configureTestingModule({
      imports: [C0401PlanesReadonlyComponent],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(C0401PlanesReadonlyComponent);
    fixture.detectChanges();
    return { fixture, sesion, root: fixture.nativeElement as HTMLElement };
  }

  it('la ruta única es lazy y exige plataformaGuard sin grants de empresa', async () => {
    const matching = routes.filter(r => r.path === 'plataforma/catalogo-planes-v22');
    expect(matching).toHaveLength(1);
    expect(matching[0].canActivate).toEqual([plataformaGuard]);
    expect(matching[0].data).toBeUndefined();
    expect(await matching[0].loadComponent?.()).toBe(C0401PlanesReadonlyComponent);
  });

  it('muestra sólo tres contratos no operativos con autoridad global confirmada', async () => {
    const { root } = await montar(admin);
    expect(root.querySelectorAll('[data-testid^="c0401-section-"]')).toHaveLength(3);
    expect(root.querySelector('[data-testid="c0401-unapproved"]')?.textContent).toContain('NO_CERTIFICADO');
    expect(root.textContent).toContain('sin tarifa aprobada');
    expect(root.querySelectorAll('button,form,input,textarea,select,iframe')).toHaveLength(0);
    expect(root.querySelector('a')?.getAttribute('href')).toBe('/plataforma');
  });

  it('anónimo, ámbito Empresa y administrador falso no ven contenido privado', async () => {
    for (const sesion of [null, { ...admin, ambito: 'Empresa' }, { ...admin, esAdministrador: false }]) {
      const { root } = await montar(sesion as PlataformaSesion | null);
      expect(root.querySelector('[data-testid="c0401-private"]')).toBeNull();
      expect(root.querySelector('[data-testid="c0401-denied"]')).not.toBeNull();
    }
  });

  it('revocación posterior oculta la referencia inmediatamente, incluso sin navegación', async () => {
    const { root, fixture, sesion } = await montar(admin);
    expect(root.querySelectorAll('[data-testid^="c0401-section-"]')).toHaveLength(3);
    sesion.set(null);
    fixture.detectChanges();
    expect(root.querySelector('[data-testid="c0401-private"]')).toBeNull();
    sesion.set({ ...admin, esAdministrador: false } as PlataformaSesion);
    fixture.detectChanges();
    expect(root.querySelectorAll('[data-testid^="c0401-section-"]')).toHaveLength(0);
  });

  it('no interpreta HTML hostil ni presenta precios fabricados', async () => {
    const { root } = await montar({ ...admin, nombreUsuario: '<img src=x onerror=alert(1)>' });
    expect(root.querySelector('img,script,iframe')).toBeNull();
    expect(root.innerHTML).not.toContain('onerror');
    expect(root.querySelectorAll('[data-precio], [data-cobro], button')).toHaveLength(0);
  });
});
