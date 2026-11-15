# SOLQARYN V5 — C15-04.M01.04: riesgos, supuestos y decisiones de auditoría UX, accesibilidad y rendimiento

**Autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Fecha:** 2026-11-14 (America/Tegucigalpa). **Alcance:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Tipo:** preparación documental, NO auditoría de cumplimiento ni cambio ejecutable.

## Autoridad, antecedentes y límites de evidencia

Fuente única: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. PLAN_MAESTRO fila 2273, COLA G/O=SI y DAG_TECNICO F=VALIDADO/G=SI al claim exclusivo. El objetivo de M01.04 es **registrar riesgos potenciales, supuestos verificables y decisiones abiertas**, no declarar defectos observados ni certificar WCAG.

Antecedentes Git: M01.01 blob `b6ce51bd1bfce97b1d6f86b9af433e63922bd9bd` (NR01–NR14 y P01–P08/N01–N08); M01.02 blob `b37356938d72e2c120d082c2bdfe1b2bf6dee152` (A01–A12, IO01–IO12, F01–F10); M01.03 blob `0bdf78db8c82b8edc0fec223012a66e2b780be2c` (S01–S12, G01–G12). Las fuentes Angular/SCSS/Playwright citadas en M01.03 identifican contratos y configuraciones presentes; **no aportan resultados de navegador, lector asistivo ni métricas de campo**.

El stack vigente conserva Angular 22.2.1 con Material/CDK y Zone.js 0.16.3, ASP.NET Core net10.0 y seguridad JWT/RBAC/tenant en backend. La UX no puede autorizar recursos de otra empresa. Los objetivos Hostinger/Cloudflare y el builder son futuros y no deben confundirse con capacidades verificadas.

## Doce riesgos potenciales (R01–R12): ABIERTOS, NO_REPRODUCIDOS

| Riesgo | Hipótesis de fallo a investigar | Brecha fuente | Prueba causal futura requerida |
| --- | --- | --- | --- |
| R01 | Landing, portal o CTA no distinguen claramente lo público de lo privado | G01/G09; NR01/NR11 | Matriz de rutas existentes y respuesta real sin autenticación/roles |
| R02 | A 320 px, zoom o texto largo se corta una acción crítica | G05/G06; NR02/NR10 | Viewports fijados, capturas y ausencia de corte interactivo |
| R03 | Menú, tabla o formulario depende de mouse | G03; NR03 | Tab, Shift+Tab, Enter y Espacio completos en browser |
| R04 | Un diálogo pierde, atrapa indebidamente o no restaura el foco | G03/G04; NR04 | Secuencia de apertura/cierre y foco anterior/posterior |
| R05 | Error de formulario se expresa sólo con color o sin nombre accesible | G04/G08; NR05 | Error asociado al campo, texto y anuncio verificables |
| R06 | Iconos, imágenes o estados dinámicos quedan sin alternativa significativa | G04; NR06 | Revisión de árbol accesible y tecnología asistiva real |
| R07 | Movimiento reducido elimina información o no reduce efectos prescindibles | G07; NR07 | Prueba con prefers-reduced-motion y comparación de estados |
| R08 | Loading, vacío y error inducen espera indefinida sin recuperación | G08; NR08 | Red degradada, fallos inducidos y reintento visible |
| R09 | Personalización de color/tema rompe contraste o consistencia | G02; NR09 | Mapa token/componente/estado con contraste medido |
| R10 | Se atribuye rendimiento adecuado sin baseline, dispositivo o red definidos | G11/G12; NR13/NR14 | Medición reproducible con SHA, fixture, red, muestra y percentiles |
| R11 | Caché o branding de tenant A muestra estado o recurso de B | G10; NR12 | Cambios A/B con respuestas API autorizadas y datos sintéticos |
| R12 | Un check omitido, cancelado, en curso o de otro SHA se presenta como PASS | G12; F10/NR14 | Comprobar status=completed, conclusion=success, head_sha exacto |

Estos doce ítems son **riesgos**, no bugs confirmados ni vulnerabilidades probadas. Una fuente CSS, una ruta Angular o un test declarado no bastan para convertirlos en hallazgos. Un fallo observado posteriormente requiere reproducción, severidad sustentada, evidencia redactada sin PII, dueño y retesteo.

## Ocho supuestos que aún necesitan verificación

| Supuesto | Hipótesis provisional | Evidencia que la confirmaría o refutaría |
| --- | --- | --- |
| S01 | Las rutas relevantes están implementadas o claramente excluidas del alcance | Catálogo de rutas, autorizaciones API y estado funcional |
| S02 | Los estilos globales se aplican de forma homogénea a todos los overlays | Capturas por componente y estados con tokens calculados |
| S03 | Las interacciones principales son alcanzables por teclado | Pruebas secuenciales por flujo y visor de accesibilidad |
| S04 | Los formularios exponen nombres, ayudas y errores a lectores asistivos | Inspección real y pruebas manuales documentadas |
| S05 | El layout conserva todas las acciones a móvil, zoom y texto largo | Matriz responsive real y dataset sintético |
| S06 | Preferencia reduced-motion está implementada en vistas aplicables | Casos browser con media query alternada |
| S07 | La experiencia responde de forma usable ante red lenta y respuestas fallidas | Red/perfil/fixture/tiempos publicados y recuperaciones |
| S08 | Branding y datos permanecen aislados cuando cambia el tenant | Backend JWT/RBAC A/B + inspección de caché y estado |

S01–S08 están **NO_VERIFICADOS**; deben invalidarse si la evidencia contradictoria aparece. No se extrapolan resultados de una pantalla a todo el ERP, landing, storefront ni builder futuro.

## Ocho decisiones abiertas, sin autorización implícita

| Decisión | Requisito pendiente de aprobación o definición | Límite actual |
| --- | --- | --- |
| D01 | Fijar rutas y flujos de muestreo, separando implementado/futuro | No tratar builder como disponible |
| D02 | Determinar WCAG aplicable, nivel y criterios por interfaz | Sin certificado ni declaración de cumplimiento |
| D03 | Definir viewports, zoom, idiomas, navegador y tecnología asistiva | Sin resultados ficticios ni uso de PII |
| D04 | Aprobar diseño de fixture sintético A/B, roles y sucursales | Sin usar clientes o datos productivos |
| D05 | Fijar umbrales y metodología de contraste, payload y métricas de experiencia | Sin inventar LCP/CLS/INP ni presupuestos |
| D06 | Clasificar defectos bloqueantes, responsables, retesteos y evidencia | No certificar ausencia de P0/P1 sin auditoría |
| D07 | Concretar CI causal Playwright/Angular y artefactos exact-SHA | Checks auxiliares no sustituyen E2E real |
| D08 | Fijar rollback y aprobación A5 independiente para cambios posteriores | No promover QA/main/PROD por documentación |

D01–D08 permanecen **PENDIENTES** hasta evidencia y decisión de autoridad competente. El presente artefacto no cambia Angular, API, DB, JWT/RBAC, CSS, proveedores, dominios ni secretos.

## Ocho pares de escenarios positivos/negativos FUTUROS_NO_EJECUTADOS

| Caso | Oráculo positivo a observar | Oráculo negativo que debe fallar |
| --- | --- | --- |
| P01/N01 | Rutas existentes distinguen visitantes y usuarios autorizados | Ruta pública revela identidad/datos privados |
| P02/N02 | Menús/CTA funcionan a 320 px y desktop | CTA tapado, scroll global o texto cortado |
| P03/N03 | Teclado completa flujo y modales restituyen foco | Interacción mouse-only o foco extraviado |
| P04/N04 | Error de formulario posee label, texto y anuncio | Error sólo cromático, sin foco o anuncio |
| P05/N05 | Tokens/branding mantienen contraste verificado | Tema A degrada legibilidad o muestra marca de B |
| P06/N06 | Motion reducido preserva estados y contexto | Animación intrusiva u ocultación de información |
| P07/N07 | Red lenta presenta progreso, error y reintento finito | Spinner infinito o recuperación silenciosa |
| P08/N08 | Pruebas y métricas pertenecen a SHA y fixtures identificados | Cifra sin baseline, job skipped o SHA distinto |

Cada futura prueba debe registrar versión/browser, viewport/zoom, red/dispositivo, fixture sintético, tenant, paso, esperado, observado, captura/traza, SHA y defectos. **Ningún P/N anterior se ejecutó en esta microtarea**. Su definición sí es un resultado documental verificable.

## Auditorías documentales, dependencias y reversión

- **A0 (estática):** revisar correspondencia R01–R12 ↔ G01–G12 y NR01–NR14, ocho supuestos, ocho decisiones y ocho pares de escenarios; no introducir afirmaciones métricas ni hallazgos sin prueba.
- **A1 (documental):** comprobar que los pares P/N tienen oráculos falsables; los tests browser quedan `FUTUROS_NO_EJECUTADOS`, no PASS.
- **A2 (seguridad):** delimitar fronteras API frente a Angular, JWT/RBAC y aislamiento tenant A/B sin modificar controles de producción.
- **A3 (integración documental):** rastrear los tres documentos M01 precedentes y 12 brechas; integraciones HTTP/MySQL/browser son `NO_APLICA_RUNTIME_DOCUMENTAL` en este diff, nunca PASS.
- **A4 (publicación):** nuevo Markdown y sección autocontenida de CHANGELOG, fast-forward/CAS `dev`, verificadores Scope Lock y procedencia V5 `completed/success` en SHA exacto, PLAN/COLA/BITACORA readback.
- **A5:** evaluación independiente de otra Task Javier sobre documento, diff y SHA; pendiente tras LISTO_DEV; no autoaprobación.

**Rollback:** git revert ordinario del commit documental con nuevas anotaciones aditivas de auditoría si procede; sin reescribir historia ni usar QA/main/PROD. M01.05 define criterios medibles, y M01.06 exige dependencias y A5 antes de liberar M02. Esta preparación no libera migraciones, tokens, bases de datos, deployment ni funciones futuras.
