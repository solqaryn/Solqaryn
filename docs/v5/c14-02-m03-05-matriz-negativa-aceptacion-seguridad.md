# SOLQARYN V17 — C14-02.M03.05: pruebas ejecutables de aceptación negativa de seguridad

**Repositorio:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **Unidad institucional:** C14-02.M03.05 / PLAN_MAESTRO fila 2070. **Autor:** PRIMARY DEV 01 JAVIER (6adbb9fff1548191a1bd5b30d9e9984a). **Riesgo:** R2 (tenancy, JWT/RBAC, BOLA/IDOR, servicios). **Candado V17:** SQV17_C14_02_M03_05 / 2000002070. **A5:** otra Task Javier.

## Entrega .NET 10 y alcance exacto

Se añade la suite xUnit ejecutable de 14 métodos en backend/tests/Solqaryn.Tests/Infrastructure/C1402SecurityAcceptanceCrossLayerNegativeMatrixTests.cs que usa las clases reales C14-02.M03.01 Domain, M03.02 Application, M03.03 gate y M03.04 Infrastructure aislada. No añade endpoints, DI, MySQL/EF, migraciones, ejecutores/colas, API de backup ni proveedores productivos.

La matriz ejercita positivos limitados que **no conceden acceso**, y negativos reproducibles para BOLA/IDOR tenant A/B, actor/recurso falso, ownership, aserción tenant de cliente, bypass de filtros, enumeración, sucursal, host ajeno, archivo vencido/revocado, job sin grant/idempotencia, caché con revocación, backup/restore sin autorización separada, sesión JWT/RBAC/membresía obsoleta, evidencia/proof insegura, source SHA obsoleto, superficie inválida, claim de privilegios de cliente y cancelación.

## Verificación exigida A0–A5

- **A0:** diff de una suite xUnit, este documento y sección autocontenida de CHANGELOG_AI.md. No se tocan reglas de autorización existentes, sesiones reales ni datos.
- **A1:** correr realmente .NET 10 Release xUnit en GitHub Actions con tests positivos, negativos, frontera y errores; 0 FAIL.
- **A2:** checks exact-SHA de seguridad multiempresa, autorización y el código de contratos de tenant BOLA/IDOR, host, archivo, job y backup. Nunca afirmar JWT HTTP real a partir de fakes.
- **A3:** ejecución real por xUnit del encadenamiento Domain → Application → Infrastructure aislada. NO_APLICA motivado: MySQL 8.4/EF, Playwright, HTTP real, R2 proveedor/backup real porque el changeset no introduce esas fronteras. Su prueba permanece pendiente en los scopes que las implementen; este suite NO certifica el sistema global.
- **A4:** verificar Scope Lock STRICT, procedencia V5, backend Release, seguridad/autorización, check-runs completed/success por SHA causal o descendiente con equivalencia de blobs y dependencias. Confirmar HEAD FF/CAS, PLAN, COLA, DAG y BITACORA con readback.
- **A5:** revisor independiente de otra de las cinco Tasks Javier, con derecho a rechazo real; se mantiene pendiente hasta evento explícito.

## Riesgo residual y reversa

Esta suite no inspecciona tokens JWT firmados, peticiones HTTP reales, filtros SQL/EF, restauración real, Cloudflare R2, secretos, caché distribuida, hosts de producción o auditoría comercial. Un preflight positivo solo exige un adaptador autenticado futuro. El requisito "cero P0/P1 sin mitigación" no puede extrapolarse a la plataforma global por esta matriz; cualquier hallazgo de CI debe permanecer abierto. Rollback ordinario git revert sin force-push y evento compensatorio auditado; no QA/main/PROD, host, DNS o datos reales.
