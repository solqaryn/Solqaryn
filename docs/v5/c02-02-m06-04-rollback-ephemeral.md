# C02-02.M06.04 — Ensayo aislado de rollback sin borrar datos legítimos

**Proyecto:** SOLQARYN `dev`, Scope Lock STRICT. **Riesgo:** R2 EF/tenancy/transacción. **Límite:** exclusivamente base aleatoria efímera MySQL 8.4 en GitHub-hosted; NO autoriza R3, producción, migraciones reales, cambio de router ni cutover de clientes.

## A0 — impacto arquitectónico

Se añade una suite `Category=Integration` xUnit .NET 10 que reutiliza el `AppDbContext` Oracle EF Core y el bootstrap existente `Phase7MySqlTestDatabase.InitializeFreshAsync`, más un workflow con servicio `mysql:8.4` y este documento. No se cambian modelos, migraciones, API, DB real, UI, JWT/RBAC/tenant, secretos ni despliegues. La ruta del consumidor son pruebas de persistencia y reversión, no un endpoint comercial. El test bloquea hosts no-locales/puertos ajenos al servicio de CI.

## A1 — aserciones que fallan ante defecto

1. Dos `Empresa` legítimas se guardan realmente en base de datos y tienen IDs independientes.
2. Intento 1: se crea un registro provisional dentro de una transacción Oracle EF y se revierte explícitamente. En un **nuevo DbContext** no existe el provisional y permanecen exactamente los dos IDs previos.
3. Intento 2: se crea otro registro provisional y se abandona la transacción sin commit (disposición del contexto/transaction, simulando interrupción). Readback de un nuevo contexto demuestra la ausencia del provisional y permanencia de ambos registros legítimos.
4. Dos casos negativos rechazan host externo y puerto 3307 antes de conectar. Se comprueba que un snapshot sin backend JWT/rol no puede autorizar conexión/migración, incluso si existen filas persistidas.

**Limitaciones explícitas:** un `DisposeAsync` sin commit no es un SIGKILL del proceso desplegado; no hay caída real del worker de SOLQARYN ni orquestador de migraciones, reconciliación de réplicas o backup/restauración cifrada. La lectura EF privilegiada de ambas filas **no demuestra row-level security** ni aislamiento HTTP IDOR/BOLA. No hay mediciones RPO/RTO; **NO_MEDIDO**.

## A2 — seguridad

El caso negativo A/B no genera autoridad: los campos de identidad sintetizados son pruebas de fail-closed, no JWT firmado ni RBAC verificado. `EnsureDeletedAsync` sólo se ejecuta sobre la base creada con prefijo `v20_c0202_m0604_` y GUID en localhost:3306; nunca borrar una base ajena o datos productivos. El secreto `root` es local al contenedor efímero del workflow, no proviene de Aiven/production.

## A3 — integración pertinente REAL

El workflow `.github/workflows/v20-c02-02-m06-04-mysql-rollback.yml` construye `backend/Solqaryn.sln` Release .NET 10 y ejecuta filtro `FullyQualifiedName~C0202M0604MySqlRollbackSafetyTests&Category=Integration` contra servicio `mysql:8.4` efímero. Para acreditar A3 exigir log de tests ejecutados y cero FAIL en el SHA causal, no sólo un job verde de `Category!=Integration`. **El commit no acredita su propio resultado** mientras GitHub Actions esté QUEUED/IN_PROGRESS. Ningún acceso Aiven, Cloudflare R2, JWT/HTTP o datos de cliente.

## A4 — publicación, CI y procedencia

Publicación atómica fast-forward en dev, SHA padre y archivos auditables, Scope Lock y V5 provenance en `completed/success` de SHA exacto, además de check dirigido realmente ejecutado; pruebas de seguridad pertinentes y PLAN/COLA/BITACORA readback. Mantener VALIDANDO sin pasar a LISTO hasta A0–A4 causales. Un estado `SKIPPED`, `CANCELLED`, `QUEUED`, `IN_PROGRESS` o `FAILURE` no cuenta como PASS.

## A5, rollback de código y decisiones posteriores

A5 requiere revisión genuina por una Task Javier distinta de DEV03. La reversión de este código se realiza con `git revert` normal bajo HEAD CAS, no mediante force push. En futuras operaciones reales de migración, respaldo, desactivación o corte, exigir autorización R3 específica y otras evidencias adicionales; este ensayo es **sólo** rollback efímero.
