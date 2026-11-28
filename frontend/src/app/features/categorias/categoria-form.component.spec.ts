import { TestBed } from '@angular/core/testing';
import { FormBuilder } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, of } from 'rxjs';
import { TenantContextService } from '../../core/auth/tenant-context.service';
import { PermisosRuntimeService } from '../../core/auth/permisos-runtime.service';
import { CategoriaService } from '../../services/categoria.service';
import { AppAlertService } from '../../shared/alerts/app-alert.service';
import { CategoriaFormComponent } from './categoria-form.component';

describe('CategoriaFormComponent: hidratación y guardado de imágenes', () => {
  const contexto = { empresaId: 1, usuarioId: 2, rolId: 3 };
  const datos = { id: 41, nombre: 'Categoría QA', descripcion: '', activa: true };
  const version = 'a'.repeat(32);
  const foto = () => new File(['imagen'], 'foto.webp', { type: 'image/webp' });
  function evento(archivo: File): Event {
    return { target: { files: [archivo], value: '' } } as unknown as Event;
  }
  function preparar() {
    const lectura = new Subject<{ data: { version: string; imagenes: [] } }>();
    const navegar = vi.fn(async () => true);
    const guardarGaleria = vi.fn((_id: number, _version: string, _orden: string[], _archivos: File[]) =>
      of({ success: true, data: { version: 'b'.repeat(32), imagenes: [{ id: 'c'.repeat(32), url: 'https://example.invalid/foto.webp' }] } }));
    TestBed.configureTestingModule({
      providers: [
        FormBuilder,
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: { get: () => '41' } } } },
        { provide: Router, useValue: { navigate: navegar } },
        { provide: CategoriaService, useValue: {
          getById: () => of({ data: datos }), obtenerGaleria: () => lectura,
          update: () => of({ data: datos }), guardarGaleria
        } },
        { provide: PermisosRuntimeService, useValue: { puede: () => true } },
        { provide: TenantContextService, useValue: { contextoVerificado: () => contexto, empresaIdVerificada: () => 1 } },
        { provide: AppAlertService, useValue: { confirmar: async () => true } }
      ]
    });
    const componente = TestBed.runInInjectionContext(() => new CategoriaFormComponent(
      TestBed.inject(CategoriaService), TestBed.inject(ActivatedRoute), TestBed.inject(Router)));
    componente.ngOnInit();
    const completar = () => { lectura.next({ data: { version, imagenes: [] } }); lectura.complete(); };
    return { componente, completar, guardarGaleria, navegar };
  }
  beforeEach(() => {
    class UrlPrueba extends URL {
      static override createObjectURL = vi.fn(() => 'blob:solqaryn-qa');
      static override revokeObjectURL = vi.fn();
    }
    vi.stubGlobal('URL', UrlPrueba);
  });
  afterEach(() => vi.unstubAllGlobals());

  it('bloquea el input hasta completar ambas lecturas y conserva la selección posterior', async () => {
    const { componente, completar, guardarGaleria, navegar } = preparar();
    expect(componente.loading()).toBe(true);
    await componente.seleccionarImagenes(evento(foto()));
    expect(componente.imagenes()).toHaveLength(0);
    completar();
    expect(componente.loading()).toBe(false);
    const archivo = foto();
    await componente.seleccionarImagenes(evento(archivo));
    expect(componente.imagenes()).toHaveLength(1);
    await componente.submit();
    expect(guardarGaleria).toHaveBeenCalledExactlyOnceWith(41, version, ['n:0'], [archivo]);
    expect(navegar).toHaveBeenCalledExactlyOnceWith(['/categorias']);
    componente.ngOnDestroy();
  });

  it('no cierra el formulario si la API devuelve una galería incompleta', async () => {
    const { componente, completar, guardarGaleria, navegar } = preparar();
    completar();
    await componente.seleccionarImagenes(evento(foto()));
    guardarGaleria.mockReturnValueOnce(of({ success: true, data: { version: 'b'.repeat(32), imagenes: [] } }));
    await componente.submit();
    expect(guardarGaleria).toHaveBeenCalledTimes(1);
    expect(navegar).not.toHaveBeenCalled();
    expect(componente.errorMessage()).toContain('No se confirmó la galería completa');
    expect(componente.imagenes()).toHaveLength(1);
    expect(componente.saving()).toBe(false);
    componente.ngOnDestroy();
  });
});
