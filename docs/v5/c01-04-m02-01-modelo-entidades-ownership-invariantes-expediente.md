# SOLQARYN V5 — C01-04.M02.01: modelo, ownership e invariantes del expediente universal

**Microtarea:** `C01-04.M02.01` · **Autor:** PRIMARY DEV 04 — JAVIER_CHATGPT, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09` · **Fecha:** 2026-11-14 (America/Tegucigalpa) · **Rama:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

**Entrega:** contrato **DOCUMENTAL** de entidades lógicas, relaciones, autoridad de dominio y oráculos de no-regresión para futuro expediente y seguimiento universal. Ninguna entidad conceptual es una tabla EF/MySQL, DTO compilado, endpoint, formulario Angular, worker, evento productor o garantía de seguridad real. Las pruebas aquí previstas están `FUTUROS_NO_EJECUTADOS`. No modificar QA/main/PROD, datos, JWT/RBAC, tenant, secretos ni automations.

## 1. Autoridad, predecesores y frontera de implementación

La fuente autorizada es el [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), con [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit), Skill canónica, `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md` y arquitectura del HEAD de `dev`. Preflight de `C01-04.M02.01`: PLAN H260=PENDIENTE/I260 vacío; COLA G260/O260=SI; DAG por ID exacto F=VALIDADO/G=SI después de A5_PASS independiente J30 de `C01-04.M01.06` sobre SHA `480e74489da8c9c1073bd66cdde8bcd556bf3690`, blob `a377ec78cd6a38dee45a6cd874cb66abd2df4dd4`. Los M01.01–05 están LISTO/A5 por Tasks distintas, manteniendo autores originales. El gate formal es `C01-04.M01.01–06+A5`, no aprobación automática de M03.

| Documento fuente versionado | Blob Git | Alcance realmente observado |
| --- | --- | --- |
| `c01-04-m01-01-objetivo-expediente-timeline.md` | `c6a8f326569e1afe780b2afd79548ad9dab4f53d` | Objetivo y no-regresión, no runtime |
| `c01-04-m01-02-actores-entradas-salidas.md` | `29184fdc66a5ccec8998c85851f1c505aa02ca80` | Actores/EXP-IO01–10 y fronteras |
| `c01-04-m01-03-inventario-contratos-codigo-evidencia.md` | `1488cb861ceef6f852bb1d9059585239d10cd0e9` | Inventario de código y brechas abiertas |
| `c01-04-m01-04-riesgos-supuestos-decisiones-expediente.md` | `d3db4eac1419e7540ca601084bfc935b746988f7` | Riesgos pendientes, no hallazgos probados |
| `c01-04-m01-05-aceptacion-expediente-timeline.md` | `1cad256ee30742ccc0384fe00eaff586a97add11` | AC01–AC18 futuros, sin pruebas realizadas |
| `c01-04-m01-06-bloqueo-rollback-liberacion.md` | `a377ec78cd6a38dee45a6cd874cb66abd2df4dd4` | Puertas, rollback y A5 independiente |

Superficies existentes inventariadas: `backend/src/API/Controllers/AuditoriaController.cs`, `backend/src/Application/DTOs/AuditoriaDto.cs`, `backend/src/Domain/Entities/RegistroAuditoria.cs`, `backend/src/Domain/Entities/CompraDocumento.cs`, `backend/src/Application/Interfaces/ICompraDocumentoStorageService.cs`, `backend/src/Domain/Security/ContextoTenantActual.cs`. La lectura de esas superficies **no prueba** expediente universal ni filtrado de tenant en todas sus consultas. El riesgo pendiente sobre `AuditoriaRepository.GetFilteredAsync` debe investigarse con casos A/B antes de reutilizarla, sin llamar vulnerabilidad a una observación estática.

## 2. Agregados lógicos EX01–EX16 (NO_IMPLEMENTADOS)

| ID | Entidad conceptual | Autoridad de propiedad y relación |
| --- | --- | --- |
| EX01 | `ExpedienteScope` | Recurso, tipo y propietario empresa/sucursal reales, resueltos por backend |
| EX02 | `ResourceReference` | Clave opaca a entidad existente, sin copiar titularidad de parámetro UI |
| EX03 | `TimelineEntry` | Expediente EX01, orden total por UTC/ID y correlación a fuente |
| EX04 | `SourceEventReference` | Referencia inmutable a hecho transaccional, sin alterar origen |
| EX05 | `Observation` | Actor, rol efectivo, texto validado, timestamps y versión |
| EX06 | `FormalDecision` | Operación autorizada, política, motivo y aprobación separada |
| EX07 | `DecisionReview` | Revisor facultado distinto cuando negocio lo requiera; no A5 automático |
| EX08 | `AttachmentReference` | Expediente + owner, MIME/tamaño y vínculo protegido; no URL pública |
| EX09 | `AttachmentRevision` | Hash, versión, retención aprobada, sin borrar evidencia anterior |
| EX10 | `ExpedienteRevision` | Relación anterior/siguiente, motivo y CAS con versión real |
| EX11 | `ActorMembershipSnapshot` | Referencia mínima a membresía server-side, sin copiar token/PII |
| EX12 | `TenantBoundaryProof` | Empresa/sucursal/owner verificados en cada acceso y efecto |
| EX13 | `AccessDecisionEvidence` | Denegación o permiso backend JWT/RBAC con correlación saneada |
| EX14 | `TimelineCursor` | Cursor opaco ligado al filtro, tenant, orden y revisión de datos |
| EX15 | `IdempotencyReceipt` | Scope/tenant/operación/clave/payload canónico, resultado único |
| EX16 | `AuditTraceLink` | Referencia autorizada a evidencia y SHA, nunca logs sensibles públicos |

`ExpedienteScope` EX01 **1→N** EX03, EX05, EX06, EX08 y EX10. EX03 **0→1** EX04 según procedencia; EX08 **1→N** EX09, EX05/06 deben estar ligados a EX11/EX12 y a un permiso efectivo antes de registrar. EX15 se vincula a una mutación de EX05/06/09 para impedir duplicados. Un hecho de plataforma no adquiere acceso a todas las empresas por el nombre de su agregado; cada enlace valida ownership del recurso y pertenencia del solicitante. Ninguna relación crea FK o DDL físico.

## 3. Invariantes IN01–IN18 que una implementación posterior debe demostrar

| ID | Invariante objetivo | Contraejemplo rechazable |
| --- | --- | --- |
| IN01 | Expediente asociado a owner confirmado backend | EmpresaId nulo se trata como global y visible |
| IN02 | Recursos de A/B quedan aislados en API y persistencia | ID de B consultado por A entrega timeline |
| IN03 | Sucursal limitada a grant y pertenencia | Usuario de sucursal A modifica decisión de B |
| IN04 | JWT firma, vigencia y membresía efectiva verificadas | Token revocado autoriza observación |
| IN05 | RBAC distingue lectura, observación y decisión | Solo rol comercial aprueba sin grant |
| IN06 | Observación nunca equivale a decisión formal | Texto libre crea aprobación irreversible |
| IN07 | Fuente transaccional permanece inmutable desde proyección | Timeline reescribe estado del módulo origen |
| IN08 | Orden estable UTC+ID incluso timestamps iguales | Paginación omite/duplica eventos por empate |
| IN09 | Filtros/cursor no alteran tenant de origen | Cursor de A reutilizado en B revela filas |
| IN10 | Idempotencia única por scope y operación | Replay produce entrada duplicada |
| IN11 | Versionado compara versión en datastore atómico | Escritura obsoleta sobrescribe decisión |
| IN12 | Adjunto y versiones verifican owner/TTL/firma | PublicId de B descarga bytes en A |
| IN13 | HTML/script/texto excesivo se valida y presenta seguro | Contenido XSS ejecuta código o filtra PII |
| IN14 | Logs minimizan datos e identidad | Authorization/JWT se registra en texto plano |
| IN15 | La UI no concede acceso por visibilidad | Componente Angular oculto sustituye RBAC |
| IN16 | Error de worker/proveedor conserva fuente y control de reintento | Timeout duplica o inventa un hecho |
| IN17 | Revisión A5 independiente y CI exact-SHA | Mismo TASK_ID firma A5 o skipped se marca PASS |
| IN18 | Dev documental no habilita otros entornos | LISTO_DEV provoca despliegue PROD |

Los invariantes son falsables, pero **no se probaron** aquí mediante HTTP, MySQL, Playwright ni ejercicios de concurrencia.

## 4. Escenarios EV01–EV12 positivos y negativos, FUTUROS_NO_EJECUTADOS

| Caso | Positivo exigible futuro | Negativo de control futuro |
| --- | --- | --- |
| EV01 | Miembro A consulta solo expediente de recurso A | A obtiene eventos de B con ID conocido |
| EV02 | Actor autorizado registra observación con versión y fuente | Miembro revocado o rol insuficiente escribe |
| EV03 | Decisión formal incluye permiso y revisión requerida | Nota de texto se promueve a aprobación |
| EV04 | Igual timestamp conserva orden estable por cursor | Páginas con empate duplican entrada |
| EV05 | Adjunto propio recupera metadatos autorizados | URL caducada o publicId de B expone bytes |
| EV06 | Replay de operación retorna efecto único | Misma clave con payload cambiado sobrescribe |
| EV07 | Dos escritores concurrentes tienen un ganador CAS | Ambos modifican la versión obsoleta |
| EV08 | UI cambia empresa y descarta respuesta anterior | Respuesta tardía A se dibuja en B |
| EV09 | Historial tiene versiones anteriores auditables | Rollback borra registros previos |
| EV10 | Revisor independiente verifica diff/CI real | Autoaprobación A5 o check skipped PASS |
| EV11 | Errores seguros producen correlación sin PII | Stacktrace/tokens salen al navegador |
| EV12 | Gate DAG exige evidencia para cada etapa | Documento activa migración o producción |

Estos 12 pares **no son ejecuciones** y no acreditan seguridad multiempresa, endpoint, storage, DTO o funcionalidad real. Pruebas posteriores necesitarán fixtures sintéticos A/B, datos de recurso/sucursal, integración EF/MySQL 8.4, HTTP .NET 10, build Angular 22.2.1, negativas de permisos y retest dirigido.

## 5. Seguridad, A0–A5 y rollback

**A0:** verificar los seis antecedentes, modelos EX01–EX16, invariantes IN01–IN18, pruebas futuras EV01–EV12, diff únicamente Markdown y procedencia de CHANGELOG. **A1:** coherencia documental de escenarios positivos/negativos, `NO_EJECUTADOS` cuando son futuros. **A2:** preservar JWT, RBAC, tenant A/B, propiedad de recursos y PII; no atribuir resultado de pentest o test HTTP. **A3:** conciliar M01.06, PLAN H/I, COLA G/O, DAG F/G y BITACORA, sin cambiar el código real; builds/EF/MySQL/Playwright `NO_APLICA_POR_DIFF_DOCUMENTAL`, no PASS sintético. **A4:** fast-forward/CAS del HEAD en `dev`, verificar parent/diff/blobs y Scope Lock/V5 `completed/success` exact-SHA, readback y evento institucional. **A5:** revisión por otra Task Javier distinta del autor y ligada a su SHA original, posterior a LISTO_DEV.

**Rollback documental:** `git revert` normal del commit y evento BITACORA aditivo, sin force-push, reset, migraciones ni borrar pruebas. Cualquier implementación posterior tiene su propia gobernanza, CI y rollback, y ninguna decisión de alcance de este archivo autoriza QA/main/PROD.
