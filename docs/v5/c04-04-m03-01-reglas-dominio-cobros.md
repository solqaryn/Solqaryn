# C04-04.M03.01 — Reglas de dominio y validaciones server-side (V20)

Fuente: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, microtarea C04-04.M03.01; SOLQARYN dev, PROJECT_SCOPE_LOCK=STRICT. Clasificación R2 por contexto de cobros. Este cambio no invoca PSP, no registra un pago, no cambia API, DI, EF, MySQL ni credenciales; no autoriza despliegue.

## Implementación acotada
- `backend/src/Domain/AutomationV5/C0404PaymentDomainRules.cs`: preflight puro que recibe contexto de servidor, snapshot de factura y solicitud propuesta. Verifica empresa/actor, membresía/RBAC/revocación, pertenencia real de factura, versión, estado pagable, saldo, cantidad decimal con dos dígitos, moneda ISO aprobada, proveedor allowlisted, operación CREATE_LINK, clave de idempotencia opaca estable y digest canónico verificado por el servidor. Nunca deriva tenant de entradas del navegador.
- La identidad durable futura es `(EmpresaId,ProveedorId,Operacion,IdempotencyKey)`; el digest de factura/importe/moneda es un campo **separado**. Repetición exacta **NO** concede llamada al proveedor: requiere nueva autorización y recuperación backend; repetir clave con digest distinto entrega código `IDEMPOTENCY_PAYLOAD_CONFLICT_409` sin efecto. La aplicación decidirá HTTP 409 y transacción SQL; esta capa no constituye endpoint.
- `backend/tests/Solqaryn.Tests/Domain/C0404M0301PaymentDomainRulesTests.cs`: casos positivos/negativos de precondiciones, aislamiento A/B, null owner, revocación, versiones, estados, importes, moneda, proveedor, replay y conflicto por clave estable. Se ejecutarán realmente en GitHub Actions por SHA exacto, sin atribuir resultados antes de CI.
- `.github/workflows/v20-c04-04-m03-01-domain.yml`: xUnit dirigido en .NET10 Release con TRX, rechazo de cero tests y `cancel-in-progress: false` por SHA. No toca servicios productivos.

## Límites pendientes y gates
A0: diff aislado código + pruebas + workflow + este documento + CHANGELOG, sin cambio de capas. A1/A2: xUnit adversarial dirigido sólo al verificador puro. A3: **NO_APLICA** MySQL/PSP/HTTP al diff de código no conectado a runtime; la **integración de implementación comercial M03 sigue pendiente** (JWT firmado, RBAC real, dos conexiones MySQL 8.4, unique real y PSP aprobado). A4: se requiere GitHub CI exact-SHA `SUCCESS`, Scope Lock/V5, bitácora y PLAN/COLA readback. A5: otra Task Javier distinta del autor DEV01.

Importante: `PagoOnlineService` actual puede iniciar flujo y usa `SemaphoreSlim` por proceso. Este helper **no sustituye** su comportamiento, no prueba corrección de cargos duplicados y no se conecta a él. No inferir seguridad de pagos por los tests puros. Al implementar integración posterior, exigir unique persistente e idempotencia con digest separado, rechazo 409 en API y protecciones tenant/JWT/RBAC independientes.

Rollback: revertir commit por fast-forward normal, sin borrar BITACORA; sin QA/main/PROD ni operaciones financieras reales.
