# SOLQARYN V19 — C09-04.M06.04: ensayo aislado de desactivación y rollback

**Autor DEV06** (`6af16bbad26881918c283abe06e1f32a`), PLAN_MAESTRO fila 1439; lock V17 `SQV17_C09_04_M06_04` ID `2000001439`; `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **R2 test-only**. No QA/main/PROD, Aiven, secretos ni datos legítimos.

## A0 — alcance y rollback
Criterio: *ensayar rollback o desactivación sin borrar datos legítimos*. Changeset solamente test Category=Integration, workflow GitHub-hosted, este artefacto y sección contigua CHANGELOG. Reutiliza `ConfiguracionWhatsAppEmpresa.Desactivar`, versión/tenancy y `MensajeOutbox` del código real en `AppDbContext`. No cambia runtime ni servicios de proveedor. Revertir el changeset, si hiciera falta, mediante `git revert` normal con HEAD y escritor verificados, sin borrar historial BITACORA.

## A1–A3: asserts causales sobre MySQL 8.4 efímero
Positivo: crear dos empresas sintéticas A/B con configuración WhatsApp de referencia opaca y dos intenciones outbox. Desactivar A solamente, confirmar que B continúa activa, que ambos mensajes siguen persistidos y los identificadores de tenant no se cruzan. Frontera: la desactivación repetida no aumenta `Version` más allá de 2. Negativos: rechazar conexión MySQL remota o puerto ajeno a 3306, y exigir cero filas de A bajo consulta de clave de B. Error/rollback: transacción de reconfiguración A + `SaveChanges` + `ROLLBACK`; nuevo contexto debe constatar versión, número, referencias y dos mensajes originales.

El workflow dirigido `v19-c09-04-m06-04-synthetic-disable-rollback.yml` exige contenedor `mysql:8.4` real local, .NET 10 Release y TRX `Category=Integration` exactamente 3/3, 0 FAIL, 0 SKIPPED. No se usa proveedor externo, números reales ni secret almacenado.

**Límite R2 importante:** `Activa=false` del modelo no demuestra que un despachador real deje de enviar; no acredita firma HMAC de webhooks, replay, revocación, consentimiento, JWT, ni idempotencia externa. Registro de riesgos R01–R10 del M06.03 permanece abierto. **A5_FAIL P2 de M06.01** identificado en BITACORA sobre afirmación inexacta de CI V5 para M05.05; no ocultarlo ni liberar gates M06 con este ensayo. Toda validación de entrega viva requiere pruebas propias y A5 independiente.

## A4/A5
Exigir `verify-solqaryn-project-scope`, V5, pruebas de integración en SUCCESS para SHA exacto, logs reales y readback PLAN/COLA/DAG/BITACORA. QUEUED/IN_PROGRESS/SKIPPED/CANCELLED/FAIL jamás PASS. Mantener VALIDANDO mientras CI externa o criterios R2/A5_FAIL no estén resueltos. A5 por otra Task Javier distinta de DEV06, con auditoría propia. Cero modificación a DAG, COLA, rama main, QA/PROD.
