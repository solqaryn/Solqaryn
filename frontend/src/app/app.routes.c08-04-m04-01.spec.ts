import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { routes } from './app.routes';
import { SectorModulesReadonlyComponent } from './features/extensiones-sectoriales/sector-modules-readonly.component';

describe('C08-04.M04.01 - superficie sectorial no autorizante', () => {
  const ruta = routes.find(item => item.path === 'extensiones-sectoriales');

  it('exige sesión y tenant verificado con guard existente, sin ruta duplicada', () => {
    expect(ruta).toBeDefined();
    expect(ruta?.canActivate).toEqual([authGuard]);
    expect(ruta?.children).toBeUndefined();
    expect(routes.filter(item => item.path === 'extensiones-sectoriales')).toHaveLength(1);
  });

  it('no declara permisos ni activaciones en metadatos y lazy-load correcto', async () => {
    expect(ruta?.data).toBeUndefined();
    expect(ruta?.redirectTo).toBeUndefined();
    expect(await (ruta?.loadComponent as () => Promise<unknown>)()).toBe(SectorModulesReadonlyComponent);
  });

  it('cuatro ámbitos y sin controles operativos ni contenido ejecutable', async () => {
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({ imports: [SectorModulesReadonlyComponent], providers: [provideRouter([])] }).compileComponents();
    const fixture = TestBed.createComponent(SectorModulesReadonlyComponent);
    fixture.detectChanges();
    const root: HTMLElement = fixture.nativeElement;
    expect(root.querySelectorAll('.sector-card')).toHaveLength(4);
    expect(root.querySelector('[role="status"]')?.textContent).toContain('Sin módulos conectados');
    expect(root.querySelectorAll('button, form, input, textarea, select')).toHaveLength(0);
    expect(root.textContent).toContain('no otorga JWT, RBAC');
    expect(root.querySelectorAll('script, iframe, [onclick]')).toHaveLength(0);
  });
});
