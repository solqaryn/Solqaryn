# P5-014.C - Delimitación causal de teclado y ARIA en menú (DEV)

- Autoridad: Plan Comercial Inicial V3 P5-014.C, origen P5-014.A. Micro-paso de una acción verificable: identificar un único archivo o escenario y conservar ruta/SHA/cambio mínimo.
- TARGET_PATH de producto (solo lectura): `frontend/src/app/shared/navigation/app-navigation-menu.component.ts`.
- SOURCE_BLOB_SHA: `b70ef4ffb252228cf2ab19754a5e30d53f64aca0` comprobado en `dev` al preparar y auditar.
- Alcance delimitado: etiquetas `aria-labelledby` de secciones, enlaces nativos con `routerLink` y `ariaCurrentWhenActive=page`, enfoque visible, blancos min-height 46px, iconos decorativos `aria-hidden`, control de visibilidad por permisos (`Administrar`) y auditoría de administrador.
- Implementación mínima: `scripts/tests/p5-014-c-delimitacion-teclado-aria-menu.test.mjs` inspecciona la fuente real y ejecuta positivos/negativos dirigidos contra pérdida de etiquetas, estado activo, foco, superficie táctil, guard de administración, auditoría e inserción HTML insegura.
- Comando en checkout autorizado: `node --check scripts/tests/p5-014-c-delimitacion-teclado-aria-menu.test.mjs && node --test scripts/tests/p5-014-c-delimitacion-teclado-aria-menu.test.mjs`.
- Preflight local 6/6 ejecutado con Node v22.16.0 sobre extracto mínimo de fuente; inspección independiente de texto íntegro en Github 8/8 mutaciones. El extracto no representa ejecución real de Angular; no reclamar E2E.
- Sin modificaciones a código runtime, rutas, guards, permisos, API/DTO, MySQL, deps, infra o QA/PROD. A2 cubre invariantes estáticos, NO una prueba de autorización real en el servidor.
- Rollback: nuevo commit fast-forward que revierta únicamente nota/test/changelog si fuese necesario, sin reescribir historia de otros autores.
