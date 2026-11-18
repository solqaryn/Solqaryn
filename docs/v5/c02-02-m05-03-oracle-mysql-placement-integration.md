# SOLQARYN V19 — C02-02.M05.03 integración Oracle EF en MySQL 8.4 efímero

**Riesgo V15:** R2 por tenancy, EF/MySQL y rollback. **Repo:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Autora PRIMARY DEV 03 (`6adc2e4bd8a48191ac7d8641ed95a1a3`), V17 lock de fila 352. PLAN/COLA/DAG habilitados y CAS owner exclusivo; no migración ni infraestructura de producción.

## A0 y alcance auténtico

Se añade prueba **Category=Integration**, que usa el Oracle EF Core real del repositorio y la utilidad `Phase7MySqlTestDatabase` sobre contenedor MySQL 8.4 de GitHub-hosted Actions. Genera base aleatoria independiente con dos `Empresa`, comprueba en DbContexts nuevos la persistencia independiente de dos registros Empresa (sin afirmar filtrado tenant automatico en AppDbContext), y valida que nunca se derive permiso storage a partir de datos persistidos sin JWT/rol/membresía y ejecuta rollback transaccional durable confirmado desde contexto nuevo. La base aleatoria se elimina al finalizar. El test rechaza host remoto y puerto distinto de 3306. No modifica AppDbContext, migraciones, rutas comerciales, valores ni configuración productiva.

## A1 y A2

Assertions positivos y negativos de seed, identidad A/B, lectura de datos reales tras guardar, frontera de host/puerto, denegación de permisos sin backend JWT, rechazo de mezcla A/B y comprobación de que una inserción rollback no permanece. No se concede ninguna operación de placement o cutover; tenant selection debe ser servidor y el control de acceso futuro requiere identidad/rol reales.

## A3 y A4

CI dirigida `.github/workflows/v19-c02-02-m05-03-mysql-integration.yml` con servicio `mysql:8.4`, .NET 10 Release, `Category=Integration`, logs de ejecución y cero FAIL. *No* se considera éxito un job sin que ejecute realmente el filtro ni si se SKIPPED/CANCELLED/QUEUED. Deben completarse también Scope Lock, V5 y checks requeridos del SHA exacto. Aiven, API HTTP JWT firmado, adaptador router real, tablas de placement dedicated, dual-write, backup/restore comercial, DSN real, Cloudflare R2 o producción no son probados ni autorizados.

## A5 y rollback

A5 por otra Task Javier, distinta de autora. Sin promoción a QA/main/PROD ni acceso live. Para revertir, `git revert` normal del commit con CAS HEAD, sin restaurar datos productivos (no hay).

## Reparacion V20 de defecto P2 de prueba adversarial (2026-11-18)

El A5_FAIL independiente encontro un predicado imposible `Id == idA && Id == idB` despues de verificar que ambos IDs difieren. Se elimina esa falsa prueba de aislamiento: un contexto EF privilegiado puede leer ambas filas, por lo que esta suite **no certifica seguridad row-level, HTTP autenticado, JWT firmado ni aislamiento BOLA/IDOR**.

En su lugar se comprueba realmente que ambas filas persisten en MySQL efimero y se ejecuta una regresion de `V5SharedDedicatedPlacementDomainRules.Evaluate` con identificadores A/B procedentes de MySQL: tras configurar un snapshot confiable sintetico que satisface los gates previos, el tenant A obtiene exclusivamente `TRUSTED_PLACEMENT_RUNTIME_VERIFICATION_REQUIRED` (que nunca autoriza conexion) y un intento de tenant B debe devolver **`CROSS_TENANT_PLACEMENT_DENIED`**. Esta asercion falla si desaparece la comparacion A/B de la regla Domain. No se crea usuario JWT, middleware HTTP, repositorio con filtros automáticos ni recursos productivos.

La reparacion requiere repetir Category=Integration real en MySQL 8.4 GitHub-hosted y comprobar Scope Lock, V5 y checks correspondientes en SHA exacto. El A5_FAIL se conserva hasta una nueva revision independiente de DEV05 y la autoria original DEV03. Rollback por git revert normal sujeto a HEAD CAS.
