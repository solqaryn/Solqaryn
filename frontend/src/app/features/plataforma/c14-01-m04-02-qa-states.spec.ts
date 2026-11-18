import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { C1401QaGlobalComponent, C1401_QA_CATALOG_LOADER, type C1401QaControl } from './c14-01-m04-01-qa-global.component';

const admin: PlataformaSesion = { nombreUsuario: 'Prueba', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true };
const controls: readonly C1401QaControl[] = [{ id:'scope', titulo:'Gobierno', alcance:'Solo inventario sin CI' }];

async function montar(loader: () => Promise<readonly C1401QaControl[]>, grant: PlataformaSesion | null = admin) {
  const sesion = signal<PlataformaSesion | null>(grant);
  TestBed.resetTestingModule();
  await TestBed.configureTestingModule({
    imports: [C1401QaGlobalComponent],
    providers: [provideRouter([]),
      { provide: PlataformaSesionService, useValue: { sesion: sesion.asReadonly() } },
      { provide: C1401_QA_CATALOG_LOADER, useValue: loader }]
  }).compileComponents();
  const fixture = TestBed.createComponent(C1401QaGlobalComponent);
  fixture.detectChanges();
  return { fixture, sesion, root: fixture.nativeElement as HTMLElement };
}

describe('C14-01.M04.02 - estados seguros de inventario QA', () => {
  it('deniega sin grant y no ejecuta el loader privado', async () => {
    let calls = 0;
    const { root } = await montar(async () => { calls++; return controls; }, null);
    expect(root.querySelector('[data-testid="qa-global-denegado"]')).not.toBeNull();
    expect(root.querySelector('[data-testid="c1401-qa-global"]')).toBeNull();
    expect(calls).toBe(0);
  });

  it('muestra carga mientras la importacion esta pendiente sin inventar PASS', async () => {
    const { root } = await montar(() => new Promise<readonly C1401QaControl[]>(() => {}));
    const section = root.querySelector('[data-testid="c1401-qa-global"]');
    expect(section?.getAttribute('aria-busy')).toBe('true');
    expect(root.querySelector('[data-testid="c1401-qa-loading"]')).not.toBeNull();
    expect(root.querySelector('.pendiente')).toBeNull();
  });

  it('muestra vacio sin declarar tests aprobados', async () => {
    const { fixture } = await montar(async () => []);
    await fixture.whenStable();
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="c1401-qa-empty"]')).not.toBeNull();
    expect(root.querySelectorAll('.pendiente')).toHaveLength(0);
  });

  it('maneja rechazo/error opaco sin filtrar secretos de proveedor', async () => {
    const { fixture } = await montar(async () => { throw Error('SECRET_TENANT_B_PRIVATE_DSN'); });
    await fixture.whenStable();
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="c1401-qa-error"]')?.getAttribute('role')).toBe('alert');
    expect(root.textContent).not.toContain('SECRET_TENANT_B_PRIVATE_DSN');
  });

  it('carga catalogo pero conserva NO_VERIFICADO, sin aprobacion automatica', async () => {
    const { fixture } = await montar(async () => controls);
    await fixture.whenStable();
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="c1401-qa-ready"]')).not.toBeNull();
    expect(root.querySelectorAll('.pendiente')).toHaveLength(1);
    expect(root.textContent).toContain('NO_VERIFICADO');
    expect(root.querySelector('button,form,input,textarea,select')).toBeNull();
  });

  it('rechaza IDs duplicados antes de exhibir inventario ambiguo', async () => {
    const { fixture } = await montar(async () => [controls[0], controls[0]]);
    await fixture.whenStable();
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="c1401-qa-error"]')).not.toBeNull();
    expect(root.querySelector('.qa-controles')).toBeNull();
  });

  it('descarta respuesta tardia al revocarse el grant global', async () => {
    let resolve!: (items: readonly C1401QaControl[]) => void;
    const pending = new Promise<readonly C1401QaControl[]>(r => { resolve = r; });
    const { fixture, sesion } = await montar(() => pending);
    sesion.set(null);
    fixture.detectChanges();
    resolve(controls);
    await fixture.whenStable();
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('[data-testid="qa-global-denegado"]')).not.toBeNull();
    expect(root.querySelector('[data-testid="c1401-qa-global"]')).toBeNull();
  });
});
