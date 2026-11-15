# SOLQARYN V17 — C13-02.M02.01: modelo conceptual de accesibilidad por superficie y tenant

**Microtarea:** C13-02.M02.01, PLAN_MAESTRO fila 1916. **Responsable autor:** PRIMARY DEV 06, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Riesgo:** R0 (contrato documental sin runtime). **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, calidad V15, pipeline V16 y candado V17.

## Alcance verificable y autoridades

Esta unidad define **entidades, ownership, relaciones e invariantes conceptuales** para la evaluación futura de teclado, foco, contraste, formularios, reflow, estado y lector de pantalla. Su criterio de aceptación es un modelo rastreable que permite construir M02.02–05 sin confundir un esquema de evidencia con una prueba ejecutada. **No crea tablas, entidades EF, servicios, componentes Angular, endpoints, migraciones, CI, proveedores, webhooks ni modificaciones de permisos.** No declara conformidad WCAG 2.2 AA ni cumplimiento legal.

Predecesores: `docs/v5/c13-02-m01-01-alcance-no-regresion-accesibilidad.md` a `c13-02-m01-05-criterios-aceptacion-accesibilidad.md`, más `c13-02-m01-06-gates-rollback-liberacion-accesibilidad.md`. La M01.06 quedó LISTO_DEV bajo DEV02 con SHA `0fb5626a332c3d0eee5e800c465085cb59f4200d`, documento blob `1831e51f6718a2fa7be8ea57a9105a98626bf9e1` y revisión A5 independiente DEV06 registrada en BITACORA con `SQ-V17-J50-C1302M0106-A5-0FB5626A-20261115T195341`. Las puertas derivadas de DAG/COLA se verifican por ID y no habilitan QA/main/PROD.

## Modelo de datos conceptual: entidades, propiedad y límites

| Entidad conceptual | Clave y ownership propuestos (NO esquema real) | Relación / propósito |
| --- | --- | --- |
| AccessibilitySurface | surface_key, module_key, ruta, revision, tenant_scope | Superficie de UI concreta; `tenant_scope` debe distinguir GLOBAL_PUBLIC de TENANT_SCOPED |
| AccessibilityActorContext | actor_role, tenant_id?, session_state, revoked | Contexto de actor para probar acceso; no almacena JWT en evidencia |
| AccessibilityControl | surface_key, control_key, semantic_role, state | 1 superficie contiene N controles y estados observables |
| AccessibilityRequirement | requirement_key, standard_version?, authority_status | Obligación propuesta; versión/nivel normativo quedan PENDIENTES de autoridad competente |
| SurfaceRequirementMap | surface_key, requirement_key, applicability, rationale | Unión N:M; documenta NO_APLICA y excepción, no inventa conformidad |
| InteractionSequence | sequence_key, surface_key, input_mode, expected_focus | Ruta de teclado, Escape y restauración de foco; versión del componente |
| VisualEvidenceProfile | viewport, zoom, theme, forced_colors, reduced_motion | Condiciones repetibles para contraste, reflow y motion |
| AccessibilityTestCase | case_key, surface_key, positive_or_negative, expected | Caso rastreable; estado de ejecución independiente del diseño del caso |
| AccessibilityRun | run_key, commit_sha, ci_run_id?, browser?, locale?, timestamp_utc | Prueba efectivamente ejecutada solamente cuando exista evidencia reproducible |
| AccessibilityObservation | run_key, case_key, observed, artifact_digest? | N observaciones por ejecución; capturas redactadas sin PII |
| AccessibilityFinding | finding_key, severity, reproduction, status, owner | Defecto verificable, no conclusión estadística por inferencia |
| AccessibilityDecision | decision_key, authority, applicability, expiry?, risk | Excepción documentada y revocable; nunca altera autorización backend |
| ReleaseEvidenceGate | surface_key, gate_key, evidence_ref?, verdict | Vincula G01–G16 de M01.06; PASS sólo con prueba causal aplicable |
| AccessibilityBaseline | surface_key, source_sha, accepted_checks, regression_link | Línea de comparación que prohíbe declarar cobertura global desde un caso |
| AccessibilityRollbackPlan | surface_key, change_sha, revert_steps, revalidation | Reversión dirigida mediante git revert normal; no rollback destructivo |
| AccessibilityAuditRecord | audit_key, task_id, event_id, immutable_ref | Historia de transiciones y A5 independiente en BITACORA, no sobrescrita |

Estos nombres son **candidatos de contrato, no clases ni migraciones aprobadas**. Para rutas públicas se admite `tenant_id=null` exclusivamente cuando no se consulta un recurso empresarial. Toda ruta privada y cualquier recurso deben resolver y validar ownership en backend. Ninguna evaluación visual autoriza operaciones HTTP.

## Invariantes de seguridad, accesibilidad y evidencias

1. **I01 Tenant:** una observación de tenant A nunca contiene recursos, rutas privadas ni datos de B; todo caso privado porta tenant de fixture y actor autorizable.
2. **I02 Backend:** guards de Angular y el estado visual no sustituyen validación .NET JWT/RBAC/membresía ni revocación.
3. **I03 IDOR:** `resource.tenant_id != actor.tenant_id` produce denegación antes de devolver datos o crear artefactos.
4. **I04 Público:** `GLOBAL_PUBLIC` no concede acceso a datos tenant-scoped; public/private se distinguen en cada Surface.
5. **I05 Claves:** `(surface_key,control_key,revision)` identifica inequívocamente el control conceptual en una versión de UI.
6. **I06 Foco:** interacción teclado tiene estado inicial, secuencia y foco esperado; el caso debe fallar ante pérdida o trampa.
7. **I07 Modal:** apertura/cierre/Escape se validan por ruta y actor; ningún fondo interactivo se declara seguro sin prueba.
8. **I08 Formulario:** nombre accesible, ayuda y error asociado; color por sí solo no acredita comunicación de errores.
9. **I09 Reflow:** medir viewport 320/390/768/1280 y zoom, sin sustituir pruebas por una captura única.
10. **I10 Contraste:** registrar tema, estado y medición real; tokens SCSS sin medición no equivalen a PASS.
11. **I11 Movimiento:** `reduced_motion` mantiene información y controles utilizables; los efectos no son condición de lectura.
12. **I12 Lectores:** idioma, landmarks, orden y descripciones tienen evidencia de prueba humana/automatizada cuando corresponda.
13. **I13 Estados:** loading/empty/success/403/500 conservan orientación, foco y textos sin secretos o PII.
14. **I14 Evidencia:** `PASS` de una prueba exige `run_key+source_sha+expected+observed+artifact`; caso diseñado sin run es FUTURO_NO_EJECUTADO.
15. **I15 Revisiones:** checks CANCELLED/SKIPPED/IN_PROGRESS/FAILED nunca son PASS requerido ni se heredan sin equivalencia de blobs y dependencias.
16. **I16 Privacidad:** no almacenar JWT, cookies, PII ni capturas no redactadas en artifacts o el modelo conceptual.
17. **I17 Excepciones:** ausencia de autoridad sobre WCAG significa NO_CERTIFICADO; excepciones requieren propietario, expiración y trazabilidad.
18. **I18 DAG:** una M02 posterior requiere su propia elegibilidad PLAN/COLA/DAG y A5 reales; no se libera M03 por este contrato.
19. **I19 Idempotencia:** `event_id` de auditoría es único; revisión/reintento no crea aprobación duplicada ni reescribe BITACORA.
20. **I20 Reversibilidad:** cambios futuros incluyen plan de git revert, re-test de permisos y verificación de experiencia previa.

## Relaciones y aceptación futura (no ejecutada)

`AccessibilitySurface 1:N AccessibilityControl`, `AccessibilitySurface N:M AccessibilityRequirement` por `SurfaceRequirementMap`, `AccessibilitySurface 1:N AccessibilityTestCase`, `AccessibilityTestCase 1:N AccessibilityObservation` mediante `AccessibilityRun`. Las decisiones de alcance/estándar y excepciones afectan el mapeo de requisitos, nunca la autorización real. `AccessibilityFinding` puede relacionarse con múltiples observaciones, y `ReleaseEvidenceGate` exige evidencias pertinentes antes de un veredicto.

| Caso futuro | Positivo esperado | Negativo que debe fallar |
| --- | --- | --- |
| P01/N01 | tenant A ve su superficie permitida | A intenta leer recurso empresarial B |
| P02/N02 | rol vigente accede a función autorizada | revocado o no miembro salta guard |
| P03/N03 | Tab/Shift+Tab/Enter siguen secuencia | control esencial sólo accesible por mouse |
| P04/N04 | modal cierra y restaura foco | foco atrapado/perdido tras Escape |
| P05/N05 | label y error textual están asociados | error sólo rojo/aria inválida |
| P06/N06 | zoom/reflow conserva acción a 320px | acción se recorta y no se alcanza |
| P07/N07 | tema/forced-colors supera medición aplicable | contraste/foco insuficiente |
| P08/N08 | reduced-motion conserva información | animación obligatoria perjudicial |
| P09/N09 | error 403/500 accesible sin PII | stack/JWT/PII visible al lector |
| P10/N10 | evidencia real vincula SHA/caso/observado | resultado PASS sin run o artifact |
| P11/N11 | decisión de aplicabilidad tiene autoridad | WCAG AA proclamado sin evaluación |
| P12/N12 | regresión se reproduce y revierte | supuesto rollback rompe permisos A/B |

**Todos P01–P12/N01–N12 son FUTUROS_NO_EJECUTADOS; NO son tests PASS.** M02.02 define interfaces/DTO únicamente después de DAG elegible; M03 o M04 con Angular/HTTP requieren Vitest/Playwright/Chromium, pruebas humanas y seguridad backend A/B dirigidas. No simular integración MySQL 8.4, navegación real o lectores con un Markdown.

## A0–A5, rollback y continuidad

- **A0 R0:** cotejar requerimiento PLAN M02.01, M01.01–06, ownership, 16 conceptos, 20 invariantes y diff exclusivo Markdown/CHANGELOG.
- **A1 R0:** coherencia estática de relaciones y casos P/N definidos; no certifica ejecución de los 12 pares.
- **A2 R0:** verificar fronteras público/tenant, A/B, IDOR, roles y revocación como requisitos pendientes de pruebas futuras, no JWT runtime probado.
- **A3 R0:** `NO_APLICA` build .NET/Angular, MySQL, HTTP, Playwright y proveedores a modificación sólo documental.
- **A4 R0:** publicación fast-forward con HEAD esperado, Scope Lock y V5 `completed/success` del SHA publicado, validación de secciones CHANGELOG y readback PLAN/COLA/DAG/BITACORA.
- **A5:** revisión independiente por otra de las cinco Tasks Javier, verificando fuentes, modelo, limitaciones y pruebas futuras; puede registrar FAIL. LISTO_DEV no sustituye A5.
- **Rollback:** git revert convencional del commit documental, lectura del HEAD y recálculo dirigido de sucesoras; BITACORA histórica se preserva. No modificar QA/main/PROD, credenciales, infraestructura, datos o configuración física de Tasks.

**Resultado esperado:** modelo conceptual trazable de accesibilidad para el próximo contrato M02, no software funcional ni certificación WCAG.
