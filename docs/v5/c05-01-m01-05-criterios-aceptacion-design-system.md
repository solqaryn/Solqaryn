# SOLQARYN V5 — C05-01.M01.05: aceptación del sistema de diseño

**Autor:** PRIMARY DEV 03 JAVIER / TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, documento Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; `PLAN_MAESTRO!A726:O726`. **Scope:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-14. Solo especificación documental M01.05, no nuevo código, cumplimiento WCAG certificado ni lanzamiento.

## Antecedentes comprobables

M01.01 define NR01–NR12 (blob `ea7c5e7d339838f021b3f746fd9a7fe307b58b8d`), M01.02 identifica actores y entradas I01–I09 (`bd2976610247668f5e638f6fff38c83c4d54a143`), M01.03 inspecciona F01–F09 de SCSS/rutas/diálogo/feedback/menú/tema/spec (`a7f5ed92b80ef68c8957af27e82c1aedbd39ed2a`) y M01.04 registra riesgos potenciales R01–R12, supuestos S01–S08 y decisiones D01–D08 aún **PENDIENTE** (`5f566142acb7a41d7dfc3b9a8e5dbf7e41975ddb`). La existencia de un archivo .spec.ts o de tokens CSS no significa ejecución real de pruebas. Las fuentes F01–F09 coincidieron por blob GitHub en la auditoría previa.

El resultado exigido de C05-01 es catálogo de componentes, contraste y responsive para tipografía, color, espacios, estados, formularios, tablas, modales y navegación. Aquí se fijan **oráculos de aceptación futura**, no se afirma que el catálogo final ya existe.

## Matriz funcional, negativa y evidencia exigida

**Estado de cada criterio AC01–AC14: NO_EJECUTADO (runtime).** `PASS_DOC` acredita solamente que el criterio es observable y coherente con las fuentes; no certifica resultado Angular/Playwright/.NET.

| Criterio | Positivo futuro observable | Negativo que debe fallar | Evidencia esperada |
| --- | --- | --- | --- |
| AC01 / N01 | Token semántico único y mapa token→consumidor por componente | Duplicación divergente sin justificación | CSS calculado y diff por vista |
| AC02 / N02 | Texto/fondo/botón/foco conservan contraste por tema y estado | Contraste insuficiente bajo tema alterno | Pares de colores medidos y capturas |
| AC03 / N03 | Tab/Shift+Tab recorren controles con foco visible | Foco oculto, salto o bloqueo sin salida | Playwright teclado y foco activo |
| AC04 / N04 | Modal conserva foco, Escape según política y restauración al cerrar | Foco en fondo o perdido tras cancelación | Traza de diálogo y aserciones |
| AC05 / N05 | 320px sin scroll horizontal del body; tabla con scroll local | Tabla o contenedor impone ancho global | Playwright 320/768/1280 |
| AC06 / N06 | Formulario anuncia label, error textual y ayuda vinculada | Error solo por color o campo sin nombre | Aserciones de aria y validación |
| AC07 / N07 | Loading/empty/success/error/warning tienen mensaje, role y aria-live/busy apropiado | Error silencioso o carga infinita sin recuperación | Test DOM y revisión asistiva |
| AC08 / N08 | Reduced-motion reduce efectos cosméticos sin ocultar controles | Animación obligatoria bloquea interacción | Emulación prefers-reduced-motion |
| AC09 / N09 | Controles táctiles cumplen objetivo --target-min 44px o excepción ensayada | Botón diminuto sin alternativa accesible | Medida de área interactiva |
| AC10 / N10 | Cambio tenant A→B elimina tema A aunque llegue respuesta tardía | Color/identidad A reaparece en B | Test Angular de carrera A→B |
| AC11 / N11 | Tema solo acepta campos/colores saneados de allowlist | CSS/HTML/JS arbitrario desde tenant | Prueba negativa de DTO/valores |
| AC12 / N12 | Backend revalida identidad JWT, RBAC y tenant aunque menú oculta ruta | Ruta/host/branding concede lectura B desde A | Test HTTP A/B, denegación 403/404 |
| AC13 / N13 | Tipografía, zoom y controles se adaptan a móvil y escritorio | Truncamiento que impide acciones con zoom | Capturas y viewport con zoom |
| AC14 / N14 | Un cambio de diseño real cumple diff, CI dirigida exact-SHA, rollback y revisión independiente | Se informa PASS por spec existente o check SKIPPED/pending | GitHub run/log, SHA y A5 |

Los casos N01–N14 son condiciones de rechazo de una **implementación futura**, no estados de fallo confirmados hoy. Los riesgos R01/R11/R12 se relacionan con AC01/13/14, R02–R07 con AC02–09 y R08–R10 con AC10–12. S01–S08 siguen falsables; D01–D08 requieren decisión de autoridad futura y no se aprueban por este archivo.

## Pruebas a realizar en sus microtareas de código

- **Frontend Angular 22.2.1 y Material/CDK:** build, pruebas unitarias de tema y estados, Playwright de teclado/diálogos y 320/768/1280px, con SHA, logs y captura/traza cuando sean pertinentes; conservar Zone.js activo.
- **Accesibilidad:** medición de contraste por tema/estado, objetivo táctil, foco, labels, error/reintento y reducción de movimiento; verificación humana con lectores cuando proceda.
- **Seguridad tenant A/B:** pruebas negativas en API .NET 10 sobre membresía revocada, roles, JWT, acceso cruzado y tema inyectado; el guard Angular o CSS nunca sustituye autorización backend.
- **Integración:** el contrato del servicio de tema debe limpiar estado de A cuando se activa B, aun si HTTP responde fuera de orden. No atribuir ejecución de `theme-applier.service.spec.ts` sin logs exact SHA.
- **Publicación:** sólo CI realmente requerida con status=completed/conclusion=success/head_sha igual al commit; checks SKIPPED/CANCELLED/FAIL/IN_PROGRESS no son PASS. No usar Vercel o proveedor pago como sustituto.

## Gate documental A0–A5 y rollback

La autorización puntual exige `PLAN_MAESTRO!H726=PENDIENTE/I726` vacío al claim, `COLA!G726/O726=SI`, `DAG_TECNICO!F687=VALIDADO/G687=SI`, lectura de predecesores C00-03/C01/C02 M01.01–05 y CAS exclusivo. No autoriza M01.06 ni nuevos IAM/JWT/RBAC, migraciones, QA/main/PROD.

**A0:** diff de solo este archivo y CHANGELOG, sin cambiar código, datos o secretos. **A1:** comprobar 14 criterios positivos y 14 negativos como especificaciones concretas, todos NO_EJECUTADOS de runtime. **A2:** conservar autorización backend, seguridad tenant A/B y allowlist de tokens. **A3:** consistencia con NR01–12, I01–09, F01–09 y R/S/D pendientes. **A4:** publicar fast-forward a dev y comprobar Scope Lock y procedencia CHANGELOG `completed/success` en SHA exacto, con PLAN/COLA/BITACORA readback. Pruebas runtime para este diff exclusivamente Markdown: **NO_APLICA por ausencia de cambio ejecutable**, nunca PASS inventado. **A5:** otra Task de las seis Javier, TASK_ID diferente a la autora, revisión independiente antes de promoción.

Rollback documental: `git revert` del changeset, sin force-push. No se modifican UI activa, API, MySQL, tenants, workers, eventos, configuración física de Tasks, QA/main/PROD, dominios ni proveedores de cobro.
