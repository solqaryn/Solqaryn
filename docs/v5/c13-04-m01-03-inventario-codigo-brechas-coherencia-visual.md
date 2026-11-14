# SOLQARYN V5 FINAL — C13-04.M01.03: inventario causal de fuentes, contratos y brechas de coherencia visual

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 06 JAVIER (`6af16bbad26881918c283abe06e1f32a`). **Repositorio y rama:** `solqaryn/Solqaryn@dev`; **base de inspeccion:** `23708db0613646282ec8877f6c7234a95836b777`. **Alcance:** M01 inventario documental de `C13-04`, interfaces existentes y coherencia de lenguaje visual, estados y responsive. No modifica Angular, SCSS, API, permisos, migraciones ni produccion.

**Autoridad:** [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN_MAESTRO/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md` y Skill canonica. Plan H=PENDIENTE/I vacio en preflight, COLA G/O=SI y DAG F=VALIDADO/G=SI; claim por CAS y posterior readback. M01.01 se encontraba en progreso bajo DEV05: este inventario independiente no lo da por completado, ni libera M01.06.

## 1. Inventario de blobs reales del HEAD inspeccionado

Los hashes son **Git blob SHA**, no resultados de test; las conclusiones se limitan al contenido inspeccionado y su contrato observable. No se infiere cobertura universal a partir de la existencia de archivos.

| Ref | Ruta de fuente | Git blob SHA | Evidencia positiva | Limite explicito |
| --- | --- | --- | --- | --- |
| F01 | `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas publicas/protegidas, lazy load y guards declarados | No prueba por si solo rutas navegables, permisos API ni experiencia accesible |
| F02 | `frontend/src/app/app.config.ts` | `cd77384e713014015d83b348955b3d83573348b3` | Router, HTTP interceptors, animaciones y ZoneChangeDetection | No acredita ejecucion E2E ni que todas las pantallas sean coherentes |
| F03 | `frontend/src/styles.scss` | `2eb48562fd77c758662af6b9683649b4f9698a64` | Importa hojas compartidas y define variables de color, foco y geometria | No verifica contraste en temas y estados reales |
| F04 | `frontend/src/styles/_enterprise-design-system.scss` | `6a188590af9a958bff2e3e0375c0c46cdb2b14e6` | Escala comun de tipografia, espaciado, breakpoints y control minimo | No demuestra adopcion total ni interaccion movil en cada modulo |
| F05 | `frontend/src/styles/_m10-accessibility.scss` | `6140d41b8a7e1e98f4fa1e43e0ab72f04b6ae29f` | Estilos transversales de foco, skip-link, encabezados y reflow | No certifica WCAG ni lectura asistida en runtime |
| F06 | `frontend/src/app/shared/navigation/app-navigation-menu.component.ts` | `b70ef4ffb252228cf2ab19754a5e30d53f64aca0` | Menu con agrupaciones semanticas, RouterLink y filtros por permisos UI | No sustituye autorizacion backend ni cubre todas las combinaciones de rol |
| F07 | `frontend/src/app/shared/navigation/navigation-contract.json` | `7142082462d7bc53ff4f0cdf96f67f21405f3a44` | Contrato de grupos, rutas y accion de permiso | No demuestra sincronizacion perfecta entre contrato, rutas y backend |
| F08 | `frontend/src/app/shared/feedback-state/feedback-state.component.ts` | `7deb40475f0c492d09e649e7351b2915bd53d356` | Estados loading/empty/success/error/warning con role y aria-live | No acredita adopcion en formularios, tableros o flujos de error |
| F09 | `frontend/src/app/core/guards/permiso.guard.ts` | `1052bd38041055cc81e84a14d5fe0364e169b1fe` | Guard de ruta y modulo/accion con consulta de capacidades | No constituye garantia de autorizacion server-side |
| F10 | `frontend/src/app/core/auth/tenant-context.service.ts` | `63a711355495d2f80efba35bffb2b8727401154a` | Distingue empresa solicitada y contexto validado por servidor | No acredita aislamiento A/B sin pruebas de API y persistencia |
| F11 | `frontend/e2e/responsive-global.spec.ts` | `aef296d8d2766f652b1a9f47800e19411867fb07` | Fuente de escenarios E2E responsivos y catalogo de rutas | Archivo de test no es resultado de ejecucion ni cobertura exhaustiva |
| F12 | `frontend/e2e/m10-ui-ux-accesibilidad.spec.ts` | `ce3d50c53b5de8447fa9eba09208160657415fa3` | Fuente Playwright de verificaciones de UI y accesibilidad | No hay certificado de ejecucion de browser/dispositivos en esta microtarea |
| F13 | `frontend/angular.json` | `8b00f8fee80fdfafe1864e233be7d5f20ad444e3` | Build Angular y budgets inicial 650kB aviso/750kB error | Son umbrales configurados, no peso medido ni SLO aprobado |
| F14 | `docs/v5/c13-02-m01-03-inventario-codigo-evidencia-accesibilidad.md` | `8846d8b30b56c659685396014230e3cd20061aa1` | Inventario previo de accesibilidad que permite cruzar brechas | Es evidencia documental historica; no sustituye test actual |

## 2. Brechas concretas que exigen validacion causal posterior

| Brecha | Riesgo o pregunta falsable | Comprobacion necesaria (aun NO ejecutada) |
| --- | --- | --- |
| B01 | Semantica de botones y enlaces diverge entre shell y feature | Captura de rol/nombre accesible en rutas representativas |
| B02 | Estados loading/empty/error no usan siempre un patron comun | Matriz por componente y prueba E2E de transiciones |
| B03 | Contraste efectivo varia con temas, disabled y overlays | Auditoria automatica y manual en tema/estado real |
| B04 | Densidad de listas y formularios no se mantiene al reflow | Capturas a 320/360/768/1280px y navegacion sin scroll horizontal injustificado |
| B05 | Foco tras navegacion y error es inconsistente | Tabulacion, focus-visible, retorno de modal y lector de pantalla |
| B06 | Rutas de navigation-contract y guards pueden divergir | Inventario mecanico de rutas + permiso por rol contra backend |
| B07 | Mensajes de error pueden exponer informacion de tenant | Pruebas negativas A/B y no filtrado de datos en UI |
| B08 | Preferencia reduced-motion puede omitirse en animaciones locales | Prueba visual con emulacion de reduced-motion |
| B09 | Mensajes y validadores no comparten vocabulario de forma | Revision de etiquetas, mensajes de campo y status aria-live |
| B10 | Tamano y peso de bundles no garantizan experiencia liviana | Build reproducible, budgets y metricas LCP/INP/CLS por entorno |
| B11 | Navegacion en tablet o touch puede perder objetivos de 44px | Hit-target E2E y prueba tactil con enfoque visible |
| B12 | Existen tests fuente, pero no evidencia runtime de toda la matriz | CI exact-SHA con artefactos Playwright y registro de rutas realmente probadas |

**Observacion causal:** `angular.json` declara presupuesto inicial 650kB advertencia/750kB error, mientras documentos previos de rendimiento reportan una politica auxiliar con otros umbrales. Es una discrepancia de fuentes por reconciliar en un scope de codigo independiente; no se afirma regresion real ni modificacion de budgets en este inventario.

## 3. Fronteras de seguridad y consumidores

El browser y las reglas visuales representan permisos, pero **API y autorizacion JWT/RBAC siguen siendo la autoridad efectiva**; una ruta oculta no sustituye denegacion HTTP. `TenantContextService` diferencia seleccion solicitada de contexto validado, por lo que una prueba A/B debe verificar datos y errores del backend, no solo etiquetas de empresa. No modificar `localStorage`, tablas, roles, secretos ni logs con PII desde este scope documental.

Consumidores: componentes standalone, navegador, responsables de CSS compartido, menus, formularios, shell, usuarios con lector de pantalla, validadores de UX, CI y soporte. No se introduce API, DTO, indice, migracion ni evento.

## 4. Criterios de verificacion propuestos, **FUTUROS_NO_EJECUTADOS**

- **P01/N01:** ruta protegida accesible con permiso valido / rechazo real con permiso ausente sin datos cruzados.
- **P02/N02:** loading y error anunciados de forma coherente / ausencia de estado o anuncio duplicado.
- **P03/N03:** foco visible y recorrido por teclado / foco perdido en cierre de modal.
- **P04/N04:** viewport 320px con acciones y texto utilizables / desborde que oculta accion critica.
- **P05/N05:** lenguaje de estados uniforme / mensajes incompatibles entre formularios y tablas.
- **P06/N06:** tenant A muestra exclusivamente su contenido / tenant B visible en A por cache o contexto stale.
- **P07/N07:** build dentro de budgets documentados / discrepancia entre politica de build y script auxiliar.
- **P08/N08:** reduced-motion respeta preferencias / animacion local forzada.
 
Estas son **especificaciones futuras**, no resultados A1/A2/A3 ni certificaciones WCAG, HTTP, Playwright o performance. La validacion A0-A4 de esta microtarea se limita a calidad de inventario documental, hashes, procedencia, checks Scope Lock y lectura de estado; pruebas runtime no causales se documentaran NO_APLICA.

## 5. Continuidad, evidencias y rollback

- Unico artefacto nuevo: este Markdown; segundo archivo editado: `CHANGELOG_AI.md`, con encabezado y viñetas autocontenidos.
- Evidencias reproducibles: base `23708db0...`, rutas y blobs F01-F14, gates del Sheet y nueva publicacion por fast-forward/CAS exact HEAD.
- **A5**: requiere revisor entre las otras cinco Tasks Javier, nunca el autor `6af16bbad26881918c283abe06e1f32a`. M01.01 de otro autor permanece independiente; no liberar M01.06 por este documento.
- Rollback si fuera necesario: `git revert` normal del commit exacto, tras evaluar cambios posteriores de otros autores. No `force-push`, reset, deploy, DNS, proveedor ni datos productivos.
