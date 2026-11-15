# SOLQARYN V5 — C03-04.M02.04 configuración tipada y límites seguros

**Unidad:** C03-04.M02.04 — Definir configuración tipada, ámbitos y feature flags seguros de respuesta y recuperación. **Autor:** PRIMARY DEV 05 JAVIER (`6adc2e55f47c81919f6374e561687076`), repo `solqaryn/Solqaryn` rama `dev`, PROJECT_SCOPE_LOCK=STRICT. **Tipo:** contrato de diseño, **no** claves, feature flags, opciones inyectadas o secretos instalados.

**Autoridad:** STANDARD V5 FINAL Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`; Skill/AGENTS.md/Scope Lock exact HEAD. Las seis M01.01–06 de C03-04 están LISTO+A5. M02.01–03 son documentos independientes de diseño, **no** autorización para cambios IAM/DB/API.

## 1. Fuentes reales y separación de autoridad

- `scripts/backup_dev.sh` (blob `d2e0ca8bf48dc8c21fcbfd5ab5256c770bdbdcf3`) y `scripts/restore_dev.sh` (blob `9be58822ae5c6189e7a84cf785998bf42a1b576c`) implementan precondiciones DEV; no prueban restauraciones exitosas.
- `.github/workflows/backup-restore-dev.yml` (blob `cee9cdd1e22d006c21d9a23cc40ead6223d7295c`) define CI de entorno descartable, pero su existencia no es un PASS.
- `docs/BACKUP_RESTORE_DESARROLLO_RUNBOOK.md` (blob `81e8b11557912fffcd741ff0f34faf69e0ed6f47`) y `docs/ROLLBACK_RUNBOOK.md` (blob `b7424464a5a79f0d5ff858a7ec4d2356a30355f0`) distinguen documentación de autorización operativa.

**Decisión básica:** un flag de UI, plan comercial o configuración no confiere `RolPermiso`, membresía, acceso a tenant B, custodia de backup, permiso destructivo ni autoridad de plataforma. Si falta una fuente, respuesta DENY. Un operador/Task tampoco adquiere autorización comercial por escribir el Sheet.

## 2. Catálogo de claves propuestas, NO IMPLEMENTADAS

Estos nombres `CFG01–CFG16` son **contratos para validación futura**, no claves que hoy deban agregarse a appsettings, Aiven, Render, Vercel o Google Sheets. `S` indica scope conceptual, no un permiso concedido.

| ID | Clave candidata (tipo conceptual) | Scope y fallo seguro |
| --- | --- | --- |
| CFG01 | `Incident.ReadEnabled` (bool) | TENANT + permiso lectura; false ante ausencia |
| CFG02 | `Incident.OpenEnabled` (bool) | TENANT y rol verificado; false |
| CFG03 | `Incident.ContainmentDraftEnabled` (bool) | TENANT/actor; false, nunca revocación automática |
| CFG04 | `Incident.EvidenceMaxBytes` (int acotado) | TENANT con límite global no elevable; 0/deny si inválido |
| CFG05 | `Incident.AuditRetentionDays` (int positivo) | PLATFORM política aprobada, no editable por tenant |
| CFG06 | `Incident.CorrelationRequired` (bool obligatorio true) | PLATFORM fijo, no false por tenant |
| CFG07 | `Recovery.BackupDryRunEnabled` (bool) | CI/DEV allowlisted, false por defecto |
| CFG08 | `Recovery.RestoreTrialEnabled` (bool) | Solo CI efímero y autoridad destructiva externa, false |
| CFG09 | `Recovery.AllowedEnvironments` (set enum cerrado) | PLATFORM, nunca incluir PROD implícito |
| CFG10 | `Recovery.ChecksumAlgorithm` (enum permitido) | PLATFORM, rechazo de valor desconocido |
| CFG11 | `Recovery.RequireManifest` (bool true) | PLATFORM fijo, no negociable por tenant |
| CFG12 | `Recovery.RequireDestructiveApproval` (bool true) | PLATFORM fijo, ninguna Task lo desactiva |
| CFG13 | `Recovery.IdempotencyWindowSeconds` (int acotado) | Plataforma con control de cuota, 0/deny si inválido |
| CFG14 | `Recovery.MaxConcurrentTrials` (int acotado) | CI/DEV, valor 0/deny si no existe contrato operativo |
| CFG15 | `Recovery.ObservedRpoRto` (medición con prueba) | Solo métricas auditadas, no objetivo numérico fabricado |
| CFG16 | `Incident.PiiLogRedactionRequired` (bool true) | PLATFORM fijo, no false por tenant ni por front-end |

**Precedencia propuesta:** la política PLATFORM establece techo/deny; TENANT puede restringir cuando exista autoridad real, nunca ampliar grants; BRANCH/RESOURCE sólo reducen capacidades y requieren propiedad validada. No habilitar host o ambiente por herencia silenciosa. Versionado `SchemaVersion` y `ExpectedVersion` debe impedir overwrite de concurrentes con un 409 seguro. No almacenar conexión SQL, token de plataforma, referencia secreta SMTP o URL firmada en registros públicos.

## 3. Validaciones y flags fail-closed

- V01 tipos cerrados y `SchemaVersion` reconocida; V02 scope y tenant obtenidos desde servidor; V03 actor/membresía vigente; V04 RBAC relacional explícito; V05 no usar `Usuario.RolId` legacy para conceder.
- V06 deny si faltan flags o falló fuente; V07 nunca convertir un false de plataforma en true por override tenant; V08 destino/restauración sólo con host allowlisted CI/DEV y autorización humana real; V09 secretos fuera de DTO/log/versionado visible.
- V10 no aceptar banderas para `disable_rbac`, `skip_audit`, `restore_prod`, `worker_forever` ni otros bypass; V11 contador/retención no superiores al máximo global; V12 correlación/idempotencia opacas y ligadas a tenant.
- V13 rollback de configuración futura mediante versión, no cambio destructivo silencioso; V14 certificados RPO/RTO sólo si hay medición/proof; V15 cualquier `queued`/`in_progress`/`cancelled`/`skipped` en CI requerida es NO_PASS; V16 no permitir configuración no tipada desde UI Angular como autoridad.

**No activar ningún flag durante M02.04.** Si el sistema no dispone del catálogo propuesto, su comportamiento actual permanece intacto y la propuesta no debe utilizarse para inferir resultados funcionales.

## 4. Matriz de oráculos FUTUROS_NO_EJECUTADOS

| Caso | Clase | Resultado que deberá probar un scope futuro |
| --- | --- | --- |
| P01 | Positivo | Tenant A con membresía+RBAC+flag permitido registra incidente A, sin acceso a B |
| P02 | Positivo | Overrides más restrictivos reducen capacidad sin exceder política PLATFORM |
| P03 | Positivo | Prueba de backup CI autorizada usa allowlist, manifiesto y hash |
| P04 | Positivo | Versionado correcto actualiza una vez y conserva auditoría |
| N01 | Negativo | Empresa A lee/modifica flag privado de B → DENY sin enumeración |
| N02 | Negativo | UI cambia `RolId` o `empresaId` arbitrario → DENY server-side |
| N03 | Negativo | Membresía revocada, role sin permiso relacional → DENY |
| N04 | Negativo | Flag inexistente, tipo inválido o clave desconocida → DENY |
| N05 | Negativo | Tenant solicita `restore_prod`/entorno no allowlisted → NO_GO |
| N06 | Negativo | Falta aprobación destructiva aunque el flag esté true → NO_GO |
| N07 | Negativo | `CorrelationRequired=false` o `RequireManifest=false` → rechazo |
| N08 | Negativo | `ExpectedVersion` obsoleta o carrera concurrente → 409 sin overwrite |
| N09 | Negativo | Replay de B con clave idempotente A → DENY |
| N10 | Negativo | Error de auditoría o checksum → mantener estado no certificado |
| N11 | Negativo | RPO/RTO inferidos de config sin prueba → `NO_ACREDITADO` |
| N12 | Negativo | Solicitud de habilitar webhook/worker/QA/PROD por documento → NO_GO |

Los 4 positivos y 12 negativos anteriores **son diseño futuro, no pruebas ejecutadas** de HTTP, MySQL, dotnet, Playwright ni acceso a servicios.

## 5. Impacto, A0–A5 y rollback

**A0:** comparar cambios solo en dos Markdown; comprobar sección CHANGELOG íntegra, límites tipados y cero secretos. **A1:** inspección estática de 16 claves propuestas y P01–04/N01–12 futuros. **A2:** verificar preservación conceptual de JWT/RBAC/tenant A/B, PII, permisos destructivos y no bypass. **A3:** vincular M01.01–06 LISTO+A5, fuentes de backup/restore y contratos M02 sin presentarlos como runtime. **A4:** git fast-forward CAS esperado sobre HEAD actual, Scope Lock y V5 `completed/success` exact SHA, PLAN/COLA/BITACORA readback. **A5:** revisión independiente por otra Task Javier, distinta de autora.

**Ningún cambio de runtime:** no escribir appsettings, CONFIG, datos MySQL, feature flags reales, secretos, endpoints, migraciones, APIs, workers, eventos, webhooks, automatizaciones, horarios, QA/main/PROD ni proveedores. Si se revierte solo esta documentación, usar `git revert` normal y evento de compensación append-only; no forzar push ni modificar BITACORA histórica.