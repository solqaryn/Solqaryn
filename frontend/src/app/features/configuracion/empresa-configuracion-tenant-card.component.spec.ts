import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, Subject } from 'rxjs';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { EmpresaConfiguracionService } from '../../services/empresa-configuracion.service';
import { EmpresaConfiguracionTenantCardComponent } from './empresa-configuracion-tenant-card.component';

describe('C01-03.M04.04 - permisos reactivos en formulario tenant', () => {
  let allowed = signal(true);
  let activeTenant = signal<number | null>(1);
  let mutation: Subject<unknown>;
  let service: {
    getTenant: ReturnType<typeof vi.fn>;
    updateTenant: ReturnType<typeof vi.fn>;
  };
  let snackBar: { open: ReturnType<typeof vi.fn> };

  const config = {
    empresaId: 1,
    nombre: 'Tenant A',
    rtn: null,
    direccion: null,
    logoUrl: null,
    moneda: 'HNL',
    zonaHoraria: 'America/Tegucigalpa',
    impuestosJson: '{}',
    emisionJson: '{}',
    correoRemitente: null,
    correoNombreRemitente: null,
    correoConfigurado: false,
    version: 1,
    plantillasCorreo: []
  };

  beforeEach(() => {
    allowed = signal(true);
    activeTenant = signal<number | null>(1);
    mutation = new Subject<unknown>();
    snackBar = { open: vi.fn() };
    service = {
      getTenant: vi.fn(() => of({ data: { ...config } })),
      updateTenant: vi.fn(() => mutation.asObservable())
    };
    TestBed.configureTestingModule({
      providers: [
        FormBuilder,
        { provide: TenantContextService, useValue: { empresaIdVerificada: activeTenant } },
        { provide: PermisosRuntimeService, useValue: { puede: (_: string, __: string) => allowed() } },
        { provide: EmpresaConfiguracionService, useValue: service },
        { provide: MatSnackBar, useValue: snackBar }
      ]
    });
  });

  function create(): EmpresaConfiguracionTenantCardComponent {
    const component = TestBed.runInInjectionContext(
      () => new EmpresaConfiguracionTenantCardComponent()
    );
    component.ngOnInit();
    TestBed.tick();
    return component;
  }

  it('disables mounted form and refuses PUT after permission revocation, then permits a fresh grant', () => {
    const component = create();
    expect(component.puedeEditar()).toBe(true);
    expect(component.form.enabled).toBe(true);

    allowed.set(false);
    TestBed.tick();
    expect(component.puedeEditar()).toBe(false);
    expect(component.form.disabled).toBe(true);
    component.guardar();
    expect(service.updateTenant).not.toHaveBeenCalled();

    allowed.set(true);
    TestBed.tick();
    expect(component.puedeEditar()).toBe(true);
    expect(component.form.enabled).toBe(true);
  });

  it('discards an in-flight successful mutation after permission revocation', () => {
    const component = create();
    component.guardar();
    expect(service.updateTenant).toHaveBeenCalledTimes(1);

    allowed.set(false);
    TestBed.tick();
    mutation.next({ data: { ...config, nombre: 'Untrusted response after revoke', version: 2 } });
    mutation.complete();
    TestBed.tick();

    expect(component.loadState()).toBe('forbidden');
    expect(component.form.disabled).toBe(true);
    expect(snackBar.open).not.toHaveBeenCalled();
  });
});
