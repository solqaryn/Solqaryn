import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { SectorModulesReadonlyComponent } from './sector-modules-readonly.component';

describe('C08-04.M05.01 contrato unitario de capacidades ilustrativas', () => {
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

  it('mantiene exactamente cuatro keys unicas y finalidades de alcance limitado', async () => {
    const fixture = await montar();
    const capabilities = fixture.componentInstance.capabilities();
    const keys = capabilities.map(capability => capability.key);
    expect(keys).toEqual(['reservas', 'membresias', 'catalogo', 'sitios']);
    expect(new Set(keys).size).toBe(keys.length);
    expect(capabilities).toHaveLength(4);
    for (const capability of capabilities) {
      expect(capability.title.trim().length).toBeGreaterThan(3);
      expect(capability.purpose.trim().length).toBeGreaterThan(20);
    }
    expect(capabilities[0].purpose).toContain('servidor');
    expect(capabilities[1].purpose).toContain('empresa');
    expect(capabilities[2].purpose).toContain('propiedad');
    expect(capabilities[3].purpose).toContain('permisos');
  });

  it('vacio y exito solo muestran previews No conectado sin operacion', async () => {
    const fixture = await montar();
    const root: HTMLElement = fixture.nativeElement;
    for (const state of ['vacio', 'exito'] as const) {
      fixture.componentInstance.estadoPresentacion.set(state);
      fixture.detectChanges();
      expect(root.querySelectorAll('.sector-card')).toHaveLength(4);
      const tags = Array.from(root.querySelectorAll('.sector-card .sector-tag'));
      expect(tags).toHaveLength(4);
      expect(tags.every(tag => tag.textContent?.trim() === 'No conectado')).toBe(true);
      expect(root.querySelectorAll('form,input,select,button,[aria-pressed]')).toHaveLength(0);
    }
    expect(root.textContent).toContain('no otorga JWT, RBAC');
  });

  it('cargando, error y sin_permiso revocan previews sin arrastrar contenido', async () => {
    const fixture = await montar();
    const root: HTMLElement = fixture.nativeElement;
    fixture.componentInstance.estadoPresentacion.set('exito');
    fixture.detectChanges();
    expect(root.querySelectorAll('.sector-card')).toHaveLength(4);
    for (const state of ['cargando', 'error', 'sin_permiso'] as const) {
      fixture.componentInstance.estadoPresentacion.set(state);
      fixture.detectChanges();
      expect(root.querySelectorAll('.sector-card,.sector-grid')).toHaveLength(0);
      expect(root.querySelectorAll('button,form,input')).toHaveLength(0);
      expect(root.textContent).not.toContain('SECRET_PROVIDER_DSN');
    }
  });

  it('escapa titulos hostiles como texto sin insertar HTML ejecutable', async () => {
    const fixture = await montar();
    const root: HTMLElement = fixture.nativeElement;
    const hostile = '<img src=x onerror=alert(1)>';
    const original = fixture.componentInstance.capabilities();
    fixture.componentInstance.capabilities.set(
      original.map((capability, index) => index === 0
        ? { ...capability, title: hostile, purpose: '<script>SECRET_PROVIDER_DSN</script>' }
        : capability)
    );
    fixture.detectChanges();
    const heading = root.querySelector('.sector-card h3');
    expect(heading?.textContent).toBe(hostile);
    expect(root.querySelectorAll('img,script,iframe')).toHaveLength(0);
    fixture.componentInstance.estadoPresentacion.set('sin_permiso');
    fixture.detectChanges();
    expect(root.querySelectorAll('.sector-card')).toHaveLength(0);
  });
});
