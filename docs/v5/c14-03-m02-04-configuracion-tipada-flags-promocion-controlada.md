# SOLQARYN V5 — C14-03.M02.04: configuración tipada y flags seguros de promoción controlada

**Responsable:** PRIMARY DEV 05 JAVIER (`6adc2e55f47c81919f6374e561687076`). **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Autoridad:** STANDARD V5 FINAL y PLAN_MAESTRO C14-03.M02.04. **Fecha:** 2026-11-15. **Tipo:** contrato documental, no código ejecutable.

## Objetivo y frontera de autorización

Proponer configuración tipada, ámbitos de aplicación y feature flags **para revisión futura, no para ejecutar promociones**. La documentación M01.06 y los contratos M02.01–03 son antecedentes de diseño; no prueban paridad real de ambientes. No se añaden variables, secretos, credenciales, workflows, API, migraciones, servicios, workers, webhooks ni tareas. Sólo se trabaja en `dev` y no se autoriza QA/main/PROD, DNS, infraestructura, datos reales o pagos.

Fuentes: `docs/v5/c14-03-m01-06-bloqueos-rollback-promocion-controlada.md`, `docs/v5/c14-03-m02-01-modelo-entidades-ownership-promocion-controlada.md`, `docs/v5/c14-03-m02-02-api-dto-errores-compatibilidad-promocion-controlada.md`, `docs/v5/c14-03-m02-03-persistencia-indices-migraciones-fixtures-promocion-controlada.md`, `AGENTS.md`, Scope Lock y Skill canónica. Preflight: PLAN=PENDIENTE sin owner, COLA G/O=SI y DAG F=VALIDADO/G=SI de este ID. Tales gates autorizan **diseño documental solamente**.

## Esquema de 16 campos conceptuales, NO IMPLEMENTADOS

| ID | Campo propuesto | Tipo / ámbito / condición fail-closed |
| --- | --- | --- |
| T01 | `CandidateRepo` | enum allowlist por candidato; sólo `solqaryn/Solqaryn` |
| T02 | `CandidateBranch` | enum por candidato; `dev`, no target implícito |
| T03 | `ExpectedCommitSha` | hex de 40 caracteres obligatorio y GitHub-verificado |
| T04 | `ExpectedTreeSha` | hex de 40 caracteres obligatorio, inmutable |
| T05 | `ArtifactDigest` | algoritmo/digest pinneado; prohibido `latest` |
| T06 | `TargetEnvironment` | enum por solicitud; default `NONE` |
| T07 | `EvidencePolicy` | enum; default `EXACT_SHA_REQUIRED` |
| T08 | `ApprovalTtlSeconds` | entero acotado; 0 significa sin aprobación |
| T09 | `DedupeWindowSeconds` | entero acotado; 0 impide efectos |
| T10 | `RollbackEvidenceRef` | referencia opaca; nula hasta ensayo real |
| T11 | `TenantBoundaryMode` | enum backend; `BACKEND_VERIFIED_ONLY` |
| T12 | `AuditRedactionLevel` | enum; default `STRICT` |
| T13 | `MaxReadItems` | entero acotado de sólo lectura; 0 bloquea |
| T14 | `ReviewRequired` | booleano; `true` sin sustituir A5 |
| T15 | `NativeReadbackRequired` | booleano; `true` |
| T16 | `WriteEffectsAllowed` | booleano; `false` fijo para este alcance |

Son tipos *propuestos*, no opciones actuales de ejecución. Valores desconocidos, corruptos, sin versión o provenientes de cliente se rechazarán. No almacenar PII, JWT, tokens, contraseñas ni secretos en manifiestos o BITACORA.

## Doce feature flags futuros, todos desactivados

| ID | Flag propuesto | Default | Límite de efecto |
| --- | --- | --- | --- |
| F01 | `ShowCandidateReadOnly` | false | Sólo metadatos de candidato permitidos |
| F02 | `ShowEvidenceSummary` | false | Referencias redactadas a checks nativos |
| F03 | `EnableGateDryRun` | false | Simulación local sin mutaciones |
| F04 | `ShowBlockingDependencies` | false | Lectura de DAG sin alterar G/O |
| F05 | `EnableSyntheticFixturePreview` | false | Sólo fixtures sintéticos |
| F06 | `ShowRollbackReadiness` | false | Mostrar evidencia, no generar PASS |
| F07 | `EnableReviewChecklistDraft` | false | Borrador no aprobatorio |
| F08 | `ShowNonSensitiveTargetMetadata` | false | Lectura filtrada con backend RBAC |
| F09 | `EnableIdempotencyDryRun` | false | Simulación, sin efectos externos |
| F10 | `ShowNativeHealthEvidence` | false | Sólo lectura autorizada del target |
| F11 | `EnableAccessDecisionPreview` | false | Visualizar veredicto, no elevar permiso |
| F12 | `ShowA5AuditTrail` | false | Historial independiente ya acreditado |

**FUTUROS_NO_EJECUTADOS:** estas claves no existen como flags activos ni habilitan deploy, cambios de rama, migración, transacciones financieras, eventos, R2, secretos ni DNS. La UI jamás sustituirá denegaciones del backend.

## Seis reglas de precedencia futura

1. **P1 Gobierno:** Scope Lock STRICT, repo/branch autorizados no sobreescribibles por usuario o bandera.
2. **P2 Permiso de ambiente:** QA y PROD requieren aprobación distinta y específica, no derivada de LISTO_DEV o A5.
3. **P3 Evidencia de candidato:** SHA, tree, digest, check-run exactos y resultado `completed/success` pertinente; CANCELLED/SKIPPED/QUEUED/FAIL no son PASS.
4. **P4 Autoridad backend:** JWT firmado, membresía, RBAC real y tenant A/B por recurso; cabeceras y `tenant_id` cliente no confieren acceso.
5. **P5 Presentación:** flags visuales sólo modifican contenido visible autorizado; nunca autoridad servidor.
6. **P6 Ausencia o contradicción:** falta de referencia, credencial, gate o readback nativo se deniega y audita sin secretos.

No heredar configuración `dev` a QA/PROD. Los ámbitos por candidato, target y actor deben validarse en backend antes de cualquier futura operación. La documentación no crea esos validadores ni los presume certificados.

## Doce pares de aceptación propuestos — FUTUROS_NO_EJECUTADOS

| Positivo futuro | Negativo que debe bloquear |
| --- | --- |
| P01 Repo/branch permitidos sólo en `dev` | N01 Repo o rama externos |
| P02 SHA/tree de 40 hex y digest coherentes | N02 SHA truncado/digest distinto |
| P03 Target NONE mantiene consulta read-only | N03 Flag UI autoriza QA/PROD |
| P04 CI nativa completed/success exact SHA | N04 Cancelled/skipped pasa como aprobado |
| P05 Tenant A con RBAC ve recursos A | N05 Tenant B obtiene datos A |
| P06 Clave tipada válida genera vista redactada | N06 Campo desconocido activa funcionalidad |
| P07 Approval vigente y del target correcto | N07 Approval expirado o de otro destino |
| P08 Dedupe por clave/ámbito impide duplicar | N08 Retry produce doble outcome |
| P09 Flag UI se aplica tras autorización backend | N09 Flag da permiso sin backend |
| P10 Auditoría oculta información sensible | N10 Logs muestran token/clave/PII |
| P11 Readback nativo confirma SHA observado | N11 Texto visual `deployed` basta |
| P12 Restore efímero probado con hashes y conteos | N12 Documentar rollback equivale a probarlo |

**No se ejecutó** prueba MySQL, JWT, browser, Docker, release, integración, CI runtime específica ni restore por esta tarea documental. Los casos son objetivos futuros con fixtures y oráculos para fases autorizadas.

## A0–A5, artefacto y reversión

**A0:** diff exclusivo de este Markdown y sección autocontenida de `CHANGELOG_AI.md`; no runtime, secrets ni nuevos permisos.

**A1:** inspección de 16 campos tipados, 12 flags en false, seis reglas y 12 pares P/N documentales; ningún PASS funcional inventado.

**A2:** fail-closed JWT/RBAC/tenant A/B, SHA, separación de QA/PROD, secret handling, Scope Lock, bitácora y owner exclusivo.

**A3:** coherencia con M01.01–06 LISTO+A5 y M02.01–03, sin promover contratos propuestos a implementación; build frontend/backend, MySQL y Playwright = NO_APLICA causal al diff Markdown.

**A4:** fast-forward/CAS GitHub a dev, diff y blobs exactos, checks Scope Lock y V5 `completed/success` del SHA, PLAN/COLA/DAG/BITACORA readbacks.

**A5:** revisión por Task Javier distinta del autor, registrada de forma idempotente. Nunca autoaprobación ni autorización de ambientes.

**Rollback:** `git revert` normal de la documentación, sin reescritura histórica. Este contrato no desplegó nada.
