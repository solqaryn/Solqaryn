import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { C0704BrandingV5Component, normalizarC0704Presentacion } from './c07-04-m04-03-branding.component';

describe('C07-04.M04.03 presentacion dinamica privada y fail-closed', () => {
  async function montar() {
    TestBed.resetTestingModule();
    const tenant = signal(true), permiso = signal(true), ruta = signal(true);
    await TestBed.configureTestingModule({
      imports: [C0704BrandingV5Component],
      providers: [
        { provide: TenantContextService, useValue: { tieneContextoVerificado: tenant } },
        { provide: PermisosRuntimeService, useValue: {
          puede: (modulo: string, accion: string) => modulo === 'Ventas' && accion === 'Ver' && permiso(),
          rutaHabilitada: (path: string) => path === 'pedidos-venta' && ruta()
        }}
      ]
    }).compileComponents();
    const fixture = TestBed.createComponent(C0704BrandingV5Component);
    fixture.detectChanges();
    return { fixture, tenant, permiso, ruta };
  }

  it('permite solo marcas, densidades y contrastes exactos', () => {
    expect(normalizarC0704Presentacion({ marca:'SOLQARYN ERP', densidad:'compacta', contraste:'alto' }))
      .toEqual({ marca:'SOLQARYN ERP', densidad:'compacta', contraste:'alto' });
    expect(normalizarC0704Presentacion(null))
      .toEqual({ marca:'SOLQARYN', densidad:'comoda', contraste:'normal' });
  });
  it('rechaza CSS, HTML, URLs, arrays y cadenas desconocidas por default seguro', () => {
    const unsafe = { marca:'<script>alert(1)</script>', densidad:'url(https://evil.example)',
      contraste:'var(--inyeccion)', css:'position:fixed', href:'javascript:alert(1)' };
    expect(normalizarC0704Presentacion(unsafe))
      .toEqual({ marca:'SOLQARYN', densidad:'comoda', contraste:'normal' });
    expect(normalizarC0704Presentacion(['compacta'])).toEqual(normalizarC0704Presentacion(null));
  });
  it('expone solo controles cosméticos con tenant, permiso y ruta válidos', async () => {
    const { fixture } = await montar();
    const el: HTMLElement = fixture.nativeElement;
    expect(el.querySelectorAll('.pedidos-branding-v5')).toHaveLength(1);
    expect(el.querySelectorAll('button[type="button"]')).toHaveLength(2);
    expect(el.querySelectorAll('form, input, a, textarea')).toHaveLength(0);
    expect(el.textContent).toContain('Sin cambios de importes');
  });
  it('alterna densidad y contraste reactivos, sin valores arbitrarios', async () => {
    const { fixture } = await montar();
    fixture.componentInstance.alternarDensidad();
    fixture.componentInstance.alternarContraste();
    fixture.detectChanges();
    const section: HTMLElement = fixture.nativeElement.querySelector('.pedidos-branding-v5');
    expect(section.getAttribute('data-densidad')).toBe('compacta');
    expect(section.getAttribute('data-contraste')).toBe('alto');
    fixture.componentInstance.alternarDensidad();
    fixture.detectChanges();
    expect(section.getAttribute('data-densidad')).toBe('comoda');
  });
  it('deniega cuando falta contexto de empresa o permiso Ventas:Ver', async () => {
    const { fixture, tenant, permiso } = await montar();
    tenant.set(false); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.pedidos-branding-v5')).toBeNull();
    tenant.set(true); permiso.set(false); fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.pedidos-branding-v5')).toBeNull();
  });
  it('revocación y ruta retirada desactivan controles y mutaciones locales', async () => {
    const { fixture, permiso, ruta } = await montar();
    permiso.set(false); fixture.detectChanges();
    fixture.componentInstance.alternarContraste();
    expect(fixture.componentInstance.presentacion().contraste).toBe('normal');
    permiso.set(true); ruta.set(false); fixture.detectChanges();
    fixture.componentInstance.alternarDensidad();
    expect(fixture.componentInstance.presentacion().densidad).toBe('comoda');
    expect(fixture.nativeElement.querySelectorAll('button')).toHaveLength(0);
  });
});
