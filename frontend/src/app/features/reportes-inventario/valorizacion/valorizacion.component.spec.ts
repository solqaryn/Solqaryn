import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';
import { PermisosRuntimeService } from '../../../core/auth/permisos-runtime.service';
import { ReporteInventarioValorizacionService } from '../../../core/services/reporte-inventario-valorizacion.service';
import { ValorizacionComponent } from './valorizacion.component';

describe('ValorizacionComponent N5.2.G', () => {
  const permisos = { puede: vi.fn() } as unknown as PermisosRuntimeService;
  const service = { getResumen: vi.fn() } as unknown as ReporteInventarioValorizacionService;

  beforeEach(async () => {
    vi.mocked(permisos.puede).mockReset();
    vi.mocked(service.getResumen).mockReset();
    await TestBed.configureTestingModule({
      imports: [ValorizacionComponent],
      providers: [
        { provide: PermisosRuntimeService, useValue: permisos },
        { provide: ReporteInventarioValorizacionService, useValue: service }
      ]
    }).compileComponents();
  });

  it('expone heading accesible y censura importes cuando Finanzas.Ver no está permitido', () => {
    vi.mocked(permisos.puede).mockReturnValue(false);
    vi.mocked(service.getResumen).mockReturnValue(of({
      success: true,
      data: {
        valorInventarioCosto: null,
        valorInventarioCostoMercaderia: null,
        valorInventarioCostoInsumosAdministrativos: null,
        valorPotencialVentaMercaderia: null
      }
    } as any));

    const fixture = TestBed.createComponent(ValorizacionComponent);
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    const section = element.querySelector('section[aria-labelledby="valorizacion-title"]');
    expect(section).not.toBeNull();
    expect(element.querySelector('#valorizacion-title')?.textContent).toContain('Valorización de inventario');
    expect(element.textContent).toContain('importes financieros están ocultos por permisos');
    expect(element.textContent).not.toContain('Costo total');
  });

  it('renderiza el estado de error como role alert para recuperación accesible', () => {
    vi.mocked(permisos.puede).mockReturnValue(true);
    vi.mocked(service.getResumen).mockReturnValue(throwError(() => new Error('network')));

    const fixture = TestBed.createComponent(ValorizacionComponent);
    fixture.detectChanges();

    const alert = fixture.nativeElement.querySelector('[role="alert"]') as HTMLElement;
    expect(alert).not.toBeNull();
    expect(alert.textContent).toContain('No se pudo cargar la valorización.');
    expect(alert.querySelector('button')?.textContent).toContain('Reintentar');
  });
});
