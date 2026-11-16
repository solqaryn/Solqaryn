import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SectorModulesReadonlyComponent, type EstadoPresentacionSectorial } from './sector-modules-readonly.component';

describe('C08-04.M04.02 - cinco estados visuales sectoriales sin autoridad', () => {
  async function montar() {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [SectorModulesReadonlyComponent],
      providers: [provideRouter([])]
    }).compileComponents();
    const fixture = TestBed.createComponent(SectorModulesReadonlyComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('por defecto comunica la ausencia real de módulos y conserva cuatro previews sin acciones', async () => {
    const fixture = await montar();
    const root: HTMLElement = fixture.nativeElement;
    expect(fixture.componentInstance.estadoPresentacion()).toBe('vacio');
    expect(root.querySelector('[aria-label="Estado de las capacidades"]')?.textContent).toContain('Sin módulos conectados');
    expect(root.querySelectorAll('.sector-card')).toHaveLength(4);
    expect(root.querySelectorAll('button, form, input, select, textarea')).toHaveLength(0);
    expect(root.textContent).toContain('No conectado');
    expect(root.textContent).toContain('no otorga JWT, RBAC');
  });

  it.each([
    ['cargando', 'Preparando la vista informativa', 'true'],
    ['error', 'No se pudo presentar la información', 'false'],
    ['sin_permiso', 'Permiso no confirmado', 'false']
  ] as const)('%s oculta todos los previews y no filtra información', async (estado, mensaje, busy) => {
    const fixture = await montar();
    fixture.componentInstance.estadoPresentacion.set(estado);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    const region = root.querySelector('[aria-label="Estado de las capacidades"]');
    expect(region?.textContent).toContain(mensaje);
    expect(region?.getAttribute('aria-busy')).toBe(busy);
    expect(root.querySelectorAll('.sector-card')).toHaveLength(0);
    expect(root.querySelectorAll('form, button, input')).toHaveLength(0);
    expect(root.textContent).not.toContain('PRIVATE_DSN');
  });

  it('éxito solo muestra catálogo ilustrativo, sin autorizar ni activar módulos', async () => {
    const fixture = await montar();
    fixture.componentInstance.estadoPresentacion.set('exito');
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[role="status"]')?.textContent).toContain('no módulos habilitados');
    expect(root.querySelectorAll('.sector-card')).toHaveLength(4);
    expect(root.querySelectorAll('button, form, input, [aria-pressed]')).toHaveLength(0);
    expect(root.textContent).toContain('No conectado');
  });

  it('la secuencia éxito -> sin permiso -> vacío revoca lo visible sin arrastrar privilegios', async () => {
    const fixture = await montar();
    const root: HTMLElement = fixture.nativeElement;
    for (const [estado, cantidad] of [
      ['exito', 4], ['sin_permiso', 0], ['vacio', 4]
    ] as const satisfies readonly (readonly [EstadoPresentacionSectorial, number])[]) {
      fixture.componentInstance.estadoPresentacion.set(estado);
      fixture.detectChanges();
      expect(root.querySelectorAll('.sector-card')).toHaveLength(cantidad);
    }
    expect(root.textContent).toContain('Sin módulos conectados');
  });
});
