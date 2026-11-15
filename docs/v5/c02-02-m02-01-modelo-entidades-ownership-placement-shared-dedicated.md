# SOLQARYN V5 FINAL — C02-02.M02.01: entidades, ownership y relaciones de placement shared/dedicated

**Autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Rama:** `solqaryn/Solqaryn@dev`. **Seguridad:** `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-15.

**Fuentes institucionales:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), PLAN_MAESTRO/COLA/DAG_TECNICO por ID, `AGENTS.md`, Skill canónica y `docs/v5/c02-02-m01-06-bloqueo-rollback-liberacion-shared-dedicated.md`.

## 1. Alcance y estado de implementación

Este artefacto define un **modelo de dominio futuro** para relacionar empresa, suscripción, emplazamiento de datos y transición de almacenamiento compartido a dedicado. Es un contrato M02, no una clase EF, tabla, API, router, conexión MySQL ni permiso de migración. El sistema actual mantiene su autoridad en el backend y sus mecanismos existentes de `AppDbContext`, `StorageTenantContext` y membresías `UsuarioEmpresa`; ninguna selección dedicada está certificada aquí.

**Entrada verificada:** PLAN H332=PENDIENTE, I332 vacío, COLA G332=SI/O332=SI, DAG F323=VALIDADO/G323=SI, antecesor C02-02.M01.06 LISTO. El owner se atribuye sólo tras reclamación individual y readback. Un check/documento anterior no sustituye la comprobación del commit final.

## 2. Modelo lógico propuesto (sin implementación de persistencia)

| Entidad conceptual | Identificador/owner lógico | Relaciones y finalidad |
| --- | --- | --- |
| TEN01 Empresa | EmpresaId, autoridad interna | Raíz de propiedad de recursos; una empresa no decide por sí misma DSN o servidor |
| TEN02 Membresia | UsuarioId + EmpresaId, vigencia | Rol/permisos comprobados por servidor; revocación invalida acceso |
| TEN03 PlacementPolicy | PolicyId, tenant owner o alcance sistema | Regla autorizada que define SHARED o DEDICATED; nunca elegida desde navegador |
| TEN04 PlacementAssignment | AssignmentId, EmpresaId y versión | A lo sumo un placement activo por empresa y versión de corte |
| TEN05 StorageBinding | BindingId, referencia opaca server-only | Alias lógico de destino; la referencia no contiene DSN, token ni credencial |
| TEN06 PlacementRevision | AssignmentId + Revision | Registro inmutable de transición, actor, fecha y SHA de contrato |
| TEN07 MigrationIntent | IntentId + EmpresaId | Solicitud de migración validada por rol y plan aprobado; no ejecuta SQL |
| TEN08 MigrationCheckpoint | IntentId + Seq | Reanudación idempotente ligada a snapshot/digest, nunca permite doble escritura |
| TEN09 DataConsistencyProof | ProofId + IntentId | Conteos, FK, checksums redactados por empresa, sin payload privado |
| TEN10 CutoverGuard | EmpresaId + Epoch/Fence | Exclusividad para cambio de ruta, lease y bloqueo de escrituras simultáneas |
| TEN11 RestoreEvidence | EvidenceId + SnapshotId | Resultado de restauración efímera comprobable; no equivale a backup real |
| TEN12 RollbackDecision | DecisionId + IntentId | Registra autorización, target anterior, ventana y verificación posterior |
| TEN13 PlacementAuditEvent | EventId/idempotency | Actor backend, empresa, motivo, SHA, resultado y correlación saneada |
| TEN14 TenantIsolationFinding | FindingId + EmpresaId | Evidencia de denegación A/B, inconsistencias y remediación sin secretos |

Las identidades anteriores son **propuestas conceptuales** y no implican tablas nuevas. Las referencias de configuración de Infrastructure nunca deben exportar DSN al cliente, log o documento. En ningún caso se reutiliza `EmpresaId` sin validar membresía activa y contexto backend.

## 3. Cardinalidades, ownership y ciclo de estado

- Empresa 1:N Membresias; sólo usuarios activos con autorización backend pueden operar en la empresa.
- Empresa 1:N PlacementRevision y 0..1 PlacementAssignment activo, conservando historial auditado.
- Assignment N:1 StorageBinding, con referencia opaca y resolución exclusivamente desde configuración protegida.
- Empresa 1:N MigrationIntent; Intent 1:N Checkpoint, 1:N DataConsistencyProof, 0..N RestoreEvidence y RollbackDecision.
- Un CutoverGuard vigente está ligado a Empresa, epoch y versión, nunca a un navegador. PlacementAuditEvent es append-only e idempotente.
- Estados propuestos: DRAFT -> REVIEWED -> PREPARED -> VERIFIED -> CUTOVER_ELIGIBLE -> ACTIVE, con FAILED/BLOCKED/ROLLED_BACK terminales y sin saltar pasos. El estado DRAFT no autoriza conexiones.

## 4. Invariantes obligatorios (INV01–INV16)

1. **INV01** `EmpresaId` autenticado se resuelve exclusivamente en backend, nunca de cabeceras/cuerpo sin comprobación.
2. **INV02** Operar placement exige JWT válido, membresía vigente y RBAC por acción.
3. **INV03** Todo binding queda limitado a un tenant y ninguna consulta cruza A/B sin autorización explícita del control plane.
4. **INV04** Sin asignación válida o con referencia rota se deniega; prohibido fallback a conexión de otra empresa.
5. **INV05** DSN, claves y credenciales sólo en almacén protegido y fuera de Git, DTO, respuestas y logs.
6. **INV06** Un cambio de placement usa versión y fence CAS; epoch viejo falla cerrado.
7. **INV07** Cutover exige snapshot con restore efímero probado, checksums y consistencia FK por tenant.
8. **INV08** Migración parcial mantiene origen autoritativo hasta verificación de destino.
9. **INV09** Reintentos con misma clave no duplican filas, eventos, jobs ni actualización de rutas.
10. **INV10** El rollback requiere target previo verificado y una ventana autorizada; no se infiere que git revert restaure MySQL.
11. **INV11** El historial es aditivo, trazable y redactado; no se sobreescribe un resultado adverso.
12. **INV12** Un pedido de cliente no puede registrar su propia evidencia de A5, CI, SHA o autoridad.
13. **INV13** `VALIDANDO` no satisface un gate técnico de antecesor que requiera LISTO+A5.
14. **INV14** Cambio de SHA invalida pruebas exigidas salvo equivalencia de blobs y controles comprobada.
15. **INV15** No se cambia QA, main, PROD, DNS, Cloudflare, Aiven ni secrets con este contrato.
16. **INV16** Ausencia de autor, revisor independiente, permisos o readback conserva NO_GO por ámbito.

## 5. Escenarios de aceptación futura — NO EJECUTADOS

| Par | Positivo futuro | Negativo obligatorio |
| --- | --- | --- |
| P01/N01 | Resolver empresa A desde identidad backend y mapping válido | Header/empresa B intenta seleccionar storage A |
| P02/N02 | Binding opaco aprobado y vigente | DSN o host inyectado en petición |
| P03/N03 | Versión vigente y fence único al cutover | Dos concurrentes reclaman la misma epoch |
| P04/N04 | Snapshot restaurado y reconciliado por conteos/FK/hash | Snapshot ausente o divergente |
| P05/N05 | Checkpoint idempotente reanudado sin duplicar | Retry duplica rows/jobs |
| P06/N06 | Tenant sin destino genera NO_GO auditado | Fallback abre destino de otro tenant |
| P07/N07 | Reversión de destino autorizada y probada | `git revert` se confunde con restore de datos |
| P08/N08 | Evidencia SHA exacta y revisión A5 independiente | CI cancelada/autor autoprobando se trata como PASS |

Estas filas son **oráculos futuros**, no xUnit, HTTP, EF/MySQL, Docker, Playwright o restauraciones ejecutadas.

## 6. Pruebas causales, handoff y reversión

**A0:** revisión de este modelo y diff exclusivo Markdown+CHANGELOG, sin modificar código runtime, migraciones, claves ni conexiones. **A1:** consistencia de TEN01–TEN14, cardinalidades e INV01–INV16 con P/N futuras explícitamente no ejecutadas. **A2:** aislamiento de tenants A/B, JWT/RBAC, secret management, denegación por defecto y modelo de propiedad. **A3:** cotejo con M01.06, arquitectura backend y dependencias PLAN/COLA/DAG; no declarar integraciones de MySQL ni IAM certificadas. **A4:** commit directo mediante fast-forward y expected HEAD, checks `verify-solqaryn-project-scope` y procedencia V5 `completed/success` sobre SHA publicado, PLAN/COLA/BITACORA readback; CI .NET/Angular/MySQL no causal a cambios exclusivamente Markdown se registra NO_APLICA, nunca PASS inventado. **A5:** requiere otra Task Javier distinta de la autora antes de liberar sucesoras que la requieran.

**Rollback aplicable:** revertir este commit documental mediante `git revert` normal contra HEAD vigente, preservando trabajo ajeno y Bitácora histórica. Ninguna operación en bases de datos, proveedores externos, release o rollback de datos está autorizada por este documento.
