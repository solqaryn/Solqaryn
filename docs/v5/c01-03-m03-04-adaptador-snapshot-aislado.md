# SOLQARYN V17 — C01-03.M03.04: adaptador aislado de snapshot de configuración dinámica

**Alcance autorizado:** PLAN_MAESTRO C01-03.M03.04, fila 233, `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Autor: PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. Riesgo **R2**: configuración sensible, tenancy y concurrencia.

## A0 — Impacto y contrato
Se implementa `C0103IsolatedCatalogSnapshotAdapter` en Infrastructure, implementando el puerto Application `IC0103TenantConfigurationCatalogSource` creado por M03.03. La dependencia queda Infrastructure → Application → Domain, sin cambiar API, EF Core, MySQL, DI ni contratos del navegador. El adaptador encapsula un snapshot **sintético** de lectura; no es un repositorio real, migración, autorización viva ni transacción durable. No instala tenant context, cambia configuración o presta permisos a otros scopes.

Captura un tenant/clave/revisión/SHA esperados y copia defensivamente la lista de claves usando `StringComparer.Ordinal`. Solo entrega el snapshot si coinciden exactamente tenant, clave canónica, versión y SHA, si los indicadores de procedencia y propietario están presentes y si la clave figura en el conjunto ordinal. Los parámetros provienen de la captura aislada, **no prueban por sí mismos autoridad backend real**. Un lector autenticado/transactional persistente requiere implementación separada, autorización específica y pruebas causales.

## A1/A2 — Pruebas y controles
Se agregan 9 hechos xUnit: lectura nominal sin concesión; tenant A/B; rechazo de clave con comparación laxa y mutación posterior del set; catálogo exacto y conjunto permitido; versión/SHA stale; lectura o ownership no verificados; cancelación; validación del constructor; composición con el gate M03.03 que **nunca** concede escritura/CAS/QA/PROD incluso con snapshot sintético. Las pruebas reales de GitHub Actions por SHA determinan PASS; no contabilizar ni afirmar resultados antes de su ejecución.

Las pruebas no representan un JWT autenticado en HTTP, membresía consultada de una base de datos real, verificación MySQL 8.4 o penetración tenant A/B sobre API. El límite de seguridad falla cerrado con `null` al leer capturas no válidas. Toda mutación durable requiere otro scope con autoridad efectiva y transacción reversible.

## A3/A4/A5 — Evidencia, rollback y revisión
A3 pertinente: compilación Release y xUnit Infrastructure/Application/Domain en GitHub Actions y controles de seguridad/authorization. **NO_APLICA** migración/Category=Integration MySQL y navegador: este changeset no toca EF, SQL, controlador, UI ni proveedor real. No confundir builds con integración persistente. A4 exige SHA publicado, Scope Lock y procedencia V5 del SHA exacto, checks pertinentes `completed/success`, readback PLAN/COLA/DAG/BITACORA y changelog con encabezado/viñetas juntas. Mantener VALIDANDO si CI externa real sigue `QUEUED/IN_PROGRESS`.

A5 pendiente de revisor con TASK_ID Javier **distinto** de DEV03. Ningún A5 propio, promoción a QA/main/PROD, cambio de secretos ni activación de proveedor. Rollback futuro: `git revert` convencional del commit en dev con readback y sin force-push; no borrar BITACORA.

## Reparación QA P2 — 16 noviembre 2026 (A5_FAIL preservado)
Revisión independiente DEV06 del SHA `408d188c2aff28a0de8f666ee5e5c4843bc68aa6`: `ReadAsync` devolvía un `HashSet<string>` mutable encapsulado en `IReadOnlySet<string>`; un consumidor podía hacer cast, agregar `jwt.override` y contaminar lecturas posteriores. La copia defensiva de la entrada por sí sola no protege la salida.

Cambio acotado R2: construir `ApprovedKeys` como `FrozenSet<string>` con comparación `StringComparer.Ordinal` usando `ToFrozenSet`; ninguna operación devuelve una colección modificable. Regresión xUnit adversarial nueva: el cast de retorno a `HashSet<string>` debe rechazarse, las mutaciones posteriores del conjunto de entrada no deben afectar lecturas futuras y el tenant B permanece denegado. La regresión anterior fallaría antes de este cambio.

Se conserva `A5_FAIL` histórico y se requiere nuevo GitHub Actions .NET Release/xUnit, seguridad, autorización, Scope Lock y procedencia V5 sobre el SHA publicado; una A5 distinta de DEV03 decidirá el resultado. No afirma ejecución de pruebas hasta que exista CI completada. No cambia Application, Domain, API, EF, MySQL, JWT, datos, permisos, secrets, QA, main ni PROD. Rollback ordinario con `git revert` del commit publicado y readback institucional.
