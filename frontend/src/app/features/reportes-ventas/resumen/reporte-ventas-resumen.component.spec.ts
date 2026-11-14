import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ReporteVentasResumenDto } from '../../../core/models/reporte-ventas.models';
import { ReporteVentasResumenComponent } from './reporte-ventas-resumen.component';

describe('ReporteVentasResumenComponent', () => {
  let component: ReporteVentasResumenComponent;
  let fixture: ComponentFixture<ReporteVentasResumenComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReporteVentasResumenComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ReporteVentasResumenComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display loading state', () => {
    component.cargando = true;
    fixture.detectChanges();
    const statusEl = fixture.debugElement.query(By.css('.status'));
    expect(statusEl).toBeTruthy();
    expect(statusEl.nativeElement.textContent).toContain('Cargando');
  });

  it('should display error state', () => {
    component.error = 'Error loading data';
    fixture.detectChanges();
    const errorEl = fixture.debugElement.query(By.css('.error'));
    expect(errorEl).toBeTruthy();
    expect(errorEl.nativeElement.textContent).toContain('Error loading data');
  });

  it('should display empty state when no summary provided', () => {
    component.resumen = null;
    component.cargando = false;
    component.error = '';
    fixture.detectChanges();
    const emptyEl = fixture.debugElement.query(By.css('.empty'));
    expect(emptyEl).toBeTruthy();
    expect(emptyEl.nativeElement.textContent).toContain('No hay datos');
  });

  it('should display summary metrics when provided', () => {
    const mockResumen: ReporteVentasResumenDto = {
      importeBruto: 1000,
      subtotal: 900,
      descuento: 100,
      impuesto: 144,
      total: 1044,
      costoTotal: 500,
      utilidadBruta: 400
    };

    component.resumen = mockResumen;
    fixture.detectChanges();

    const kpis = fixture.debugElement.queryAll(By.css('.kpi'));
    expect(kpis.length).toBe(7);

    const textContent = fixture.nativeElement.textContent;
    expect(textContent).toContain('1,000.00');
    expect(textContent).toContain('900.00');
    expect(textContent).toContain('100.00');
    expect(textContent).toContain('144.00');
    expect(textContent).toContain('1,044.00');
    expect(textContent).toContain('500.00');
    expect(textContent).toContain('400.00');
  });
});
