import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { routes } from '../../app.routes';
import { COBERTURA_FUENTE_V5, CoberturaTrazabilidadV5Component, type CoberturaDimensionV5 } from './cobertura-trazabilidad-v5.component';

describe('C15-01.M04.01 cobertura privada de solo lectura', () => {
  const auth: PlataformaSesion = { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true };
  async function montar(value: PlataformaSesion | null, fuente?: () => Promise<readonly CoberturaDimensionV5[]>, esperar = true) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(value);
    await TestBed.configureTestingModule({
      imports: [CoberturaTrazabilidadV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion } }, ...(fuente ? [{ provide: COBERTURA_FUENTE_V5, useValue: fuente }] : [])]
    }).compileComponents();
    const fixture = TestBed.createComponent(CoberturaTrazabilidadV5Component);
    fixture.detectChanges();
    if (esperar) { await fixture.whenStable(); fixture.detectChanges(); }
    return { fixture, sesion };
  }

  it('ruta privada lazy requiere exactamente plataformaGuard', async () => {
    const matches = routes.filter(route => route.path === 'plataforma/trazabilidad-cobertura-v5');
    expect(matches).toHaveLength(1);
    expect(matches[0].canActivate).toEqual([plataformaGuard]);
    expect(await matches[0].loadComponent?.()).toBe(CoberturaTrazabilidadV5Component);
  });
  it('autorizado ve cuatro dimensiones sin inventar resultados ni acciones', async () => {
    const { fixture } = await montar(auth);
    expect(fixture.nativeElement.querySelectorAll('.coverage-list li')).toHaveLength(4);
    expect(fixture.nativeElement.querySelectorAll('.coverage-shell [role="status"]')).toHaveLength(1);
    expect(fixture.nativeElement.textContent).toContain('NO_MEDIDO');
    expect(fixture.nativeElement.querySelectorAll('.unverified')).toHaveLength(4);
    expect(fixture.nativeElement.querySelectorAll('button,form,input,textarea')).toHaveLength(0);
  });
  it('anónimo y ambito Empresa no reciben datos internos', async () => {
    for (const value of [null, { ...auth, ambito: 'Empresa' } as unknown as PlataformaSesion]) {
      const { fixture } = await montar(value);
      expect(fixture.nativeElement.querySelectorAll('.coverage-list li')).toHaveLength(0);
      expect(fixture.nativeElement.querySelector('[role="alert"]')?.textContent).toContain('ocultos');
    }
  });
  it('revocación elimina inmediatamente tabla de dimensiones', async () => {
    const { fixture, sesion } = await montar(auth);
    expect(fixture.nativeElement.querySelectorAll('.coverage-list li')).toHaveLength(4);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.coverage-list li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });

  it('C15-01.M04.02 muestra carga y después éxito con frontera de una dimensión', async () => {
    let finalizar!: (value: readonly CoberturaDimensionV5[]) => void;
    const pendiente = new Promise<readonly CoberturaDimensionV5[]>(resolver => { finalizar = resolver; });
    const { fixture } = await montar(auth, () => pendiente, false);
    expect(fixture.nativeElement.querySelector('[data-testid="c1501-loading"]')).not.toBeNull();
    finalizar([{ clave: 'una', titulo: 'Una dimensión', descripcion: 'Referencia sin certificación' }]);
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('[data-testid="c1501-loaded"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('.coverage-list li')).toHaveLength(1);
  });

  it('C15-01.M04.02 distingue vacío de éxito sin fingir resultados', async () => {
    const { fixture } = await montar(auth, async () => []);
    expect(fixture.nativeElement.querySelector('[data-testid="c1501-empty"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('.coverage-list li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).toContain('NO_MEDIDO');
  });

  it('C15-01.M04.02 falla cerrado, no filtra errores y permite reintentar', async () => {
    let intentos = 0;
    const { fixture } = await montar(auth, async () => {
      if (++intentos === 1) throw new Error('NO_EXPOSER_DETALLE_INTERNO');
      return [{ clave: 'recuperada', titulo: 'Recuperada', descripcion: 'Solo referencia local' }];
    });
    expect(fixture.nativeElement.querySelector('[data-testid="c1501-error"]')).not.toBeNull();
    expect(fixture.nativeElement.textContent).not.toContain('NO_EXPOSER_DETALLE_INTERNO');
    (fixture.nativeElement.querySelector('.retry') as HTMLButtonElement).click();
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.coverage-list li')).toHaveLength(1);
    expect(fixture.nativeElement.querySelector('[data-testid="c1501-error"]')).toBeNull();
  });

  it('C15-01.M04.02 rechaza respuesta local mal formada sin mostrar elementos', async () => {
    const { fixture } = await montar(auth, async () => [{ clave: '', titulo: '', descripcion: '' }]);
    expect(fixture.nativeElement.querySelector('[data-testid="c1501-error"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('.coverage-list li')).toHaveLength(0);
  });

  it('C15-01.M04.02 revoca lectura aún pendiente sin reexponer dimensiones', async () => {
    let finalizar!: (value: readonly CoberturaDimensionV5[]) => void;
    const pendiente = new Promise<readonly CoberturaDimensionV5[]>(resolver => { finalizar = resolver; });
    const { fixture, sesion } = await montar(auth, () => pendiente, false);
    sesion.set(null);
    fixture.detectChanges();
    finalizar([{ clave: 'privada', titulo: 'Privada', descripcion: 'No mostrar' }]);
    await fixture.whenStable();
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.coverage-list li')).toHaveLength(0);
    expect(fixture.nativeElement.textContent).not.toContain('Privada');
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
  });
});
