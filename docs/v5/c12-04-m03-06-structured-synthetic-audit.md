# SOLQARYN V19 — C12-04.M03.06: auditoría estructurada sintética fail-closed

Autor: Javier DEV04; TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. Solo solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. Riesgo R2 por auditoría, privacidad y comportamiento ante error. PLAN fila 1855, lock V17 SQV17_C12_04_M03_06 (2000001855).

## A0 — alcance y arquitectura
Nuevo C1204SyntheticManifestAuditSink en Infrastructure .NET 10, con pruebas xUnit y workflow GitHub-hosted, para registrar resultados estructurados del worker sintético C12-04.M03.05. Recibe exclusivamente C1204SyntheticManifestReviewWorker.Review desde un llamador confiable in-process y no toca el worker original. Rechaza resultados forjados: solo cinco códigos de denegación y uno de coincidencia, Examined entre 1 y 8 para coincidencias y cero para denegaciones. El logger recibe solo código allowlisted o conteo numérico, jamás tenant, actor, clave, SHA, excepción, JWT, DSN ni texto libre.

Los delegados LoggerMessage.Define usan SkipEnabledCheck después de una única comprobación IsEnabled, evitando el falso resultado de un logger cuyo valor cambia entre comprobaciones. La respuesta LogInvoked solo acredita una llamada ILogger sin excepción, NO almacenamiento durable ni recepción por un proveedor real. Los flags de restauración MySQL, acceso a tenant, JWT/HTTP, despliegue y cutover son false en todos los casos. No se registra como DI, HostedService, API ni endpoint, ni se conecta a EF, base de datos, proveedor de backup, cola o cron. No hay migraciones, escrituras de negocio, secretos ni datos productivos.

## A1–A3 — pruebas e integración limitada
La suite xUnit incluye: worker+adaptador real en memoria con resultado positivo y log estructurado; cinco denegaciones; código malicioso; conteos fuera de límites; logger deshabilitado; proveedor IsEnabled y Log que lanza con secretos; cancelación del proveedor y cancelación del llamador; logger de estado variable que no debe provocar un segundo IsEnabled. El sink no genera trazas con datos privados ni trata un log emitido como evidencia de cutover.

A2: tests adversariales, resultado fail-closed, sin filtración por código de error; seguridad multiempresa real NO_APLICA a este diff aislado (no toca JWT/RBAC/HTTP). A3: integración in-process con adaptador original real y prueba .NET en GitHub-hosted; MySQL 8.4, EF, browser, red y restauración real NO_APLICA por no modificarse ni utilizarse. Ningún mock prueba despliegue, base real o auditoría persistente.

## A4/A5 y reversión
Exigir .NET 10 Release targeted xUnit en SHA exacto, backend/security pertinentes, Scope Lock y procedencia V5 SUCCESS o equivalencia de blobs auditada; SKIPPED/CANCELLED/QUEUED/IN_PROGRESS no son PASS. TRANSICIÓN VALIDANDO y run/sha reales; LISTO solo con A0–A4 y readback institucional. A5 requiere revisor Task Javier diferente. Rollback limitado a revert convencional del commit, con preservación de BITACORA; ninguna acción QA/main/PROD.
