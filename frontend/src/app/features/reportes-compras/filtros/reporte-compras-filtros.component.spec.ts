import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { vi } from 'vitest';
import { ReporteComprasFiltrosComponent } from './reporte-compras-filtros.component';

describe('ReporteComprasFiltrosComponent', () => {
  let component: ReporteComprasFiltrosComponent;
  let fixture: ComponentFixture<ReporteComprasFiltrosComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ReactiveFormsModule, ReporteComprasFiltrosComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(ReporteComprasFiltrosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('sincroniza disabled y carga con el estado del FormGroup', () => {
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();
    expect(component.form.disabled).toBe(true);

    fixture.componentRef.setInput('disabled', false);
    fixture.componentRef.setInput('cargandoSelectores', true);
    fixture.detectChanges();
    expect(component.form.disabled).toBe(true);

    fixture.componentRef.setInput('cargandoSelectores', false);
    fixture.detectChanges();
    expect(component.form.enabled).toBe(true);
  });

  it('rejects reversed date ranges', () => {
    component.form.patchValue({ desdeUtc: '2024-12-31', hastaUtc: '2024-01-01' });
    expect(component.rangoInvalido).toBe(true);
  });

  it('emits the accepted backend contract with UTC bounds', () => {
    const emitSpy = vi.spyOn(component.filterChanged, 'emit');
    component.form.patchValue({
      desdeUtc: '2024-01-01',
      hastaUtc: '2024-12-31',
      proveedorId: 1,
      productoId: 2,
      productoVarianteId: 3,
      pageSize: 50,
    });

    component.emitir();

    expect(emitSpy).toHaveBeenCalledWith({
      page: 1,
      pageSize: 50,
      desdeUtc: '2024-01-01T00:00:00.000Z',
      hastaUtc: '2024-12-31T23:59:59.999Z',
      proveedorId: 1,
      productoId: 2,
      productoVarianteId: 3,
    });
  });

  it('does not emit while disabled or with a non-positive id', () => {
    const emitSpy = vi.spyOn(component.filterChanged, 'emit');
    component.form.patchValue({ proveedorId: 0 });
    component.emitir();
    expect(emitSpy).not.toHaveBeenCalled();

    component.form.patchValue({ proveedorId: 1 });
    component.disabled = true;
    component.emitir();
    expect(emitSpy).not.toHaveBeenCalled();
  });
});
