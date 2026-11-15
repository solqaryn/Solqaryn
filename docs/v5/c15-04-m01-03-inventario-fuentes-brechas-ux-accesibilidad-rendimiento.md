# SOLQARYN V5 — C15-04.M01.03: inventario de fuentes y brechas de auditoría UX, accesibilidad y rendimiento

**Autor:** PRIMARY DEV 03 JAVIER — TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Corte:** `solqaryn/Solqaryn@dev`, HEAD `aa3f535258f26dbf95e2eb18cdd6d3b7e05041a7` (2026-11-14 America/Tegucigalpa). **Alcance:** preparación documental M01.03 con PROJECT_SCOPE_LOCK=STRICT; sin certificación de WCAG, rendimiento ni experiencia E2E.

## Autoridad y límites

Fuente ejecutable: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO fila 2272, COLA G/O=SI, DAG_TECNICO F=VALIDADO/G=SI. Se revisaron sus antecedentes M01.01 (objetivo, NR01–NR14, P01–P08/N01–N08) y M01.02 (actores A01–A12, IO01–IO12, fronteras F01–F10). La salida de esta unidad es **trazabilidad de archivos reales y brechas por cerrar**, no pruebas ejecutadas.

## Doce fuentes verificadas por lectura Git en el HEAD de entrada

| Ref | Archivo exacto | Blob SHA leído | Observación delimitada / no inferir |
| --- | --- | --- | --- |
| S01 | `frontend/package.json` | `e6d030f643421cb343cc12617e742469e88223c4` | Declara Angular 22.2.1, tareas build/test; no acredita build de este documento |
| S02 | `frontend/src/app/app.routes.ts` | `2f560c14693455b79ac4546e9333fb74586ef234` | Rutas Angular y guards de experiencia; no sustituyen autorización .NET/JWT/RBAC |
| S03 | `frontend/src/styles.scss` | `2eb48562fd77c758662af6b9683649b4f9698a64` | Importa SCSS y tokens de color; no demuestra contraste en cada estado |
| S04 | `frontend/src/styles/_enterprise-design-system.scss` | `6a188590af9a958bff2e3e0375c0c46cdb2b14e6` | Escala, tipografía, espaciamiento y estados base; no demuestra responsive de páginas |
| S05 | `frontend/src/styles/_m10-accessibility.scss` | `6140d41b8a7e1e98f4fa1e43e0ab72f04b6ae29f` | Reglas skip-link/foco; no demuestra navegación completa por teclado |
| S06 | `frontend/playwright.config.ts` | `87946080e7fe87f5fa9e9c8d4c8da8a36f49d5da` | Configura fixtures E2E; no equivale a ejecutar Playwright o usar lector |
| S07 | `docs/v5/c15-04-m01-01-alcance-no-regresion-auditoria-ux-accesibilidad-rendimiento.md` | `b6ce51bd1bfce97b1d6f86b9af433e63922bd9bd` | Contrato de alcance y 14 NR; obligaciones futuras |
| S08 | `docs/v5/c15-04-m01-02-actores-interfaces-auditoria-ux-accesibilidad-rendimiento.md` | `b37356938d72e2c120d082c2bdfe1b2bf6dee152` | Contrato de actores/IO/fronteras; escenarios futuros |
| S09 | `docs/v5/c13-02-m01-01-alcance-no-regresion-accesibilidad.md` | `7625e8638e3b228f8773ffe11541b0299228fda9` | Objetivos de accesibilidad; no auditoría WCAG real |
| S10 | `docs/v5/c13-02-m01-02-actores-interfaces-superficies-accesibilidad.md` | `fdf905569b0d289006d86f28b59bc70b26a648e7` | Actores y superficies accesibles por revisar; no cobertura browser |
| S11 | `docs/v5/c05-01-m01-01-objetivo-no-alcance-design-tokens.md` | `ea7c5e7d339838f021b3f746fd9a7fe307b58b8d` | Contrato de design tokens y componentes, no medición de contraste |
| S12 | `docs/v5/c05-01-m01-03-contratos-codigo-evidencia-design-system.md` | `a7f5ed92b80ef68c8957af27e82c1aedbd39ed2a` | Lectura técnica de design system; no certificación transversal |

Los doce blobs fueron consultados en el repositorio original, no en forks ni copias. **Lectura de archivos ≠ prueba de UI, de WCAG, de Core Web Vitals o de rendimiento real.** Esta lista es una línea base parcial orientada al contrato y no afirma un censo exhaustivo de componentes Angular ni de rutas públicas/privadas.

## Doce brechas de evidencia que permanecen abiertas

| Brecha | Evidencia ausente y siguiente trabajo permitido |
| --- | --- |
| G01 | Falta matriz de rutas públicas vs privadas con estado de implementación y permisos server-side validados |
| G02 | Falta matriz página/componente→token→estados→contraste calculado y auditoría visual |
| G03 | Falta prueba de teclado Tab/Shift+Tab/Enter/Espacio con focus-visible y recuperación de foco |
| G04 | Falta auditoría de nombres accesibles, roles, etiquetas, descripciones y errores en lector asistivo |
| G05 | Falta revisión real de layout a 320 px, tablet y desktop, zoom y contenido traducido/largo |
| G06 | Falta comprobar overflow contenido en tablas, controles del filtro y botones en viewport reducido |
| G07 | Falta evaluación de prefers-reduced-motion sobre estados y diálogos existentes |
| G08 | Faltan capturas/fixtures de estados loading/empty/error y de reintento ante red lenta |
| G09 | Falta separar las capacidades implementadas de landing, storefront, portal y builder futuro |
| G10 | Falta prueba de ausencia de filtración de tenant A/B por ruta, caché, branding o estado Angular |
| G11 | Faltan presupuestos aprobados y mediciones p50/p95, payload, LCP/CLS/INP según aplique y dispositivo/red |
| G12 | Faltan resultados E2E/Playwright/teclado y auditoría asistiva con run, commit SHA, versión, captura y defecto trazable |

**G01–G12 están ABIERTAS / NO_VERIFICADAS, no representan defectos reproducidos ni vulnerabilidades confirmadas**. Resolver una brecha exige artefacto causal y alcance adicional, nunca un check marcado PASS a partir de una especificación.

## Matriz de trazabilidad por superficie

| Superficie futura | Fuentes relacionadas | Brechas/criterios a verificar |
| --- | --- | --- |
| Landing institucional y portal público | S02–S05, S07–S08 | G01, G02, G05, G08, G09, G11; NR01/02/13 |
| Autenticación y operación privada ERP | S02–S06, S07–S10 | G01–G06, G10, G12; NR03/04/05/11 |
| Tablas, filtros, formularios y modales | S03–S06, S09–S12 | G02–G08, G12; NR03–NR10 |
| Storefront con rutas existentes | S02, S07–S08 | G01, G05, G08–G12; no suponer checkout completo |
| Builder y personalización futura | S04, S07, S11–S12 | G02, G07, G09–G11; capacidad no implementada ≠ PASS |
| Tenancy y datos de marca | S02, S07–S08 | G01, G10, G12; JWT/RBAC debe validarse en backend |

## Pruebas y evidencia causal exigibles en fases posteriores (no ejecutadas)

1. Definir navegador/versión, resolución/zoom, teclado, configuración de tecnologías asistivas, tipo de red, dataset sintético y SHA. Citar norma WCAG aplicable, nivel y criterios exactos sólo tras decisión de alcance.
2. Ejecutar casos positivos y negativos P01–P08/N01–N08 de M01.01 y F01–F10 de M01.02 en rutas **existentes**. Registrar resultados observados, capturas, trazas, tiempos, evidencia del defecto y retesteo en el mismo contexto.
3. Tratar contrastes, tamaños de objetivos, movimiento, semántica, foco, diálogos, tablas y loading como comportamientos medidos; las reglas SCSS no bastan como certificado.
4. Separar controles UI (experiencia) de controles API de JWT/RBAC y aislamiento multitenant A/B. Ante cualquier sospecha de filtración realizar reproducción aislada y no exponer datos reales.
5. Para resultados de CI exigir run, `head_sha`, `completed/success` del job pertinente y artefactos; `skipped/cancelled/in_progress` no es PASS. Los checks que sólo validan Markdown no sustituyen E2E.
6. Conservar owner, EVENT_ID, IDEMPOTENCY_KEY y readback PLAN/COLA/BITACORA. Un A5 de otra Task Javier es posterior y no otorga permiso QA/main/PROD.

**Cambio de M01.03:** únicamente nuevo Markdown y una sección autocontenida de `CHANGELOG_AI.md`. A0 fuentes+blobs, A1 trazabilidad y criterios, A2 segregación JWT/RBAC/tenant UI/backend, A3 matriz G01–G12 y prerrequisitos; A4 check Scope Lock/procedencia V5 en SHA publicado más readbacks. Build/Playwright/MySQL/HTTP no aplican al diff exclusivamente documental y no son PASS atribuibles. M01.06 permanece condicionado. **Rollback:** revert Git ordinario, sin reescribir historia ni alterar QA/main/PROD.
