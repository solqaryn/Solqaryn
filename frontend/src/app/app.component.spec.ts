import { NavigationEnd } from '@angular/router';
import { Subject } from 'rxjs';
import { AppComponent } from './app.component';

type ConstructorArgs = ConstructorParameters<typeof AppComponent>;
type RespuestaTienda = { tema: { colorPrimario: string } } | null;

function crearEscenario(respuestas: Array<Subject<RespuestaTienda>>) {
  const navegacion = new Subject<NavigationEnd>();
  const temas: string[] = [];
  const seo: string[] = [];
  let consulta = 0;
  let plataformas = 0;

  const app = new AppComponent(
    { isAuthenticated: () => false } as unknown as ConstructorArgs[0],
    { limpiar: () => undefined } as unknown as ConstructorArgs[1],
    { usarPlataforma: () => undefined } as unknown as ConstructorArgs[2],
    { detener: () => undefined } as unknown as ConstructorArgs[3],
    { tieneContextoVerificado: () => false } as unknown as ConstructorArgs[4],
    { url: '/tienda', events: navegacion.asObservable() } as unknown as ConstructorArgs[5],
    {
      aplicar: (tema: { colorPrimario: string }) => temas.push(tema.colorPrimario),
      aplicarTemaGuardado: () => undefined,
      aplicarTemaPlataforma: () => { plataformas += 1; }
    } as unknown as ConstructorArgs[6],
    {
      aplicarRuta: (url: string) => seo.push(url),
      aplicarNoIndex: () => seo.push('NO_INDEX')
    } as unknown as ConstructorArgs[7],
    {
      cargar: () => {
        const respuesta = respuestas[consulta++];
        if (!respuesta) throw new Error('No hay respuesta de tienda para esta ruta.');
        return respuesta.asObservable();
      },
      config: () => ({ nombreComercial: 'Tienda de prueba' })
    } as unknown as ConstructorArgs[8],
    { start: () => undefined } as unknown as ConstructorArgs[9],
    document
  );

  return {
    app,
    temas,
    seo,
    contarPlataformas: () => plataformas,
    navegar: (url: string) => navegacion.next(new NavigationEnd(consulta, url, url))
  };
}

describe('V5 C00-01.M05.01 — contexto de ruta frente a respuestas tardías', () => {
  it('impide que tienda A reponga tema o SEO al entrar en /login', () => {
    const respuestaA = new Subject<RespuestaTienda>();
    const caso = crearEscenario([respuestaA]);

    caso.navegar('/login');
    expect(caso.contarPlataformas()).toBe(1);
    respuestaA.next({ tema: { colorPrimario: '#111111' } });

    expect(caso.temas).toEqual([]);
    expect(caso.seo).toEqual(['NO_INDEX']);
    caso.app.ngOnDestroy();
  });

  it('conserva el tema de la nueva ruta B frente a la respuesta antigua A', () => {
    const respuestaA = new Subject<RespuestaTienda>();
    const respuestaB = new Subject<RespuestaTienda>();
    const caso = crearEscenario([respuestaA, respuestaB]);

    caso.navegar('/tienda/productos');
    respuestaB.next({ tema: { colorPrimario: '#222222' } });
    respuestaA.next({ tema: { colorPrimario: '#111111' } });

    expect(caso.temas).toEqual(['#222222']);
    expect(caso.seo).toEqual(['/tienda/productos']);
    caso.app.ngOnDestroy();
  });

  it('descarta una respuesta que llega después de destruir el shell', () => {
    const respuestaA = new Subject<RespuestaTienda>();
    const caso = crearEscenario([respuestaA]);

    caso.app.ngOnDestroy();
    respuestaA.next({ tema: { colorPrimario: '#111111' } });

    expect(caso.temas).toEqual([]);
    expect(caso.seo).toEqual([]);
  });
});
