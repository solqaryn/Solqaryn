import { HttpClient } from '@angular/common/http';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { convertToParamMap, ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { SucursalService } from '../../services/sucursal.service';
import { SucursalFormComponent } from './sucursal-form.component';

describe('SucursalFormComponent tenant ownership', () => {
  let fixture: ComponentFixture<SucursalFormComponent>;
  let component: SucursalFormComponent;
  const sucursalService = {
    getById: vi.fn(),
    create: vi.fn(),
    update: vi.fn()
  };
  const router = {
    navigate: vi.fn()
  };
  const http = {
    get: vi.fn(() => of({ data: [{ id: 7, nombre: 'Empresa Siete', activa: true }] }))
  };

  beforeEach(async () => {
    vi.clearAllMocks();

    await TestBed.configureTestingModule({
      imports: [SucursalFormComponent],
      providers: [
        { provide: SucursalService, useValue: sucursalService },
        { provide: HttpClient, useValue: http },
        { provide: Router, useValue: router },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({}) } }
        }
      ]
    })
      .overrideComponent(SucursalFormComponent, { set: { template: '' } })
      .compileComponents();

    fixture = TestBed.createComponent(SucursalFormComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('loads the Empresa catalog used by the owner selector', () => {
    expect(http.get).toHaveBeenCalledWith(`${environment.apiUrl}/empresas`);
    expect(component.empresas()).toEqual([{ id: 7, nombre: 'Empresa Siete', activa: true }]);
    expect(component.empresasLoading()).toBe(false);
  });

  it('requires an explicit EmpresaId greater than zero', () => {
    const empresaId = component.form.controls.empresaId;

    expect(empresaId.hasError('required')).toBe(true);

    empresaId.setValue(0);
    expect(empresaId.hasError('min')).toBe(true);

    empresaId.setValue(7);
    expect(empresaId.valid).toBe(true);
  });

  it('keeps Empresa ownership fail-closed when catalog loading fails before edit data arrives', () => {
    http.get.mockReturnValueOnce(throwError(() => new Error('catalog unavailable')));
    (component as unknown as { cargarEmpresas: () => void }).cargarEmpresas();

    sucursalService.getById.mockReturnValueOnce(of({
      data: {
        empresaId: 7,
        codigo: 'TGU-01',
        nombre: 'Tegucigalpa',
        direccion: null,
        telefono: null,
        correo: null,
        zonaHoraria: 'America/Tegucigalpa'
      }
    }));
    (component as unknown as { cargarSucursal: (id: number) => void }).cargarSucursal(1);

    expect(component.empresasError()).toBe('No se pudieron cargar las empresas. Reintenta antes de guardar.');
    expect(component.form.controls.empresaId.disabled).toBe(true);

    component.submit();

    expect(sucursalService.create).not.toHaveBeenCalled();
    expect(sucursalService.update).not.toHaveBeenCalled();
  });

  it('does not write a sucursal without tenant ownership', () => {
    component.form.patchValue({
      empresaId: null,
      codigo: 'TGU-01',
      nombre: 'Tegucigalpa',
      zonaHoraria: 'America/Tegucigalpa'
    });

    component.submit();

    expect(sucursalService.create).not.toHaveBeenCalled();
    expect(sucursalService.update).not.toHaveBeenCalled();
  });

  it('sends the explicit EmpresaId in the create contract', () => {
    sucursalService.create.mockReturnValue(of({ data: {} } as never));
    component.form.patchValue({
      empresaId: 7,
      codigo: 'TGU-01',
      nombre: 'Tegucigalpa',
      direccion: '',
      telefono: '',
      correo: '',
      zonaHoraria: 'America/Tegucigalpa'
    });

    component.submit();

    expect(sucursalService.create).toHaveBeenCalledWith(expect.objectContaining({ empresaId: 7 }));
    expect(router.navigate).toHaveBeenCalledWith(['/sucursales']);
  });
});
