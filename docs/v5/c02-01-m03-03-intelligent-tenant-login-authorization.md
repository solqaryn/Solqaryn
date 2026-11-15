# SOLQARYN V17 — C02-01.M03.03: autorización de empresa 0/1/N

**Riesgo R2, DEV02 Javier, solqaryn/Solqaryn@dev, Scope Lock STRICT.**
Fuente: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, anexo UX_LOGIN_INTELIGENTE_V1, PLAN_MAESTRO ID C02-01.M03.03, M03.01 Domain y M03.02 Application.

## A0 — cambios y frontera de confianza

Se incorpora el servicio ejecutable Application `V17IntelligentTenantLoginAuthorization` con lector `ITrustedTenantLoginAuthorityReader`. El futuro adaptador backend DEBE verificar JWT actual, usuario activo, membresías persistidas, rol relacional, revocación y preferencia por el **mismo usuario** antes de crear la instantánea; el cliente no declara autoridad con empresa/host/rol. El algoritmo calcula: 0 membresías = denegación (consola global requiere adaptador distinto), 1 = contexto directo sin selector, N con preferencia válida = contexto directo, N ambiguas = sólo lista verificada. Preferencia expirada o de otra cuenta no concede acceso. Sesión vigente exige revalidación backend; switch exige sesión y objetivo actualmente autorizado, purga de caché/permisos y reemisión.

Los resultados nunca emiten JWT, instalan tenant HTTP, conceden acceso, cambian base de datos ni validan la consola global. El código es **política Application ejecutable y probada**; no sustituye la conexión autenticada, control de sesión, persistencia de preferencia ni UX exigidos en C02-01.M03.04/M04 y C03-01.

## A1/A2 — aceptación y aislamiento

15 pruebas xUnit adversariales positivas, negativas y frontera cubren empresa única, N, preferencia correcta/inválida/ajena, 0, global separado, tenant B, estado y revocación de rol, JWT y store no verificados, cambio explícito, sesión caducada, actor ajeno, duplicados, nombre inseguro, límite de 128, fallo del proveedor y cancelación. Ningún test mock es evidencia de autenticación HTTP o base de datos real. Exigir backend Release, seguridad multiempresa y autorización GitHub Actions exact-SHA antes de declarar A1/A2 aprobados.

## A3/A4 — publicación y reversión

No se alteran EF, MySQL, API, JWT HTTP, DI, Angular, Playwright ni proveedores, por lo que sus pruebas de integración reales son **NO_APLICA motivado a este changeset**, no acreditadas. Exigir Scope Lock STRICT, V5 procedencia, build/tests y autorización/seguridad SUCCESS causal, diff, SHA y readback PLAN/COLA/BITACORA. A5 independiente corresponde a otra Task Javier, no DEV02. Rollback: git revert normal del commit conservando HEAD concurrente, nunca force-push. Sin QA, main, PROD, secretos o eventos ejecutables.
