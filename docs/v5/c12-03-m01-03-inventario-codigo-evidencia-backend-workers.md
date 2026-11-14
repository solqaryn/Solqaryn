# SOLQARYN V5 — C12-03.M01.03: inventario verificable backend y workers separados

**Fecha:** 2026-11-14 · **ID:** C12-03.M01.03 · **PLAN fila:** 1804 · **Autor:** PRIMARY DEV 02 JAVIER / `6af16bb9492c8191b8e8c7d606f11507` · **Rama única:** `solqaryn/Solqaryn@dev` · **Alcance:** solo lectura/inventario documental M01.

## Autoridad, método y límites
Fuente: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), [PLAN/COLA/DAG](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Al reclamar se constató PLAN H1804=PENDIENTE, owner I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI y CAS único con readback. La meta es **leer código, contratos y evidencia versionada**, no certificar instalaciones/proveedores/worker.

Las quince rutas se consultaron contra `dev` con su **SHA de blob Git**, no con un hash generado ad hoc. Un blob demuestra contenido versionado y permite comprobar deriva, pero NO demuestra compilación, servicio desplegado, seguridad runtime, A5 ni funcionamiento de proveedores. La lista conserva el nombre y alcance real de cada archivo; no representa prueba funcional.

## Inventario Git verificable

| ID | Archivo | Git blob SHA | Valor de referencia |
|---|---|---|---|
| SRC01 | `backend/src/API/Program.cs` | `01425cff56eb13e12022b1b81a2f322d17fd1fff` | Composición, configuración, DI y HTTP |
| SRC02 | `backend/src/API/Solqaryn.API.csproj` | `2a7b33a0974d1ebe13676593d84b458b1243e037` | TargetFramework net10.0 y dependencias API |
| SRC03 | `backend/src/Application/Solqaryn.Application.csproj` | `5adef1e1f22327300ad640e282928a8fb3d78f99` | Application→Domain y validación |
| SRC04 | `backend/src/Domain/Solqaryn.Domain.csproj` | `8ffb17e6f7fdeb92030585078e65f396780657d5` | Contrato de proyecto Domain |
| SRC05 | `backend/src/Infrastructure/Solqaryn.Infrastructure.csproj` | `6210d8b12abd77171d95d17e2292c1a319b89263` | EF Core, provider MySQL e integraciones |
| SRC06 | `backend/src/Infrastructure/Persistence/AppDbContext.cs` | `c4b32f6637f1b4426655bb0cd12af8dd9d229866` | Contexto EF y resolutores Verified* de tenant HTTP |
| SRC07 | `backend/src/Infrastructure.Migrations/Solqaryn.Infrastructure.Migrations.csproj` | `4fb87115e0c1e38a436d777c898341068c6cc668` | Proyecto separado de migraciones |
| SRC08 | `ARCHITECTURE.md` | `28a53dfd91b80ed7aa7a9579b72324dc526eeb5b` | Arquitectura documentada e historial |
| SRC09 | `PROJECT_CONTEXT.md` | `48064b93299e973c33145662897d772712d2fa29` | Contexto del proyecto |
| SRC10 | `AGENTS.md` | `063d342891ec6e0b23b9adb195ef3b358b51548d` | Allowlist y contrato V5 |
| SRC11 | `docs/PROJECT_SCOPE_LOCK.md` | `7dabef6946d75e0381f121a70458240ac702a30f` | Scope Lock STRICT |
| SRC12 | `docs/AUTOMATION_STATUS.md` | `4b25c0e88e8fb593cbafb5f8836db42c7d703fca` | Estado físico/operativo documentado |
| SRC13 | `docs/v5/c12-03-m01-01-alcance-backend-workers-separados.md` | `3376e57fb3e4972a9929dfd56cf78d56c544a6af` | Objetivo y 16 invariantes M01 |
| SRC14 | `docs/v5/c12-03-m01-02-actores-superficies-backend-workers-separados.md` | `0ddca96a9697e6ea4b18c1e52e3fe3759b39ea9e` | Actores, flujos y fronteras |
| SRC15 | `.agents/skills/solqaryn-project-governance/references/architecture.md` | `1931e1229752028229b6ad1bb3368038e2c6769b` | Arquitectura canónica de skill |

## Hallazgos técnicos limitados a fuente
1. `API/Program.cs` configura WebApplication, servicios y dependencias; la conexión MySQL se obtiene de configuración y el código tiene guardas por ambiente. No prueba configuración real de servidores.
2. La referencia `API.csproj` expresa `net10.0`, proyecto Infrastructure.Migrations y JwtBearer; Infrastructure declara EF Core `10.0.12` y Oracle MySql.EntityFrameworkCore `10.0.9`.
3. `AppDbContext.cs` expone marcas verificadas de lectura para solicitudes HTTP, incluyendo `VerifiedCommercialTenant`, `VerifiedPublicStoreTenant`, `VerifiedPlatformSupportTenant`. Un worker sin `HttpContext` necesita identidad y contexto tenant **específicos, revalidados y deny-by-default**; el inventario no permite inferir bypass.
4. `ARCHITECTURE.md` y la Skill preservan Domain ← Application ← Infrastructure, API como composition root, JWT/RBAC, tenancy y secreto externo; los docs M01.01 y M01.02 solo preparan la decisión futura.
5. El plan habla de backend/worker separados, jobs y cola. **No existe evidencia en este alcance de un dispatcher o proveedor de colas instalado y aprobado**: queda pendiente validación en fases de código.

## Brechas G01–G12: evidencia faltante, NO certificada

| Brecha | Incertidumbre | Prueba/evidencia necesaria en fases futuras |
|---|---|---|
| G01 | No hay prueba de worker operativo independiente del proceso API | Prueba de arranque con proceso aislado en CI efímera |
| G02 | No está acreditada una cola duradera/redelivery/ack en el alcance | Contrato persistente y prueba determinista de reentrega |
| G03 | Contexto tenant en job fuera de HttpContext aún requiere binding verificado | Prueba A/B y ausencia de contexto fail-closed |
| G04 | No está acreditada revalidación de RBAC al consumir job diferido | Casos de permiso/membresía revocada antes del consumo |
| G05 | No hay certificado de idempotencia para reintentos con efecto externo | Duplicado exacto con un solo efecto y registro de deduplicación |
| G06 | No hay evidencia de exclusividad de migración entre API y worker | Prueba concurrente EF/MySQL con dueño único |
| G07 | No se acredita rollback transaccional ante kill/restart | Pruebas crash-before-commit y reanudación |
| G08 | No se acreditan secretos por proceso para deployment futuro | Test ausencia de secret y escaneo de logs |
| G09 | No está probado readiness diferencial para procesos separados | Health fail con dependencia MySQL indisponible |
| G10 | No se acredita trazabilidad segura job↔tenant con datos ficticios | Log correlación sin PII/JWT y auditoría A/B |
| G11 | No hay prueba de compatibilidad/rollback HTTP al separar workers | Contract tests API+DTO y rollback de versión |
| G12 | Hostinger/Cloudflare/red privada y DNS objetivo no constan desplegados aquí | Autorización futura y pruebas live de proveedor, no inferencias |

## Matriz de aceptación futura — NO_EJECUTADA

Estos pares son plan de validación, no resultados de CI, servicios ni MySQL en esta entrega.

| Par | Positivo futuro | Negativo futuro |
|---|---|---|
| P01/N01 | Api net10 compila con guardas | API sin JWT no crea job |
| P02/N02 | A y B quedan aislados en jobs | Job A no accede a B |
| P03/N03 | Reintento idempotente | Redelivery no duplica efecto |
| P04/N04 | Readiness refleja dependencia | Caída MySQL da not-ready |
| P05/N05 | Migraciones exclusivas | Dos procesos no corren DDL simultáneo |
| P06/N06 | Recovery tras reinicio | Crash no confirma trabajo parcial |
| P07/N07 | Secrets provistos en ambiente | Secret ausente falla cerrado |
| P08/N08 | Auditoría correlacionable | Logs no filtran token o PII |

## Seguridad, impacto y reversión
Impacto actual restringido a un documento `docs/v5/` y `CHANGELOG_AI.md`: cero cambios de API, DTO, Domain, Application, Infrastructure, base, migraciones, secrets, jobs, colas, worker, DNS, Cloudflare, Hostinger, QA/main o PROD. Conservar controles JWT/RBAC/tenant A/B y redacción UI proporcionales.

**A0:** fuentes y scope contrastados; **A1:** inventario/consistencia documental de 15 blobs (sin suite runtime); **A2:** revisión estática de fronteras/tenant/PII, sin pentest; **A3:** trazabilidad entre repositorio, arquitectura y Plan, sin integración ejecutada; **A4:** publicación fast-forward y CI pertinente exact-SHA `verify-solqaryn-project-scope` y V5 procedencia, readback. Todo backend build o security check no requerido para Markdown se marca NO_APLICA con razón, no PASS ficticio. **A5:** necesariamente por otra Task Javier. Reversión mediante git revert regular, preservando BITACORA histórica y cambios ajenos.

**Resultado:** inventario verificable de código y brechas abiertas; nada desplegado, ninguna prueba MySQL/worker/carga realizada ni liberación de M02–M06.
