# SOLQARYN V16 — C12-02.M02.03: persistencia, índices, migraciones y datos de prueba del frontend separado

**ID:** C12-02.M02.03, PLAN_MAESTRO fila 1774. **Autor y owner:** PRIMARY DEV 04 JAVIER (`6aeacb7820ac8191b7d768a6f33dfa09`). **Repositorio:** `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo V15:** R0 documental, sin ejecución ni cambio de runtime.

**Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN_MAESTRO/COLA/DAG. **Objetivo:** proponer metadatos persistibles, claves de consulta, etapas futuras de migración y fixtures para el frontend separado; documentar oráculos positivos y negativos sin fingir implementación.

## 1. Dependencias, fronteras y riesgo

`C12-02.M01.01–06` son precedentes documentales con revisión A5 distinta; `M02.01` aporta E01–E12 e INV01–INV16; `M02.02` aporta OP01–08/DTO01–08/ERR01–12; `M02.04` aporta CFG01–14/VAL01–14. La elegibilidad comprobada para el claim fue PLAN `H1774=PENDIENTE, I1774 vacío`, COLA `G1774/O1774=SI`, DAG `F1735=VALIDADO/G1735=SI`. Se conserva el alcance exacto de M02.03, sin usar estado de M02.04 como permiso para deploy.

**Frontera arquitectónica:** Angular 22.2.1 no posee MySQL ni puede autorizar tenants. La persistencia empresarial reside en backend .NET 10 / EF Core 10 / MySQL. El frontend solamente consume manifiestos públicos de release, políticas allowlisted y metadatos versionados que un futuro componente autorizado podría almacenar. No hay aquí tablas, índices SQL, `DbContext`, migración EF, IndexedDB de sesiones, localStorage de JWT, modificaciones de API, navegador, DNS o proveedor. `tenantId`, `Host`, slug, cookies o headers cliente jamás conceden permisos; autorizaciones y ownership se verifican server-side con JWT/RBAC.

**Decisiones pendientes:** M01 D01–D10 (proxy, CDN, caché, DNS/TLS, CSP/CORS, SEO, telemetría, versionado, rendimiento y promoción). La discrepancia de presupuesto inicial **650–750 KB vs 1–2 MiB** continúa abierta, sin resolverla mediante documento. LCP/INP/CLS = `NO_MEDIDO`. Este trabajo no toca `frontend/angular.json`, `frontend/vercel.json`, Vercel, Render, Aiven, Hostinger, Cloudflare, secretos, bases reales, QA, main ni PROD.

## 2. Doce contratos P01–P12 de persistencia conceptual — NO IMPLEMENTADOS

| ID | Metadato conceptual | Condición de protección/propiedad |
| --- | --- | --- |
| P01 | ReleaseManifest | SHA, version y build verificados; inmutables |
| P02 | AssetManifest | hash de cada asset; solo paths públicos allowlisted |
| P03 | RoutePolicySnapshot | rutas públicas; nunca indexar ERP privado |
| P04 | ProxyAliasPolicy | alias/backend validado; sin destino arbitrario SSRF |
| P05 | SecurityHeaderRevision | CSP/CORS restrictivos versionados |
| P06 | PublicCachePolicy | TTL acotado para assets públicos |
| P07 | PrivateResponsePolicy | no-store; sin sesión o token cacheado entre tenants |
| P08 | DomainBindingProposal | host acreditado fuera del navegador, por entorno |
| P09 | BuildCompatibilityManifest | versión mínima y error tipado si incompatibilidad |
| P10 | RollbackPointer | SHA previo validado, sin promover automáticamente |
| P11 | PublicSeoProjection | robots/sitemap exclusivamente públicos |
| P12 | ReleaseAuditEnvelope | SHA, resultado y correlación minimizados sin secretos |

Son esquemas hipotéticos, no clases C#, interfaces TS ni tablas. La información privada por usuario/empresa siempre requiere segmentación backend y `no-store`, nunca caché pública compartida; un hash por sí solo no prueba autorización.

## 3. Diez claves lógicas IDX01–IDX10 — no índices de base de datos

| ID | Consulta candidata | Restricción |
| --- | --- | --- |
| IDX01 | releaseSha+artifactHash | identidad reproducible de artefacto |
| IDX02 | releaseSha+assetPath | hash por ruta pública |
| IDX03 | environment+routePolicyRevision | rutas y privacidad separadas por ambiente |
| IDX04 | environment+proxyAlias | alias allowlisted sin URL libre |
| IDX05 | environment+headerRevision | seguridad versión por ambiente |
| IDX06 | environment+cacheClass | segregación público/privado |
| IDX07 | releaseSha+minClientVersion | compatibilidad de cliente |
| IDX08 | domainBinding+proofRevision | vinculación sólo con prueba externa |
| IDX09 | releaseSha+rollbackSha | reversión a release acreditado |
| IDX10 | releaseSha+auditCorrelation | trazabilidad sin PII |

Las claves sirven para especificar búsquedas futuras de metadatos de build. **Cero DDL y cero índices MySQL creados.** Cualquier persistencia real posterior debe inspeccionar PK/FK/índices de su esquema y probar `Category=Integration` en MySQL 8.4 efímero si modifica DB. No inferir provider distinto de MySql.EntityFrameworkCore 10.0.9 de Oracle.

## 4. Ocho pasos MIG01–MIG08 para migración reversible futura — NO EJECUTADOS

| ID | Paso propuesto | Bloqueo/rollback |
| --- | --- | --- |
| MIG01 | Inventario SHA de builds, rutas y propietarios | hash divergente aborta |
| MIG02 | Clasificación público/privado/secreto | PII/token en artefacto público aborta |
| MIG03 | Ensayo de manifiesto aislado | hash o SRI inválido aborta |
| MIG04 | Rutas/headers/deep-link con negativos | 401/403 transformado en 200 aborta |
| MIG05 | Aislamiento de caché y host | cookie o respuesta A visible a B aborta |
| MIG06 | Compatibilidad de versiones de cliente | versión incompatibile exige error seguro |
| MIG07 | Ensayo de rollback a SHA anterior | SHA sin evidencia aborta |
| MIG08 | Solicitud de permiso explícito para activación | sin permiso no hay deploy/DNS |

No hay rollback de infraestructura aquí: una migración de CDN, dominio o proveedores necesitará autorización nueva, respaldo real, prueba de reversión y validación R2/R3. No se activa worker, webhook, plan de despliegue ni cambio de DNS por describir pasos.

## 5. Doce datos de prueba F01–F12 sintéticos — NO GENERADOS

| ID | Fixture futuro | Oráculo |
| --- | --- | --- |
| F01 | Release SHA A y manifest con hashes coincidentes | aceptación de integridad |
| F02 | Manifest adulterado | rechazo de publicación |
| F03 | Ruta de landing pública | robots/sitemap público |
| F04 | Ruta ERP privada + token sintético | no-store/noindex |
| F05 | Dos sesiones sintéticas tenant A/B | sin contaminación de caché |
| F06 | Alias API permitido por ambiente | destino verificado conserva auth |
| F07 | URL localhost/red privada | SSRF negado |
| F08 | CSP/CORS restrictivos | sin wildcard credenciales |
| F09 | Versión de cliente anterior compatible | compatibilidad reproducible |
| F10 | Rollback a SHA inexistente | sin mutación |
| F11 | Host sin verificación | sin binding |
| F12 | Log de muestra con token/PII sintéticos | redacción/denegación |

Se describen valores sintéticos, sin records, semillas, cuentas reales ni datos importados. Cada fixture será implementado por una microtarea técnica futura con su propio dueño y pruebas causales.

## 6. Doce pares T01–T12 positivos/negativos — FUTUROS_NO_EJECUTADOS

| ID | Positivo futuro | Negativo, frontera o error futuro |
| --- | --- | --- |
| T01 | Release y hashes idénticos | hash alterado rechaza |
| T02 | Sitemap sólo público | ERP/PII indexados fallan |
| T03 | Proxy alias allowlisted | redirect a host no aprobado falla |
| T04 | Caché pública TTL dentro de política | respuesta privada con Authorization cacheada falla |
| T05 | Tenant A aislado | tenant B servido en sesión A falla |
| T06 | CSP/CORS correctos | script arbitrario o wildcard+credenciales falla |
| T07 | Deep-link autorizado | 401/403 reescrito como éxito falla |
| T08 | Versiones/manifest compatibles | SRI inválido falla |
| T09 | Dominio acreditado por servidor | Host header otorga tenant y falla |
| T10 | Rollback a SHA probado | SHA revocado/no probado falla |
| T11 | Log auditado y minimizado | DSN/token/PII expuesto falla |
| T12 | Timeout sin duplicación | retry crea publicación duplicada falla |

**Pruebas runtime ejecutadas en esta unidad: 0/12.** Compilación .NET, Angular, Playwright, HTTP, MySQL 8.4, EF, proveedor CDN y test de seguridad live son `NO_APLICA` al diff exclusivo R0 documental. Los oráculos de seguridad no equivalen a un test pasado. Backend y JWT/RBAC siguen siendo autoridad de tenant.

## 7. Cierre causal A0–A5 y reversión segura

- **A0:** requisitos M02.03, dependencias y diff limitado a este Markdown y una sección autocontenida en `CHANGELOG_AI.md`; riesgo R0 sin nuevas superficies ejecutables.
- **A1:** 12 P, 10 IDX, 8 MIG, 12 F y 12 pares T revisables; negativos de errores, sesión A/B, versión, red y replay. Ninguna prueba futura ejecutada ni contada PASS.
- **A2:** restricciones conceptuales de JWT/RBAC, revocación, recursos privados, CSP/CORS, SSRF, PII, host y caché fail-closed; no se afirma aislamiento HTTP implementado.
- **A3:** relación estática con M02.01/02/04 y configuración actual; MySQL/.NET/Angular/HTTP/Playwright/proveedores `NO_APLICA` porque no se cambia código.
- **A4:** publicación secuencial fast-forward/CAS en `dev`; `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` del SHA causal, más BITACORA A:Z y readback PLAN/COLA/DAG. `QUEUED/IN_PROGRESS/CANCELLED/SKIPPED/FAIL` de un control requerido nunca es PASS.
- **A5:** revisión real por otra Task Javier de TASK_ID distinto, SHA y blobs acreditados. LISTO_DEV documental no autoriza M03/M06, QA/main/PROD ni promociones por sí solo.

**Reversión de este R0:** `git revert` no destructivo del changeset sobre HEAD revalidado; conservar BITACORA y fórmulas. No cambiar datos, entornos, schedules, secretos o reglas de seguridad.
