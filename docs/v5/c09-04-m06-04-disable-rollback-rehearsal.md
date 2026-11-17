# SOLQARYN V19 — C09-04.M06.04 | Rehearsal de rollback transaccional real aislado

**Owner DEV06**, TASK_ID `6af16bbad26881918c283abe06e1f32a`; PLAN fila 1439, candado V17 `2000001439`; sólo `solqaryn/Solqaryn@dev` y `PROJECT_SCOPE_LOCK=STRICT`. Clasificación R2 por EF/MySQL, tenancy y rollback; ningún dato legítimo o secreto real, QA/main/PROD.

## A0, criterio y causa de reparación
Criterio institucional: «Ensayar rollback **o** desactivación sin borrar datos legítimos». El commit inicial `00cce68fa995092d2c0840563566f5be6eb84000` ejecutó MySQL 8.4.13 y xUnit real, pero falló **1/3** por `CK_ConfiguracionesWhatsAppEmpresa_Numero_E164` al persistir teléfono sintético válido `+50499990001` (GitHub Actions run `44147985415`, job `134268231673`). Los otros dos negativos de conexión pasaron 2/3. El CHECK de esquema existe en `ConfiguracionWhatsAppEmpresaConfiguration.cs` y migración N76C; requiere reparación independiente con revisión de DDL/migración sobre MySQL y no se declara solucionado aquí. Se registró `QA_DEFECT_FOUND` en BITACORA, con riesgo P1 potencial de alta WhatsApp y P2 del ensayo inicial.

**Cambio correctivo dirigido:** para no ocultar la falla ni alterar el modelo/migraciones de otra unidad, restringir el ensayo al *rollback* real del estado outbox en MySQL efímero. No se aprueba desactivación de configuración WhatsApp ni el gate de envío de proveedor. Revertir este changeset mediante `git revert` normal tras HEAD/exclusividad; no borrar datos ni BITACORA.

## A1–A3 — prueba causal de rollback y casos negativos
- Dos negativos: rechazar host MySQL remoto y puerto 3307; nunca abrir conexión a Aiven/producción.
- Crear dos empresas A/B sintéticas y dos intenciones outbox durables, idempotencia independiente. Asserts positivos de estado `Pendiente`, intentos cero, payload íntegro y aislamiento A/B.
- Dentro de una transacción SQL real: marcar como `Procesando` sólo el outbox de A, `SaveChanges`, comprobar intento 1 y ejecutar `ROLLBACK`.
- Desde **otro** `DbContext`: ambas intenciones deben conservar `Pendiente`, intentos 0, cero timestamps de procesamiento, payload íntegro, dos filas, ninguna fuga cross-tenant.
- xUnit `Category=Integration` en GitHub-hosted con servicio `mysql:8.4` efímero, .NET 10 Release, TRX exactamente 3/3 PASS/0 FAIL/0 SKIPPED. Un test simulado no equivale a este resultado.

**No demostrados:** bloqueo del dispatcher, webhook HMAC, replay proveedor, consentimiento, revocación, JWT firmado, RPO/RTO, ni configuración WhatsApp en DB por CHECK defectuoso. Riesgos de M06.03 R01–R10 permanecen. **A5_FAIL P2 sobre M06.01** por CI atribuida incorrectamente a SHA M05.05 permanece abierto, no convertirlo en PASS ni liberar DAG/manual.

## A4–A5
Sólo prueba nueva, este documento, sección contigua CHANGELOG; workflow actual conserva filtro y oráculo TRX. A4 exige `verify-solqaryn-project-scope`, V5 y job MySQL dirigidos `completed/success` del SHA exacto, más readback PLAN/COLA/DAG/BITACORA y ninguna P0/P1 sin resolver. Si CI FAIL, SKIPPED, CANCELLED, QUEUED o IN_PROGRESS, mantener VALIDANDO. A5 por Task Javier distinta de DEV06, con lectura independiente de diff, criterios y logs. No QA/main/PROD.


## Reparacion causal V19 - E164, migracion correctiva y ensayo original (2026-11-17)

El error de MySQL 8.4 en run 44147985415 fue el CHECK CK_ConfiguracionesWhatsAppEmpresa_Numero_E164 que rechazo el E164 sintetico valido +50499990001. La correccion cambia solo el patron a ^[+][0-9]{8,15}$, sin escapes SQL, conservando la longitud aceptada en modelo y snapshot y agregando una migracion nueva forward-only. No se reescribe la migracion historica N76C.
Se restaura el ensayo original de desactivar configuracion A, mantener B y sus outbox, comprobar versionado, y hacer ROLLBACK transaccional de una reconfiguracion A sin afectar los datos originales. Se conserva ensayo MySQL A/B de rollback outbox y dos negativos host/puerto, con total esperado 4/4. Solo MySQL efimero GitHub Actions, sin Aiven/PROD, envios reales ni credenciales. Down prohibe restaurar el CHECK defectuoso. CI exact-SHA, Scope Lock y V5 requieren SUCCESS antes de contar PASS; A5 distinto, historia de defectos se preserva.


## Corrección de snapshot Oracle EF Core (2026-11-17)
El CI del commit 9d131c4b aprobó el bootstrap MySQL fresh (139 tablas, pending=0), pero `dotnet-ef migrations has-pending-model-changes` falló porque el proyecto `Infrastructure.Migrations` utiliza el snapshot `OracleAppDbContextModelSnapshot.cs` (diferente de `AppDbContextModelSnapshot.N76C.cs`), que conservaba el CHECK anterior. Este cambio aplica el mismo patrón sin escapes `^[+][0-9]{8,15}$` al snapshot Oracle real. No atribuir PASS hasta que el job Oracle de CI del nuevo SHA complete SUCCESS. Sin despliegue ni cambio productivo.
