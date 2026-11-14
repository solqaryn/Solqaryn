# C01-02.M02.05 — Contratos de eventos, idempotencia y correlación de capacidades

**Naturaleza:** especificación documental SOLQARYN V5 FINAL, NO creación ni publicación de eventos. **Autora:** PRIMARY DEV 06 — JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`, `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Autoridades: [Plan V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit).

**No se implementa todavía** webhook, event dispatcher, outbox, cola, broker, worker, endpoint, migración, inter-Task call, nueva Task ni activación de proveedor. M02.05 es un contrato futuro de coherencia y auditabilidad, no prueba de un mensaje enviado. Una decisión de capabilities **no** es un grant RBAC; el servidor siempre revalida tenant/membresía/permiso/recurso antes de aceptar una operación.

## 1. Evidencia y fronteras inspeccionadas

| Fuente de Git dev | Blob exacto | Consecuencia de diseño |
|---|---|---|
| `backend/src/Domain/Entities/FeatureFlagsSaaS.cs` | `ec0062f4d76fd106365c20f3ba50c214369a3de5` | `PoliticaModulosSaaS.Evaluar` es filtro comercial fail-closed por empresa/suscripción/plan/regla; NO sustituye permisos ni ownership de recurso. |
| `backend/src/Application/Services/PermisoService.cs` | `278a8fa90dec43609d20157208a143f5794dcc35` | Grants empresariales relacionales y permiso actual verificado; eventos o plan no crearán grants. |
| `backend/src/Infrastructure/Persistence/Configurations/UsuarioEmpresaConfiguration.cs` | `adb6f7db929ecafcfb101031b4da23595477388b` | Identidad física de membresía única `UsuarioId,EmpresaId`; rol mutable; revocación del owner debe invalidar operación pendiente. |
| `backend/src/Infrastructure/Persistence/Configurations/ConfigEmpresaConfiguration.cs` | `c26a46d4381618c960c93bb71806f802969ddcb8` | La versión de config tenant es token de concurrencia operativo, no una autorización de capability. |
| `docs/v5/c01-02-m02-01-modelo-capabilities-ownership.md` | `85344880fe45907bd9777dfdeb29dce1f07db3fb` | Entidades conceptuales de capacidad y reglas de plan, sin tablas existentes. |
| `docs/v5/c01-02-m02-02-contratos-api-dto-capabilities.md` | `126802a61e74816785242c0fc815e36a70398139` | API y DTO futuros con error seguro, sin rutas implementadas. |
| `docs/v5/c01-02-m02-03-persistencia-indices-migraciones-fixtures-capabilities.md` | `32fbba1c338acc45d78a8bb66d727c8a05480132` | Versiones, solapes, FKs e índices son diseño NO aplicado. |
| `docs/v5/c01-02-m02-04-configuracion-tipada-planes-rubros-flags.md` | `a4365613ff7ac601c744e967d2c47a4cae655ef5` | Reglas tipadas, defaults DENY y alcances, sin flags habilitados. |
| `docs/v5/c01-03-m02-05-eventos-idempotencia-correlacion-limites.md` | `202d8cb2f2e79adbc2a9ac9a36303665b2dfbb90` | Contrato paralelo para config general; no crear una cola o dispatcher duplicados. |

Este documento diferencia **cambio de política comercial** de **asignación de permisos**: el evento hipotético comunica una revisión de filtro, jamás ordena otorgar un privilegio. La BITACORA institucional de automatizaciones (A:Z) es otra autoridad y nunca se transforma en event bus.

## 2. Sobre de evento FUTURO; no existe publicación

| Campo propuesto | Regla y límite |
|---|---|
| `eventId` | UUID opaco generado por servidor **solo tras commit**, único por cambio lógico confirmado. |
| `eventType` | Enum cerrado `CapabilityPolicyCommitted.v1` o `CapabilityPolicyRevoked.v1`; esquema versionado sin deserialización arbitraria. |
| `occurredAtUtc` | Hora UTC de confirmación, nunca hora aportada por el cliente. |
| `companyRef` | Referencia interna tenant ya verificada, no una pista para consultar políticas de empresa ajena. |
| `scopeKind/scopeRef` | Alcance restringido `PLAN_VERSION`, `RUBRO`, `TENANT`, `BRANCH` o `RESOURCE`, con propietario real. |
| `capabilityCode` | Código canónico de catálogo allowlisted, sin SQL/JS/HTML libre. |
| `policyVersionBefore/After` | Revisión monotónica; jamás modificar si `expectedVersion` obsoleta. |
| `correlationId` | Opaco, acotado, sanitizado, generado/regenerado en servidor; sin CRLF, email, JWT o PII. |
| `actorRef` | Identificador técnico pseudonimizado de actor autorizado, nunca token o datos privados. |
| `idempotencyDigest` | Huella de `tenant+actor+operacion+recurso+requestCanonico+version` con aislamiento entre empresas. |
| `schemaVersion` | Compatibilidad explícita; un consumidor desconocido se detiene sin fallback inseguro. |

El sobre contiene **identificadores y versiones**, no valores privados, rutas de fichero, hash de password, secretos, JWT, conexión MySQL, detalles de plan de empresa B ni PII. Un consumidor futuro deberá **volver a comprobar** grants actuales antes de cualquier acceso a recurso; no aceptar evento firmado como autorización para operar por sí mismo.

## 3. Protocolo idempotente y límites, sólo diseño

1. Antes de cualquier mutación futura: autenticar actor, membership vigente, grant RBAC explícito, propiedad de tenant/sucursal/recurso, entitlement comercial y versión esperada. Sin cualquiera: DENY, sin evento.
2. Canonicalizar solicitud (`capabilityCode`, tipo, plan version, alcance, versión), verificar clave de idempotencia opaca de longitud acotada y computar digest separado por empresa/actor; clave idéntica de A no lee resultado de B.
3. En **una transacción autorizada futura**: verificar exclusión/versión, escribir cambio y registro idempotente; el diseño de outbox requerirá implementación separada. No afirmar success antes del commit ni emitir evento sin durabilidad.
4. Repetir misma clave+huella+actor+tenant activo devuelve resultado vigente autorizado sin doble mutación; misma clave y payload distinto produce conflicto seguro (409). Revalidar autorización incluso si hay respuesta previa almacenada.
5. Rechazar operación al revocar membresía/rol/plan, ante indisponibilidad de DB, versión stale, duplicidad o solape; ningún cache ALLOW indefinido.
6. Para entrega eventual, proponer al-menos-una-vez con consumidor idempotente; **no prometer exactly-once**. No habilitar webhooks, nuevas Tasks ni dispatcher.
7. Cotas de payload, TTL, número de reintentos, rate, paralelismo, retención y eliminación de registros serán definidas con pruebas antes de M03/M05; exceder cuota => 429/409/DENY, nunca escrituras ilimitadas.

La revocación precede a notificación: una decisión ya cacheada o enviada no tiene autoridad de permiso. El frontend solo refleja estado y recupera una proyección autorizada; una notificación tardía no puede volver a habilitar plan ni grant revocado.

## 4. Oráculos positivos y negativos FUTUROS_NO_EJECUTADOS

| ID | Signo | Resultado contractual futuro |
|---|---|---|
| P01 | + | Primera modificación válida A con grant/plan/recurso correcto y versión vigente: un solo commit lógico e ID único. |
| P02 | + | Retry idéntico del actor A con misma huella y autorización actual: mismo resultado, cero efecto extra. |
| P03 | + | Cambio de versión que restringe plan A: deniega acción tras revalidación, sin cambiar grants. |
| P04 | + | Dos empresas A/B con clave textual igual: espacios de deduplicación independientes. |
| N01 | - | Solicitud A refiere recurso B: DENY, sin evento ni filtración de B. |
| N02 | - | Plan habilitado sin grant RBAC: DENY, no emitir. |
| N03 | - | Grant presente pero suscripción caducada/revocada: DENY. |
| N04 | - | `EsAdministrador` legacy sin grant: DENY. |
| N05 | - | Misma clave con request canónico distinto: 409 sin mutación. |
| N06 | - | Version stale / dos actualizaciones concurrentes: solo una puede comprometer; otra 409. |
| N07 | - | Retry tras revocar usuario/rol: denegar, no reproducir respuesta privada cacheada. |
| N08 | - | Timeout antes del commit: no afirmar aplicado ni enviar evento. |
| N09 | - | Fallo después del commit antes de entrega: outbox/consumidor FUTUROS deben evitar doble efecto, no inventar entrega. |
| N10 | - | Empresa A y B comparten correlationId/idem key literal: no compartir payload/resultado. |
| N11 | - | Payload incluye SQL/HTML/JS, CRLF, JWT o PII: rechazar/redactar, no publicar. |
| N12 | - | Fuente de permisos/plan/DB falla: DENY sin fallback o evento. |
| N13 | - | Suscripción suspendida durante ventana de reintento: no reactivar por evento tardío. |
| N14 | - | Se interpreta BITACORA A:Z como cola de eventos runtime: rechazo de autoridad paralela. |
| N15 | - | EventType/schemaVersion desconocido o falso scope: consumidor futuro falla cerrado. |
| N16 | - | Se pretende crear worker, webhook, broker, scheduler, migración o QA/main/PROD por este M02: NO_GO. |

Son **4 escenarios positivos y 16 negativos documentales, no ejecutados**. Ni un test xUnit, integración MySQL, HTTP, Docker o Playwright se acredita por estas filas. No hay event dispatcher operativo en este changeset.

## 5. Impacto, A0–A5, reversión y dependencias

**Cambio físico:** este Markdown y `CHANGELOG_AI.md` únicamente. **Sin cambios** Domain/Application/Infrastructure/API, Angular, persistencia, tests ejecutables, seguridad efectiva, secretos ni proveedores. M02.01–04 suministran modelo y ámbitos; M02.06 revisará salida; M03/microtareas posteriores requerirán implementación, CI dirigida real y autorización DAG propia. No confundir diseño de eventos con trabajo del runtime Tasks.

- **A0** revisar nueve fuentes con blobs, datos sensibles, encabezados CHANGELOG contiguos, diff limitado a dos Markdown.
- **A1** inspeccionar P01–P04/N01–N16 y decisiones fail-closed como oráculos escritos; **no son tests ejecutados**.
- **A2** aislar tenant A/B, JWT, RBAC, revocación, scope, correlación segura, PII/secretos y reintentos.
- **A3** verificar compatibilidad documental con FeatureFlagsSaaS y M02.01–04/C01-03.M02.05; sin integración runtime nueva.
- **A4** commit FF/CAS directo en dev, Scope Lock y check V5 provenance `completed/success` sobre SHA publicado; readback H/I, COLA y BITACORA. Backend, Angular, MySQL, Playwright = NO_APLICA **al diff Markdown**, nunca PASS inventado.
- **A5** pendiente de otra Task Javier distinta a la autora; no promover QA/main/PROD por A0–A4.
- **Rollback:** git revert normal del changeset en dev, evento compensatorio append-only, sin force-push ni borrar historia ni mutar tenants.

**Gates:** PLAN H192/PENDIENTE+I vacío, COLA G/O SI y DAG F/G VALIDADO/SI al claim; A0–A4 antes de LISTO. No liberar M02.06, M03 ni permisos comerciales por un documento sin A5 independiente.
