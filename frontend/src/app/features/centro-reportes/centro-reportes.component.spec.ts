import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { CentroReportesComponent } from './centro-reportes.component';

describe('CentroReportesComponent N5.2.G QA takeover', () => {
  const permisos = {
    puede: vi.fn(),
    esAdministrador: vi.fn(),
    cargar: vi.fn(() => of(true))
  } as unknown as PermisosRuntimeService;

  beforeEach(async () => {
    vi.mocked(permisos.puede).mockReset();
    vi.mocked(permisos.esAdministrador).mockReset();
    vi.mocked(permisos.esAdministrador).mockReturnValue(false);
    vi.mocked(permisos.cargar).mockClear();

    await TestBed.configureTestingModule({
      imports: [CentroReportesComponent],
      providers: [
        provideRouter([]),
        { provide: PermisosRuntimeService, useValue: permisos }
      ]
    }).compileComponents();
  });

  it('expone navegación accesible de inventario sólo para permisos autorizados', () => {
    vi.mocked(permisos.puede).mockImplementation((modulo: string, accion: string) =>
      (modulo === 'Inventario' && accion === 'Ver') ||
      (modulo === 'MovimientosInventario' && accion === 'ConsultarHistorial')
    );

    const fixture = TestBed.createComponent(CentroReportesComponent);
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('section[aria-labelledby="centro-reportes-title"]')).not.toBeNull();
    expect(element.querySelector('#centro-reportes-title')?.textContent).toContain('Centro de Reportes');

    const nav = element.querySelector('nav[aria-label="Tipos de reportes disponibles"]');
    expect(nav).not.toBeNull();
    const links = Array.from(nav!.querySelectorAll('a'));
    const labels = links.map((link) => link.textContent?.trim());
    expect(labels).toEqual(['Valorización', 'Kardex', 'Salud de inventario', 'Reconciliación']);
    expect(links.map((link) => link.getAttribute('href'))).toEqual([
      '/inventario/valorizacion',
      '/inventario/kardex',
      '/inventario/stock-health',
      '/inventario/reconciliacion'
    ]);
  });

  it('mantiene fail-closed la superficie cuando no existe ningún reporte autorizado', () => {
    vi.mocked(permisos.puede).mockReturnValue(false);
    vi.mocked(permisos.esAdministrador).mockReturnValue(false);

    const fixture = TestBed.createComponent(CentroReportesComponent);
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('nav')).toBeNull();
    const status = element.querySelector('[role="status"]') as HTMLElement | null;
    expect(status).not.toBeNull();
    expect(status?.textContent).toContain('No tienes reportes habilitados');
  });

  it('no expone reportes administrativos sólo por rol administrador sin permiso relacional', () => {
    vi.mocked(permisos.esAdministrador).mockReturnValue(true);
    vi.mocked(permisos.puede).mockReturnValue(false);

    const fixture = TestBed.createComponent(CentroReportesComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).not.toContain('Reportes administrativos');
    expect(fixture.nativeElement.querySelector('[role="status"]')).not.toBeNull();
  });
});
