# SOLQARYN V5 FINAL — C13-02.M01.04: riesgos, supuestos y decisiones de accesibilidad

**Microtarea:** C13-02.M01.04 — registrar riesgos, supuestos y decisiones pendientes. **Autor:** PRIMARY DEV 05 JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Fecha:** 2026-11-14. **Alcance:** preparación documental M01, exclusivamente `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

**Autoridades:** [STANDARD DE IMPLEMENTACIÓN V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit); [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). PLAN_MAESTRO fila 1913, DAG_TECNICO fila 1874: F=VALIDADO/G=SI sólo habilita esta preparación; no habilita migraciones, M01.06, QA, main ni PROD.

## 1. Baseline de evidencia y límites

Los contratos M01.01 (NR01–NR14; blob `7625e8638e3b228f8773ffe11541b0299228fda9`), M01.02 (actores A01–A14, entradas I01–I12, salidas S01–S10, fronteras F01–F08; blob `fdf905569b0d289006d86f28b59bc70b26a648e7`) y M01.03 (inventario de 12 fuentes y brechas B01–B14; blob `8846d8b30b56c659685396014230e3cd20061aa1`) sirven de insumo. En Git existen `frontend/e2e/c00-03-m04-05-accessibility.spec.ts`, `frontend/src/app/features/centros-costo/centros-costo.a11y.spec.ts` y `frontend/src/styles/_m10-accessibility.scss`, pero **la existencia del archivo no constituye ejecución, cobertura integral ni certificación WCAG**.

**Clasificación:** los R01–R14 siguientes son *riesgos potenciales*, no defectos observados ni vulnerabilidades demostradas; los S01–S10 son *supuestos por comprobar*; los D01–D10 son *decisiones abiertas*. No se ejecutó scanner, lector de pantalla, browser, medición de contraste, prueba HTTP/MySQL ni cambios de código en esta microtarea. Todos los casos de la sección 4 son FUTUROS_NO_EJECUTADOS.

## 2. Registro de riesgos potenciales R01–R14

| ID / brecha | Riesgo potencial y consecuencia | Control preventivo y evidencia futura requerida |
| --- | --- | --- |
| R01 / B01 | Rutas públicas/privadas fuera de muestra se consideran accesibles sin auditoría | Inventario por ruta, rol, tenant, viewport, severidad y muestreo justificado |
| R02 / B02 | Navegación sólo con mouse o foco invisible bloquea acción esencial | Tab/Shift+Tab/Enter/Escape manual y Playwright; orden y captura de foco por SHA |
| R03 / B03 | Overlay o modal atrapa foco o no lo devuelve al disparador | Prueba apertura/cierre/Escape/retorno del foco y árbol accesible |
| R04 / B04 | Formulario sin label/error asociado impide recuperación del usuario | Comprobación nombre accesible, `aria-describedby`, texto de error y anuncio |
| R05 / B05 | Tabla/scroll complejo oculta acciones a zoom o pantalla reducida | Inspección semántica de encabezados, scroll y reflow por viewport |
| R06 / B06 | Tema, estado disabled o forced-colors oculta texto/foco | Contraste medido en navegador y criterios aplicables aprobados |
| R07 / B07 | Reflow 320/390/768/1280 y zoom recortan controles | Capturas y pruebas de teclado/zoom en rutas representativas |
| R08 / B08 | Scanner automático se interpreta como conformidad WCAG integral | Complementar con evaluación manual, lector de pantalla y evidencia de entorno |
| R09 / B09 | Movimiento, flashing o medios sin alternativa excluyen usuarios | Test `prefers-reduced-motion`, alternativas y verificación humana |
| R10 / B10 | Traducciones, idioma de documento o lectura semántica inconsistente | Pruebas por idioma, heading/region/link y orden de lectura |
| R11 / B11 | Estado loading/error/403 no se anuncia o revela información ajena | Estados claros, `aria-live` apropiado, test negativo A/B sin PII en logs |
| R12 / B12 | Se confunde Angular guard con autorización de API; exposición cross-tenant | HTTP/JWT/RBAC y pertenencia A/B negativos sobre backend real antes de afirmar PASS |
| R13 / B13 | Un `.spec.ts` presente o check queued/skipped/cancelled se reporta PASS | Registrar run exact-SHA, status=completed/conclusion=success, artefactos y versión |
| R14 / B14 | Se declara nivel WCAG o cumplimiento legal sin alcance y autoridad | Definir versión, nivel, excepciones, responsables y revisión competente; no certificación anticipada |

**Política transversal de mitigación:** priorizar fallos bloqueantes de interacción, seguridad, datos y orientación de usuario. No desactivar JWT/RBAC/tenant por facilitar las pruebas UI; la interfaz sólo presenta resultados autorizados por el backend.

## 3. Supuestos NO_VERIFICADOS S01–S10 y decisiones ABIERTAS D01–D10

| ID | Supuesto por validar | Decisión pendiente y criterio de resolución |
| --- | --- | --- |
| S01 / D01 | Existe un conjunto representativo de rutas para auditar | Aprobar matriz ruta × rol × dispositivo, con prioridad por impacto y cobertura |
| S02 / D02 | Hay combinación disponible de OS/browser/lector de pantalla | Elegir matriz de tecnologías asistivas y preservar versiones en evidencia |
| S03 / D03 | La norma y nivel aplicables serán definidos formalmente | Fijar WCAG versión/nivel/obligaciones por autoridad competente; no inferir AA |
| S04 / D04 | La navegación por teclado puede observarse de forma repetible | Elegir criterios de foco visible, secuencia, restauración y tests manuales |
| S05 / D05 | Los componentes Material y custom comparten convenciones | Definir patrón de modal, autocomplete, menú y tabla y su prueba negativa |
| S06 / D06 | Los tokens actuales admiten medición por tema/estado | Aprobar umbral aplicable, tooling y muestra de contraste, incluido forced-colors |
| S07 / D07 | Responsive mantiene acciones esenciales a zoom/reflow | Definir matriz 320/390/768/1280 y zoom aplicable, con capturas y tolerancias |
| S08 / D08 | Mensajes async y traducciones pueden anunciarse sin ruido | Definir idiomas/lectura/aria-live, severidad y cuándo desplazar o no el foco |
| S09 / D09 | Fixtures de permisos y tenant A/B son reproducibles | Preparar pruebas HTTP seguras con datos sintéticos, JWT revocado y membresía |
| S10 / D10 | Se puede archivar evidencia sin exponer datos personales | Acordar retención, redacción, acceso a artefactos, ambiente y rollback |

Cada decisión requiere **responsable humano designado, justificación, ámbito, versión, fecha, evidencia y condición de rechazo** antes de convertirse en regla o gate ejecutable. Hasta su aprobación permanece ABIERTA y no bloquea falsamente otras M01 documentales que el DAG marque como VALIDADO/SI.

## 4. Oráculos positivos/negativos futuros, no ejecutados

| Caso | Positivo futuro | Negativo futuro que debe detectarse |
| --- | --- | --- |
| P01/N01 | Teclado recorre y activa acciones con foco visible | Control crítico sólo funciona con mouse |
| P02/N02 | Dialog restaura foco después de Escape | Foco perdido, fondo activo o trampa |
| P03/N03 | Inputs tienen nombre, instrucciones y errores anunciados | Error exclusivamente rojo/input sin nombre |
| P04/N04 | Tabla sigue usable con zoom/reflow | Celdas o botón esencial ocultos en móvil |
| P05/N05 | Contraste por tema/estado satisface criterio aprobado | forced-colors o tema alterno incumple |
| P06/N06 | Cambios async se anuncian sin robo de foco | Loading o 403 silenciosos |
| P07/N07 | Reduced-motion evita efecto no esencial | Animación obligatoria no reducible |
| P08/N08 | API rechaza acceso de tenant A a recursos B | Guard cliente oculta pero backend filtra datos |

Todos: **FUTUROS_NO_EJECUTADOS**. Evidencia posterior por caso: SHA de build, URL/ruta, actor/rol/tenant, fixtures sintéticos, OS/browser/lector, viewport/zoom, pasos, salida esperada/observada, captura/árbol accesible, logs sin credenciales, severidad, corrección, nueva prueba y rollback. Ni el contrato ni los archivos existentes sustituyen resultados.

## 5. Impacto, criterios de revisión y rollback

- **UI/Angular:** sólo se documentan riesgos y acuerdos pendientes; no se editan componentes, routes, CSS, paquetes, zoneless, guards ni Playwright. Stack vigente Angular 22.2.1, Material/CDK 22.2.1, Zone.js 0.16.3.
- **API/capas/MySQL:** sin endpoints, DTO, EF, migraciones ni datos; ASP.NET Core net10.0, MySQL y fronteras Domain/Application/Infrastructure/API permanecen. No se atribuye prueba HTTP/MySQL.
- **Seguridad/tenant:** A/B y JWT/RBAC son obligaciones a validar en backend; no se almacenan secretos/PII ni se propone eludir permisos para accesibilidad.
- **Integraciones/observabilidad:** ningún SDK, worker, evento, analytics, proveedor, infraestructura ni schedule creado. La evidencia futura debe ser reproducible y redactada.
- **A0:** estructura de Markdown, R14/S10/D10/P8-N8, diff limitado a dos Markdown, ausencia de secretos y CHANGELOG con sección y viñetas autocontenidas.
- **A1 documental:** correspondencia entre R01–R14, B01–B14, S/D y oráculos futuros; NO sustituir por prueba funcional.
- **A2 documental:** revisión de autorización server-side, JWT/RBAC/tenant A/B, PII, evidencia sin datos sensibles; tests runtime NO_APLICA a un diff documental.
- **A3 documental:** verificar continuidad M01.01–03 y blobs citados; no inferir compatibilidad runtime ni Playwright PASS.
- **A4:** fast-forward serializado con CAS en `dev`, check `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success del SHA exacto; readback Git, PLAN/COLA y BITACORA.
- **A5:** pendiente de otra Task Javier distinta de la autora. Una A5 documental no certifica WCAG ni autoriza QA/main/PROD.
- **Rollback:** revertir en nuevo commit normal tras revalidar HEAD; evento compensatorio BITACORA append-only. No sobrescribir historial ni compartir evidencia no redactada.

**Resultado de esta M01.04:** registro estructurado de riesgos y decisiones, no producto implementado, no prueba de conformidad ni aprobación regulatoria.
