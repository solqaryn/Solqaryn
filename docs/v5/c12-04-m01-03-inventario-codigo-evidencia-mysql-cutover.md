# SOLQARYN V5 — C12-04.M01.03: inventario verificable de código/evidencia para MySQL privado y cutover

**Unidad:** C12-04.M01.03 · PLAN_MAESTRO fila 1840 · **Autor:** PRIMARY DEV 04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09` · **Fecha:** 2026-11-14 · **Ámbito:** `solqaryn/Solqaryn@dev`, PROJECT_SCOPE_LOCK=STRICT.

**Naturaleza:** INVENTARIO_ESTATICO_GIT, NO_CERTIFICACION_RUNTIME. Se identifican fuentes y brechas para una eventual exportación, restore de ensayo, comparación, ventana de cambio y vuelta atrás; **no se ejecuta export, snapshot, migración, DDL, consulta Aiven, lectura de datos personales, cambio DNS/TLS ni aprobación de infraestructura**. La autoridad ejecutable es el [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y la [Sheet PLAN/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit).

## Procedimiento de procedencia y límites

Se consultaron por `GitHub.fetch_file` las **16 rutas originales** del repositorio único en el HEAD `213d08d646bf82e7a7e3648c41b5eb8f254642a8` (lectura de las primeras líneas y SHA blob devuelto por GitHub), después de verificar el Scope Lock de ese SHA. Una huella de blob acredita contenido versionado de un archivo, **no** runtime real, estado MySQL, acceso a otra infraestructura o consistencia de datos. Se revisaron fuentes .NET, Angular, DbContext, migraciones y gobierno; no se usaron repositorios externos, mirrors ni planes archivados.

Preflight de esta microtarea: PLAN!H1840 `PENDIENTE`, I vacía, COLA G/O=SI, DAG F=`VALIDADO`, G=SI; claim condicional exclusivo y readback de autor. `C12-04.M01.01` pertenece a otra Task Javier y no se reescribe; la fase documental no habilita M01.06 ni migraciones.

## S01–S16: fuentes Git y huellas

| Ref | Ruta del repositorio | SHA de blob verificado | Evidencia que aporta / límite |
| --- | --- | --- | --- |
| S01 | `docs/v5/c12-04-m01-01-alcance-migracion-restore-reversion.md` | `31772bba4315c7669d1b73c8aaac480fe6a1d383` | Alcance M01, NR16 y gates export/restore/no operación |
| S02 | `docs/v5/c12-04-m01-02-actores-interfaces-mysql-privado-cutover.md` | `4117d3060be6861e0ee3be85c54a340fe5719aa7` | Actores A14, I/O12 y fronteras F10 del cutover futuro |
| S03 | `backend/src/Infrastructure/Persistence/AppDbContext.cs` | `c4b32f6637f1b4426655bb0cd12af8dd9d229866` | DbContext y Verified*; tenant HTTP no equivale a contexto de worker |
| S04 | `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` | `6210d8b12abd77171d95d17e2292c1a319b89263` | net10.0, EF Core 10.0.12, Oracle MySql.EntityFrameworkCore 10.0.9 |
| S05 | `backend/src/API/Solqaryn.API.csproj` | `2a7b33a0974d1ebe13676593d84b458b1243e037` | API net10.0, JwtBearer y dependencias EF/migrations |
| S06 | `backend/src/API/Program.cs` | `01425cff56eb13e12022b1b81a2f322d17fd1fff` | Composición API, infraestructura MySQL, middleware y autorización |
| S07 | `backend/src/Infrastructure.Migrations/Solqaryn.Infrastructure.Migrations.csproj` | `4fb87115e0c1e38a436d777c898341068c6cc668` | Proyecto de migraciones separado, no acredita DDL ejecutado |
| S08 | `backend/src/Application/Solqaryn.Application.csproj` | `5adef1e1f22327300ad640e282928a8fb3d78f99` | Separación Application/Domain, contratos y validaciones |
| S09 | `ARCHITECTURE.md` | `28a53dfd91b80ed7aa7a9579b72324dc526eeb5b` | Arquitectura vigente y cambios anotados, no topología viva certificada |
| S10 | `.agents/skills/solqaryn-project-governance/references/architecture.md` | `1931e1229752028229b6ad1bb3368038e2c6769b` | Referencia Angular/.NET/EF/MySQL y seguridad vigente |
| S11 | `docs/PROJECT_SCOPE_LOCK.md` | `7dabef6946d75e0381f121a70458240ac702a30f` | Scope Lock STRICT y límites dev |
| S12 | `docs/AUTOMATION_STATUS.md` | `4b25c0e88e8fb593cbafb5f8836db42c7d703fca` | Seis Tasks Javier, exclusividad y A0–A5 |
| S13 | `docs/v5/c12-03-m01-03-inventario-codigo-evidencia-backend-workers.md` | `f597ecf5bfe29a2ce8ada3ae41984b2a8b73be4d` | Inventario backend/worker futuro, no prueba de worker vivo |
| S14 | `docs/v5/c12-02-m01-03-inventario-codigo-evidencia-frontend-separado.md` | `878c620a20e48de70003f84f777a8bffc5fd11a1` | Inventario frontend separado y proxy, sin nuevo hosting |
| S15 | `docs/v5/c10-04-m01-03-inventario-codigo-cache-jobs-backup-exportaciones.md` | `da3ab0ef3606bae512038caeffa3160c427995af` | Inventario de jobs/backup/export actuales y brechas; sin restore real |
| S16 | `frontend/package.json` | `e6d030f643421cb343cc12617e742469e88223c4` | Angular 22.2.1, Zone.js 0.16.3, pruebas UI existentes |

Los SHA anteriores son **huellas de esos blobs en el HEAD citado**, no hashes de bases, tablas, backups, archivos de clientes ni artefactos de restore. `AppDbContext.AuthorizedReadEmpresaId` resuelve verificaciones desde marcas servidor asociadas a `HttpContext`; no demuestra que un servicio sin HTTP pueda inferir tenant, ni concede lectura global. El proyecto `Infrastructure.Migrations` no demuestra despliegue ni ejecución de DDL.

## G01–G14: brechas de evidencia y gate requerido

| ID | Lo que **NO** se puede certificar leyendo Git | Evidencia causal exigible en fase autorizada |
| --- | --- | --- |
| G01 | Inventario de tablas, filas por tenant, datos personales y volúmenes reales desconocido | Obtener de owner autorizado con acceso mínimo; no inferir ni leer DB real en M01 |
| G02 | No hay manifest de snapshot/dump verificado | Diseñar manifest de SHA, momento UTC, fuente, alcance, retención y firma |
| G03 | No hay restore efímero causal documentado para este cutover | Ensayo con datos sintéticos aislados, hashes y FK comprobados |
| G04 | No se acredita consistencia entre inventario/ventas/contabilidad después de restore | Pruebas transaccionales y conciliación por tenant con datos de ensayo |
| G05 | No hay confirmación de collation, time zone, versión MySQL destino ni compatibilidad EF | Matriz versionada y pruebas de migración con servicio MySQL efímero |
| G06 | No se acredita exclusividad real de un ejecutor DDL futuro | Gate de migrador único y lock/lease probado ante dos procesos |
| G07 | Binding tenant sin HttpContext para procesos de restore/worker no certificado | Contrato de identidad de operación y pertenencia server-side fail-closed |
| G08 | No hay matriz de JWT/RBAC posterior al cutover verificada | Tests positivos/negativos sobre A/B y privilegios temporales |
| G09 | Conservación de archivos Cloudinary/otros proveedores no inventariada desde Git | Inventario real y cadena de custodia por owner autorizado, sin asumir cero |
| G10 | No se acredita estrategia para delta de escrituras durante freeze y rollback | Cutover/return con punto de no retorno, reconciliación y stop-the-world |
| G11 | No hay RPO/RTO/TTL ni cuotas objetivos aprobados | Definir cifras tras medir y obtener aprobación; no inventarlas |
| G12 | No se verifica topología Hostinger/MySQL privado ni Cloudflare/DNS/TLS | Permiso independiente, hardening, secreto, reachability y staging previo |
| G13 | No se acredita política de cifrado, retención ni acceso mínimo para dumps | Threat model y controles demostrables de transferencia/almacenamiento |
| G14 | No hay CI runtime dirigida de integración de restore/rollback en el presente changeset | CI exact-SHA con MySQL efímero al pasar a implementación |

`INVENTARIO_DESCONOCIDO` **no significa** `CERO_DATOS`. No se puede aseverar cuántas empresas, filas, respaldos, archivos, usuarios ni medios existen. Credenciales, cookies, secrets y conexiones reales están fuera de este inventario. La disponibilidad del destino tampoco se confirma por menciones a Hostinger/Cloudflare en documentación.

## P01/N01–P08/N08: pruebas diseñadas, FUTURAS_NO_EJECUTADAS

| Caso | Positivo futuro | Negativo / rechazo futuro |
| --- | --- | --- |
| P01/N01 | Manifest del dump sintético conserva fuente, fecha y hash verificable | Blob corrupto, sin hash o sin firma se descarta |
| P02/N02 | Restore efímero valida conteos, FK, collation y secuencias | Conteo/FK divergente bloquea cambio |
| P03/N03 | Tenant A conserva únicamente datos A | Petición de B, header falso o tenant cambiado es denegada |
| P04/N04 | JWT/RBAC posterior al restore aplica membresía | Token expirado o permiso insuficiente no se habilita |
| P05/N05 | Migrador único ejecuta schema con lock autorizado | Segundo migrador simultáneo no aplica DDL |
| P06/N06 | Transacciones financieras conservan idempotencia y orden | Reintento no duplica factura, pago o movimiento |
| P07/N07 | Health/readiness refleja dependencia real del DB | DB inaccesible no devuelve ready |
| P08/N08 | Rollback de ensayo reconcilia delta y deja auditoría | Pérdida de escrituras, secretos en logs o restore ajeno causa FAIL |

Estos casos se **documentaron** pero no se ejecutaron. Antes de un cutover real se necesitan resultados y logs de CI dirigida .NET/MySQL efímero, permisos aprobados, manifiesto de hash/conteos real, ventana, reconciliación y rollback ensayado, cada uno con propietario autorizado.

## Arquitectura, seguridad, impacto y rollback

**Capas actuales:** Angular 22.2.1 → API ASP.NET Core 10 → Application/Domain → Infrastructure/EF Core 10.0.12 → MySQL; JWT, RBAC relacional, tenant A/B, auditoría, CORS y rate limit. **Meta no activa:** MySQL privado/Hostinger y posible Cloudflare DNS/R2 con controles nuevos de disponibilidad y permisos, sin cambiar topología mediante documentación.

**Superficies afectadas por este changeset:** solamente `docs/v5/c12-04-m01-03-inventario-codigo-evidencia-mysql-cutover.md` y una sección entera de `CHANGELOG_AI.md`. Interfaces públicas, DTO, tablas, migraciones, datos reales, API, JWT/RBAC, secretos, archivos, proveedores y jobs no cambian. Las etapas futuras deberán medir consistencia, validar aislamiento A/B y autorización, evidenciar compatibilidad de EF/MySQL y ensayo de reversión.

**Auditorías de la unidad:** A0 fuentes y gates PLAN/COLA/DAG/SHA; A1 inventario 16/16 + ocho pares diseñados, sin prueba runtime; A2 revisión estática de tenant, privilegios, secretos y DB, sin certificar MySQL live; A3 trazabilidad con documentos C10/C12 y código vigente, integración real `NO_APLICA_DOC`; A4 CAS fast-forward de dos Markdown, check `verify-solqaryn-project-scope` y check `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` exact-SHA y readback institucional. Las CI generales `queued/in_progress/cancelled/skipped` nunca son PASS requerido, y para el diff Markdown no se convierten en pruebas de restore.

**A5:** pendiente de un TASK_ID diferente entre las seis Tasks Javier. **Rollback documental:** `git revert` normal tras revisar HEAD, preservando trabajo concurrente, BITACORA y registros ajenos; sin force-push ni borrar historia. M01 no libera M01.06, M03 runtime, QA/main/PROD, compras, provisión de MySQL privado, DNS ni transferencias de datos.
