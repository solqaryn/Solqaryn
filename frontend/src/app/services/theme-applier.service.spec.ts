import { Subject } from 'rxjs';
import { ApiResponse } from '../core/models/api-response.model';
import { TemaVisual } from '../core/models/tema-visual.model';
import { ColorContrastService } from './color-contrast.service';
import { TemaVisualService } from './tema-visual.service';
import { ThemeApplierService } from './theme-applier.service';

describe('QA — contrato visual empresarial', () => {
  const tema: TemaVisual = {
    colorPrimario: '#245b49', colorSecundario: '#204a3a', colorAcento: '#996b22',
    fondoPrincipal: '#f4eee4', fondoTarjetas: '#e7f0e8', menuLateral: '#182c25',
    barraSuperior: '#14251e', encabezados: '#182c25', botonesPrincipales: '#245b49',
    textoPrincipal: '#182c25', textoSecundario: '#354c40', colorExito: '#166534',
    colorAdvertencia: '#a16207', colorError: '#b91c1c', colorInformacion: '#1d4ed8'
  };
  const contraste = new ColorContrastService();
  let applier: ThemeApplierService;
  let solicitudes: Subject<ApiResponse<TemaVisual>>[];
  let original: string | null;
  const token = (name: string) => document.documentElement.style.getPropertyValue(name);
  const respuesta = (data: TemaVisual) => ({ success: true, data, message: '', errors: [] });

  beforeEach(() => {
    original = document.documentElement.getAttribute('style');
    solicitudes = [];
    applier = new ThemeApplierService({ get: () => {
      const solicitud = new Subject<ApiResponse<TemaVisual>>();
      solicitudes.push(solicitud);
      return solicitud;
    } } as unknown as TemaVisualService, contraste);
  });
  afterEach(() => {
    applier.aplicarTemaPlataforma();
    if (original === null) document.documentElement.removeAttribute('style');
    else document.documentElement.setAttribute('style', original);
  });

  it('conserva los fondos configurados y calcula el contraste sobre la barra superior real', () => {
    applier.aplicar(tema);
    expect(token('--color-bg')).toBe(tema.fondoPrincipal);
    expect(token('--color-surface')).toBe(tema.fondoTarjetas);
    expect(token('--color-topbar')).toBe(tema.barraSuperior);
    expect(contraste.contrastBetween(token('--color-on-topbar'), tema.barraSuperior)).toBeGreaterThanOrEqual(4.5);
    expect(contraste.contrastBetween(token('--color-on-primary'), tema.botonesPrincipales)).toBeGreaterThanOrEqual(4.5);
    applier.aplicar({ ...tema, barraSuperior: '#ffffff', fondoTarjetas: '' });
    expect(token('--color-surface')).toBe('');
    expect(contraste.contrastBetween(token('--color-on-topbar'), '#ffffff')).toBeGreaterThanOrEqual(4.5);
    applier.aplicarTemaPlataforma();
    expect(token('--color-on-topbar')).toBe('');
  });

  it('una carga anterior no restaura el tema después de un cambio más reciente', () => {
    applier.aplicarTemaGuardado();
    applier.aplicarTemaGuardado();
    const reciente = { ...tema, fondoTarjetas: '#e2e8f0' };
    solicitudes[1].next(respuesta(reciente));
    solicitudes[0].next(respuesta(tema));
    expect(token('--color-surface')).toBe(reciente.fondoTarjetas);
    applier.aplicarTemaGuardado();
    applier.aplicarTemaPlataforma();
    solicitudes[2].next(respuesta(tema));
    expect(token('--color-surface')).toBe('');
  });

  it('no aplica una respuesta rechazada por la API', () => {
    applier.aplicar(tema);
    applier.aplicarTemaGuardado();
    solicitudes[0].next({ ...respuesta({ ...tema, fondoTarjetas: '#eeeeee' }), success: false });
    expect(token('--color-surface')).toBe(tema.fondoTarjetas);
  });
});
