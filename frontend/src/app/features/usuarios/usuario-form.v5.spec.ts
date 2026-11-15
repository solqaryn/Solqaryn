import { TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, Subject } from 'rxjs';
import { vi } from 'vitest';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { RolService } from '../../services/rol.service';
import { UsuarioService } from '../../services/usuario.service';
import { UsuarioFormComponent } from './usuario-form.component';

/** C01-01.M04.04: pruebas de la superficie privada, sin identidad/permiso simulados como autoridad backend. */
describe('V5 C01-01.M04.04 formularios y doble accion en Usuarios', () => {
  let pendiente: Subject<unknown>;
  const usuarioApi = {
    getById: vi.fn(() => of({ data: {
      id: 7, nombreUsuario: 'usuario7', nombreCompleto: 'Usuario Siete',
      rolId: 5, rol: 'Vendedor', rolNombre: 'Vendedor'
    } })),
    update: vi.fn(() => pendiente.asObservable())
  };
  const rolesApi = { getAll: vi.fn(() => of({ data: [] })) };
  const permisos = { puede: vi.fn((_modulo: string, _accion: string) => false) };
  const router = { navigate: vi.fn() };
  const snackBar = { open: vi.fn() };
  const ruta = { snapshot: { paramMap: convertToParamMap({ id: '7' }) } };

  beforeEach(() => {
    vi.clearAllMocks();
    pendiente = new Subject<unknown>();
    ruta.snapshot.paramMap = convertToParamMap({ id: '7' });
    TestBed.configureTestingModule({
      providers: [
        FormBuilder,
        { provide: PermisosRuntimeService, useValue: permisos }
      ]
    });
  });

  function crear(): UsuarioFormComponent {
    const componente = TestBed.runInInjectionContext(() => new UsuarioFormComponent(
      ruta as unknown as ActivatedRoute,
      router as unknown as Router,
      usuarioApi as unknown as UsuarioService,
      rolesApi as unknown as RolService,
      snackBar as unknown as MatSnackBar
    ));
    componente.ngOnInit();
    return componente;
  }

  it('deniega formulario inválido sin mutación ni navegación', () => {
    const c = crear();
    c.form.controls.nombreUsuario.setValue('x');
    c.guardar();
    expect(c.form.invalid).toBe(true);
    expect(usuarioApi.update).not.toHaveBeenCalled();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('conserva rol previo y omite contraseña cuando no hay permisos de acción', () => {
    const c = crear();
    expect(c.puedeAsignarRol()).toBe(false);
    expect(c.puedeRestablecerPassword()).toBe(false);
    c.form.controls.rolId.setValue(999);
    c.form.controls.nuevaPassword.setValue('ClaveNoAutorizada!2026');
    c.guardar();
    expect(usuarioApi.update).toHaveBeenCalledTimes(1);
    expect(usuarioApi.update).toHaveBeenCalledWith(7, expect.objectContaining({
      rol: 'Vendedor', rolId: 5, nuevaPassword: undefined
    }));
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('impide doble envío hasta respuesta; solo navega ante éxito confirmado', () => {
    const c = crear();
    c.guardar();
    c.guardar();
    expect(usuarioApi.update).toHaveBeenCalledTimes(1);
    expect(c.saving()).toBe(true);
    expect(router.navigate).not.toHaveBeenCalled();
    expect(snackBar.open).not.toHaveBeenCalled();
    pendiente.next({ success: true });
    pendiente.complete();
    expect(c.saving()).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(['/usuarios', 7]);
    expect(snackBar.open).toHaveBeenCalledTimes(1);
  });

  it('403 mantiene el formulario y no repite automáticamente; permite reintento consciente', () => {
    const c = crear();
    c.guardar();
    pendiente.error({ status: 403, error: { message: 'No autorizado' } });
    expect(c.saving()).toBe(false);
    expect(c.errorMessage()).toBe('No autorizado');
    expect(router.navigate).not.toHaveBeenCalled();
    expect(usuarioApi.update).toHaveBeenCalledTimes(1);
    pendiente = new Subject<unknown>();
    c.guardar();
    expect(usuarioApi.update).toHaveBeenCalledTimes(2);
    expect(c.saving()).toBe(true);
  });

  it('ID de ruta inválido no consulta ni escribe usuario', () => {
    ruta.snapshot.paramMap = convertToParamMap({ id: 'invalido' });
    const c = crear();
    expect(c.loading()).toBe(true);
    expect(usuarioApi.getById).not.toHaveBeenCalled();
    expect(usuarioApi.update).not.toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/usuarios']);
  });
});
