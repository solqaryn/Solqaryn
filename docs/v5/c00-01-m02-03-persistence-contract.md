# C00-01.M02.03 - Contrato de persistencia, indices y migraciones V5 FINAL

Repositorio `solqaryn/Solqaryn`, rama `dev`, `PROJECT_SCOPE_LOCK=STRICT`.
Fuente unica: V5 FINAL Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, seguimiento `PLAN_MAESTRO!H` y `COLA!E/G/O` del Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.
Autor comercial: PRIMARY_DEV_03 / JAVIER_CHATGPT / TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`.

## Alcance documental y codigo fuente verificado

Definir, no desplegar, persistencia MySQL/EF Core, indices por empresa, migracion reversible y fixtures de prueba sinteticamente aislados. No modificar DB, API, DTO, EF migrations ni codigo productivo con este contrato. Los archivos exactos en dev aportan evidencia de declaracion estatica, no acreditan ejecucion live:

- `backend/src/Infrastructure/Persistence/AppDbContext.cs`, Git blob `c4b32f6637f1b4426655bb0cd12af8dd9d229866`: DbSets y resolucion de EmpresaId autorizada.
- `backend/src/Infrastructure/Persistence/Configurations/ClienteConfiguration.cs`, Git blob `f8fc3f65f99f5e24a8691b5c4eaa070ff4f9f7c8`: `UX_Clientes_Empresa_Id`, `UX_Clientes_Empresa_IdentidadORTN`, FK restrictiva.
- `backend/src/Infrastructure/Persistence/Configurations/ProductoConfiguration.cs`, Git blob `70e7e58d4d3286423038437152786b15aa992e08`: `UX_Productos_Empresa_Id`, filtro de eliminacion logica.
- `backend/src/Infrastructure.Migrations/Migrations/20261013181500_CommercialTenantScopedRootIndexes.cs`, Git blob `0a3d6e2dfb33f3d6cc49cd8a70639f9886964f16`: migracion `Up` e indices compuestos por empresa.
- `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj`: `net10.0`, EF Core `10.0.12`, Oracle `MySql.EntityFrameworkCore` `10.0.9`.

## Matriz de contrato y casos obligatorios

| ID | Invariante y consumidor | Prueba positiva futura | Prueba negativa futura | Evidencia requerida | Estado |
| --- | --- | --- | --- | --- | --- |
| DB01 | AppDbContext y resolucion verificada de empresa | Consulta autorizada A retorna solo registros A | Token B no puede consultar fila A | HTTP + MySQL + RBAC autenticado | DEFINIDO_NO_CERTIFICADO |
| DB02 | Cliente indexado por EmpresaId e Id | Registro con empresa A e identidad A unica | Duplicado normalizado dentro de A rechazado | Explain/SHOW INDEX + insert real | DEFINIDO_NO_CERTIFICADO |
| DB03 | Producto indexado por EmpresaId e Id | Producto legitimo de B visible solo a B | Relacion empresa A con producto de B bloqueada | FK y query tenant contra MySQL | DEFINIDO_NO_CERTIFICADO |
| DB04 | FKs restrictivas y nulidad heredada | Migracion conserva filas legacy sin atribucion | Backfill ficticio EmpresaId de legacy rechazado | Preflight integridad y conteos | DEFINIDO_NO_CERTIFICADO |
| DB05 | Migraciones Oracle idempotentes | Up sobre DB aislada conserva datos y crea indices | Error a mitad de migracion impide promocion | Version EF, SQL generado y rollback | DEFINIDO_NO_CERTIFICADO |
| DB06 | Reversion segura no destructiva | Down o estrategia compensatoria en DB desechable | Borrar datos reales para hacer rollback prohibido | Snapshot, backup, restauracion y diff | DEFINIDO_NO_CERTIFICADO |
| DB07 | Transacciones e idempotencia funcional | Doble retry no duplica compra o inventario | Fallo parcial deja invariantes de saldos validas | API, DB, transaccion, audit log | DEFINIDO_NO_CERTIFICADO |
| DB08 | Fixtures de prueba por tenant A/B | Datos sinteticos sin PII insertados en DB efimera | Fixture con datos reales o claves expuestas rechazado | Seed auditado y teardown verificable | DEFINIDO_NO_CERTIFICADO |
| DB09 | Publicacion segura y control independiente | A0-A4 PASS y A5_PENDIENTE con SHA dev | A5_PASS inventado o CI SKIPPED no liberan QA/PROD | GitHub exact-SHA y BITACORA 21 campos | DEFINIDO_NO_CERTIFICADO |

## Criterio de salida de esta unidad

La aceptacion DOCUMENTAL exige nueve filas DB01-DB09 unicas, rutas/blobs verificables, negativos explicitos, herramientas y evidencia futura. Test Node comprueba integridad de la matriz y rechaza omisiones, no ejecuta SQL. Cualquier implementacion real de indices, EF, API, JWT/RBAC o tenancy necesita pruebas .NET/MySQL/HTTP autenticas antes de declararse PASS de ese cambio; fixture o mock NO sustituye la prueba de datos. No afirmar `MySQL_live_PASS`, `HTTP_live_PASS`, despliegue o migracion realizada por este documento. `Hostinger`, `Cloudflare` y R2 son objetivo futuro; inventario de imagenes historicas cero = NO_APLICA.

El flujo de esta microtarea es PENDIENTE -> EN_PROGRESO -> VALIDANDO antes de auditorias A0-A3 -> commit directo fast-forward a dev -> A4 readback -> LISTO_DEV solamente con A0-A4 PASS y A5_PENDIENTE de otro TASK_ID. A5_PASS independiente y autorizacion adicional son obligatorios para qa/main/PROD. Rollback de este documento: `git revert <SHA_PUBLICADO>` aditivo, sin force-push ni reset, preservando BITACORA e historial de otros actores. No crear PR, ramas, rulesets ni Tasks.
