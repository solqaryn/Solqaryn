import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { RentabilidadFiltrosComponent } from './rentabilidad-filtros.component';

describe('RentabilidadFiltrosComponent', () => {
  let component: RentabilidadFiltrosComponent;
  let fixture: ComponentFixture<RentabilidadFiltrosComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [RentabilidadFiltrosComponent, ReactiveFormsModule],
    }).compileComponents();

    fixture = TestBed.createComponent(RentabilidadFiltrosComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should emit filter on submit when valid', () => {
    const emitSpy = vi.spyOn(component.filterChanged, 'emit');

    component.form.patchValue({
      vendedorId: 1,
      desde: '2023-01-01',
    });

    component.emitir();

    expect(emitSpy).toHaveBeenCalledWith(expect.objectContaining({
      vendedorId: 1,
      desde: '2023-01-01',
      page: 1,
      pageSize: 20,
    }));
  });

  it('should not emit while loading', () => {
    const emitSpy = vi.spyOn(component.filterChanged, 'emit');
    component.cargandoSelectores = true;
    component.emitir();
    expect(emitSpy).not.toHaveBeenCalled();
  });

  it('should not emit while the form is disabled', () => {
    const emitSpy = vi.spyOn(component.filterChanged, 'emit');
    component.disabled = true;
    component.emitir();
    expect(component.form.disabled).toBe(true);
    expect(emitSpy).not.toHaveBeenCalled();
  });

  it('should invalidate when desde > hasta', () => {
    component.form.patchValue({
      desde: '2023-12-31',
      hasta: '2023-01-01',
    });
    expect(component.rangoInvalido).toBe(true);
  });

  it('should disable form when disabled input is true', () => {
    component.disabled = true;
    expect(component.form.disabled).toBe(true);
  });
});
