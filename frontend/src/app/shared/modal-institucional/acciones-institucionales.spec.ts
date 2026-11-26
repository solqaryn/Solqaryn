import { TestBed } from '@angular/core/testing';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { AppAlertService } from '../alerts/app-alert.service';
import { crearAccionesInstitucionales } from './acciones-institucionales';

describe('Acciones institucionales', () => {
  let contexto: object | null;
  let autorizado: boolean;
  let confirmar: ReturnType<typeof vi.fn>;
  beforeEach(() => {
    contexto = { empresaId: 1, usuarioId: 2, rolId: 3 };
    autorizado = true;
    confirmar = vi.fn(async () => true);
    TestBed.configureTestingModule({ providers: [
      { provide: TenantContextService, useValue: { contextoVerificado: () => contexto } },
      { provide: PermisosRuntimeService, useValue: { puede: () => autorizado } },
      { provide: AppAlertService, useValue: { confirmar } }
    ] });
  });
  const control = () => TestBed.runInInjectionContext(() => crearAccionesInstitucionales('Clientes'));
  const datos = { titulo: 'Guardar', mensaje: 'Confirmar cambios' };
  it('solo confirma una aceptación explícita', async () => {
    const c = control();
    confirmar.mockResolvedValue(false);
    expect(await c.confirmar('Editar', datos)).toBe(false);
    confirmar.mockResolvedValue(true);
    expect(await c.confirmar('Editar', datos)).toBe(true);
  });
  it('no abre alerta sin contexto o autorización', async () => {
    const c = control();
    autorizado = false;
    expect(await c.confirmar('Editar', datos)).toBe(false);
    autorizado = true;
    contexto = null;
    expect(await c.confirmar('Editar', datos)).toBe(false);
    expect(confirmar).not.toHaveBeenCalled();
  });
  it('rechaza cambio de empresa y revocación de permiso mientras se confirma', async () => {
    const c = control();
    confirmar.mockImplementationOnce(async () => { contexto = { empresaId: 9 }; return true; });
    expect(await c.confirmar('Editar', datos)).toBe(false);
    confirmar.mockImplementationOnce(async () => { autorizado = false; return true; });
    expect(await c.confirmar('Editar', datos)).toBe(false);
  });
});
