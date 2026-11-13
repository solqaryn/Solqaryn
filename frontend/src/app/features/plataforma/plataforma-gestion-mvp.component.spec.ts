import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { signal } from '@angular/core';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';
import { PlataformaGestionMvpService } from '../../core/auth/plataforma-gestion-mvp.service';
import { PlataformaSesionService } from '../../core/auth/plataforma-sesion.service';
import { PlataformaGestionMvpComponent } from './plataforma-gestion-mvp.component';
import { PlataformaComponent } from './plataforma.component';
import { PlataformaEmpresasService } from '../../core/auth/plataforma-empresas.service';
import { plataformaGuard } from '../../core/guards/plataforma.guard';
import { routes } from '../../app.routes';

describe('P1-014.A — alta guiada empresarial', () => {
  const api = {
    altaControlada: vi.fn(() => of({ empresaId: 82, administradorUsuarioId: 7 })),
    altaControladaConAdministradorNuevo: vi.fn(() => of({
      empresaId: 82, administradorUsuarioId: 7, operacionComercialHabilitada: false
    }))
  };

  beforeEach(async () => {
    vi.clearAllMocks();
    await TestBed.configureTestingModule({
      imports: [PlataformaGestionMvpComponent, NoopAnimationsModule],
      providers: [
        { provide: PlataformaGestionMvpService, useValue: api },
        { provide: PlataformaSesionService, useValue: { limpiar: vi.fn() } },
        { provide: Router, useValue: { navigate: vi.fn() } }
      ]
    }).compileComponents();
  });

  function vista(puedeCrear = false) {
    const fixture = TestBed.createComponent(PlataformaGestionMvpComponent);
    fixture.componentRef.setInput('directorio', { items: [], puedeCrear });
    fixture.detectChanges();
    return fixture;
  }

  it('P1014A no avanza con nombre inválido y no ejecuta altas', () => {
    const f = vista(true), c = f.componentInstance;
    c.avanzarAlta();
    c.empresaNueva.set('  A  ');
    c.avanzarAlta();
    expect(c.pasoAlta()).toBe(1);
    expect(c.nombreAltaValido()).toBe(false);
    expect(api.altaControlada).not.toHaveBeenCalled();
  });

  it('P1014A exige identidad verificada y conserva los datos al retroceder', () => {
    const f = vista(true), c = f.componentInstance;
    c.empresaNueva.set('Empresa A');
    c.avanzarAlta();
    c.avanzarAlta();
    expect(c.pasoAlta()).toBe(2);
    c.buscado.set({ id: 7, nombreUsuario: 'admin', nombreCompleto: 'Admin',
      activo: false, bloqueado: true, pendienteAsignacion: false });
    expect(c.administradorAltaValido()).toBe(false);
    c.buscado.set({ id: 7, nombreUsuario: 'admin', nombreCompleto: 'Admin',
      activo: false, bloqueado: false, pendienteAsignacion: true });
    c.avanzarAlta();
    f.detectChanges();
    expect(c.pasoAlta()).toBe(3);
    expect(f.nativeElement.textContent).toContain('Configuración inicial');
    c.retrocederAlta();
    c.retrocederAlta();
    expect(c.empresaNueva()).toBe('Empresa A');
  });

  it('P1014A no avanza con contraseña débil ni expone la contraseña en resumen', () => {
    const f = vista(true), c = f.componentInstance;
    c.empresaNueva.set('Empresa B');
    c.avanzarAlta();
    c.modoAlta.set('nuevo');
    c.adminEmpresaUsuarioNuevo.set('admin.b');
    c.adminEmpresaNombreNuevo.set('Administrador B');
    c.adminEmpresaClaveNueva.set('debil');
    c.avanzarAlta();
    expect(c.pasoAlta()).toBe(2);
    c.adminEmpresaClaveNueva.set('SecretaFuerte!42');
    c.avanzarAlta();
    f.detectChanges();
    expect(c.pasoAlta()).toBe(3);
    expect(f.nativeElement.querySelector('#admin-empresa-clave')).toBeNull();
    expect(f.nativeElement.textContent).not.toContain('SecretaFuerte!42');
  });

  it('P1014A bloquea confirmación sin flag de servidor, incluso con pasos válidos', () => {
    const f = vista(false), c = f.componentInstance;
    c.empresaNueva.set('Empresa C');
    c.avanzarAlta();
    c.buscado.set({ id: 7, nombreUsuario: 'admin', nombreCompleto: 'Admin',
      activo: true, bloqueado: false, pendienteAsignacion: false });
    c.avanzarAlta();
    f.detectChanges();
    const button = Array.from((f.nativeElement as HTMLElement).querySelectorAll('button'))
      .find(x => x.textContent?.includes('Confirmar alta empresarial'))!;
    expect(button.disabled).toBe(true);
    c.crearEmpresa();
    expect(api.altaControlada).not.toHaveBeenCalled();
  });

  it('P1014A confirma una sola petición original y reinicia el paso', () => {
    const f = vista(true), c = f.componentInstance;
    c.empresaNueva.set('Empresa D');
    c.avanzarAlta();
    c.buscado.set({ id: 7, nombreUsuario: 'admin', nombreCompleto: 'Admin',
      activo: true, bloqueado: false, pendienteAsignacion: false });
    c.avanzarAlta();
    f.detectChanges();
    const button = Array.from((f.nativeElement as HTMLElement).querySelectorAll('button'))
      .find(x => x.textContent?.includes('Confirmar alta empresarial'))!;
    expect(button.disabled).toBe(false);
    button.click();
    f.detectChanges();
    expect(api.altaControlada).toHaveBeenCalledOnce();
    expect(api.altaControlada).toHaveBeenCalledWith('Empresa D', 7);
    expect(c.pasoAlta()).toBe(1);
    expect(c.empresaNueva()).toBe('');
  });

  it('P1014D conserva el alta guiada cuando el backend rechaza con HTTP 403', () => {
    api.altaControlada.mockImplementationOnce(() => throwError(() => ({
      status: 403, error: { message: 'Operacion no autorizada' }
    })));
    const f = vista(true), c = f.componentInstance;
    c.empresaNueva.set('Empresa Rechazada');
    c.avanzarAlta();
    c.buscado.set({ id: 7, nombreUsuario: 'admin', nombreCompleto: 'Admin',
      activo: true, bloqueado: false, pendienteAsignacion: false });
    c.avanzarAlta();
    f.detectChanges();
    expect(c.pasoAlta()).toBe(3);
    c.crearEmpresa();
    f.detectChanges();
    expect(api.altaControlada).toHaveBeenCalledOnce();
    expect(api.altaControlada).toHaveBeenCalledWith('Empresa Rechazada', 7);
    expect(api.altaControladaConAdministradorNuevo).not.toHaveBeenCalled();
    expect(c.pasoAlta()).toBe(3);
    expect(c.empresaNueva()).toBe('Empresa Rechazada');
    expect(c.error()).toBeTruthy();
    expect(c.ocupado()).toBe(false);
  });

  it('P1014E flujo positivo crea empresa y administrador nuevo con una sola confirmación', () => {
    const f = vista(true), c = f.componentInstance;
    c.empresaNueva.set('Empresa Positiva');
    c.avanzarAlta();
    c.modoAlta.set('nuevo');
    c.adminEmpresaUsuarioNuevo.set('admin.positivo');
    c.adminEmpresaNombreNuevo.set('Administrador Positivo');
    c.adminEmpresaClaveNueva.set('ClaveSegura!2026');
    c.avanzarAlta();
    f.detectChanges();
    expect(c.pasoAlta()).toBe(3);

    const confirmar = Array.from((f.nativeElement as HTMLElement).querySelectorAll('button'))
      .find(x => x.textContent?.includes('Confirmar alta empresarial'));
    expect(confirmar).toBeDefined();
    expect(confirmar!.disabled).toBe(false);
    confirmar!.click();
    f.detectChanges();

    expect(api.altaControladaConAdministradorNuevo).toHaveBeenCalledOnce();
    expect(api.altaControladaConAdministradorNuevo).toHaveBeenCalledWith('Empresa Positiva', {
      nombreUsuario: 'admin.positivo',
      nombreCompleto: 'Administrador Positivo',
      password: 'ClaveSegura!2026'
    });
    expect(api.altaControlada).not.toHaveBeenCalled();
    expect(c.pasoAlta()).toBe(1);
    expect(c.empresaNueva()).toBe('');
    expect(c.adminEmpresaClaveNueva()).toBe('');
    expect(c.buscado()?.nombreUsuario).toBe('admin.positivo');
    expect(c.exito()).toContain('creados de forma atómica');
  });

  it('P1014B no anuncia éxito mientras la respuesta HTTP sigue pendiente y rechaza error tardío', () => {
    // El servidor solo puede responder OK después de confirmar su transacción.
    // Esta prueba de cliente NO pretende simular ni certificar un COMMIT MySQL.
    const respuesta = new Subject<{ empresaId: number; administradorUsuarioId: number }>();
    api.altaControlada.mockReturnValueOnce(respuesta.asObservable());
    const f = vista(true), c = f.componentInstance;
    const refrescos = vi.fn();
    const suscripcion = c.actualizarDirectorio.subscribe(refrescos);
    c.empresaNueva.set('Empresa Commit Pendiente');
    c.avanzarAlta();
    c.buscado.set({ id: 7, nombreUsuario: 'admin', nombreCompleto: 'Admin',
      activo: true, bloqueado: false, pendienteAsignacion: false });
    c.avanzarAlta();
    expect(c.pasoAlta()).toBe(3);
    c.crearEmpresa();
    f.detectChanges();
    expect(api.altaControlada).toHaveBeenCalledOnce();
    expect(c.ocupado()).toBe(true);
    expect(c.exito()).toBeNull();
    expect(c.pasoAlta()).toBe(3);
    expect(refrescos).not.toHaveBeenCalled();

    // Simula respuesta tardía de error/rollback del backend, no un COMMIT.
    respuesta.error({ status: 500, error: { message: 'Transacción no confirmada' } });
    f.detectChanges();
    expect(c.ocupado()).toBe(false);
    expect(c.exito()).toBeNull();
    expect(c.error()).toBeTruthy();
    expect(c.pasoAlta()).toBe(3);
    expect(c.empresaNueva()).toBe('Empresa Commit Pendiente');
    expect(refrescos).not.toHaveBeenCalled();
    suscripcion.unsubscribe();
  });

  it('P1014B emite éxito solo después de respuesta confirmada, exactamente una vez', () => {
    const respuesta = new Subject<{ empresaId: number; administradorUsuarioId: number }>();
    api.altaControlada.mockReturnValueOnce(respuesta.asObservable());
    const f = vista(true), c = f.componentInstance;
    const refrescos = vi.fn();
    const suscripcion = c.actualizarDirectorio.subscribe(refrescos);
    c.empresaNueva.set('Empresa Commit Confirmado');
    c.avanzarAlta();
    c.buscado.set({ id: 7, nombreUsuario: 'admin', nombreCompleto: 'Admin',
      activo: true, bloqueado: false, pendienteAsignacion: false });
    c.avanzarAlta();
    c.crearEmpresa();
    expect(c.exito()).toBeNull();
    expect(refrescos).not.toHaveBeenCalled();
    respuesta.next({ empresaId: 91, administradorUsuarioId: 7 });
    respuesta.complete();
    f.detectChanges();
    expect(c.ocupado()).toBe(false);
    expect(c.exito()).toContain('Empresa creada con administrador existente. ID: 91');
    expect(c.pasoAlta()).toBe(1);
    expect(refrescos).toHaveBeenCalledOnce();
    suscripcion.unsubscribe();
  });

});

describe('C00-02.M04.01 - ruta y pantalla contractual privada', () => {
  const ruta = routes.find((item) => item.path === 'plataforma/flujo-v5');

  it('mantiene la comprobacion global server-side y el componente lazy existente', async () => {
    expect(ruta?.canActivate).toEqual([plataformaGuard]);
    expect(ruta?.data?.['modulo']).toBeUndefined();
    const componente = await (ruta?.loadComponent as () => Promise<unknown>)();
    expect(componente).toBe(PlataformaComponent);
  });

  it('solo ofrece lectura y no solicita directorio ni privilegios del tenant', async () => {
    const listar = vi.fn();
    await TestBed.configureTestingModule({
      imports: [PlataformaComponent, NoopAnimationsModule],
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { routeConfig: { path: 'plataforma/flujo-v5' } } } },
        { provide: PlataformaSesionService, useValue: { sesion: signal({
          nombreUsuario: 'plataforma-test', rolNombre: 'Super Admin', ambito: 'Plataforma', esAdministrador: true
        }), obtener: vi.fn() } },
        { provide: PlataformaEmpresasService, useValue: { listar } },
        { provide: Router, useValue: { navigate: vi.fn() } }
      ]
    }).compileComponents();
    const fixture = TestBed.createComponent(PlataformaComponent);
    fixture.detectChanges();
    const texto = String(fixture.nativeElement.textContent);
    expect(texto).toContain('De la solicitud a la evidencia DEV');
    expect(texto).toContain('Sin operaciones');
    expect(texto).toContain('No consulta PLAN_MAESTRO');
    expect(texto).not.toContain('Directorio global de empresas');
    expect(listar).not.toHaveBeenCalled();
  });
});

describe('C00-03.M04.01 - estado vacio honesto de cobertura', () => {
  it('no consulta directorio ni inventa evidencia para la cadena requisito a prueba', async () => {
    const listar = vi.fn();
    await TestBed.configureTestingModule({
      imports: [PlataformaComponent, NoopAnimationsModule],
      providers: [
        { provide: ActivatedRoute, useValue: { snapshot: { routeConfig: { path: 'plataforma/cobertura-v5' } } } },
        { provide: PlataformaSesionService, useValue: { sesion: signal({
          nombreUsuario: 'plataforma-test', rolNombre: 'Super Admin', ambito: 'Plataforma', esAdministrador: true
        }), obtener: vi.fn() } },
        { provide: PlataformaEmpresasService, useValue: { listar } },
        { provide: Router, useValue: { navigate: vi.fn() } }
      ]
    }).compileComponents();
    const fixture = TestBed.createComponent(PlataformaComponent);
    fixture.detectChanges();
    const texto = String(fixture.nativeElement.textContent);
    expect(texto).toContain('Cadena de trazabilidad');
    expect(texto).toContain('Sin datos de cobertura verificados');
    expect(texto).toContain('Sin escritura');
    expect(texto).not.toContain('Directorio global de empresas');
    expect(listar).not.toHaveBeenCalled();
  });
});
