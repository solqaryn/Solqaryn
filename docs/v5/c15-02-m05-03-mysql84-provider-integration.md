# C15-02.M05.03 — integración Oracle EF 10 y MySQL 8.4 real aislado

Proyecto: SOLQARYN, exclusivamente dev, PROJECT_SCOPE_LOCK=STRICT. PLAN_MAESTRO D2224 exige ejecutar una integración de persistencia o proveedor pertinente. E2224 describe la meta acumulativa M05, no el cierre integral de todos los proveedores en esta unidad.

## A0: impacto y salida atómica

- R2: suite xUnit de integración, workflow GitHub-hosted, documento y sección CHANGELOG_AI.md. No cambia runtime, controlador, migraciones, secreto, endpoint, QA/main/PROD ni configuración de despliegue.
- Ejecuta realmente MySqlConnector y el proveedor Oracle MySql.EntityFrameworkCore ya declarados, sobre mysql:8.4 efímero en puerto 127.0.0.1:3306 y base descartable solqaryn_c1502_m0503.
- Gate antiuso de recursos externos: verifica servidor 127.0.0.1, puerto 3306, base exacta, root temporal y TLS del contenedor. Credencial de prueba local sin acceso a Aiven.
- Tabla independiente de CI; seis asserts ejecutables: version servidor 8.4 y Oracle EF CanConnect; inserción y query scope A/B con IDOR negativo; rollback transaccional InnoDB; tenant hostil parametrizado; cancelación de escritura; rechazo tenant vacío.
- GitHub Actions exige TRX total=6 passed=6 failed=0 notExecuted=0. Ningún SKIPPED/CANCELLED/IN_PROGRESS cuenta como PASS.

## A1–A3: riesgos y limitaciones explícitas

- A1 positivos, negativos, frontera y error reproducibles con consultas a base MySQL real.
- A2 muestra solo filtro tenant de contexto EF en tests aislados, NO certifica el JWT firmado ni la autorización RBAC del backend real, ni revocación de membresías productivas o IDOR de endpoint HTTP. Ese trabajo mantiene sus gates particulares.
- A3 integración MySQL 8.4 REAL del proveedor Oracle EF Core en GH-hosted, NO mocks. No aplica a este diff de test-only la integración externa de Hostinger, Cloudflare, R2, SMTP, DNS o dominios; permanecen NO_VERIFICADO para la fase acumulativa. No se aplican migraciones de dominio ni backups comerciales.
- Rollback Git revert ordinario sobre dev; MySQL efímero se destruye al terminar el job. No force-push, no datos reales.

## A4 y A5

Exigir CI propia exact-SHA completed/success, seis de seis pruebas TRX, backend Release, Security, Scope Lock y procedencia V5 necesarios por riesgo. Mantener H=VALIDANDO y owner DEV05 si CI en curso o fallida, corregir causalmente con nuevo SHA. A5 genuina por otra Task Javier, no por DEV05. Nunca certificar la fase global M05 ni QA/main/PROD.
