# SOLQARYN V5 — C14-03.M02.01: entidades, ownership e invariantes de promoción controlada

**Autor:** PRIMARY DEV 03 (`6adc2e4bd8a48191ac7d8641ed95a1a3`). **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). **Fecha:** 2026-11-15. **Tipo:** contrato documental de diseño M02, **no despliegue**.

## Alcance autorizado y referencias

Se define un **modelo conceptual** de artefactos, actores, autoridad, gates, evidencia y rollback para una futura **promoción controlada**. No se implementa release manager, endpoint, CI, runner, evento, webhook, tabla de control o integración de despliegue. La frontera real del trabajo permanece `dev`; no se interactúa con QA, main ni PROD.

`C14-03.M01.01–06` se encuentran LISTO y con A5 independiente sobre cada artefacto; la M01.06 documentó G01–G16, RB01–RB10 y P/N futuros sin ejecutar pruebas runtime. El claim de M02.01 verificó por ID `DAG_TECNICO F=VALIDADO/G=SI` y `COLA G/O=SI` además de PLAN PENDIENTE sin owner. Ese resultado **autoriza sólo documentar este contrato**, no afirmar paridad real de ambientes ni autorizar la promoción.

Fuentes a verificar en implementación futura: arquitectura de `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_AUTHORITY.md`, check-runs GitHub Actions del SHA exacto, seguridad tenant/JWT/RBAC y credenciales fuera de repo. No suponer que la existencia de un check implica PASS.

## Entidades y ownership propuestos — no persistidos

| Entidad conceptual | Propietario de decisión | Reglas y relaciones |
| --- | --- | --- |
| `ReleaseCandidate` | Responsable DEV de cambio autorizado | Referencia inmutable a SHA, tree y artefactos, nunca mutable por un estado de Sheets |
| `ArtifactManifest` | Proceso de build controlado | Digest, versiones .NET/Angular, componentes y dependencias; vínculo N:1 con candidato |
| `TargetEnvironment` | Administrador de ambiente autorizado | Identidad de QA o PROD fuera de esta autorización; `dev` nunca equivale a target |
| `PromotionApproval` | Humano/rol autorizado de ambiente | Alcance, objetivo, expiración, SHA, revisor y justificación separadas de A5 |
| `EvidenceBundle` | Autor/verificador del check-run | Pruebas pertinentes, outcome nativo, SHA y logs seguros por gate |
| `SecurityGate` | Seguridad técnica independiente | JWT/RBAC/tenant A/B, secretos, escaneo y config observables sin bypass |
| `ChangeDependency` | Plan/DAG institucional | Antecesores LISTO+A5 por ID exacto; DAG no equivale a permiso del ambiente |
| `DeploymentIntent` | Ejecutor privilegiado futuro | Acción propuesta no ejecutada, idempotencia y CAS contra versión del destino |
| `RollbackPlan` | Propietario de servicio y datos | Estado anterior verificable, precondiciones, backup/restore probado y plan de reversión |
| `HealthEvidence` | Observabilidad de entorno | Señales reales de disponibilidad/seguridad posteriores, no inferidas de texto |
| `AuditDecision` | Auditoría institucional | Evento inmutable actor/objeto/acción/resultado/timestamp sin PII/secretos |
| `PromotionOutcome` | Proceso de release autorizado | Estado resultado y SHA efectivo, sujeto a readback del destino; sin falsos PASS |

Son propuestas de estructura de datos, **no tablas EF/MySQL ni clases C#**. Una release candidate puede tener múltiples evidencias/gates; una aprobación corresponde a una combinación exacta de candidato, objetivo y ventana; un outcome real requiere una operación autorizada y evidencia de destino, nunca solo texto.

## Invariantes para futuras implementaciones

1. **REL01:** cada candidato referencia SHA/tree inmutables, y su revisión es invalidada cuando cambia su contenido afectado.
2. **REL02:** aprobar DEV A0–A5 no autoriza QA, main o producción; cada target precisa permiso separado.
3. **REL03:** ninguna Task de este allowlist infiere rol de despliegue de otro ambiente por su nombre, schedule o propiedad.
4. **REL04:** un gate `queued`, `in_progress`, `cancelled`, `skipped` o `failure` no satisface un check obligatorio.
5. **REL05:** CI causal debe ser `completed/success` sobre SHA exacto, o equivalencia causal comprobada sobre blobs afectados y SHA nuevo.
6. **REL06:** Scope Lock STRICT siempre se verifica en SHA de publicación; ningún fallback permite rama o repositorio distintos.
7. **REL07:** manifest y dependencias tienen digest/versiones verificables, no etiquetas `latest` mutables.
8. **REL08:** promoción no puede modificar datos productivos sin respaldo, restauración y rollback probados bajo autorización adicional.
9. **REL09:** las pruebas tenant A/B incluyen JWT válido, revocado, membresía activa, RBAC por operación, backend y consulta/worker.
10. **REL10:** jamás exponer secretos, tokens, endpoints sensibles, PII ni connection strings en manifests/logs.
11. **REL11:** la prueba de paridad debe considerar .NET 10, Angular 22.2.1, MySQL 8.4 y contratos externos por componente.
12. **REL12:** A5 independiente es una revisión técnica DEV; no se convierte en autorización humana de QA/PROD.
13. **REL13:** bloqueo de DAG, dependencia sin A5 o propietario ajeno activo impide tomar el scope de ese ID.
14. **REL14:** CAS de Git requiere HEAD esperado fast-forward; Sheets read+update+readback no equivale a CAS atómico fuerte.
15. **REL15:** cambios de configuración requieren scope por ambiente; feature flags del frontend no sustituyen permisos backend.
16. **REL16:** política de release debe ser fail-closed ante rollback no ensayado, check faltante o estado remoto no corroborado.
17. **REL17:** cada ejecución futura debe preservar BITACORA append-only, EVENT_ID idempotente y readbacks de fuente única.
18. **REL18:** health/readiness y rendimiento han de provenir de telemetría real de target tras una acción autorizada, no del artefacto.
19. **REL19:** jobs en progreso pueden mantener scope VALIDANDO; no bloquear otras tareas independientes por una CI irrelevante.
20. **REL20:** la presente M02.01 no crea Task, PR, rama, dispatcher, deployment, workflow nuevo, secret ni cuenta.

## Oráculos positivos y negativos FUTUROS, NO EJECUTADOS

| Positivo hipotético | Negativo a bloquear |
| --- | --- |
| P01 Candidato tiene SHA, tree y manifest verificables | N01 Digest difiere del árbol publicado |
| P02 Gate requerido completó success exact-SHA | N02 Cancelled/skipped cuenta como PASS |
| P03 A5 independiente revisa autor distinto | N03 Autor se autopresenta como revisor |
| P04 QA tiene autorización humana específica y auditada | N04 LISTO_DEV se usa para desplegar QA |
| P05 PROD requiere aprobación distinta de QA | N05 QA PASS activa PROD automáticamente |
| P06 Tenant A conserva acceso exclusivo a recursos A | N06 Tenant B obtiene datos A por endpoint/worker |
| P07 Migración efímera demuestra rollback reversible | N07 MySQL irreversible no tiene restore probado |
| P08 Secretos están fuera de artefactos y logs | N08 Logs revelan token/clave o firma |
| P09 CAS de HEAD y destino impide sobrescribir otro cambio | N09 Ref cambió y la publicación no revalida |
| P10 Health real se observa en target autorizado | N10 Documento se toma como métrica real |

**Todos son criterios futuros no ejecutados.** No hay permisos a QA/main/PROD, no se corrió Playwright, no se probó restore de MySQL ni se ha desplegado nada desde este contrato.

## A0–A5 y rollback

**A0:** inspección de este documento y sección CHANGELOG, owner y ausencia de cambios ejecutables/secretos. **A1:** presencia y coherencia de 12 entidades, 20 invariantes y diez pares P/N FUTUROS. **A2:** bloqueos de seguridad JWT/RBAC/tenant A/B, permisos por ambiente, data rollback y secretos. **A3:** trazabilidad M01.01–06 LISTO+A5 y DAG, sin inferir tests de entorno; `.NET/Angular/MySQL/Playwright` = NO_APLICA causal a dos Markdown. **A4:** commit fast-forward CAS en dev, Scope Lock/V5 check `completed/success` exact SHA, PLAN/COLA/DAG/BITACORA readbacks. **A5:** revisión independiente por otra Task Javier; nunca revisión por Task autora.

**Rollback de esta entrega:** `git revert` normal del changeset Markdown, conservando historia institucional. No se cambian proveedores, DNS, QA/main/PROD, datos productivos, secrets, releases ni schedules.
