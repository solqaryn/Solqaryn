import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SectorModulesReadonlyComponent, normalizarMarcaVisualSectorial } from './sector-modules-readonly.component';

describe('C08-04.M04.03 - branding sectorial local allowlist', () => {
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

  it('mantiene marca institucional por defecto y declara que no autoriza módulos', async () => {
    const fixture = await montar();
    const root: HTMLElement = fixture.nativeElement;
    expect(fixture.componentInstance.marcaVisual()).toBe('institucional');
    expect(root.querySelector('.sector-readonly--contraste')).toBeNull();
    expect(root.querySelector('[data-testid="c0804-brand-preview"]')?.textContent).toContain('Institucional');
    expect(root.textContent).toContain('No activa módulos ni modifica permisos');
    expect(root.querySelectorAll('button, form, input')).toHaveLength(0);
  });

  it('permite exclusivamente el preset contraste y conserva previews informativos', async () => {
    const fixture = await montar();
    fixture.componentInstance.aplicarMarcaVisualPropuesta('contraste');
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(fixture.componentInstance.marcaVisual()).toBe('contraste');
    expect(root.querySelector('.sector-readonly--contraste')).not.toBeNull();
    expect(root.querySelector('[data-testid="c0804-brand-preview"]')?.textContent).toContain('Contraste alto');
    expect(root.querySelectorAll('.sector-card')).toHaveLength(4);
    expect(root.querySelectorAll('button, form, input, [aria-pressed]')).toHaveLength(0);
  });

  it.each([null, undefined, '', 'CONTRASTE', '<script>', 'contraste;url(https://bad.invalid)', 42])(
    'entrada no autorizada %s se normaliza a institucional sin CSS arbitrario', async candidate => {
      expect(normalizarMarcaVisualSectorial(candidate)).toBe('institucional');
      const fixture = await montar();
      fixture.componentInstance.aplicarMarcaVisualPropuesta('contraste');
      fixture.componentInstance.aplicarMarcaVisualPropuesta(candidate);
      fixture.detectChanges();
      expect(fixture.componentInstance.marcaVisual()).toBe('institucional');
      expect((fixture.nativeElement as HTMLElement).querySelector('.sector-readonly--contraste')).toBeNull();
    }
  );

  it('no evalúa propiedades ni getters maliciosos al normalizar configuración externa', () => {
    const malformed = Object.defineProperty({}, 'toString', {
      get() { throw new Error('PRIVATE_DSN'); }
    });
    expect(() => normalizarMarcaVisualSectorial(malformed)).not.toThrow();
    expect(normalizarMarcaVisualSectorial(malformed)).toBe('institucional');
  });

  it('cambiar apariencia no revela capacidades durante denegación ni durante un error', async () => {
    const fixture = await montar();
    fixture.componentInstance.aplicarMarcaVisualPropuesta('contraste');
    for (const estado of ['sin_permiso', 'error'] as const) {
      fixture.componentInstance.estadoPresentacion.set(estado);
      fixture.detectChanges();
      const root: HTMLElement = fixture.nativeElement;
      expect(root.querySelectorAll('.sector-card')).toHaveLength(0);
      expect(root.querySelectorAll('button, form, input')).toHaveLength(0);
      expect(root.textContent).not.toContain('PRIVATE_DSN');
    }
  });
});
