import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { routes } from '../../app.routes';
import { COBERTURA_FUENTE_V5, COBERTURA_PRESENTACION_V5, CoberturaTrazabilidadV5Component, normalizarCoberturaPresentacionV5, type CoberturaDimensionV5 } from './cobertura-trazabilidad-v5.component';

describe('C15-01.M04.01 cobertura privada de solo lectura', () => {
  const auth: PlataformaSesion = { nombreUsuario: 'Admin', rolNombre: 'Admin', ambito: 'Plataforma', esAdministrador: true };
  async function montar(value: PlataformaSesion | null, fuente?: () => Promise<readonly CoberturaDimensionV5[]>, esperar = true, apariencia?: unknown) {
    TestBed.resetTestingModule();
    const sesion = signal<PlataformaSesion | null>(value);
    await TestBed.configureTestingModule({
      imports: [CoberturaTrazabilidadV5Component],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion } }, ...(fuente ? [{ provide: COBERTURA_FUENTE_V5, useValue: fuente }] : []),
        ...(apariencia !== undefined ? [{ provide: COBERTURA_PRESENTACION_V5, useValue: apariencia }] : [])]
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

  it('C15-01.M04.03 permite solo marca/densidad de lista blanca y valores inmutables', () => {
    expect(normalizarCoberturaPresentacionV5(null)).toEqual({ marca: 'institucional', densidad: 'regular' });
    const válida = normalizarCoberturaPresentacionV5({ marca: 'neutra', densidad: 'compacta' });
    expect(válida).toEqual({ marca: 'neutra', densidad: 'compacta' });
    expect(Object.isFrozen(válida)).toBe(true);
    expect(normalizarCoberturaPresentacionV5({ marca: 'javascript:alert(1)', densidad: 'compacta' }))
      .toEqual({ marca: 'neutra', densidad: 'regular' });
    expect(normalizarCoberturaPresentacionV5(['institucional', 'compacta']))
      .toEqual({ marca: 'neutra', densidad: 'regular' });
  });

  it('C15-01.M04.03 representa variante neutra compacta sin resultados ni acciones de escritura', async () => {
    const { fixture } = await montar(auth, undefined, true, { marca: 'neutra', densidad: 'compacta' });
    const shell = fixture.nativeElement.querySelector('.coverage-shell') as HTMLElement;
    expect(shell.classList.contains('coverage-neutral')).toBe(true);
    expect(shell.classList.contains('coverage-compacta')).toBe(true);
    expect(shell.getAttribute('data-presentacion-marca')).toBe('neutra');
    expect(shell.textContent).toContain('Referencia · Cobertura');
    expect(shell.textContent).toContain('NO_MEDIDO');
    expect(fixture.nativeElement.querySelectorAll('.coverage-list li')).toHaveLength(4);
    expect(fixture.nativeElement.querySelectorAll('form,input,button,textarea')).toHaveLength(0);
  });

  it('C15-01.M04.03 entrada hostil no crea clases, HTML ni marca arbitraria', async () => {
    const payload = '<svg onload=alert(1)>';
    const { fixture } = await montar(auth, undefined, true, { marca: payload, densidad: 'compacta', clase: 'admin' });
    const shell = fixture.nativeElement.querySelector('.coverage-shell') as HTMLElement;
    expect(shell.getAttribute('data-presentacion-marca')).toBe('neutra');
    expect(shell.getAttribute('data-presentacion-densidad')).toBe('regular');
    expect(shell.classList.contains('admin')).toBe(false);
    expect(shell.textContent).not.toContain(payload);
    expect(shell.querySelector('svg')).toBeNull();
    expect(shell.textContent).toContain('NO_MEDIDO');
  });

  it('C15-01.M04.03 apariencia visual no sustituye revocación o ámbito', async () => {
    const { fixture, sesion } = await montar(auth, undefined, true, { marca: 'institucional', densidad: 'compacta' });
    expect(fixture.nativeElement.querySelectorAll('.coverage-list li')).toHaveLength(4);
    sesion.set(null);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelectorAll('.coverage-list li')).toHaveLength(0);
    expect(fixture.nativeElement.querySelector('[role="alert"]')).not.toBeNull();
    expect(fixture.nativeElement.querySelectorAll('form,input')).toHaveLength(0);
  });

  it('C15-01.M04.03 captura getters variables una sola vez antes de aplicar la lista blanca', () => {
    let lecturasMarca = 0;
    let lecturasDensidad = 0;
    const hostil = {
      get marca() { return ++lecturasMarca === 1 ? 'institucional' : '<svg onload=alert(1)>'; },
      get densidad() { return ++lecturasDensidad === 1 ? 'compacta' : 'cambiar-permisos'; }
    };
    const normalizada = normalizarCoberturaPresentacionV5(hostil);
    expect(normalizada).toEqual({ marca: 'institucional', densidad: 'compacta' });
    expect(lecturasMarca).toBe(1);
    expect(lecturasDensidad).toBe(1);
    expect(Object.isFrozen(normalizada)).toBe(true);
  });

  it('C15-01.M04.03 getter que lanza y Proxy revocado deniegan por defecto', () => {
    const getterHostil = {
      get marca(): string { throw new Error('DIAGNOSTICO_INTERNO_SENSIBLE'); },
      densidad: 'compacta'
    };
    expect(normalizarCoberturaPresentacionV5(getterHostil))
      .toEqual({ marca: 'neutra', densidad: 'regular' });
    const revocable = Proxy.revocable({ marca: 'institucional', densidad: 'compacta' }, {});
    revocable.revoke();
    expect(normalizarCoberturaPresentacionV5(revocable.proxy))
      .toEqual({ marca: 'neutra', densidad: 'regular' });
  });

  it('C15-01.M04.03 Angular monta con getter hostil sin filtrar datos ni romper autorizacion', async () => {
    const { fixture } = await montar(auth, undefined, true, {
      get marca(): string { throw new Error('NO_EXPOSER_DETALLE_INTERNO'); },
      densidad: 'compacta'
    });
    const shell = fixture.nativeElement.querySelector('.coverage-shell') as HTMLElement;
    expect(shell.getAttribute('data-presentacion-marca')).toBe('neutra');
    expect(shell.getAttribute('data-presentacion-densidad')).toBe('regular');
    expect(shell.textContent).toContain('NO_MEDIDO');
    expect(shell.textContent).not.toContain('NO_EXPOSER_DETALLE_INTERNO');
    expect(shell.querySelectorAll('svg,form,input')).toHaveLength(0);
    expect(shell.querySelectorAll('.coverage-list li')).toHaveLength(4);
  });
});
