import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient, withXhr } from '@angular/common/http';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of } from 'rxjs';
import { AsientosContablesComponent } from './asientos-contables.component';
import { AsientoContableService } from '../../services/asiento-contable.service';
import { CuentaContableService } from '../../services/cuenta-contable.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';

class AsientoServiceStub {
  getAll() { return of({ data: { items: [], total: 0 } }); }
  create() { return of({ data: {} }); }
}

class CuentaServiceStub {
  getAll() { return of({ data: [] }); }
}

class PermisosRuntimeStub {
  puede() { return true; }
}

describe('AsientosContablesComponent', () => {
  let component: AsientosContablesComponent;
  let fixture: ComponentFixture<AsientosContablesComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [AsientosContablesComponent, NoopAnimationsModule],
      providers: [
        provideHttpClient(withXhr()),
        { provide: AsientoContableService, useClass: AsientoServiceStub },
        { provide: CuentaContableService, useClass: CuentaServiceStub },
        { provide: PermisosRuntimeService, useClass: PermisosRuntimeStub }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AsientosContablesComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('creates', () => {
    expect(component).toBeTruthy();
  });

  it('starts a new balanced-entry form with two details', () => {
    component.nuevoAsiento();
    expect(component.mostrarFormulario()).toBe(true);
    expect(component.detallesFormArray.length).toBe(2);
  });

  it('rejects an unbalanced asiento', () => {
    component.nuevoAsiento();
    component.asientoForm.patchValue({ concepto: 'Prueba' });
    component.detallesFormArray.at(0).patchValue({ cuentaContableId: 1, debe: 100, haber: 0 });
    component.detallesFormArray.at(1).patchValue({ cuentaContableId: 2, debe: 0, haber: 90 });
    component.asientoForm.updateValueAndValidity();
    expect(component.asientoForm.hasError('descuadrado')).toBe(true);
  });

  it('accepts a balanced asiento', () => {
    component.nuevoAsiento();
    component.asientoForm.patchValue({ concepto: 'Prueba' });
    component.detallesFormArray.at(0).patchValue({ cuentaContableId: 1, debe: 100, haber: 0 });
    component.detallesFormArray.at(1).patchValue({ cuentaContableId: 2, debe: 0, haber: 100 });
    component.asientoForm.updateValueAndValidity();
    expect(component.asientoForm.hasError('descuadrado')).toBe(false);
  });
});
