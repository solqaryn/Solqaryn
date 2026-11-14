import { Subject, of } from 'rxjs';
import { ThemeApplierService } from './theme-applier.service';
import { TemaVisualService } from './tema-visual.service';
import { ColorContrastService } from './color-contrast.service';
import { TemaVisual } from '../core/models/tema-visual.model';

const BASE: TemaVisual = {
  colorPrimario: '#123456',
  colorSecundario: '#234567',
  colorAcento: '#345678',
  fondoPrincipal: '#ffffff',
  fondoTarjetas: '#ffffff',
  menuLateral: '#102030',
  barraSuperior: '#123456',
  encabezados: '#121212',
  botonesPrincipales: '#123456',
  textoPrincipal: '#1f2937',
  textoSecundario: '#374151',
  colorExito: '#14532d',
  colorAdvertencia: '#854d0e',
  colorError: '#991b1b',
  colorInformacion: '#1d4ed8'
};

function crearTema(get: () => ReturnType<TemaVisualService['get']>): ThemeApplierService {
  return new ThemeApplierService(
    { get } as TemaVisualService,
    new ColorContrastService()
  );
}

describe('V5 C00-01.M04.03 - branding de tenant seguro', () => {
  afterEach(() => {
    const servicio = crearTema(() => of({ success: true, data: BASE, message: '', errors: [] }));
    servicio.aplicarTemaPlataforma();
  });

  it('aplica únicamente colores permitidos y calcula contraste legible', () => {
    const servicio = crearTema(() => of({ success: true, data: BASE, message: '', errors: [] }));
    servicio.aplicarTemaGuardado();

    const root = document.documentElement.style;
    expect(root.getPropertyValue('--color-primary')).toBe('#123456');
    expect(root.getPropertyValue('--color-sidebar')).toBe('#102030');
    expect(root.getPropertyValue('--color-on-primary')).toBe(
      new ColorContrastService().foregroundFor('#123456')
    );
  });

  it('descarta CSS no permitido y elimina tokens del tenant anterior', () => {
    const servicio = crearTema(() => of({ success: true, data: BASE, message: '', errors: [] }));
    servicio.aplicar({ ...BASE, colorPrimario: '#abcdef', menuLateral: '#102030' });
    servicio.aplicar({
      ...BASE,
      colorPrimario: 'url(https://invalid.example/track)',
      menuLateral: 'var(--otro-tenant)',
      textoPrincipal: 'red'
    });

    const root = document.documentElement.style;
    expect(root.getPropertyValue('--color-primary')).toBe('');
    expect(root.getPropertyValue('--color-sidebar')).toBe('');
    expect(root.getPropertyValue('--color-on-sidebar')).toBe('');
    expect(root.getPropertyValue('--color-text')).toBe('');
  });

  it('no repone tema A si el usuario regresó a plataforma', () => {
    const pendienteA = new Subject<{ success: boolean; data: TemaVisual; message: string; errors: string[] }>();
    const servicio = crearTema(() => pendienteA.asObservable());
    servicio.aplicarTemaGuardado();
    servicio.aplicarTemaPlataforma();
    pendienteA.next({ success: true, data: BASE, message: '', errors: [] });

    expect(document.documentElement.style.getPropertyValue('--color-primary')).toBe('');
  });

  it('mantiene branding B ante una respuesta atrasada de A', () => {
    const a = new Subject<{ success: boolean; data: TemaVisual; message: string; errors: string[] }>();
    const b = new Subject<{ success: boolean; data: TemaVisual; message: string; errors: string[] }>();
    let turnos = 0;
    const servicio = crearTema(() => (++turnos === 1 ? a : b).asObservable());
    servicio.aplicarTemaGuardado();
    servicio.aplicarTemaGuardado();
    b.next({ success: true, data: { ...BASE, colorPrimario: '#abcdef' }, message: '', errors: [] });
    a.next({ success: true, data: BASE, message: '', errors: [] });

    expect(document.documentElement.style.getPropertyValue('--color-primary')).toBe('#abcdef');
  });
});
