# SOLQARYN V5 — C15-04.M01.05: criterios de aceptación UX, accesibilidad y rendimiento

**Autor:** PRIMARY DEV 04 JAVIER; TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Fecha:** 2026-11-14 America/Tegucigalpa. **Scope:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Modalidad:** especificación documental, no certificación ni ejecución browser.

## Alcance y fuentes de verdad

STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; PLAN_MAESTRO fila 2274; COLA G/O=SI, DAG_TECNICO F=VALIDADO/G=SI y owner único por CAS al inicio. M01.01 `b6ce51bd1bfce97b1d6f86b9af433e63922bd9bd` contiene NR01–NR14; M01.02 `b37356938d72e2c120d082c2bdfe1b2bf6dee152` describe A01–A12, IO01–IO12 y F01–F10; M01.03 `0bdf78db8c82b8edc0fec223012a66e2b780be2c` describe fuentes S01–S12 y brechas G01–G12; M01.04 `a58bb12ee7d4c40799cebb00d1d5e87a2918a01e` define R01–R12, S01–S08 y D01–D08 como **hipótesis NO_VERIFICADAS**.

El contrato diferencia rutas Angular presentes de landing, storefront, portal y builder aún por comprobar. Angular/Material/CDK 22.2.1 y Zone.js 0.16.3 no sustituyen reglas API .NET 10, JWT, RBAC o aislamiento de tenant A/B. **Esta unidad no ejecuta Playwright, Lighthouse, lector de pantalla, pruebas HTTP ni mediciones de rendimiento.** Ningún criterio que sigue representa un PASS real.

## Dieciséis criterios AC01–AC16, cada uno con prueba positiva y negativa FUTURA_NO_EJECUTADA

| Criterio | Positivo observable exigido en un test futuro | Negativo que debe detectarse o rechazarse | Trazabilidad |
| --- | --- | --- | --- |
| AC01 | Ruta pública realmente implementada accesible sin identidad privada | Informa token, nombre o recurso privado a visitante | NR01/11, G01, R01 |
| AC02 | Rutas privadas muestran solo acciones autorizadas | UI oculta mal controles o concede acción de API por tema/guard | NR11, F02, R01 |
| AC03 | CTA principal conserva significado y orden visual | CTA solapado, duplicado ambiguo o inaccesible | NR01, G05 |
| AC04 | Desde 320 px a desktop mantiene contenido y acciones clave | Corte, overlay involuntario o scroll horizontal global | NR02, G05/G06, R02 |
| AC05 | Tab/Shift+Tab recorre componentes con orden lógico | Foco invisible, secuencia saltada o interacción mouse-only | NR03/04, G03, R03 |
| AC06 | Enter/Espacio activan controles nativos/compatibles | Control clicable sin operación por teclado | NR03, IO03 |
| AC07 | Diálogo contiene foco de forma correcta y lo restaura al cerrar | Foco perdido o atrapado tras cierre | NR04, F05, R04 |
| AC08 | Campo presenta nombre, ayuda y validación textual asociada | Error sólo color o ausencia de anuncio accesible | NR05, G04, R05 |
| AC09 | Iconos e imágenes relevantes tienen alternativas apropiadas | Icono exclusivo o imagen crítica sin nombre/semántica | NR06, G04, R06 |
| AC10 | Preferencia reduced-motion conserva estado sin efectos no esenciales | Movimiento intrusivo o contexto desaparece | NR07, G07, R07 |
| AC11 | Carga, vacío y error proporcionan información y recuperación | Spinner infinito, fallo silencioso o acción sin reintento | NR08, G08, R08 |
| AC12 | Tokens de color/typography y estados se leen con contraste medido | Branding deja controles ilegibles o usa sólo color | NR09, G02, R09 |
| AC13 | Tabla/lista mantiene filtros y scroll en contenedor apropiado | Tabla desborda página o bloquea teclado | NR10, G06 |
| AC14 | Cambiar tenant A/B no conserva caché, marca ni datos no autorizados | Recurso de B visible para A o viceversa | NR12, G10, R11 |
| AC15 | Captura de rendimiento cita commit, red, dispositivo, muestra y baseline | LCP/CLS/INP/latencia declarada sin contexto o fuente | NR13/14, G11, R10 |
| AC16 | Evidencia A0–A5 y CI corresponde a commit SHA exacto | Check skipped, cancelled, queued o ajeno computado PASS; A5 propio | NR14, F10, R12 |

**AC01–AC16 y sus negativos se encuentran NO_EJECUTADOS.** Para funciones no implementadas debe registrarse `NO_APLICA_FUNCIONALIDAD_NO_EXISTENTE`, sin sustituir una prueba por un documento. No se fija una cifra de accesibilidad o rendimiento sin norma y baseline aprobados; los criterios de severidad dependen de evidencia reproducida.

## Diez requisitos no funcionales a medir en fases autorizadas

| RNF | Exigencia y evidencia mínima antes de afirmar cumplimiento |
| --- | --- |
| RNF01 | Responsive: viewport, zoom, dispositivo y texto largo documentados, sin pérdida de funcionalidad |
| RNF02 | Operación de teclado: matriz de navegación y foco trazada por ruta y control |
| RNF03 | Semántica accesible: nombres/roles/estados evaluados con árbol accesible y revisión manual apropiada |
| RNF04 | Contraste: tema, token, estado y método de medición registrados con criterio normativo aprobado |
| RNF05 | Movimiento: pruebas con reduced-motion habilitado y deshabilitado, sin pérdida informativa |
| RNF06 | Estabilidad visual: baseline y mediciones reproducibles de layout, con muestra y distribución |
| RNF07 | Percepción de carga: red simulada, hardware y percentiles definibles antes de fijar presupuesto |
| RNF08 | Robustez: comportamiento observable ante fallo de API, desconexión y reintento con datos sintéticos |
| RNF09 | Seguridad multiempresa: prueba negativa A/B que valide backend JWT/RBAC y limpieza de estado/caché |
| RNF10 | Trazabilidad: enlace entre ID, ruta, fixture, caso, SHA, CI, hallazgo, responsable y retesteo |

Los RNF01–RNF10 se consideran `PENDIENTE_MEDICION`. La especificación de un presupuesto no equivale al cumplimiento de ese presupuesto. No usar screenshots como certificación de autorización server-side.

## Matriz de ejecución futura y decisión de aprobación

Para cada caso AC registrar: ID de contrato, ruta/componente efectivamente existente, capacidades excluidas, navegador/versión, viewport/zoom, dispositivo, tenant/rol/fixture sintético, preferencia motion, red, paso a paso, esperado y observado, captura/trace, versión SHA, run/job, estado de CI, severidad y rollback. Probar al menos un positivo y un negativo por criterio aplicable, incluyendo cambios entre A y B. Correlacionar defectos con G01–G12 y riesgos R01–R12, sin asumir que todos se materializaron.

**Condiciones para un PASS futuro:** prueba realmente ejecutada y pertinente, evidencia reproducible, `head_sha` correcto y `completed/success`, resolución de defectos críticos, revisión humana donde corresponda y aprobación A5 independiente. Un run en curso, cancelado, omitido, de SHA anterior o procedente de un caso no relacionado no es PASS.

**M01.06 (cierre de preparación)** no se libera automáticamente por completar M01.05: exige M01.01–05 completadas, A5 independiente, ausencia de gate causal bloqueante y nuevo preflight DAG/PLAN/COLA. Ningún resultado aquí autoriza QA, main, producción, DNS, Hostinger, Cloudflare, migraciones ni integración de builder.

## Alcance A0–A5, evidencia y rollback

- **A0:** consistencia entre los 16 criterios, 16 negativos, 10 RNF, NR01–NR14 y brechas G01–G12, sin datos empíricos inventados.
- **A1 documental:** evaluación lógica de oráculos positivos/negativos y condiciones de `NO_APLICA` por función no implementada; test browser `FUTURO_NO_EJECUTADO`.
- **A2 seguridad:** diferencia entre experiencia Angular y autorización real .NET/JWT/RBAC/tenant A/B, sin cambio de código ni permisos.
- **A3 integración documental:** traza de fuentes M01.01–04, rutas/SCSS/Playwright citados y criterios de evidencia; HTTP/MySQL/Playwright runtime `NO_APLICA` para diff sólo Markdown.
- **A4:** GitHub commit directo por fast-forward y CAS a `dev`, diff de este documento + sección completa CHANGELOG_AI.md, Scope Lock y procedencia V5 exact-SHA `completed/success`, readback PLAN/COLA/BITACORA.
- **A5:** revisión posterior por otra Task Javier con TASK_ID distinto del autor, conservando el owner histórico, sin promoción automática.

**Rollback:** git revert ordinario de los cambios documentales, preservando BITACORA como historial aditivo; no force-push, cambios de producción ni silenciamiento de defectos.
