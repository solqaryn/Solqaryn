import { NavigationEnd } from '@angular/router';
import { Subject, of } from 'rxjs';
import { AppComponent } from './app.component';

type ConstructorArgs = ConstructorParameters<typeof AppComponent>;
type RespuestaTienda = { tema: { colorPrimario: string } } | null;

function crearEscenario(respuestas: Array<Subject<RespuestaTienda>>, opciones: {
  autenticado?: boolean; tenantVerificado?: boolean; urlInicial?: string;
} = {}) {
  const navegacion = new Subject<NavigationEnd>();
  const temas: string[] = [];
  const seo: string[] = [];
  let consulta = 0;
  let plataformas = 0;
  let temasTenant = 0;
  let identidadesPlataforma = 0;

  const app = new AppComponent(
    { isAuthenticated: () => opciones.autenticado === true } as unknown as ConstructorArgs[0],
    { limpiar: () => undefined, cargar: () => of(null) } as unknown as ConstructorArgs[1],
    { usarPlataforma: () => { identidadesPlataforma += 1; }, cargar: () => of(null) } as unknown as ConstructorArgs[2],
    { detener: () => undefined, iniciar: () => undefined } as unknown as ConstructorArgs[3],
    { tieneContextoVerificado: () => opciones.tenantVerificado === true } as unknown as ConstructorArgs[4],
    { url: opciones.urlInicial ?? '/tienda', events: navegacion.asObservable() } as unknown as ConstructorArgs[5],
    {
      aplicar: (tema: { colorPrimario: string }) => temas.push(tema.colorPrimario),
      aplicarTemaGuardado: () => { temasTenant += 1; },
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
    contarTemasTenant: () => temasTenant,
    contarIdentidadesPlataforma: () => identidadesPlataforma,
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

describe('C00-02.M04.03 — branding aislado del control plane V5', () => {
  it('al iniciar en /plataforma/flujo-v5 borra tema de empresa aunque exista contexto tenant verificado', () => {
    const caso = crearEscenario([], { autenticado: true, tenantVerificado: true, urlInicial: '/plataforma/flujo-v5' });
    expect(caso.contarPlataformas()).toBe(1);
    expect(caso.contarIdentidadesPlataforma()).toBe(1);
    expect(caso.contarTemasTenant()).toBe(0);
    expect(caso.seo).toEqual(['NO_INDEX']);
    caso.app.ngOnDestroy();
  });

  it('conserva branding dinamico en ruta empresarial y lo retira al volver a cobertura V5', () => {
    const caso = crearEscenario([], { autenticado: true, tenantVerificado: true, urlInicial: '/plataforma' });
    caso.navegar('/dashboard');
    expect(caso.contarTemasTenant()).toBe(1);
    caso.navegar('/plataforma/cobertura-v5?from=erp');
    expect(caso.contarPlataformas()).toBe(2);
    expect(caso.contarTemasTenant()).toBe(1);
    expect(caso.contarIdentidadesPlataforma()).toBe(2);
    caso.app.ngOnDestroy();
  });

  it('no confunde prefijos ajenos con plataforma y conserva la politica empresarial normal', () => {
    const caso = crearEscenario([], { autenticado: true, tenantVerificado: true, urlInicial: '/plataforma-externa' });
    expect(caso.contarPlataformas()).toBe(0);
    expect(caso.contarTemasTenant()).toBe(1);
    caso.navegar('/plataforma/');
    expect(caso.contarPlataformas()).toBe(1);
    caso.app.ngOnDestroy();
  });
});
