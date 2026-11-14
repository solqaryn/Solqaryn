import { ComponentFixture, TestBed } from '@angular/core/testing';
import { RentabilidadResultadosTablaComponent } from './rentabilidad-resultados-tabla.component';

describe('RentabilidadResultadosTablaComponent', () => {
  let component: RentabilidadResultadosTablaComponent;
  let fixture: ComponentFixture<RentabilidadResultadosTablaComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RentabilidadResultadosTablaComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(RentabilidadResultadosTablaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show loading state', () => {
    component.isLoading = true;
    fixture.detectChanges();
    const element = fixture.nativeElement;
    expect(element.querySelector('.loading-state')).toBeTruthy();
  });

  it('should show error state', () => {
    component.error = 'Hubo un error';
    fixture.detectChanges();
    const element = fixture.nativeElement;
    expect(element.querySelector('.error-state')).toBeTruthy();
    expect(element.querySelector('.error-state').textContent).toContain('Hubo un error');
  });

  it('should show empty state if results are empty', () => {
    component.resultados = [];
    fixture.detectChanges();
    const element = fixture.nativeElement;
    expect(element.querySelector('.empty-state')).toBeTruthy();
  });

  it('should display the accepted camelCase DTO contract', () => {
    component.resultados = [
      {
        agrupacionId: 1,
        agrupacion: 'Grupo A',
        nombre: 'Item A',
        venta: 100,
        costo: 50,
        utilidadBruta: 50,
        incluyeDescuentoEncabezadoEnUtilidad: false,
        semantica: 'Positivo',
      },
    ];
    fixture.detectChanges();
    const element = fixture.nativeElement;
    const rows = element.querySelectorAll('tbody tr');
    expect(rows.length).toBe(1);
    expect(rows[0].cells[0].textContent.trim()).toBe('Grupo A');
    expect(rows[0].cells[1].textContent.trim()).toBe('Item A');
  });
});
