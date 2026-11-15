# SOLQARYN V5 — C13-04.M02.03: modelo conceptual de evidencia, índices, reversibilidad y fixtures de auditoría visual

**Fuente ejecutable:** STANDARD V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO/COLA/DAG_TECNICO institucional (Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`), Skill canónica y `docs/PROJECT_SCOPE_LOCK.md`. **Autor:** PRIMARY DEV 02, Task `6af16bb9492c8191b8e8c7d606f11507`. **Fecha:** 2026-11-15. **Rama:** `solqaryn/Solqaryn@dev` exclusivamente.

**Frontera:** contrato M02 de diseño de auditoría visual. No modifica Angular, SCSS, API, MySQL, tablas, migraciones, permisos JWT/RBAC, tenant, proveedores, webhooks ni entornos QA/main/PROD. Los casos descritos son `FUTUROS_NO_EJECUTADOS`; no acreditan WCAG, integración, aislamiento A/B ni rendimiento medido. Requiere A5 independiente de una Task Javier distinta de la autora.

## 1. Decisión de persistencia y límites

La evidencia mínima de la fase C13-04 es una matriz **ruta/pantalla × rol/tenant × estado × acción × viewport × resultado accesible × SHA**. Se mantiene como artefacto reproducible de pruebas y revisiones, **no se crea una tabla por cada widget**. La fuente actual `frontend/src/app/app.routes.ts` y los estilos compartidos `frontend/src/styles.scss` describen superficies y tokens; `frontend/e2e/responsive-global.spec.ts` y `frontend/e2e/m10-ui-ux-accesibilidad.spec.ts` son fuentes de pruebas existentes, no PASS acreditados.

El contrato de M01.03 contiene catorce referencias Git verificadas y doce brechas hipotéticas. M01.01 fija NR01–NR14; M01.05 especifica dieciséis pares positivos/negativos futuros. M02.03 clasifica **cómo guardar evidencia sintética en CI** si una implementación posterior resulta elegible, sin establecer base de datos ni usar registros de clientes.

| Entidad conceptual | Identidad / ownership | Campos propuestos, no implementados | Protección |
| --- | --- | --- | --- |
| `VisualAuditCase` | `caseKey` versionado por repositorio | ruta lógica, acción, estado, viewport, oráculo | No URL con credenciales |
| `VisualAuditRun` | `runKey` + `commitSha` | configuración de navegador, ejecutor CI, resultado | Sin confundir run local con ID nativo |
| `VisualAuditObservation` | `caseKey` + `runKey` | texto de feedback, foco, resultado, evidencia depurada | Sin JWT, PII ni capturas sin sanitizar |
| `TenantVisualContext` | `tenantAlias` sintético + sesión | tema permitido, modo visual y permiso simulado | Nunca persistir contexto A en B |
| `VisualFixture` | `fixtureId` estable | roles ficticios, datos ficticios, viewport y acción | Sin importar datos productivos |
| `EvidenceManifest` | `commitSha` + catálogo de artefactos | ruta de artefacto, checksum, procedencia, oráculo | Inmutable y auditable |

La persistencia sugerida **para CI** son archivos efímeros sanitizados asociados a un SHA exacto, sin sincronización con MySQL ni retención de información personal. Un almacenamiento server-side futuro requeriría nuevo alcance, análisis de PII/retención, migración separada y autorización propia. No se declara ese diseño como esquema SQL vigente.

## 2. Claves, consultas e índices conceptuales

| Consulta futura | Clave conceptual | Requisito de integridad |
| --- | --- | --- |
| Evidencia por fuente | `commitSha,caseKey,runKey` | Nunca combinar resultados de SHA distintos sin equivalencia probada |
| Resultado por superficie | `routeKey,actionKey,viewportKey` | Normalizar rutas, no registrar query strings sensibles |
| Aislamiento de contexto | `tenantAlias,sessionKey` | Filtrar en backend si se propusiera persistencia posterior |
| Comparación de regresión | `baselineSha,candidateSha,caseKey` | Baseline identificado y resultado reproducible |
| Estado de accesibilidad | `caseKey,stateKey,inputMode` | Mantener foco, nombre accesible y mensaje textual |

Son **claves de agrupación documental**, no índices MySQL creados. No existen `CREATE INDEX`, EF migration, rollback SQL ni DDL para aprobar en esta microtarea. Si el proyecto autoriza persistir evidencia, diseñar primero una migración aditiva con restricciones de empresa y prueba de restauración en CI efímera; dejar rollback escrito y validar que la eliminación del artefacto nuevo no afecte datos de clientes.

## 3. Fixtures sintéticos y negativos (plan, no resultados)

| Fixture | Positivo futuro | Negativo que debe rechazarse |
| --- | --- | --- |
| F01 público | Tienda pública muestra estado vacío legible | Contenido de backoffice visible |
| F02 operador | Menú muestra acciones permitidas | Ocultar botón se toma como autorización servidor |
| F03 tenant A | Marca y datos pertenecen a A | Estado/caché de B contamina la vista |
| F04 tenant B | Sesión cambia contexto y limpia estado | Evidencia de A se reutiliza en B |
| F05 320px | Reflow conserva CTA, encabezados y acciones | Desbordamiento global o botón inaccesible |
| F06 390/768px | Reflow estable con tablas responsivas | Encabezados y celdas desalineados |
| F07 teclado | Escape y Tab conservan secuencia y retorno foco | Trampa de foco o foco perdido |
| F08 lector | Mensaje de error tiene nombre/rol y texto | Estado solo de color o aria-live duplicado |
| F09 reduced motion | Preferencia conserva comprensión del estado | Feedback depende únicamente de animación |
| F10 error 403 | Error seguro y recuperable | Stacktrace, token o PII filtrada |

## 4. Evidencia, validación causal y rollback

- A0: inspeccionar diff contra M01.01/M01.03/M01.05, inventario F01–F14 y alcance conceptual; no inferir resultado browser.
- A1/A2: comprobar matriz, fixtures positivos y rechazos en texto. Pruebas de teclado, WCAG y tenant A/B runtime son **NO_APLICA al cambio documental**, y quedan previstas para fases de UI/API.
- A3: sin integración o migración real; relación futura con Playwright y CI declarada pero no ejecutada.
- A4: verificar publicación fast-forward, archivo exacto, `CHANGELOG_AI.md` y Scope Lock + check V5 en `completed/success` del SHA publicado; otro estado no habilita LISTO.
- Reversión: `git revert` del changeset documental con resguardo de cambios ajenos; no ejecutar rollback de datos, no modificar historial de BITACORA.

**Dependencia posterior:** M02.06 permanece bloqueada hasta satisfacer sus predecesoras M02.01–05 y A5 independientes según DAG vivo. No forzar COLA G/O=SI.
