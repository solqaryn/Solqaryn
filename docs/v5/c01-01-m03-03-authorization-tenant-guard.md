# C01-01.M03.03 — Autorización tenant y denegación por defecto

## Alcance y confianza
La guardia \`IdentidadUniversalAuthorizationGuard\` es código Application .NET 10 que exige un \`ContextoTenantActual\` ya autenticado y una instantánea **actual** cargada por \`IIdentidadUniversalSnapshotSource\` desde una fuente servidora confiable. La interfaz de snapshot no acepta DTOs, encabezados, slugs ni identificadores suministrados por el navegador como fuente de autoridad. Esta microtarea no registra DI, endpoints o middleware ni conecta todavía una fuente EF/MySQL real.

## Decisiones y casos
- P01: actor tenant A con membresía activa, rol empresarial coincidente y permiso relacional explícito activo obtiene el mismo contexto tenant, sin depender de \`Usuario.RolId\` legacy.
- P02: sucursal A activa con propiedad verificada se acepta únicamente cuando fue solicitada explícitamente.
- N01: actor A intentando empresa B se rechaza **antes del acceso a la fuente**.
- N02: membresía revocada o rol modificado rechaza un contexto anterior.
- N03: administrador sin permiso explícito, permiso inactivo/eliminado, permiso de plataforma, rol distinto o acción no concedida se rechaza.
- N04: sucursal de empresa B, sucursal inesperada, ausencia de snapshot e identificador de módulo inválido se rechazan.
- N05: ninguna decisión puede surgir de rol legacy, permisos de otra empresa o asociación inconsistente RolPermiso/Permiso.

## Seguridad, dependencia e integración
Predecesores: M01.06 y M02.01–06 de C01-01; M03.01/02 de C01-01 con A5 independiente; cierres documentales C00-01.M06.06, C00-03.M01.06 y C00-03.M06.06. Gate individual DAG técnico \`F=VALIDADO/G=SI\` y propietario único en PLAN. El guard es **inerte hasta integración explícita posterior**; no sustituye JWT Bearer, RBAC actual en API, filtrado EF o aislamiento de datos, ni autoriza acceso productivo.

A0: inspección estática de nuevos archivos; A1: xUnit positivos/negativos dirigidos; A2: tenant A/B, revocación y permiso por rol; A3: build .NET 10 y suites backend/seguridad en GitHub Actions sobre SHA exacto; A4: Scope Lock, diff y readback; A5: otra Task Javier. Cualquier check obligatorio pendiente/fallido conserva VALIDANDO, sin PASS ficticio. Rollback: \`git revert\` normal del commit de DEV después de verificar HEAD y consumidores, nunca force-push. Sin base de datos, migraciones, secretos, QA/main/PROD, workers o eventos.
