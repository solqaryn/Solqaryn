# SOLQARYN V5 FINAL — C02-03.M02.04: configuración tipada y flags fail-closed de lifecycle tenant

**Origen:** STANDARD V5 FINAL y PLAN_MAESTRO fila 371; C02-03.M01.01–06 LISTO con revisión A5 independiente. **Autor:** PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Ámbito:** solqaryn/Solqaryn/dev, PROJECT_SCOPE_LOCK=STRICT.

**Estado del artefacto:** contrato de DISEÑO M02 únicamente. Ninguna clase de configuración, bandera de ejecución, endpoint, migración, secreta, control plane, worker o mecanismo de aprovisionamiento se crea o activa aquí. `Empresa.Activa` es un atributo legacy real, no una prueba de flujo de estados completo. `Empresa.Eliminado` NotMapped no concede baja lógica.

## 1. Ámbitos autorizados y resolución de autoridad

- El ámbito **plataforma** sólo se resuelve por autenticación global en backend y permiso específico; no depende de una solicitud del browser.
- El ámbito **empresa** se deriva de contexto tenant confirmado server-side, no de query, `localStorage`, nombre de host ni `X-Empresa-Id` sin comprobación.
- El ámbito **ambiente** (dev/QA/PROD) debe provenir de configuración interna aprobada, no de un DTO de usuario. Este documento NO autoriza salida de dev.
- El ámbito **operación** requiere key durable, actor, versión y evidencia. Si falta cualquiera, resolución final `DENEGADO`.
- En desacuerdo entre banderas o fuentes, usar el valor más restrictivo y emitir observación redactada sin exponer secretos.
- Las credenciales y DSN no aparecen en respuestas HTTP, logs, JSON público, `appsettings` versionado ni variables frontend.

## 2. Propuesta FUTURA `TenantLifecycleOptionsV1` (no implementada)

| Campo lógico | Tipo y default propuesto | Regla/invariante |
| --- | --- | --- |
| `SchemaVersion` | entero, `1` | Rechazar versiones desconocidas |
| `LifecycleEnabled` | bool, `false` | Master gate fail-closed, no bypass por tenant |
| `EnableProvisioning` | bool, `false` | Requiere certificado de alta y compensación |
| `EnableSuspend` | bool, `false` | Exige revocación de sesiones/caché/jobs |
| `EnableReactivate` | bool, `false` | Exige revalidación JWT/RBAC/membresías |
| `EnableArchive` | bool, `false` | Sin retención y restore probado se rechaza |
| `EnableLogicalDelete` | bool, `false` | No equivale a `Empresa.Eliminado` NotMapped |
| `RequirePlatformOperator` | bool, `true` | Nunca se admite `false` en ambientes reales |
| `RequireTenantIsolationProof` | bool, `true` | Exige pruebas tenant A/B y denegación |
| `RequireReversibleMigration` | bool, `true` | Plan y test con servicio MySQL efímero |
| `RequireIdempotency` | bool, `true` | Registro durable con identidad y payload hash |
| `RequireExpectedVersion` | bool, `true` | Control CAS antes de cualquier mutación |
| `RequireA5Independent` | bool, `true` | A5 de TASK_ID distinto, no aprobación propia |
| `RequireExactShaCi` | bool, `true` | Cancelled/skipped/failed/queued no son PASS |
| `OperationTimeoutSeconds` | entero, `30` | Rango 5–120, timeout no confirma éxito |
| `MaxRetryAttempts` | entero, `0` | Sólo elevar tras política, sin retries no idempotentes |
| `AuditRetentionDays` | entero, sin valor operativo | Definición legal pendiente, no inventar periodo |
| `ConfigRevision` | hash/version opaco | Relectura bajo lock; versión vieja se rechaza |

Los defaults aquí son propuestas para un futuro diseño tipado; **NO son afirmaciones sobre valores actuales del servicio**. No configuran ejecutores. El valor `false` para cada capacidad operativa domina cualquier excepción de empresa o usuario.

## 3. Matriz de evaluación y precedencia

1. Un `EnableArchive=true` aislado nunca habilita archivo si `LifecycleEnabled=false`.
2. Un permiso de tenant no se convierte en permiso de plataforma por flag o claim enviado por cliente.
3. Configuración sin firma/autoridad, nula o con versión desconocida produce DENEGADO sin reintento automático.
4. `RequireA5Independent=true` conserva independencia de autor en todos los ambientes, aun si comparten identidad GitHub.
5. Un cambio de configuración durante una transición invalida la revisión previa y exige relectura de `ConfigRevision`.
6. En suspensión, si revocación parcial no se confirma, estado final no puede declararse SUSPENDIDO correctamente aplicado.
7. En reactivación, permisos/roles previamente revocados no se restauran por defecto ni por un flag.
8. Archivo y baja lógica dependen de la retención contable, backup íntegro y restore efímero; `git revert` no recupera MySQL.
9. La ausencia de observabilidad institucional o idempotencia durable provoca DENEGADO para escrituras sensibles.
10. Las opciones de consola se evalúan en backend; ninguna propiedad en localStorage o cookie configura un permiso.
11. El aumento de un límite de reintentos no debe permitir doble alta, doble membresía ni doble envío de un evento.
12. La migración de esquema no se ejecuta por leer la nueva configuración; requiere artefacto, autorización y CI independientes.

## 4. Compatibilidad y cambios seguros a futuro

La API actual `empresas` usa GET/lista/id, POST, PUT, PATCH activar/desactivar con permisos de Configuración, y el control global `plataforma/empresas` incluye borrador/alta-controlada; ninguna ruta se altera aquí. La futura configuración no cambia semántica HTTP de las acciones legadas ni transforma desactivar en archivar. Los campos propuestos no deben admitirse dentro de DTO empresarial de escritura ni devolverse como secreto; el acceso administrativo requiere autenticación global por acción.

Rollback **actual**: revertir el commit documental mediante Git normal. Rollback **futuro**: restauración de configuración tipada con `ConfigRevision` auditada, compensación de operación no confirmada y restauración de datos probada; queda pendiente.

## 5. Oráculos positivos y negativos DOCUMENTALES

| Par | Positivo de especificación | Negativo / rechazo obligatorio |
| --- | --- | --- |
| P01/N01 | Master gate apagado impide transiciones | Flag aislado `true` no hace bypass |
| P02/N02 | Identidad operador plataforma validada | Tenant A no puede alterar B |
| P03/N03 | `ConfigRevision` y CAS correctos | Revisión vieja se rechaza sin efectos |
| P04/N04 | Key idempotente con hash coincide | Mismo key, otro payload = conflicto |
| P05/N05 | Revocación completa antes de confirmar suspensión | Fallo de job/caché = no success |
| P06/N06 | Reactivación verifica memberships actuales | No resucitar grants antiguos |
| P07/N07 | Restore/retención aprobados antes de archivo | No usar `Empresa.Eliminado` ficticio |
| P08/N08 | Log redactado por correlación | Nunca filtrar JWT, DSN, emails ni SQL |

Estas ocho parejas son criterios para pruebas futuras, no ejecuciones xUnit/API/EF/MySQL/browser.

## 6. Gates A0–A5 y handoff

**A0:** comprobar dos Markdown (este documento y sección autocontenida de CHANGELOG), fuentes canónicas y ningún archivo de runtime. **A1:** revisar matriz de 18 opciones, 12 reglas de precedencia y ocho pares P/N documentales. **A2:** autoridad global, scopes, tenant A/B, JWT/RBAC, idempotencia y retención en estado no implementado. **A3:** reconciliación con API vigente sin nuevas rutas ni dependencias falsas; DAG validado y prerequisitos institucionales. **A4:** commit Git fast-forward/CAS en dev, Scope Lock y V5 procedencia `completed/success` exact-SHA, BITACORA/PLAN/COLA/DAG readback. CI backend/Angular/MySQL NO_APLICA al cambio exclusivamente documental, no un PASS inventado. **A5:** revisión formal independiente por otra Task Javier para gates posteriores.

Sin autorizaciones de QA/main/PROD, servicios de pago, DNS, real R2, cuentas, secrets, migraciones o despliegue.
