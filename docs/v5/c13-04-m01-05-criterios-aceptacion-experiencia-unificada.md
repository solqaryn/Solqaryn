# SOLQARYN V5 FINAL — C13-04.M01.05: criterios observables de aceptación de experiencia visual unificada

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Repo/branch:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** definición documental M01 de aceptación futura. No se ha modificado Angular, SCSS, servidor, API, MySQL, JWT/RBAC, permisos ni entorno.

**Fuente de autoridad:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Preflight PLAN H=PENDIENTE/I vacio, COLA G/O=SI, DAG F=VALIDADO/G=SI; reclamación CAS y readback. Invariantes NR01–NR14 definidos en [M01.01](./c13-04-m01-01-alcance-no-regresion-experiencia-unificada.md), interfaces y actores en [M01.02](./c13-04-m01-02-actores-entradas-salidas-superficies-ui.md), inventario de 14 blobs y 12 brechas en [M01.03](./c13-04-m01-03-inventario-codigo-brechas-coherencia-visual.md). M01.04 es trabajo de otra Task y sus resultados no son requisito ficticio para esta preparación independiente; M01.06 no se libera por este documento.

## 1. Matriz de aceptación funcional — 16 pares positivos/negativos FUTUROS_NO_EJECUTADOS

Las pruebas son criterios que debe ejecutar un cambio de implementación posterior con fixtures sintéticos; no son PASS, WCAG certificado, comportamiento medido ni evidencia E2E completada.

| Caso | Superficie | Positivo observable futuro | Negativo bloqueante futuro | Oráculo/código versionado |
| --- | --- | --- | --- | --- |
| P01/N01 | Login y campos | Input con label/error asociado; Enter realiza accion y foco retorna al campo invalido | Error por color, sin texto o foco perdido | `frontend/src/app/app.routes.ts; frontend/e2e/m10-ui-ux-accesibilidad.spec.ts` |
| P02/N02 | Jerarquia de pantalla | Titulo, contexto, CTA y feedback visibles antes de acciones secundarias | CTA oculto o estado inesperado sin explicacion | `frontend/src/styles/_enterprise-design-system.scss` |
| P03/N03 | Estado de carga | Loading y empty tienen region de estado y texto comprensible | Spinner sin fin o falta de empty state | `frontend/src/app/shared/feedback-state/feedback-state.component.ts` |
| P04/N04 | Error recuperable | Error informa proximo paso sin exponer detalles internos | Stacktrace, dato sensible o mensaje exclusivamente cromatico | `frontend/src/app/shared/feedback-state/feedback-state.component.ts` |
| P05/N05 | Menu y rutas | Enlace autorizado mantiene indicador de ruta activa y jerarquia semantica | Menu ofrece accion prohibida o ruta sin guard | `frontend/src/app/shared/navigation/app-navigation-menu.component.ts; frontend/src/app/core/guards/permiso.guard.ts` |
| P06/N06 | Tenant A/B | Cambio A a B limpia tema, datos y cache visibles y exige nueva autorizacion API | Precio, nombre, logo privado o respuesta A persiste en B | `frontend/src/app/core/auth/tenant-context.service.ts` |
| P07/N07 | Permiso backend | Usuario con rol valido puede operar solo su alcance | Ocultar un boton se usa como sustituto de denegacion backend | `frontend/src/app/core/guards/permiso.guard.ts` |
| P08/N08 | Viewport 320 px | Accion principal, tabla y lectura permanecen disponibles sin scroll global indebido | Accion cortada o menu impide operar | `frontend/e2e/responsive-global.spec.ts` |
| P09/N09 | Viewport 390/768/1280 | Reflow conserva informacion y orden de navegacion | Layout salta estados, oculta campo requerido o cambia orden de foco | `frontend/src/styles.scss` |
| P10/N10 | Teclado y dialogo | Tab, Escape y cierre de modal restauran foco al invocante | Trap de foco, salto a body o control sin nombre | `frontend/src/styles/_m10-accessibility.scss` |
| P11/N11 | Lectores y aria-live | Estado y validacion se anuncian exactamente en contexto | Anuncio duplicado o feedback solo mediante icono | `frontend/src/app/shared/feedback-state/feedback-state.component.ts` |
| P12/N12 | Movimiento reducido | La preferencia reduced-motion conserva funcionalidad y contexto | Estado solo comprensible por animacion | `frontend/src/styles/_m10-accessibility.scss` |
| P13/N13 | Tema/branding | Tokens de marca autorizados conservan contraste y aislacion A/B | CSS arbitrario o branding privado de A visible en B | `frontend/src/styles/_enterprise-design-system.scss` |
| P14/N14 | Densidad de tabla | Cabeceras, filtros y acciones se asocian con filas y mantienen scroll propio | Fila pierde seleccion o accion tras paginacion | `frontend/e2e/responsive-global.spec.ts` |
| P15/N15 | Rendimiento y presupuesto | Build medido con presupuesto vigente initial warning 650kB/error 750kB | Un script auxiliar divergente se presenta como valor medido exitoso | `frontend/angular.json; frontend/scripts/performance-bundle-baseline.mjs` |
| P16/N16 | Integridad de certificacion | CI exact-SHA y matriz ruta/rol/tenant/viewport guarda evidencia positiva y negativa | Check SKIPPED, QUEUED, CANCELLED o FAIL contado PASS | `docs/PROJECT_SCOPE_LOCK.md; docs/AUTOMATION_STATUS.md` |

## 2. Requisitos no funcionales y pruebas requeridas posteriormente

| ID | Atributo | Evidencia medible exigida |
| --- | --- | --- |
| NF01 | Lenguaje semantico | Títulos, botones y mensajes consistentes en seis superficies muestreadas |
| NF02 | Navegacion accesible | Sin trampas de foco y con nombre/rol verificable en controles principales |
| NF03 | Reflow responsive | 320, 390, 768 y 1280 px sin perdida de CTA ni scroll global ocultante |
| NF04 | Contraste observable | Comparacion real normal/disabled/error/forced-colors, sin certificar WCAG por archivo |
| NF05 | Tenancy y permisos | Matriz con fixtures A/B y verificacion HTTP backend de 403 y datos segregados |
| NF06 | Feedback de red | Loading, empty, success, warning y error con mensaje recuperable |
| NF07 | Movimiento | Emulacion prefers-reduced-motion sin dependencia de animacion |
| NF08 | Evidencia causal | Guardar SHA, browser, viewport, rol, tenant sintetico, resultado, logs limpios |
| NF09 | Peso/velocidad | Bundle y LCP/INP/CLS medidos por dispositivo/red antes de definir SLO |
| NF10 | Rollback controlado | Revert no destructivo y verificacion relectura de estado/CI exactos |

La matriz de una ejecución válida debe registrar al menos: `CASO_ID`, `SHA` exacto, ruta real, pantalla, actor/rol y pertenencia al tenant, estado (loading/empty/error/ready), dispositivo, viewport, zoom, browser, versión Angular/Node, precondiciones, fixture sintético, pasos positivos, resultado negativo, accesibilidad, screenshot/trace saneados, estado check-run y rollback. Una spec existente no sustituye este resultado.

## 3. Priorización por riesgos y relaciones con otras microtareas

- **P0 seguridad:** N06/N07/N13 contra contaminación tenant, bypass UI de JWT/RBAC y estilos ajenos. La autorización real pertenece al backend; un guard Angular o menú oculto no crea privilegios. Para PASS exigen HTTP y persistencia aislada A/B; no se certifica seguridad por esta documentación.
- **P1 accesibilidad:** N01, N10, N11 y N12 para foco, errores semánticos, lector, reduced-motion y forced-colors. La presencia de SCSS y Playwright no es ejecución, y este documento no declara conformidad WCAG.
- **P1 experiencia:** N02/N03/N04/N05/N08/N09/N14 para reflow y recuperación de usuario en storefront, login, backoffice, diálogos y tablas.
- **P1 evidencia:** N15/N16 para discrepancia entre thresholds de `angular.json` (warning initial 650kB, error 750kB) y script de baseline con umbral distinto; reconciliar sin modificar controles en M01.05. No inventar LCP/INP/CLS, no mezclar SHA.
- **Dependencia:** C13-04.M01.06 es terminal: exige predecesores y revisión A5 independiente pertinentes. M01.05 no habilita por sí sola migraciones, QA/main/PROD ni fases M02–M06.

## 4. Impacto, método de validación documental A0–A4 y A5

**A0 (estática):** revisar dos Markdown, encabezado y viñetas contiguos en CHANGELOG; cada caso P01/N01–P16/N16 debe tener positivo, negativo y oráculo; buscar ausencia de tokens, contraseñas y PII.

**A1 (funcional documental):** cotejar 16 pares y diez NF contra flujos en M01.01 y M01.02, y exigir que los negativos sean condiciones de rechazo observables; no declarar tests browser ejecutados.

**A2 (seguridad documental):** asegurar casos A/B, 403 real, ausencia de privilegio en frontend, errores seguros y fixtures sin PII. Ningún DTO/migración ni secreto modificado.

**A3 (integración documental):** correlacionar M01.03 con fuentes reales `app.routes.ts`, `permiso.guard.ts`, `tenant-context.service.ts`, `styles.scss`, `_enterprise-design-system.scss`, `feedback-state.component.ts`, `responsive-global.spec.ts`, `angular.json`; verificar que son artefactos y no PASS runtime.

**A4 (publicación):** commit directo en `dev` fast-forward con comparación de HEAD antes de escribir; comprobar diff, blobs y check-runs `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` con `head_sha` exacto. Compilaciones de backend, frontend, seguridad o Playwright no causales al cambio solo Markdown son `NO_APLICA` motivado, no PASS; nunca aceptar `queued/skipped/failed/cancelled` como éxito requerido.

**A5:** revisión posterior de otra Task Javier distinta de `6af16bbad26881918c283abe06e1f32a`, con comparativa de blob, contratos, negativos, checks exact-SHA y readback. No hay autoaprobación ni permiso QA/main/PROD.

**Rollback:** `git revert` del commit concreto sin force-push ni alterar historia de BITACORA; preservar autoría y verificar nuevas dependencias antes de reversión. Sin eventos, workers, migraciones, nuevas cuentas o producción.
