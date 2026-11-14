import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { RentabilidadAgrupacionComponent, RentabilidadAgrupacion } from './rentabilidad-agrupacion.component';

describe('RentabilidadAgrupacionComponent', () => {
  let component: RentabilidadAgrupacionComponent;
  let fixture: ComponentFixture<RentabilidadAgrupacionComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({ imports: [RentabilidadAgrupacionComponent] }).compileComponents();
    fixture = TestBed.createComponent(RentabilidadAgrupacionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render the four supported groupings accessibly', () => {
    const select = fixture.debugElement.query(By.css('select')).nativeElement as HTMLSelectElement;
    const label = fixture.debugElement.query(By.css('label')).nativeElement as HTMLLabelElement;
    expect(select.id).toBe('agrupacion-select');
    expect(label.htmlFor).toBe('agrupacion-select');
    expect(Array.from(select.options).map(option => option.value)).toEqual([
      'vendedor', 'cliente', 'producto', 'categoria',
    ]);
  });

  it('emits the selected grouping', () => {
    let emitted: RentabilidadAgrupacion | undefined;
    component.valueChange.subscribe(value => emitted = value);
    component.agrupacionControl.setValue('producto');
    component.onSelectionChange();
    expect(emitted).toBe('producto');
  });

  it('synchronizes value and disabled inputs', () => {
    component.value = 'categoria';
    component.disabled = true;
    fixture.detectChanges();
    expect(component.agrupacionControl.value).toBe('categoria');
    expect(component.agrupacionControl.disabled).toBe(true);

    component.disabled = false;
    expect(component.agrupacionControl.enabled).toBe(true);
  });

  it('does not emit a programmatic selection while disabled', () => {
    let emitted: RentabilidadAgrupacion | undefined;
    component.valueChange.subscribe(value => emitted = value);
    component.disabled = true;
    component.agrupacionControl.setValue('cliente', { emitEvent: false });
    component.onSelectionChange();
    expect(emitted).toBeUndefined();
  });
});
