import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { EstadoSelectorComponent } from './estado-selector.component';
import { FormControl } from '@angular/forms';

describe('EstadoSelectorComponent', () => {
  let component: EstadoSelectorComponent;
  let fixture: ComponentFixture<EstadoSelectorComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EstadoSelectorComponent, NoopAnimationsModule]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(EstadoSelectorComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load EstadoOrden options correctly', () => {
    component.tipoEstado = 'EstadoOrden';
    component.ngOnChanges({
      tipoEstado: {
        currentValue: 'EstadoOrden',
        previousValue: null,
        firstChange: false,
        isFirstChange: () => false
      }
    });
    
    expect(component.opciones.length).toBe(4);
    expect(component.opciones[0].label).toBe('Borrador');
    expect(component.opciones[3].label).toBe('Cancelada');
  });

  it('should emit selectionChange when an option is selected', () => {
    const emitSpy = vi.spyOn(component.selectionChange, 'emit');
    component.onSelectionChange(2);
    expect(emitSpy).toHaveBeenCalledWith(2);
  });
});
