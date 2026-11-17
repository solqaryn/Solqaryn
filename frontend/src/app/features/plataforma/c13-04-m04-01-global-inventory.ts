import type { Routes } from '@angular/router';

/** C13-04.M04.01: fuente única del inventario son los Route[] registrados por Angular.
 * La lista de 16 programas corresponde a PLAN_MAESTRO C00–C15, no afirma implementación.
 */
export const PROGRAMAS_PLAN_V19 = Object.freeze([
  { id: 'C00', nombre: 'Gobernanza, baseline y continuidad' },
  { id: 'C01', nombre: 'Núcleo universal de plataforma' },
  { id: 'C02', nombre: 'Tenancy avanzado y control plane' },
  { id: 'C03', nombre: 'Identidad, seguridad y privacidad' },
  { id: 'C04', nombre: 'SaaS billing, contratos y suscripciones' },
  { id: 'C05', nombre: 'Builder, design system y configuración segura' },
  { id: 'C06', nombre: 'Landing pages, sitios públicos y experiencia de cliente' },
  { id: 'C07', nombre: 'Catálogo, comercio y continuidad ERP' },
  { id: 'C08', nombre: 'Extensiones sectoriales y recursos' },
  { id: 'C09', nombre: 'CRM, fidelización, marketing y mensajería' },
  { id: 'C10', nombre: 'Archivos, Cloudflare R2 y resiliencia' },
  { id: 'C11', nombre: 'Datos, API, eventos y observabilidad' },
  { id: 'C12', nombre: 'Arquitectura Hostinger y migración de proveedores' },
  { id: 'C13', nombre: 'Legal, accesibilidad, rendimiento y experiencia ligera' },
  { id: 'C14', nombre: 'Aceptación, release y salida comercial' },
  { id: 'C15', nombre: 'Cinco auditorías y reconciliación continua' }
] as const);

export interface RutaInventariadaV19 {
  readonly id: string;
  readonly path: string;
  readonly superficie: 'plataforma' | 'erp' | 'publica';
  readonly tipo: 'detalle' | 'edicion' | 'creacion' | 'lista_o_pantalla' | 'redirect' | 'comodin' | 'matcher';
  readonly protegidaPorRuta: boolean;
  readonly rutaNavegable: string | null;
  readonly hijosLazySinResolver: boolean;
}

/** Enumeración exhaustiva de entradas registradas, incluidas children y spreads importados.
 * No ejecuta lazy loaders ni eleva privilegios; los subárboles no cargados quedan explícitos.
 */
export function inventariarRutasV19(rutas: Routes): readonly RutaInventariadaV19[] {
  const resultado: RutaInventariadaV19[] = [];
  function caminar(entradas: Routes, base: string, guardPadre: boolean): void {
    for (const entrada of entradas) {
      const segmento = typeof entrada.path === 'string' ? entrada.path : '[matcher]';
      const path = [base, segmento].filter(Boolean).join('/').replace(/\/{2,}/g, '/');
      const dinamica = path.includes(':') || path.includes('*') || segmento === '[matcher]';
      const redirect = entrada.redirectTo !== undefined;
      const esEdicion = /(?:^|\/)(?:editar|edicion)(?:\/|$)/i.test(path);
      const esDetalle = /:(?:id|slug|key|storeKey)(?:\/|$)/i.test(path);
      const tipo: RutaInventariadaV19['tipo'] =
        segmento === '**' ? 'comodin' : segmento === '[matcher]' ? 'matcher' :
        redirect ? 'redirect' : esEdicion ? 'edicion' : esDetalle ? 'detalle' :
        /(?:^|\/)(?:nuevo|nueva|crear)(?:\/|$)/i.test(path) ? 'creacion' : 'lista_o_pantalla';
      const protegida = guardPadre || !!entrada.canActivate?.length || !!entrada.canMatch?.length;
      const publica = /^(tienda|tiendas|login)(?:\/|$)/.test(path) || path === '';
      const plataforma = /^plataforma(?:\/|$)/.test(path);
      // Enlaces exclusivamente a rutas estáticas declaradas; ningún :id inventado ni esquema remoto.
      const linkValido = !dinamica && !redirect && path.length > 0 && /^[a-zA-Z0-9/_-]+$/.test(path);
      resultado.push(Object.freeze({
        id: String(resultado.length + 1),
        path: path || '(raíz)',
        superficie: plataforma ? 'plataforma' : publica ? 'publica' : 'erp',
        tipo,
        protegidaPorRuta: protegida,
        rutaNavegable: linkValido ? '/' + path : null,
        hijosLazySinResolver: typeof entrada.loadChildren === 'function'
      }));
      if (entrada.children?.length) caminar(entrada.children, path, protegida);
    }
  }
  caminar(rutas, '', false);
  return Object.freeze(resultado);
}
