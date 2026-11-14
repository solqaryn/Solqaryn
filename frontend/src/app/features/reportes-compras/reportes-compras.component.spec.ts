import { HttpClientTestingModule } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, throwError } from 'rxjs';
import { ReporteComprasService } from '../../core/services/reporte-compras.service';
import { ReportesComprasComponent } from './reportes-compras.component';

describe('ReportesComprasComponent', () => {
  let fixture: ComponentFixture<ReportesComprasComponent>;
  let component: ReportesComprasComponent;
  const service = {
    getDetalle: vi.fn(),
  };

  beforeEach(async () => {
    service.getDetalle.mockReset();
    service.getDetalle.mockReturnValue(of({ success: true, data: { items: [], page: 1, pageSize: 50, totalCount: 0, totalPages: 0 }, message: '', errors: [] }));

    await TestBed.configureTestingModule({
      imports: [ReportesComprasComponent, HttpClientTestingModule, NoopAnimationsModule],
      providers: [{ provide: ReporteComprasService, useValue: service }],
    }).compileComponents();

    fixture = TestBed.createComponent(ReportesComprasComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('carga el reporte y representa el estado vacío sin inventar métricas', () => {
    expect(service.getDetalle).toHaveBeenCalledWith({ page: 1, pageSize: 50 });
    expect(component.state).toBe('empty');
    expect(fixture.nativeElement.textContent).toContain('No hay datos');
  });

  it('reaplica estados y reinicia la página', () => {
    component.filtro = { page: 4, pageSize: 20 };
    component.estadoFactura.setValue(2);
    component.aplicarEstados();
    expect(service.getDetalle).toHaveBeenCalledWith(expect.objectContaining({ page: 1, pageSize: 20, estadoFactura: 2 }));
  });

  it('propaga errores de transporte al disclosure accesible', () => {
    service.getDetalle.mockReturnValue(throwError(() => new Error('network')));
    component.aplicarFiltros({ page: 1, pageSize: 50 });
    fixture.detectChanges();
    expect(component.state).toBe('error');
    expect(fixture.nativeElement.textContent).toContain('Error al cargar el reporte');
  });
});
