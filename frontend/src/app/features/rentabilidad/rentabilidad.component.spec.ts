import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { of, throwError } from 'rxjs';
import { ReporteRentabilidadService } from '../../core/services/reporte-rentabilidad.service';
import { RentabilidadComponent } from './rentabilidad.component';

describe('RentabilidadComponent', () => {
  let fixture: ComponentFixture<RentabilidadComponent>;
  const service = {
    obtener: vi.fn(),
  };

  beforeEach(async () => {
    service.obtener.mockReset();
    service.obtener.mockReturnValue(of({ success: true, data: [], message: '', errors: [] }));

    await TestBed.configureTestingModule({
      imports: [RentabilidadComponent],
      providers: [{ provide: ReporteRentabilidadService, useValue: service }],
    }).compileComponents();

    fixture = TestBed.createComponent(RentabilidadComponent);
    fixture.detectChanges();
  });

  it('loads the accepted absolute-profitability contract on init', () => {
    expect(service.obtener).toHaveBeenCalledWith('vendedor', { page: 1, pageSize: 20 });
  });

  it('reloads when grouping changes without inventing margin percentage', () => {
    fixture.componentInstance.cambiarAgrupacion('producto');
    fixture.detectChanges();

    expect(service.obtener).toHaveBeenLastCalledWith('producto', { page: 1, pageSize: 20 });
    expect(fixture.nativeElement.textContent).not.toContain('Margen %');
  });

  it('renders backend semantics when rows are returned', () => {
    service.obtener.mockReturnValue(of({
      success: true,
      data: [{
        agrupacionId: 1,
        agrupacion: 'Vendedor',
        nombre: 'Ana',
        venta: 100,
        costo: 60,
        utilidadBruta: 40,
        incluyeDescuentoEncabezadoEnUtilidad: false,
        semantica: 'Utilidad bruta absoluta',
      }],
      message: '',
      errors: [],
    }));

    fixture.componentInstance.cambiarAgrupacion('cliente');
    fixture.detectChanges();

    const disclosure = fixture.debugElement.query(By.css('app-rentabilidad-disclosure'));
    expect(disclosure).not.toBeNull();
    expect(fixture.nativeElement.textContent).toContain('Utilidad bruta absoluta');
  });

  it('exposes a stable error state on transport failure', () => {
    service.obtener.mockReturnValue(throwError(() => new Error('network')));
    fixture.componentInstance.cambiarAgrupacion('categoria');
    fixture.detectChanges();

    expect(fixture.componentInstance.resultados).toEqual([]);
    expect(fixture.componentInstance.error).toContain('No fue posible cargar');
  });
});
