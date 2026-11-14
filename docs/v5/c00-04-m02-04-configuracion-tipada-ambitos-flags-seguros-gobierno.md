# SOLQARYN V5 FINAL — C00-04.M02.04: configuración tipada, ámbitos y feature flags seguros del gobierno de cambios

**Alcance:** contrato documental de `C00-04.M02.04`, autor PRIMARY DEV 02 JAVIER (`TASK_ID=6af16bb9492c8191b8e8c7d606f11507`), repositorio `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. No es implementación de flags ni de motor de gobierno de decisiones. **Ninguna** autorización, configuración o ambiente cambia por publicar este documento.

**Resultado exigido:** definir cómo una futura implementación autorizada representaría configuración tipada, ámbitos, precedencia, valores por defecto, cambios revisables y desactivación segura sin crear nuevas facultades de ejecución. Fuente única ejecutable: documento V5 FINAL `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; estados y gates: Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`.

## 1. Dependencias, trazabilidad e impacto

La puerta individual `DAG_TECNICO!A149:J149` cubre **solo** este ID: nueve predecesores `C00-04.M01.01–06` y `C00-04.M02.01–03`; `PLAN_MAESTRO!H110:H118` = nueve LISTO, `K110:K114` cinco A5 correctivas y `K115:K118` cuatro A5 independientes, incluida M02.03 revisada por otra Task Javier en BITACORA fila 3436. `G149` es fórmula dinámica condicionada, no un SI permanente. Se comprobó `PLAN H119=PENDIENTE/I119` vacío y `COLA G119/O119=SI` antes de reclamación exclusiva.

Fuentes técnicas reproducibles: `docs/v5/c00-04-m02-01-entidades-ownership-invariantes.md` (blob `aaf0e385f2d59440b66e923d355f77721f2917f9`), `docs/v5/c00-04-m02-02-contratos-api-dto-errores-gobierno.md` (blob `94c46293e0e168f20875846f7e8b13e963a752a8`), `docs/v5/c00-04-m02-03-persistencia-indices-migraciones-fixtures-gobierno.md` (blob `8d40c510c9d427266ec6cd24a3892214537e4f3e`). Arquitectura vigente: Angular 22.2.1, .NET 10, EF Core 10.0.12, MySql.EntityFrameworkCore 10.0.9, JWT/RBAC, aislamiento tenant A/B.

**Superficie editada:** exclusivamente este Markdown y sección aditiva de `CHANGELOG_AI.md`. No se toca Domain, Application, Infrastructure, API, Angular, `appsettings`, secrets, rutas, migrations, MySQL, GitHub Actions, otros repositorios, Tasks ni historial de BITACORA. No cambia la arquitectura. Consumidores futuros requieren permisos y scope propios; no existe consumidor runtime de este contrato.

## 2. Esquema lógico tipado (FUTURO, no creado en código)

| Campo conceptual | Tipo / validación | Restricción fail-closed |
| --- | --- | --- |
| `ConfigKey` | identificador `enum` de allowlist versionada | Rechazar desconocido; prohibido nombre de permiso arbitrario |
| `ValueType` y `TypedValue` | booleano, entero acotado, duración acotada o enumeración cerrada | Rechazar texto ejecutable, SQL, HTML, JS, código y coerciones ambiguas |
| `ScopeKind` / `ScopeId` | enumeración `PLATFORM`, `ENVIRONMENT` o `TENANT_BOUND` con binding server-side | Nunca confiar en slug, header, cookie, URL, `EmpresaId` remitido por cliente |
| `SchemaVersion` / `SourceRevision` | identificadores inmutables no nulos | Rechazar revisión obsoleta o lectura sin procedencia |
| `Enabled` / `ExpiresAtUtc` | booleano explícito, TTL acotado | Ausente/caducado/malformado = deshabilitado |
| `OwnerAuthorityRef` | referencia a autoridad revisada y vigente | Nunca transformar la referencia en aprobación por sí misma |
| `EvidenceSha` / `AuditEventId` | Git SHA + evento institucional único | Sin check exact-SHA y evento A:Z verificado no conceder efecto |

Este es un **catálogo de diseño**, no nuevas clases, tablas o columnas en producción. La autoridad institucional sigue en `PLAN_MAESTRO!H/I`, elegibilidad en `COLA!G/O` con DAG individual `F=VALIDADO/G=SI`, y eventos en `BITACORA!A:Z`. `ScopeKind` institucional no equivale a `EmpresaId` ni a claims JWT.

## 3. Precedencia, lectura y control de cambios

La configuración hipotética se resolvería de la siguiente forma: (1) política de seguridad y autoridad efectiva del servidor, **no modificable** por flags; (2) allowlist/schema por versión de entorno; (3) valor institucional `PLATFORM`; (4) override `TENANT_BOUND` solamente para claves explícitamente permitidas por la política, con tenant obtenido de identidad server-side; (5) default cerrado. La precedencia nunca puede conceder un rol, ampliar un scope de archivos, activar una Task ni sustituir políticas de seguridad. Para cualquier conflicto, fuente corrupta, expiración, revocación, ambigüedad o revisión desactualizada: **denegar** y conservar último estado institucional validado sin ejecutar transiciones.

Una futura mutación requeriría decisión autorizada, identidad humana/Task autorizada, revisión de diff, comparación por versión atómica real, evidencia exact-SHA, auditoría append-only y readback. `get` + `update` de Sheets no es una transacción CAS garantizada; no proclamar exclusión distribuida a partir de dos llamadas. No crear aquí event dispatcher, webhook, worker, reconciliador, ni llamadas Task→Task.

## 4. Feature flags y reversibilidad segura

Los flags de ejemplo `governance.ui.preview` (solo lectura) y `governance.audit.exportPreview` (solo reporte sin PII) son **nombres conceptuales no registrados**. Cualquier flag operativo, especialmente migración, IAM, JWT, RBAC, tenant binding, persistencia o promoción de entorno, permanece deshabilitado hasta otra aprobación individual. Ningún flag altera autorización en backend ni acepta permisos de la UI. Se requiere estrategia futura de rollback por versión, cache invalidable, TTL corto, verificación A/B y garantía de que desactivar un flag no borra información legítima. No autorizar compras, dominios, QA, main ni PROD mediante un override.

## 5. Oráculos documentales y controles de abuso

Estos casos se **inspeccionan como contrato**, no fueron ejecutados como tests runtime:

| Caso | Entrada simbólica | Resultado exigido |
| --- | --- | --- |
| P01 | Nueve predecesores LISTO+A5 y DAG149 válido para M02.04 | Se puede documentar esta unidad sin habilitar otras |
| P02 | Clave allowlisted booleana, scope autenticado por servidor y revisión actual | Valor tipado válido, sin nuevos permisos |
| P03 | Flag de vista previa ausente/deshabilitado | Se mantiene experiencia básica sin permisos añadidos |
| P04 | Versión anterior auditada y revisión autorizada compatible | Rollback conceptual reversible sin pérdida de BITACORA |
| N01 | Clave desconocida, tipo incorrecto o conversión no válida | Rechazo sin fallback permisivo |
| N02 | `<script>`, SQL, HTML, payload arbitrario o secreto inline | Rechazo y no persistencia |
| N03 | Tenant B solicitado mediante header, URL o `EmpresaId` elegido por cliente A | Denegación y no lectura cruzada |
| N04 | Feature flag intenta otorgar JWT, RBAC, permiso administrativo o A5 propio | No se concede autoridad |
| N05 | Override ENV/TENANT fuera de allowlist de ámbito o expirado | Default cerrado; no herencia privilegiada |
| N06 | Revisión obsoleta, fuente inconsistente o dos escritoras sobre el mismo ID | NO_GO hasta exclusión y readback real |
| N07 | `COLA G/O=SI` sin DAG individual validado o predecesor no LISTO | NO_GO de claim y liberación |
| N08 | CI requerida `QUEUED`, `SKIPPED`, `CANCELLED`, `FAILED` o `IN_PROGRESS` | No se declara PASS |
| N09 | Task externa al allowlist o autor pretende aprobar su A5 | Rechazo y evidencia preservada |
| N10 | Worker/webhook/Task→Task pretende ejecutar un cambio sin scope nuevo | Rechazo; solo documentación |
| N11 | Flag propone SQL/migración, token, proveedor de pago, QA/main/PROD | Requiere autorización y pruebas separadas |

## 6. Auditorías A0–A5 y rollback

**A0:** inspección estática de scope, fuentes originales, blobs, secciones Markdown, prohibiciones, no secretos y procedencia del CHANGELOG. **A1:** revisión documental de P01–P04 y N01–N11 (no tests ejecutables). **A2:** negativa JWT/RBAC/tenant A/B, PII, secretos, separación de autor y revisión. **A3:** correspondencia de nueve precedentes, DAG149, PLAN/COLA/BITACORA y contratos M02.01–03; sin cambios de runtime. **A4:** publicación de dos Markdown por fast-forward CAS sobre HEAD actual, diff/árbol/blobs, Scope Lock y check V5 exact `head_sha`, `completed/success`, estados y readback. `NET`, Angular, MySQL, Docker, Playwright y E2E son **NO_APLICA** únicamente porque no hay cambios ejecutables, nunca PASS inventado. **A5:** evaluación independiente por otra de las cinco Tasks Javier, conservando el autor.

**Rollback:** revert ordinario del commit documental, verificando descendientes y HEAD; si hay error institucional, agregar evento compensatorio a BITACORA, jamás editar historia ni forzar ramas. `C00-04.M02.05–06`, implementación de flags y toda promoción requieren DAG y permisos propios. Ninguna aprobación QA/main/PROD se deriva de este cierre.
