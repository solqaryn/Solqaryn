# SOLQARYN V5 — C12-03.M02.03: persistencia, índices y fixtures para separación backend/workers

**Microtarea:** `C12-03.M02.03`, capítulo C12-03, fase documental M02. **Autor:** PRIMARY DEV 05, `TASK_ID=6adc2e55f47c81919f6374e561687076`. **Repository:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK STRICT. Fuentes operativas únicas: STANDARD V5 FINAL, PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA.

## 1. Estado real, frontera de autorización y alcance

El presente artefacto es **contrato de persistencia conceptual**, no implementación: no modifica clases EF, migraciones, SQL, conexión a MySQL/Aiven, worker, cron, service bus, hosting, DNS, secretos ni pipelines. La separación futura API/worker requiere revalidación de tenant y autoridad fuera del HttpContext, migrador de esquema único y evidencia de rollback antes de cualquier acción de runtime.

Fuentes versionadas:
- `backend/src/API/Solqaryn.API.csproj`, blob `2a7b33a0974d1ebe13676593d84b458b1243e037`: ASP.NET Core `net10.0`; `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj`, blob `6210d8b12abd77171d95d17e2292c1a319b89263`: EF Core 10.0.12 y proveedor Oracle MySQL 10.0.9.
- `backend/src/Infrastructure/Persistence/AppDbContext.cs`, blob inventariado `c4b32f6637f1b4426655bb0cd12af8dd9d229866`: contexto EF; su existencia no certifica esquema remoto.
- `backend/src/Domain/Entities/MensajeOutbox.cs`, blob `891572626c19cd6cf8f522fca5ec876333286ffb`: identidad de empresa, evento, estado, intentos, fecha UTC y clave idempotente.
- `backend/src/Infrastructure/Repositories/MensajeOutboxRepository.cs`, blob `056d5aa380b53ed697ddcb6852df55394e6544a7`: claims tenant-bound con actualización condicional y reintentos.
- `backend/tests/Solqaryn.Tests/N72DOutboxMySqlConcurrencyTests.cs`, blob `ebf6fd4861c55145dc40021152bf1c73ff4edce4`: fuente de test con MySQL efímero, **no ejecutada** por este cambio Markdown.
- `docs/v5/c12-03-m01-06-gates-rollback-backend-workers.md`, blob `3771d1eb465cdb165a3402dbfc673c325b23f78b`: barreras B01–B16, rollback R01–R10, escenarios futuros; A5 independiente verificada.
- `docs/v5/c12-03-m02-01-entidades-ownership-backend-workers.md`: contrato anterior de 12 componentes, 16 invariantes y frontera sin HttpContext, en LISTO_DEV; no se presume A5 propia.

## 2. Doce proyecciones conceptuales T01–T12

Las siguientes **no son nombres de tablas creadas**, sino vistas de ownership para evaluar contra el esquema existente antes de futuras migraciones.

| Ref. | Datos/relación por diseñar | Restricción tenant, integridad y lease |
| --- | --- | --- |
| T01 | `(EmpresaId, Id, TipoTrabajo)` — trabajo conceptual | Toda lectura/claim debe filtrar empresa persistida |
| T02 | `(EmpresaId, ClaveIdempotencia)` — intención durable | Unicidad tenant-local, replay igual o conflicto |
| T03 | `(EmpresaId, Estado, DisponibleDesdeUtc)` — pendientes | Ventana UTC y estados elegibles explícitos |
| T04 | `(EmpresaId, Id, Intentos)` — claim vigente | CAS por intento, nunca solo timestamp |
| T05 | `(EmpresaId, ProcesandoDesdeUtc)` — recuperación stale | Releer lease/escritor antes de recobrar |
| T06 | `(EmpresaId, Id, Estado)` — confirmación de efecto | ACK únicamente para intento actual |
| T07 | `(EmpresaId, FechaActualizacion)` — auditoría | Hash/código seguro, sin JWT ni payload |
| T08 | `(EmpresaId, TipoAgregado, IdAgregado)` — causa negocio | Agregado debe pertenecer al mismo tenant |
| T09 | `(EmpresaId, CorrelationId)` — trazabilidad | Correlación no concede autorización |
| T10 | `(EmpresaId, VersionContrato)` — futura proyección | Versión propuesta, no campo EF existente |
| T11 | `(InstanciaMigrador, LeaseDDL)` — futura proyección | Único actor de DDL, requiere control de entorno |
| T12 | `(WorkerIdentity, GrantId, TenantScope)` — futura proyección | Permisos efímeros; no persistir token ni secreto |

T01–T09 reutilizan conceptos presentes en los contratos de outbox/servicios; T10–T12 son **futuros** y no implican crear una tabla ni otorgar acceso nuevo.

## 3. Ocho índices candidatos IX01–IX08, DDL_NO_EJECUTADO

| Índice de evaluación | Clave conceptual | Justificación, con validación pendiente |
| --- | --- | --- |
| IX01 | `UNIQUE(EmpresaId, ClaveIdempotencia)` | No duplicar índice real existente; medir bajo carrera |
| IX02 | `UNIQUE(EmpresaId, EventoId)` | Protección identidad durable por empresa |
| IX03 | `(EmpresaId, Estado, DisponibleDesdeUtc, Id)` | Claim vencido con orden estable |
| IX04 | `(EmpresaId, Estado, ProcesandoDesdeUtc, Id)` | Detección de stale sin escaneo global |
| IX05 | `(EmpresaId, Id, Intentos, Estado)` | Confirmación condicional de claim |
| IX06 | `(EmpresaId, TipoAgregado, IdAgregado)` | Consulta de operaciones del agregado tenant-bound |
| IX07 | `(EmpresaId, CorrelationId, CreadoEnUtc)` | Traza acotada para operación/auditoría |
| IX08 | `(EmpresaId, Estado, UltimoIntentoEnUtc)` | Diagnóstico fallido/DeadLetter sin alta cardinalidad |

No ejecutar `CREATE INDEX`, `ALTER TABLE`, `Database.Migrate` ni asumir su ausencia. El migrador futuro debe consultar metadata EF/SQL, examinar EXPLAIN, costos de locks y readback en MySQL 8.4 **efímero** y reversión antes de cualquier cambio permitido.

## 4. Seis pasos condicionados de migración M01–M06 (NO_EJECUTADOS)

| Paso | Acción futura sujeta a gate | Evidencia positiva/negativa imprescindible |
| --- | --- | --- |
| M01 | Inventariar migraciones existentes y esquema EF en dev | Hash de manifiesto, no inferir DB productiva |
| M02 | Proponer expand aditivo, evitar modificar datos activos | Idempotencia de migración y rollback de índice |
| M03 | Ejecutar en MySQL efímero con tenants sintéticos A/B | FK, índices, conteos, aislamiento y colisión |
| M04 | Demostrar migrador único ante dos procesos API/worker | Uno obtiene lease de DDL, otro rechaza |
| M05 | Ensayar claim/stale/ACK con dos contextos concurrentes | Un ganador y rechazo de intento anterior |
| M06 | Ensayar down/restore e integridad de negocio/outbox | Checksums y reversión reproducible antes de cerrar |

Estas condiciones son **planes**, no resultados. El hosting definitivo, migraciones reales, backups, restore productivo, transferencias y esquemas de proveedores quedan fuera de autorización.

## 5. Doce fixtures sintéticos FX01–FX12 — ESPECIFICACIONES, NO REGISTROS

| Fixture | Estado futuro esperado | Negativo que deberá rechazarse |
| --- | --- | --- |
| FX01 | Empresa A con intención Pendiente | EmpresaId cero o no validada |
| FX02 | Empresa B aislada con su intención | Query global cruza mensajes A/B |
| FX03 | Misma key idempotente en A dos veces | Duplicado genera efecto doble |
| FX04 | Misma key textual en A y B, aislada | Dedupe global bloquea tenant B |
| FX05 | Dos workers intentan claim simultáneo | Dos ganadores del mismo mensaje |
| FX06 | Recuperación stale con lease realmente vencida | Tomar claim aún activo |
| FX07 | ACK condicionado por intento | ACK supersedido marca Entregado |
| FX08 | Retry expira hasta DeadLetter | Retry infinito o DeadLetter autoactivado |
| FX09 | Commit negocio+outbox en una transacción | Fallo SQL deja una sola mitad persistida |
| FX10 | Migración ejecutada por actor único | API y worker cambian DDL al mismo tiempo |
| FX11 | Readiness falla al faltar DB/secret | Liveness sano se interpreta ready |
| FX12 | Auditoría saneada por tenant | Log expone secreto, payload o PII |

Los doce casos son oráculos futuros positivos/negativos, no tests xUnit ejecutados, no tablas insertadas y no evidencia productiva. Su fase real M03 necesitará .NET 10, MySQL efímero, integración con servicio de tests y seguridad tenant A/B, incluyendo rollback.

## 6. Control preventivo de riesgos

- **Ownership:** un actor autenticado de API y un worker futuro no comparten sesión ni grants. El worker obtiene una identidad de servicio verificable y una autorización separada para el tenant del recurso; sin ella se rechaza antes de acceso SQL.
- **Atomicidad:** el registro del outbox en EF no confirma transacción por sí solo; un negocio debe acoplar transaccionalmente intención y mutación si corresponden.
- **Concurrencia:** ni lectura previa ni timestamp son CAS. En recuperación stale, confirmación/rollbacks deben comprobar intentos actuales y no pisar el resultado ajeno.
- **Seguridad:** JWT/RBAC, errores y logs redacted, datos privados, fail-closed de configuración, migrador único y cuotas finitas no se sustituyen por un diagrama.
- **Deployment:** documento no habilita servidores o servicios pagos, ni DNS, QA, main o PROD. Scope Lock y exact-SHA continúan obligatorios.

## 7. A0–A5, alcance de pruebas y rollback

**A0:** preflight de Skill/HEAD, PLAN pendiente sin owner, COLA G/O SI, DAG F VALIDADO/G SI, CAS exclusivo antes de escribir. **A1:** inspección documental de 12 proyecciones, 8 índices, 6 pasos y 12 fixtures — no atribuir DDL ejecutado. **A2:** tenant A/B, IAM, JWT/RBAC, CAS/leases, idempotencia, logs privados, migrador único y denegación sin HttpContext. **A3:** coherencia conceptual con EF, Repositories, Domain y contratos M01/M02; builds .NET/Angular y MySQL son `NO_APLICA` al diff solo Markdown, nunca PASS. **A4:** commit serializado fast-forward/CAS en `dev`, únicamente este documento y CHANGELOG, encabezado con viñetas contiguas, Scope Lock + V5 `completed/success` del SHA exacto, readbacks PLAN/COLA/DAG/BITACORA. **A5:** revisión obligatoria por otra Task Javier, distinta del autor, antes de gates descendientes.

**Rollback real de esta microtarea:** `git revert` normal del commit documental tras releer HEAD, preservando cambios ajenos. No hay proceso, DB ni secreto que restaurar, porque **no se ejecutó** SQL, MySQL, EF, Docker, workers, eventos, migración, restauración ni despliegue.
