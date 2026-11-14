import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReportesComprasTablaComponent } from './reportes-compras-tabla.component';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

describe('ReportesComprasTablaComponent', () => {
  let component: ReportesComprasTablaComponent;
  let fixture: ComponentFixture<ReportesComprasTablaComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [
        ReportesComprasTablaComponent,
        NoopAnimationsModule
      ]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ReportesComprasTablaComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show loading overlay when isLoading is true', () => {
    component.isLoading = true;
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.loading-overlay')).toBeTruthy();
  });

  it('should render table headers', () => {
    const compiled = fixture.nativeElement as HTMLElement;
    const headers = compiled.querySelectorAll('th');
    expect(headers.length).toBeGreaterThan(0);
    expect(headers[0].textContent?.trim()).toContain('Orden');
    expect(headers[1].textContent?.trim()).toContain('Proveedor');
  });

  it('should render data row correctly', () => {
    component.result = {
      items: [
        {
          ordenCompraId: 1,
          ordenCompraDetalleId: 1,
          numeroOrden: 'ORD-001',
          fechaCreacionUtc: '2023-10-01T12:00:00Z',
          proveedorId: 10,
          proveedorNombre: 'Test Proveedor',
          moneda: 'USD',
          productoId: 100,
          productoNombre: 'Test Producto',
          cantidadOrdenada: 50,
          precioUnitarioOrdenado: 10.5,
          cantidadRecibida: 50,
          cantidadAceptada: 50,
          cantidadDanada: 0,
          cantidadFaltante: 0,
          cantidadSobrante: 0,
          cantidadDevueltaEfectiva: 0
        }
      ],
      page: 1,
      pageSize: 50,
      totalCount: 1,
      totalPages: 1
    };
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const rows = compiled.querySelectorAll('tbody tr');
    expect(rows.length).toBe(1);
    
    const cells = rows[0].querySelectorAll('td');
    expect(cells[0].textContent).toContain('ORD-001');
    expect(cells[1].textContent).toContain('Test Proveedor');
    expect(cells[2].textContent).toContain('Test Producto');
  });
});
