import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { vi } from 'vitest';
import { PagedResult } from '../../../core/models/api-response.model';
import { ReporteVentasDetalleDto } from '../../../core/models/reporte-ventas.models';
import { ReporteVentasDetalleComponent } from './reporte-ventas-detalle.component';

describe('ReporteVentasDetalleComponent', () => {
  let component: ReporteVentasDetalleComponent;
  let fixture: ComponentFixture<ReporteVentasDetalleComponent>;

  const fila: ReporteVentasDetalleDto = {
    ventaId: 15,
    numeroVenta: 'V-0015',
    fecha: '2026-09-09T10:15:00Z',
    clienteId: 9,
    clienteNombre: 'Cliente histórico',
    vendedorId: 4,
    vendedorNombre: 'Vendedor histórico',
    sucursalId: 2,
    categoriaId: 3,
    productoId: 20,
    productoVarianteId: 21,
    productoNombre: 'Producto histórico',
    productoMarca: 'Marca histórica',
    productoModelo: 'Modelo histórico',
    productoColor: 'Azul',
    productoTalla: 'M',
    productoSku: 'SKU-HIST-21',
    cantidad: 2,
    precioUnitario: 125,
    costoUnitario: 80,
    subtotal: 250,
    utilidadBruta: 90,
  };

  const detalle: PagedResult<ReporteVentasDetalleDto> = {
    items: [fila],
    page: 1,
    pageSize: 20,
    totalCount: 21,
    totalPages: 2,
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReporteVentasDetalleComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ReporteVentasDetalleComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render loading, error and empty states', () => {
    component.cargando = true;
    fixture.detectChanges();
    expect(fixture.debugElement.query(By.css('[role="status"]'))?.nativeElement.textContent).toContain('Cargando');

    component.cargando = false;
    component.error = 'No fue posible cargar';
    fixture.detectChanges();
    expect(fixture.debugElement.query(By.css('[role="alert"]'))?.nativeElement.textContent).toContain('No fue posible cargar');

    component.error = '';
    component.detalle = null;
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('No hay ventas');
  });

  it('should render durable historical dimensions and paging metadata', () => {
    component.detalle = detalle;
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(fixture.debugElement.query(By.css('table'))).toBeTruthy();
    expect(text).toContain('V-0015');
    expect(text).toContain('Cliente histórico');
    expect(text).toContain('Vendedor histórico');
    expect(text).toContain('Producto histórico');
    expect(text).toContain('SKU-HIST-21');
    expect(text).toContain('Marca histórica');
    expect(text).toContain('Modelo histórico');
    expect(text).toContain('Azul');
    expect(text).toContain('M');
    expect(text).toContain('Página 1 de 2');
    expect(text).toContain('21 registros');
  });

  it('should emit only valid page changes', () => {
    component.detalle = detalle;
    const emitSpy = vi.spyOn(component.paginaChange, 'emit');

    component.irPagina(2);
    component.irPagina(1);
    component.irPagina(0);
    component.irPagina(3);

    expect(emitSpy).toHaveBeenCalledTimes(1);
    expect(emitSpy).toHaveBeenCalledWith(2);
  });
});
