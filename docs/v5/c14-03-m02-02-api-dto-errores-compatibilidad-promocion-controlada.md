# SOLQARYN V5 — C14-03.M02.02: contratos API/DTO, errores y compatibilidad para promoción controlada

**Autor:** PRIMARY DEV 03 — `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. **Fecha:** 2026-11-15. **Estado de este artefacto:** propuesta de diseño documental M02, NO endpoints o API desplegados.

## Naturaleza, límites y compatibilidad

La microtarea define únicamente el **contrato futuro de operaciones, DTO, respuestas de error, validaciones y compatibilidad** para el control de releases. No crea rutas HTTP, controllers, tablas, workflows, tokens, Actions, ejecutores ni servicios. `C14-03.M01.01–06` consta LISTO con A5 independiente; `DAG_TECNICO F=VALIDADO/G=SI` y `COLA G/O=SI` de M02.02 habilitaron documentación, no promoción. La arquitectura vigente ASP.NET Core 10 + Angular 22.2.1 + MySQL 8.4 no se modifica.

**Ningún endpoint a QA/main/PROD está autorizado**, aunque un usuario o Task tenga acceso a dev. El derecho de escribir una solicitud de release futura no es derecho de ejecutarla. Se conserva la segregación de responsabilidades: autor, revisor técnico A5, aprobador de ambiente y ejecutor futuro pueden requerir identidades diferentes; esta especificación no inventa ni crea esas identidades.

## Operaciones contractuales futuras (NO IMPLEMENTADAS)

| Operación conceptual | Input conceptual | Semántica de denegación |
| --- | --- | --- |
| `GetReleaseCandidate` | Referencia opaca + SHA | Sólo metadata autorizada de dev, sin secretos ni rutas privadas |
| `RequestPromotionReview` | SHA exacto, target, alcance, motivo, correlación | No es `ExecuteDeployment`; target requiere permisos distintos |
| `AttachEvidenceManifest` | SHA/tree/digests y refs de check-run | No confía en status de cliente, comprueba fuente GitHub y conclusión nativa |
| `EvaluatePromotionGates` | CandidateId, reglas, DAG, check-run IDs | Devuelve decisión/auditoría, nunca inicia despliegue |
| `RequestIndependentApproval` | Scope, reviewer/role, periodo y digest | Revisor distinto del autor cuando gobernanza exige independencia |
| `RegisterRollbackContract` | SHA destino/origen, recurso, precondición, prueba | No realiza rollback ni DDL |
| `GetPromotionEvidence` | ID autorizado y versión | Responde datos redactados, sin PII ni token |
| `RevokeApproval` | ApprovalId + razón/actor validado | Invalidar permiso futuro; jamás reactivar sin nueva evaluación |
| `RecordOutcomeReadback` | Target, sha observado, timestamp de plataforma | Requiere lectura nativa del destino; no aceptar texto `success` |
| `GetBlockingDependencies` | Scope, DAG, estados y owners | Explica gates, nunca escribe fórmulas ni forza SI |

Los nombres describen contratos futuros. No son rutas REST existentes, DTO C# actuales ni funciones de una API de pagos.

## DTO conceptuales propuestos

| DTO | Campos mínimos futuros | Controles |
| --- | --- | --- |
| `ReleaseCandidateDto` | CandidateId, Repo, Branch, CommitSha, TreeSha, ManifestDigest | Repositorio/rama sujetos a allowlist y Scope Lock |
| `PromotionReviewRequestDto` | CandidateId, TargetEnvironment, Reason, CorrelationId, ValidUntil | No incluye credenciales, no concede ejecución |
| `GateEvidenceDto` | GateId, CommitSha, CheckId, NativeStatus, NativeConclusion, ObservedAt | `success` sólo si completed y SHA coincide |
| `ApprovalDecisionDto` | CandidateId, Target, ApproverRole, Decision, ExpiresAt | Firma/rol nativo obligatorio, no inferido |
| `RollbackContractDto` | Target, FromSha, ToSha, RequiresDataRestore, ArtifactDigest | No se ejecuta sin autorización de ambiente |
| `AuditOutcomeDto` | EventId, Scope, ActorRef, Decision, ErrorCode, RecordedAtUtc | Redactado, sin PII, secretos ni contenido token |

Cualquier serialización de DTO tendrá límites de tamaño, tipos validados y autorización backend. `ApprovalDecisionDto` nunca constituye un permiso real por su mera existencia.

## Errores seguros de diseño

| Código proyectado | Causa/control futuro |
| --- | --- |
| `IDENTITY_NOT_VERIFIED` | Sin sesión/membresía actual |
| `SCOPE_LOCK_VIOLATION` | Repo/rama/entorno fuera de alcance |
| `AUTHOR_REVIEWER_NOT_INDEPENDENT` | A5 propio o rol incompatible |
| `EXACT_SHA_EVIDENCE_REQUIRED` | SHA/tree/digest faltante o inconsistente |
| `REQUIRED_CHECK_NOT_SUCCESS` | Fallo, queued, cancelled o skipped de prueba pertinente |
| `DEPENDENCY_NOT_RELEASED` | DAG/PLAN/COLA no satisfacen gate individual |
| `ENVIRONMENT_PERMISSION_REQUIRED` | Dev no tiene permiso para QA/PROD |
| `TARGET_CONCURRENT_UPDATE` | Lease/CAS del destino ya cambió |
| `ROLLBACK_NOT_VALIDATED` | Sin restore de datos efímero o reversión reproducible |
| `TENANT_ISOLATION_UNVERIFIED` | Sin prueba A/B de código y datos |
| `SECRETS_OR_PII_EXPOSURE` | Artefacto/log contiene material sensible |
| `OUTCOME_NOT_OBSERVED` | Éxito no leído de plataforma objetivo |

Estos códigos son especificaciones de error, no mensajes que hayan devuelto endpoints reales.

## Invariantes de compatibilidad y autorización

1. Cada petición identifica candidato con SHA y tree exactos, no `latest` mutable.
2. GitHub Apps/Actions proporcionan la evidencia de CI; una cadena de texto cliente nunca sustituye status/completion de servidor.
3. Cuando cambia HEAD deben revalidarse archivos y checks causales; CANCELLED no se convierte en PASS.
4. Identidad de Task y revisión A5 se conservan separadas de permisos de ambiente QA/PROD.
5. JWT validado, membresía, RBAC y tenant A/B deben resolverse del backend para cada operación futura.
6. Rutas, dominios, hostnames, cabeceras o EmpresaId recibidos no confieren autoridad.
7. Plan, DAG y COLA son fuentes de selección; un gate SI no concede derechos comerciales, secretos o despliegue.
8. Aprobación con ámbito distinto del target, SHA o ventana se rechaza.
9. Intentos concurrentes con CAS distinto no sobrescriben estado anterior ni ejecutan doble deployment.
10. Secrets nunca se almacenarán en DTO, repositorio o BITACORA; auditoría mantiene mínimos.
11. Contratos legacy de construcción y permisos actuales permanecen invariantes en DEV.
12. Error de integración deja estado verificable y recuperable, no éxito ficticio.
13. Operaciones de lectura y escritura autorizadas se separan; lectura no produce promoción.
14. Rollback de base de datos real exige respaldo/restore autorizados y pruebas independientes.
15. La interfaz de usuario puede mostrar estado, pero nunca sustituye decisión de backend.
16. Los destinos QA/main/PROD se mantienen no tocados y no autorizados por esta Task.

## Oráculos positivos/negativos FUTUROS, NO EJECUTADOS

| Positivo propuesto | Negativo que debe denegarse |
| --- | --- |
| P01 SHA/manifest correcto obtiene evaluación de lectura | N01 SHA erróneo toma checks de otro commit |
| P02 Check completed/success pasa gate requerido | N02 Queued/cancelled/skipped cuenta PASS |
| P03 A5 distinta de autor se acredita | N03 Autor se autoaprueba |
| P04 Consulta DAG informa bloqueo actual | N04 Client fuerza G/O=SI |
| P05 Petición de QA con permiso específico se evalúa | N05 Dev LISTO autoriza QA por sí solo |
| P06 Approval exact-target/SHA válido tiene identidad verificada | N06 Approval caducado o de otro target |
| P07 Readback nativo del resultado confirma versión | N07 Texto `deployed` del frontend basta |
| P08 Auditoría de error redacta material sensible | N08 Log contiene token, secret o PII |
| P09 Rollback efímero probado conserva datos | N09 Se ejecuta DDL sin restauración verificada |
| P10 Actor tenant A ve sólo su ámbito A | N10 Actor B accede evidencia o política A |

Ninguno es prueba xUnit, API, MySQL, Playwright, integración ni deployment ejecutado.

## A0–A5 y reversión

**A0:** diff intencional sólo este documento y sección CHANGELOG autocontenida, owner dev y ausencia de secretos. **A1:** inspección de diez operaciones, seis DTO, doce errores, 16 invariantes y diez pares de criterios futuros. **A2:** barreras JWT/RBAC, A/B, SHA, secreto, segregación de roles y permisos de ambiente. **A3:** coherencia con M01.01–06 LISTO+A5 y arquitectura; pruebas backend/frontend/DB/browser NO_APLICA causal al cambio Markdown. **A4:** publicar fast-forward/CAS sobre HEAD y verificar Scope Lock/V5 `completed/success` del SHA exacto más readback PLAN/COLA/DAG/BITACORA. **A5:** revisor Task Javier distinto del autor sobre evidencia publicada.

**Rollback** de estos dos Markdown: `git revert` normal en dev, BITACORA histórica intacta. No crear PR, ramas, Task, workflow, endpoint, webhook, secreto, migración ni desplegar QA/main/PROD.
