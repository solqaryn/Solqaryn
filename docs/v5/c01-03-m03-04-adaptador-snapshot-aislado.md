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
