import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { routes } from '../../app.routes';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { PlataformaSesionService, type PlataformaSesion } from '../../core/auth/plataforma-sesion.service';
import { C1401QaGlobalComponent } from './c14-01-m04-01-qa-global.component';

describe('C14-01.M04.01 - superficie privada de QA global', () => {
  const ruta = routes.find(x => x.path === 'plataforma/qa-global-v19');
  it('declara ruta lazy unica con guard de plataforma', async () => {
    expect(routes.filter(x => x.path === 'plataforma/qa-global-v19')).toHaveLength(1);
    expect(ruta?.canActivate).toEqual([plataformaGuard]);
    expect(ruta?.children).toBeUndefined();
    expect(await (ruta?.loadComponent as () => Promise<unknown>)()).toBe(C1401QaGlobalComponent);
  });

  it('oculta evidencia sin grant y solo muestra cinco areas NO_VERIFICADO con grant global', async () => {
    const sesion = signal<PlataformaSesion | null>(null);
    TestBed.resetTestingModule();
    await TestBed.configureTestingModule({
      imports: [C1401QaGlobalComponent],
      providers: [provideRouter([]), { provide: PlataformaSesionService, useValue: { sesion: sesion.asReadonly() } }]
    }).compileComponents();
    const fixture = TestBed.createComponent(C1401QaGlobalComponent);
    fixture.detectChanges();
    let root: HTMLElement = fixture.nativeElement;
    expect(root.querySelector('[data-testid="qa-global-denegado"]')).not.toBeNull();
    expect(root.querySelector('[data-testid="c1401-qa-global"]')).toBeNull();
    expect(root.querySelector('a[href*="github.com"]')).toBeNull();
    sesion.set({ nombreUsuario: 'Prueba', rolNombre: 'Plataforma', ambito: 'Plataforma', esAdministrador: true });
    fixture.detectChanges();
    await fixture.whenStable();
    fixture.detectChanges();
    root = fixture.nativeElement;
    expect(root.querySelector('[data-testid="c1401-qa-global"]')).not.toBeNull();
    expect(root.querySelectorAll('.qa-controles li')).toHaveLength(5);
    expect(root.querySelectorAll('button,form,input,textarea,select')).toHaveLength(0);
    expect(root.querySelectorAll('.pendiente')).toHaveLength(5);
    expect(root.textContent).toContain('NO_VERIFICADO');
    expect(root.querySelector('a[href*="github.com"]')?.getAttribute('rel')).toContain('noopener');
  });
});
