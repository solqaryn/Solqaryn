# SOLQARYN V17 — C04-03.M02.04: configuración tipada, ámbitos y flags seguros de metering

**Autor:** PRIMARY DEV 03 — JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Riesgo:** R0, contrato documental sin código ejecutable. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, PLAN_MAESTRO!A659:M659, COLA!A659:O659, DAG_TECNICO!A620:G620. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Candado:** `SQV17_C04_03_M02_04` / `2000000659`.

## A0 — Resultado y frontera

Se proponen opciones **tipadas y no activas** para la futura medición de usuarios, almacenamiento, operaciones y archivos por empresa. M02.04 define validaciones, precedencia, revocación, observabilidad y rollback; no implementa configuración real, feature toggles, tablas, EF/MySQL, JWT, API, pagos, cupos ni facturación. Ningún valor comercial D01–D08 (precios, retención, unidades, límites, reconciliación) queda aprobado. La disponibilidad DAG sólo autoriza esta documentación; no libera M03, QA, main ni PROD.

**Seis ámbitos conceptuales, de mayor a menor autoridad:** (1) límites de seguridad inmutables del backend y Scope Lock; (2) catálogo de métricas versionado autorizado; (3) política comercial aprobada y firmada; (4) pertenencia y derechos efectivos del tenant autenticado; (5) revisión temporal de ventana UTC; (6) vista de lectura por usuario autorizado. Un ámbito inferior **nunca** habilita lo que otro superior deniega. Nada se acepta del cliente como `EmpresaId`, política, `MetricCode`, precio, cantidad ni identidad de idempotencia. La UI sólo presenta, no decide.

## Esquema tipado propuesto (NO IMPLEMENTADO)

| Clave conceptual | Tipo | Valor seguro ante ausencia | Validación futura y autoridad |
| --- | --- | --- | --- |
| `MeteringEnabled` | `bool` | `false` | Activación explícita aprobada, nunca por cliente |
| `MetricCatalogVersion` | `VersionRef` | `UNCONFIGURED` | Debe existir y coincidir con catálogo inmutable |
| `MeteringPolicyRevision` | `PolicyRef` | `UNCONFIGURED` | Revisión aprobada, vigente y específica del tenant |
| `EntitlementRevision` | `RevisionRef` | `UNCONFIGURED` | Lectura revocable desde autoridad comercial, no browser |
| `WindowKind` | `enum UTC_WINDOW` | `UNCONFIGURED` | Intervalos UTC y límites documentados en política aprobada |
| `UsageUnit` | `enum METRIC_UNIT` | `UNCONFIGURED` | Compatibilidad exacta con catálogo, jamás suma heterogénea |
| `QuotaEnforcement` | `enum DISABLED/AUDIT/ENFORCE` | `DISABLED` | `ENFORCE` sólo con decisión comercial y pruebas R2 |
| `MaxAcceptedEventsPerBatch` | `PositiveInt` | `UNCONFIGURED` | Nunca interpretar ausente como ilimitado |
| `MaxAcceptedPayloadBytes` | `PositiveInt` | `UNCONFIGURED` | Rechazar overflow, valores negativos o fuera de política |
| `DeduplicationRetention` | `PositiveDuration` | `UNCONFIGURED` | No truncar historial idempotente sin política aprobada |
| `MaxRetryAttempts` | `NonNegativeInt` | `0` | Backoff acotado y cancelación real; 0 = sin reintentos |
| `RetryBackoff` | `PositiveDuration` | `UNCONFIGURED` | No reintentar sin plazo positivo y máximo permitido |
| `AllowHistoricalBackfill` | `bool` | `false` | Requiere autorización y auditoría de corrección |
| `AllowMeteringAdjustments` | `bool` | `false` | Sólo entradas aditivas con link original; nunca delete |
| `AllowUsageExport` | `bool` | `false` | RBAC/PII/tenant A-B, exportación mínima versionada |
| `AllowAutomaticCharges` | `bool` | `false` | Permanece denegado; este contrato no autoriza cobros |

`UNCONFIGURED` es un estado lógico propuesto, **no** un valor a persistir ni un default productivo. Las opciones incompatibles, ambiguas, desconocidas, no firmadas o antiguas obligan a denegar la operación sensible, no a aplicar una política alternativa silenciosa. Un cliente puede consultar sólo una vista explícitamente autorizada, sin secretos ni umbrales ajenos.

## Invariantes y compatibilidad que deberá satisfacer el código futuro

1. `EmpresaId` deriva de identidad autenticada y se combina en todas las claves, consultas, caches y logs; no puede sobrescribirse desde request.
2. Resolver `PolicyRevision` y `EntitlementRevision` de fuentes reales vigentes en cada decisión crítica; revocación tiene prioridad.
3. Una `MetricCatalogVersion` usada se conserva inmutable; cambio de unidad o precio no reinterpreta ledger anterior.
4. Validación tipada rechaza `null`, cadenas en lugar de números, NaN, overflow, enums desconocidos, negativos y mezclas de unidades.
5. Flags se evalúan en backend en modo fail-closed; frontend, cache o feature flag local nunca conceden derechos.
6. Conflicto de idempotencia con mismo key y digest diferente produce rechazo auditable, no segundo efecto.
7. Reloj cliente, timezone ambiguo y rango UTC inválido jamás alteran ventana, cuota o cargo.
8. Cancelación, timeout y fallos de persistencia no se convierten en éxito ni borran evidencia histórica.
9. Lecturas/exportaciones no filtran PII, política comercial o datos de tenant B a tenant A.
10. Un flag deshabilitado niega llamadas incluso si un frontend antiguo lo muestra como habilitado.
11. La migración futura sería aditiva, reversible y probaría A/B y concurrencia en MySQL 8.4 efímero; aquí no hay SQL.
12. Versionado compatible de config: cambio de significado requiere revisión nueva y rollback documentado; nunca reinterpretación silenciosa.

## A1 — matriz FUTURA de aceptación (NO EJECUTADA)

| Caso positivo futuro | Negativo/adversarial futuro |
| --- | --- |
| P01 lectura de política firmada para tenant A | N01 tenant B o `EmpresaId` de UI fuerza política A |
| P02 opción booleana exacta y permitida | N02 cadena arbitraria `"true"` concede permiso |
| P03 versión de catálogo válida e inmutable | N03 versión retirada o unidad incompatible se acepta |
| P04 flag deshabilitado deniega cargo | N04 browser habilita cargo deshabilitado |
| P05 revocación invalida entitlement y cache | N05 entitlement revocado sigue autorizando uso |
| P06 UTC estable crea una ventana única | N06 reloj local manipulado cambia corte |
| P07 retries acotados conservan idempotencia | N07 dos workers duplican ledger |
| P08 rechazo de overflow mantiene integridad | N08 decimal/entero excedido se trunca |
| P09 exportación autorizada minimiza PII | N09 exportación permite ver tenant B |
| P10 rollback normal conserva historial | N10 rollback elimina ledger, secretos o logs |

Estas son pruebas que deberá implementar y ejecutar el scope R2 correspondiente. **No se ejecutaron** xUnit, MySQL, HTTP, Angular ni Chromium para esta entrega documental. `NO_MEDIDO` para su resultado funcional.

## A2/A3 — seguridad e integración

Revisión documental: el contrato exige JWT/RBAC, revocación, tenant A/B, auditoría sin PII, fail-closed y idempotencia. No cambia una superficie runtime; A2 sólo comprueba estas exigencias escritas. A3: `NO_APLICA` a ejecución de .NET 10, Oracle EF/MySQL 8.4, APIs, browser, proveedores, migraciones y despliegues. D01–D08 permanecen sin autorización; ninguna integración real o pago se activa aquí.

## A4/A5 — publicación y reversión

Entrega de **un único documento** y una sección autocontenida en `CHANGELOG_AI.md` mediante commit fast-forward con comparación de HEAD. Validar `verify-solqaryn-project-scope` y procedencia V5 en SHA exacto, hashes de ambos archivos, lectura posterior de PLAN/COLA/DAG y BITACORA. Mantener `VALIDANDO` si un check obligatorio permanece queued/in_progress; no convertir SUCCESS de builds ajenos en evidencia de integraciones no ejecutadas. Cierre `LISTO_DEV` sólo con A0–A4 R0 comprobados y revisión A5 posterior por otra Task Javier. Reversión: `git revert` aditivo del commit, sin force-push y sin tocar historial de bitácora.
