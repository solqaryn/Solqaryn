# SOLQARYN V5 — C15-04.M02.06: gates, rollback y salida de contratos UX, accesibilidad y rendimiento

**Autor:** PRIMARY DEV 01 — JAVIER_CHATGPT, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Fecha:** 2026-11-14 (America/Tegucigalpa). **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Naturaleza:** cierre de **contratos documentales M02**, **NO** prueba de WCAG, rendimiento, UX E2E ni habilitación automática de M03, QA/main/PROD.

## 1. Base de autorización y verificación previa

La fuente ejecutable única es el [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y el [Sheet institucional](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). En el preflight de `C15-04.M02.06`: PLAN_MAESTRO!H2281=PENDIENTE / I vacío, COLA G/O2281=SI, DAG_TECNICO ID exacto con F=VALIDADO/G=SI, y los cinco predecesores M02.01–M02.05 estaban LISTO con A5_PASS independiente. Se ejecutó claim condicional H2281 hacia EN_PROGRESO, se mantuvo owner único y se exigió readback y BITACORA A:Z.

Los contratos precedentes **son fuentes documentales** (no nuevas funcionalidades):
- `C15-04.M02.01`: modelo, ownership e invariantes UX01–UX12/INV01–INV16;
- `C15-04.M02.02`: contratos API/DTO, errores, validaciones y compatibilidad;
- `C15-04.M02.03`: persistencia propuesta, índices condicionales y fixtures;
- `C15-04.M02.04`: configuración tipada y feature flags propuestos;
- `C15-04.M02.05`: señales e idempotencia conceptual, límites de correlación y retención; A5 revisada de forma independiente por DEV01 sobre SHA `328f7b2787dcfa567a3cf2abb30d23393fe7ca90`.

**No se infiere** que M02 haya implementado una API, una tabla, un flag, un dispatcher, un sistema de eventos o una medición de accesibilidad. Este contrato delimita qué evidencia será exigible antes de considerar una implementación futura M03. La liberación real depende de A5 independiente de **esta unidad** y de nuevas lecturas del DAG, no de una declaración en Markdown.

## 2. Puertas de salida contractuales B01–B14

| Gate | Evidencia requerida antes de implementar o promover | Situación en este M02.06 |
| --- | --- | --- |
| B01 | ID y dependencias C15-04 unidos por ID exacto, PLAN/COLA/DAG sin gates falsos | Preflight documental; estado dinámico debe releerse |
| B02 | M02.01–05 LISTO y A5 independiente acreditado | Verificado por readback institucional para el claim |
| B03 | Contratos UI y componentes Angular existentes inventariados por blob Git | No hay auditoría exhaustiva E2E en esta entrega |
| B04 | Rutas públicas/privadas con authGuard/permissionGuard y permisos backend consistentes | No se modifican guards, tokens ni controladores |
| B05 | WCAG 2.2 AA por teclado, foco visible, nombre accesible y navegación de errores | **NO_CERTIFICADO** sin pruebas browser y evaluación humana |
| B06 | Zoom/reflow, temas y contraste suficiente en formularios, tablas y overlays | **NO_MEDIDO**; requerirá fixtures de viewport y escenarios negativos |
| B07 | Motion, preferencias de movimiento reducido y estados carga/error/vacío accesibles | **NO_EJECUTADO**, no se introducen animaciones |
| B08 | Presupuesto de rendimiento por métrica, dispositivo, red y baseline acordado | **SIN_MEDICIONES**; no inventar LCP/INP/CLS, latencia ni TTI |
| B09 | Seguridad multiempresa A/B: backend autorizado, JWT/RBAC y no filtración por UI | **NO_PENTEST**; conservación conceptual de fronteras existentes |
| B10 | Persistencia, migraciones e índices futuros probados en MySQL efímero si se requieren | **NO_EJECUTADO**; documento no crea DDL |
| B11 | Señales propuestas sin dispatcher, webhook, workers ni Tasks adicionales | Se preserva prohibición de instalar transporte en esta fase |
| B12 | A0–A4 reales de cada cambio y CI exact-SHA (Scope Lock, V5 y suites pertinentes) | Para dos Markdown: controles documentales; runtime NO_APLICA |
| B13 | A5 de M02.06 por otra Task Javier; no revisión por autor | **PENDIENTE** hasta evidencia independiente |
| B14 | Rollback trazable, preservación de BITACORA y autorización específica de QA/main/PROD | **NO_HABILITADO** por cierre documental |

Un B02 documental positivo no convierte B05–B10 en PASS funcional. Los gates B05–B10 describen evidencias *para futura implementación* y no bloquean la publicación de la propia especificación documental M02.06 si A0–A4 proporcionales del documento son satisfactorias. Sí bloquean certificación funcional y cualquier promoción que las requiera.

## 3. Escenarios positivos y negativos futuros sin ejecución

| ID | Positivo futuro esperado | Negativo crítico que debe rechazar |
| --- | --- | --- |
| AT01 | Ruta protegida verifica permisos reales de backend | Guard frontend aislado concede datos sin permiso |
| AT02 | Navegación completa por teclado y foco visible | Modal encierra el foco o impide salida |
| AT03 | Texto, labels y mensajes de error son anunciables | Control sin nombre o error sólo por color |
| AT04 | 200–400% zoom/reflow conserva operación | Horizontal scroll obligatorio en contenido crítico sin alternativa |
| AT05 | Preferencia reduced-motion evita animación no esencial | Transición forzada produce mareo o pérdida de foco |
| AT06 | Interacción vacía/timeout recupera foco y estado | Pérdida de datos o spinner perpetuo |
| AT07 | Muestra de rendimiento declara red, entorno y distribución | Número sin baseline, unidades o fecha se presenta como PASS |
| AT08 | Control tenant A/B en API y UI evita filtración | ID directo del tenant B devuelve recurso a A |
| AT09 | SQL de auditoría futura respeta acceso mínimo y rollback | Migración destructiva sin copia ni prueba reversa |
| AT10 | CI del SHA pertinente verifica suites causales | CANCELLED, SKIPPED o IN_PROGRESS se interpreta SUCCESS |
| AT11 | A5 registra revisor diferente y blob original | Autor firma su propio entregable |
| AT12 | Bitácora append-only y evento idempotente | Reintento edita fila histórica o duplica claim |

**AT01–AT12 / sus negativos son FUTUROS_NO_EJECUTADOS.** No equivalen a resultados de axe, Lighthouse, Playwright, simulación de dos empresas, benchmark, prueba HTTP, MySQL o validación WCAG. Un evaluador debe indicar denominadores, herramientas, fecha, SHA y defectos hallados antes de certificar una fase ejecutable.

## 4. Matriz de avance y no-avance

| Decisión | Hecho requerido | Acción segura |
| --- | --- | --- |
| Publicar contrato M02.06 en dev | PLAN/COLA/DAG aptos + owner exclusivo + dos Markdown + CI Scope Lock/V5 exact SHA | Permitir sólo LISTO_DEV documental con A5 pendiente |
| Habilitar sucesoras M03 | M02.01–06 LISTO y A5 según DAG, código/alcance propio autorizado | Releer PLAN/COLA/DAG tras A5 de M02.06; nunca forzar SI |
| Implementar UI Angular 22.2.1 | Story/archivo real, guards y superficie permitida, pruebas dirigidas | Cambio posterior separado con alcance y rollback |
| Cambiar API JWT/RBAC/tenant | Permiso explícito, matriz de autorización, regresiones tenant A/B | No ampliar permisos desde documento M02 |
| Introducir MySQL/EF, outbox o eventos | Scope de código realmente habilitado y CI efímera | No tocar DB ni encender transporte en M02.06 |
| Promover QA/main/PROD | Orden diferenciada del propietario y checks pertinentes | Permanecer en dev; A5 no promueve por sí misma |

La política V5 de despacho inmediato exige reevaluar las sucesoras **en el mismo run** después de CLAIM, VALIDANDO, LISTO y A5_PASS. Sin A5 de este M02.06 no se declara M03 habilitada. Si la fórmula no cambia pese a verdaderos prerrequisitos, diagnosticar por ID/fecha/owner y corregir únicamente error probado, no editar gates para fingir disponibilidad.

## 5. Plan de contención y rollback de M02.06

1. **Gate A0:** revisar diff mínimo, objetivos y no-alcance; preservar `CHANGELOG_AI.md` sin encabezados vacíos ni viñetas cruzadas; no introducir secretos.
2. **Gate A1 documental:** verificar tabla B01–B14 y AT01–AT12 positivos/negativos; describirlos como no ejecutados; no marcar PASS de E2E/seguridad real.
3. **Gate A2:** contrastar fronteras JWT/RBAC/tenant A/B, permisos backend, PII, accesibilidad y Scope Lock; prohibir downgrade de controles.
4. **Gate A3:** enlazar de manera verificable M02.01–05, M01.06 y fuentes PLAN/COLA/DAG/BITACORA; .NET, Angular, MySQL, Playwright sin diff runtime: NO_APLICA_DOCUMENTAL.
5. **Gate A4:** publicar en `dev` con fast-forward y expected-HEAD; sólo este Markdown más `CHANGELOG_AI.md`. Verificar checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` como `completed/success` exact-SHA o una equivalencia posterior demostrada con blob inalterado y CI SUCCESS en el nuevo HEAD. CANCELLED original nunca es PASS.
6. **Gate A5:** revisor otra Task Javier valida SHA, diff, dependencia, contenido y CI, agrega evento idempotente a BITACORA, actualiza K y relee fórmula y sucesoras.

**Rollback documental:** `git revert` normal en dev del commit de dos Markdown, revalidación de HEAD, checks y evento nuevo; no `reset --hard`, no force-push, no borrar registros históricos, no cambiar fórmulas ni anular A5 anteriores. La marcha atrás de una implementación posterior sólo puede definirse por su alcance real.

## 6. Resultado exigido y límites de certificación

Resultado verificable de esta unidad: **un contrato completo de salida y riesgos M02** que mantiene explícitos los catorce gates y la frontera entre preparación y pruebas. M02.06 puede pasar a LISTO_DEV sólo cuando su artefacto, checks documentales, diff y readbacks sean reales; **A5_PENDIENTE** hasta revisión independiente. No se certifica conformidad WCAG, rendimiento medido, ausencia de P0/P1, restauración de datos ni seguridad de producción.

Se conservan las seis Tasks Javier y sus horarios; no se instalan servicios, triggers ni bucles entre Tasks. Cualquier impedimento requiere diagnóstico causal, conservación de owner/estado y continuación con otra unidad independiente segura.
