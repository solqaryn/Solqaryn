# C14-02.M05.04 — aceptación positiva del preflight de seguridad V20

**Riesgo R2.** Fuente: STANDARD V5, C14-02, M05.04; `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Autor DEV05 `6adc2e55f47c81919f6374e561687076`.

## Alcance A0

El estándar exige aceptación de seguridad BOLA/IDOR, spoofing, host, archivos, jobs, caché, backup y sesiones, con cero P0/P1 sin mitigar. Este changeset amplía cobertura positiva sobre el **gate Application real** `C1402SecurityAcceptanceAuthorizationGate` sin modificar contratos o permisos de runtime. Comprueba lectura/escritura empresarial, sucursal, host, archivo y caché con evidencia consistente, más job con grant de servicio e idempotencia. El gate siempre exige adaptador autenticado y **nunca autoriza acceso ni publicación**.

## A1–A3, brechas

Pruebas xUnit reales con seis casos parametrizados más caso positivo de job y negativos de sucursal ajena/restore. Se verifica código exacto y banderas de autorización en falso. Workflow GitHub-hosted .NET 10 Release, Scope Lock, TRX y cancel-in-progress=false por SHA.

**Límite:** fixtures VerifiedEvidence son sintéticas: no hay JWT firmado, HTTP TestHost, RBAC de sesión en MySQL, inyección adversarial real de host/archivos/jobs ni aislamiento empresarial A/B real. MySQL/Chromium son NO_APLICA al diff test-only pero **sí quedan pendientes para aceptación integral C14-02**. No se afirma PASS hasta que el workflow del SHA causal concluya `success`.

## A4/A5, rollback

Conservar VALIDANDO hasta pruebas y checks exact-SHA pertinentes; revisión A5 por otra Task Javier distinta de autora y reparadora. Sin cambiar fórmulas DAG, dependencias, QA/main/PROD, datos productivos, secretos ni cuentas. Rollback: git revert fast-forward ordinario.
