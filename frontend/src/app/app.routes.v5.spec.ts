import { Routes } from '@angular/router';
import { plataformaGuard } from './core/guards/plataforma.guard';
import { PlataformaComponent } from './features/plataforma/plataforma.component';
import { routes } from './app.routes';

describe('C00-02.M04.01 - superficie privada de lectura V5', () => {
  const ruta = routes.find((item) => item.path === 'plataforma/flujo-v5');

  it('mantiene guard de plataforma validado en servidor', () => {
    expect(ruta).toBeDefined();
    expect(ruta?.canActivate).toEqual([plataformaGuard]);
    expect(ruta?.path).not.toBe('tienda/flujo-v5');
  });

  it('no expone acciones operativas ni un nuevo endpoint en la ruta', () => {
    expect(ruta?.data?.['modulo']).toBeUndefined();
    expect(ruta?.children).toBeUndefined();
    expect(ruta?.redirectTo).toBeUndefined();
  });

  it('lazy-load reutiliza pantalla validada sin inventar un componente fuera del catalogo', async () => {
    const loader = ruta?.loadComponent;
    expect(typeof loader).toBe('function');
    const cargado = await (loader as NonNullable<Routes[number]['loadComponent']>)!();
    expect(cargado).toBe(PlataformaComponent);
  });
});
