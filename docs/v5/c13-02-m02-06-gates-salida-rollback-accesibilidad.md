# SOLQARYN V17 — C13-02.M02.06: contrato terminal de salida M02 y rollback de accesibilidad

**Microtarea:** `C13-02.M02.06`, PLAN_MAESTRO fila 1921. **Autor:** PRIMARY DEV 01 JAVIER, TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Repositorio/rama:** `solqaryn/Solqaryn@dev`; **Scope Lock:** STRICT. **Clase:** R0 estrictamente documental; el código futuro de seguridad/tenant/accesibilidad se evalúa R1/R2. **Alcance único:** fijar dependencias, barreras de liberación y rollback del lote M02 sin implementar ni certificar interfaz, servicios, base de datos o estándares normativos.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO, COLA, DAG_TECNICO, V15 calidad, V16 single-active y V17 candado. La condición de elegibilidad de la unidad se acreditó únicamente con `PLAN!H1921=PENDIENTE/I vacío`, `COLA!G/O=SI` y `DAG_TECNICO!F1882=VALIDADO/G1882=SI`; **no concede permiso de desarrollo M03 ni publicación QA/main/PROD.**

## 1. Fuentes, revisión independiente y estado probatorio

| Predecesora | Documento verificado en dev | Blob Git del documento | Límite de su alcance |
| --- | --- | --- | --- |
| M01.06 | `docs/v5/c13-02-m01-06-gates-rollback-liberacion-accesibilidad.md` | `1831e51f6718a2fa7be8ea57a9105a98626bf9e1` | G01–G16 preliminares, A5 independiente, no WCAG |
| M02.01 | `docs/v5/c13-02-m02-01-modelo-conceptual-accesibilidad.md` | `cfa62f35f49c9f588986cd13eddcc69309c38060` | 16 conceptos, 20 invariantes, 12 P/N no ejecutados |
| M02.02 | documento R0 de contratos/DTO de accesibilidad del SHA `43f2e679345dd0ecb395ea6b0fe87f5d79051dc5` | `111e4e9ea6395e3d398ea5856fa6fb746726b18a` | O01–08, D01–08, 12 errores, 16 validaciones |
| M02.03 | `docs/v5/c13-02-m02-03-persistencia-indices-migraciones-fixtures-accesibilidad.md` | `a38fc1d1f62bedb4fc527ca1c073999c633427b1` | 12 registros, 12 claves, seis etapas futuras |
| M02.04 | `docs/v5/c13-02-m02-04-configuracion-tipada-flags-accesibilidad.md` | `1c9f925d0af02101d12f8883e1eac2fa01a8a3c4` | 24 opciones conceptuales, cinco ámbitos, mínimos no desactivables |
| M02.05 | `docs/v5/c13-02-m02-05-eventos-idempotencia-correlacion-accesibilidad.md` | `095503a24165a76fbb51c435c1585dd88c9db3de` | 12 eventos INERTES, 18 invariantes, sin emisor |

La revisión **A5 independiente** de cada M02.01–05 consta por separado en PLAN/BITACORA y precede a la elegibilidad de M02.06: `M02.01` revisada por DEV01, `M02.02` revisada por DEV06, `M02.03/.04/.05` revisadas por DEV01; autoría DEV06 en .01/.03/.04/.05 y DEV02 en .02. El gate `DAG G1882=SI` fue leído después de esas transiciones. Este documento terminal requiere **su propia A5 posterior**, por una Task Javier distinta de DEV01.

**Evidencia no transferible:** las pruebas de navegador, interfaz, lectores, rendimiento o integración de cualquier otro SHA no equivalen a certificar estas funcionalidades. Las P/N de M02 están expresamente FUTUROS_NO_EJECUTADOS; no se crea capacidad real mediante un contrato.

## 2. Dieciséis gates de salida y bloqueo para la fase técnica futura

| ID | Condición verificable | Bloquear si |
| --- | --- | --- |
| G01 | Skill canónica y arquitectura en HEAD, identidad Javier, branch dev y Scope Lock STRICT | permisos, SHA o identidad no acreditados |
| G02 | PLAN H/I y COLA G/O y DAG F/G del ID son actuales; precedentes LISTO+A5 real | un NO, VALIDANDO o revisión pendiente/fallida |
| G03 | V17 lock único por fila y CAS H=1, autor en I, BITACORA/idempotencia y readbacks | conflicto de lock/owner/escritor activo |
| G04 | Alcance de cada superficie versionado con rutas públicas/tenant y matriz actor-rol | cobertura inferida desde rutas no inventariadas |
| G05 | Estándar WCAG aplicable, nivel, autoridad y excepciones identificados antes de proclamar conformidad | falta de decisión o evidencia reglamentaria |
| G06 | Foco visible y navegación Tab/Shift+Tab/Enter/Escape comprobados en rutas afectadas | control de teclado no alcanzable o foco perdido |
| G07 | Overlays/diálogos bloquean fondo y restauran foco al cierre | trampa de foco o escape no funcional |
| G08 | Labels, ayuda, mensajes de error, estados y ARIA comprobados con negativos | error comunicado sólo por color |
| G09 | Reflow/zoom 320/390/768/1280 y estados sin recorte crítico de controles | una captura sustituye escenarios reproducibles |
| G10 | Contraste medido por tema/estado/foco/forced-colors y excepción documentada | tokens CSS usados como PASS |
| G11 | Reduced-motion, landmark/idioma, lector y alternativas evaluadas según aplicabilidad | cumplimiento lector asumido sin prueba |
| G12 | Loading/empty/success/403/500 no exponen datos sensibles y preservan orientación | spinner sin salida, PII o error no recuperable |
| G13 | .NET/API verifica JWT/RBAC/membresía actual, tenant A/B y revocación | guard Angular permite acceso backend |
| G14 | Vitest/Angular y Playwright/Chromium dirigidos, con asserts negativos y logs reproducibles | build verde aislado como cobertura E2E |
| G15 | EF/MySQL 8.4 efímero con Category=Integration realmente ejecutada cuando se cambia DB | mocks/build sustituyen integración |
| G16 | Scope Lock/V5 y checks obligatorios SUCCESS en SHA, revisión A5 independiente, rollback probado | FAIL/CANCELLED/SKIPPED/IN_PROGRESS requerido contado PASS |

G04–G16 describen exigencias **técnicas futuras** cuando se implementen superficies o servicios; no son PASS de esta unidad Markdown. Un cumplimiento visual nunca cambia permisos de servidor.

## 3. Doce pares P/N futuros y criterios anti-regresión (NO EJECUTADOS)

| Par | Comportamiento positivo futuro | Fallo/rechazo obligatorio futuro |
| --- | --- | --- |
| P01/N01 | Tenant A ve sólo datos autorizados A | tenant B o sesión revocada accede por UI/IDOR |
| P02/N02 | Tab, Shift+Tab y Enter recorren controles útiles | sólo mouse accede a acción primaria |
| P03/N03 | Dialog cierra y restaura foco | Escape deja foco oculto o fondo activo |
| P04/N04 | Error/label textual se anuncia de modo semántico | color exclusivo o aria inválida oculta error |
| P05/N05 | Reflow 320px/zoom mantiene controles utilizables | overflow o foco inaccesible impide la acción |
| P06/N06 | Contraste medido por tema satisface criterio aprobado | color no medido obtiene PASS automático |
| P07/N07 | Forced-colors y reduced-motion preservan significado | animación forzosa o foco invisible |
| P08/N08 | Lector/idioma/landmarks respaldados por revisión pertinente | conformidad WCAG declarada sólo desde Markdown |
| P09/N09 | Carga/empty/403/500 mantienen orientación accesible | JWT, PII o stack aparece en error |
| P10/N10 | Evidencia lleva run/commit SHA/caso/resultados/artefacto | SKIPPED/CANCELLED o NOT_RUN cuenta PASS |
| P11/N11 | Revision A5 de autor distinto verifica pruebas y defectos | revisor es autor o aprobación duplicada |
| P12/N12 | Revert/rollback conserva seguridad tenant y documentación histórica | reversión destruye datos de otra empresa |

**0/12 casos ejecutados en esta entrega R0.** Métricas de cumplimiento, cobertura de navegador, MySQL y conformidad WCAG **NO_MEDIDO**. El test runtime futuro deberá tener asserts positivos, negativos, límites y fallos capaces de detectar errores verdaderos, con fixtures multiempresa sintéticos y artefactos trazables.

## 4. Liberación por niveles, dependencia DAG y rollback

**M02.06 documental:** publicar sólo este Markdown y sección autocontenida de `CHANGELOG_AI.md` mediante fast-forward/CAS en dev; registrar transición PENDIENTE→EN_PROGRESO→VALIDANDO→LISTO, resultados A0–A4 y readbacks institucionales. `verify-solqaryn-project-scope` y procedencia V5 requieren `completed/success` sobre SHA causal (o descendiente equivalente con blobs intactos). R0: A1 inspección de diseño, A2 riesgos estáticos JWT/RBAC/tenant/PII, A3 runtime NO_APLICA motivado; no certificar Playwright, Angular, HTTP, MySQL o lectores.

**A5:** un reviewer de las otras Tasks Javier distinto de autor DEV01 revisa de modo independiente fuentes originales, código inexistente de la unidad, contrato, riesgo, SHA y controles; puede rechazar. Hasta ese A5, no tratar sucesoras técnicas como habilitadas.

**M03/M04/M05/M06:** requieren nuevas reclamaciones DAG vigentes y entregas reales, nunca solo este documento. UI/Angular 22.2.1 y Playwright/Chromium dirigen foco/teclado/reflow; backend .NET10 JWT/RBAC/tenant y pruebas de rechazo A/B, EF Core 10.0.12 MySQL 8.4 efímero Category=Integration sólo si cambia persistencia; más autoridad de estándares, trazabilidad por SHA, rollback y A5 por otro autor. R3 QA/main/PROD/irreversible permanece NO_GO sin permiso específico. Nunca forzar `G/O=SI`.

**Rollback R0:** `git revert` normal sobre commit de dos Markdown tras releer HEAD; no rewrite/force-push, ni borrar eventos BITACORA. Si CI obligatoria falla, leer logs del SHA y corregir defecto verificable conservando FAIL, jamás desactivar test. Si un lock queda huérfano, rescate sólo con ausencia de escritor/run y CAS/exclusividad real.

**Conclusión limitada:** salida contractual R0 del lote M02 con doce P/N FUTUROS y gates G01–G16; **sin certificación WCAG, sin implementación de runtime ni autorización para QA/main/PROD**.
