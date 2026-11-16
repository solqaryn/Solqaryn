# SOLQARYN V17 — C04-03.M02.01: entidades, ownership, relaciones e invariantes de metering

**Autor:** PRIMARY DEV 04 Javier, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Riesgo:** R0 documental. **Origen:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL; PLAN_MAESTRO!A656:M656, COLA!A656:O656 y DAG_TECNICO!A617:G617. **Exclusividad:** candado V17 `SQV17_C04_03_M02_01`, ID `2000000656`. Solo `solqaryn/Solqaryn@dev` y `PROJECT_SCOPE_LOCK=STRICT`.

## A0 — límite y objetivo verificable

M02.01 define un **modelo conceptual, no implementado**, para medir usuarios, almacenamiento, operaciones, archivos y consumo por tenant. No crea tablas, columnas, EF Core, migraciones, código, endpoints, jobs, cuotas, facturas ni cobros. Procede únicamente después de `C04-03.M01.01–06` LISTO y A5 independientes, incluido M01.06; **D01–D08** (precio, política, retención y decisiones comerciales) continúan pendientes y no se resuelven por este documento. M02.02–06 conservan sus propios gates, y ningún M03, QA, main o PROD se autoriza aquí.

## Entidades conceptuales y propiedad

| Entidad futura | Identidad lógica / owner | Propósito y restricciones, NO SQL |
| --- | --- | --- |
| `TenantMeteringScope` | `EmpresaId` autenticado | Raíz obligatoria de todas las lecturas/escrituras de uso; jamás desde browser |
| `MetricCatalogVersion` | `MetricCode + Version`, catálogo aprobado | Unidad, escala y procedencia; inmutable una vez usada |
| `MeteringPolicyRevision` | `EmpresaId + PolicyRevision` | Referencia auditable al permiso vigente; no decide precios |
| `SourceOperationEvidence` | `EmpresaId + TrustedOperationId` | Correlación backend de operación, actor verificado y evidencia de origen |
| `UsageEventEnvelope` | `EmpresaId + EventId` | Evento inmutable con origen, métrica, cantidad y fecha UTC verificadas |
| `UsageIdempotencyIdentity` | `EmpresaId + SourceSystem + IdempotencyKey` | Identidad de deduplicación transaccional y replay |
| `UsageAttribution` | `EmpresaId + EventId + SubjectRef` | Atribución mínima de usuario/archivo/recurso sin PII innecesaria |
| `MeteringWindow` | `EmpresaId + MetricCode + WindowKey + PolicyRevision` | Periodo UTC versionado; fronteras abiertas/cerradas explícitas |
| `UsageLedgerEntry` | `EmpresaId + LedgerEntryId` | Registro aditivo de consumos verificados, sin edición destructiva |
| `UsageCounterProjection` | `EmpresaId + WindowKey + MetricCode + ProjectionVersion` | Proyección recalculable, nunca autoridad independiente del ledger |
| `QuotaEntitlementSnapshot` | `EmpresaId + EntitlementRevision` | Lectura revocable de límite/estado desde autoridad comercial real futura |
| `MeteringAdjustment` | `EmpresaId + AdjustmentId + OriginalEntryId` | Compensación firmada/auditada, nunca borrado de consumo |
| `ReconciliationCase` | `EmpresaId + CaseId` | Discrepancia, decisión humana autorizada y correlación a evidencias |
| `MeteringAuditLink` | `EmpresaId + EvidenceId` | Traza mínima de decisión, SHA/checks/autor sin secretos ni payload PII |

**Relaciones previstas:** Tenant 1:N eventos, operaciones y ventanas; catálogo versionado 1:N eventos; operación confiable 1:N eventos; `UsageEventEnvelope` 1:0..1 idempotencia efectiva; evento 1:N entradas de ledger (consumo o ajustes identificados); ventana 1:N ledger y 1:N proyecciones; caso de conciliación N:M ledger mediante enlaces auditables. Todas las referencias dependientes deben incluir `EmpresaId` en el criterio de enlace y en claves únicas compuestas cuando lleguen a existir. Un `SubjectRef` o `IdempotencyKey` global aislado no autoriza acceso entre empresas.

## Invariantes que el código futuro debe demostrar

- **I01 Tenant:** `EmpresaId` procede exclusivamente de backend autenticado, no query/header/host/selección UI.
- **I02 Aislamiento A/B:** ninguna lectura, JOIN, proyección, ajuste o exportación cruza el tenant, incluso con IDs repetidos.
- **I03 Actor y rol:** JWT vigente, membresía real y RBAC se revalidan; revocación/usuario inactivo deniega.
- **I04 Origen:** cantidad, recurso, unidad y operación se acreditan desde evento server-side, nunca del browser.
- **I05 Idempotencia:** reintento, concurrencia y replay de la misma identidad producen como máximo un efecto contable.
- **I06 Atomicidad:** identidad idempotente, ledger y proyección confirmada comparten transacción/reconciliación segura.
- **I07 Immutabilidad:** un evento aceptado y su `MetricCatalogVersion` no se reinterpretan ni eliminan retroactivamente.
- **I08 UTC:** ventana y corte usan hora confiable UTC y frontera definida; reloj cliente no cambia la facturación.
- **I09 Unidades:** nunca sumar bytes a operaciones o usuarios; magnitudes, redondeos y escalas requieren política aprobada.
- **I10 Límites:** `NULL`, cero, no configurado y "ilimitado" no son equivalentes; fallo de política implica denegar operación sensible.
- **I11 Cancelación:** suspensión/revocación de entitlement no permite nuevos consumos privilegiados; eventos históricos permanecen.
- **I12 Compensación:** corrección produce ajuste aditivo vinculado al evento original, sin reescribir historial.
- **I13 Conciliación:** diferencias de ledger/proyección generan casos auditables y reintentos acotados, no cobros automáticos.
- **I14 Privacidad:** sólo campos mínimos; acceso/exportación con RBAC, retención aprobada y ausencia de secretos.
- **I15 Fallos:** error de DB, timeout o cancelación nunca se presenta como éxito de consumo ni duplica entradas.
- **I16 Aprobación:** D01–D08, A5 y CI causal son requisitos separados; M02 documental no activa pagos ni migraciones.

## A1 — matriz de aceptación **FUTURA, no ejecutada**

| Positivo a acreditar posteriormente | Negativo/adversarial que debe fallar |
| --- | --- |
| P01 evento A se asigna únicamente a tenant A | N01 ID/host/actor B reutilizado con permiso A |
| P02 retry concurrente confirma una sola entrada | N02 duplicación de ledger bajo dos workers |
| P03 operación autorizada aporta origen y unidad | N03 cantidad o `MetricCode` manipulados por cliente |
| P04 frontera UTC produce una ventana estable | N04 timestamp local falsificado altera periodo |
| P05 entitlement vigente se lee de autoridad real | N05 membresía o permiso revocados conservan acceso |
| P06 ajuste conserva historial y reconciliación | N06 rollback borra eventos o emite factura |
| P07 fallo parcial se compensa sin doble cargo | N07 operación devuelve éxito aunque persistencia falló |
| P08 lectura/exportación respeta política de tenant | N08 filtración de evidencia o PII de B |
| P09 ledger y proyección se reconcilian por versión | N09 proyección se trata como fuente financiera |
| P10 auditoría permite reconstruir fuente/revisión | N10 evento sin idempotencia o trazabilidad se acepta |

Estos son criterios especificados para tareas de código **posteriores**; ningún test HTTP/MySQL, xUnit, Playwright, migración o integración se ejecutó para este contrato R0. Sus resultados actuales son `NO_MEDIDO`, no PASS.

## A2/A3 — límites de seguridad e integración

Los contratos describen preservación de JWT/RBAC/tenancy, PII, concurrencia y reversión. No cambia un ejecutable ni una autoridad comercial; A2 es cotejo documental, y A3 es **NO_APLICA** para ejecución .NET/MySQL 8.4/EF Core/HTTP/browser/servicios externos. El futuro código R2 deberá demostrar pruebas positivas y negativas en proveedor MySQL 8.4 efímero con rollback, transacción, tenant A/B, revocación y concurrencia, cuando su diff afecte esas capas. Ningún dato de Aiven/Render/Vercel/PROD ni secreto se consulta o modifica aquí.

## A4, A5 y reversión

Exigir este documento y su encabezado de CHANGELOG autocontenido en **un commit fast-forward/CAS** de `dev`, Scope Lock exact SHA `completed/success`, procedencia V5 `completed/success`, comparación de blobs y readback PLAN/COLA/DAG/BITACORA. Compilaciones no causales no son pruebas R0. Sólo entonces `LISTO_DEV`; A5 exige reviewer de otra de las cinco Tasks Javier, independiente del autor. Si CI sigue queued/in_progress, conservar `VALIDANDO` con SHA/check/owner. Reversión futura por `git revert` normal, nunca force-push ni edición de BITACORA histórica.
