# SOLQARYN V5 — C05-04.M03.02: servicio de aplicación para preflight de ligereza

**Autor:** PRIMARY DEV 03, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3` · proyecto `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT` · fuente STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL.

## Implementación real y frontera de confianza

Se implementa `V5UiLightnessApplicationService` (.NET 10 Application) que acepta un `ContextoTenantActual` **previamente verificado por el backend**, empresa y recurso opaco; valida empresa antes de I/O, pide snapshot a la interfaz `IV5UiLightnessTrustedSnapshotSource` (únicamente proveedor de datos autenticados en backend), verifica empresa del snapshot y delega a las reglas Domain preexistentes `V5UiLightnessDomainRules.Evaluate`. Devuelve un **veredicto de preflight, no una autorización**. No registra DI/endpoint ni se conecta a HTTP, cache real, almacenamiento o MySQL.

La seguridad requiere que cualquier futura implementación del adaptador resuelva membresía/RBAC vigentes y propietario de recurso en servidor, no reciba `TrustedRequestContext` desde navegador. La interfaz no autoriza invocar desde un controlador sin autenticación: integraciones, revocación, SSR, JWT y API requieren gates propios.

## Ocho casos xUnit ejecutables
- P01: snapshot A válido retorna exclusivamente `EXTERNAL_RUNTIME_AND_BROWSER_VERIFICATION_REQUIRED`, una lectura.
- N01: actor tenant A pide B, denegación **antes** de consultar fuente.
- N02: fuente confiable devuelve B al solicitar A, rechazo.
- N03: fuente no entrega evidencia; `TRUSTED_EVIDENCE_REQUIRED` sin PASS implícito.
- N04: snapshot declara membresía revocada; `BACKEND_AUTHORITY_REQUIRED`.
- N05: resourceId vacío, error antes de I/O.
- N06: token cancelado, error antes de I/O.
- N07: revisión de recurso obsoleta, denegación de caché.

Los casos son código xUnit de Application/Domain real que debe ejecutarse en GitHub Actions del SHA publicado; no se declara PASS antes del check. No mide LCP, CLS, INP, peso real de bundle ni evita duplicación de peticiones HTTP por sí solo. No modifica budgets Angular, CSS, UI, renderizado, cache real, API, RBAC/JWT, EF/MySQL, tenants existentes, datos ni secretos.

## Precedencia, A0–A5, rollback

Preflight institucional: C05-04.M02.01–06 LISTO+A5, C05-04.M03.01 LISTO+A5 independiente de otra Task, COLA G/O=SI, DAG F=VALIDADO/G=SI, PLAN H=PENDIENTE I libre y claim exclusivo para C05-04.M03.02.

- A0: Skill/Scope Lock, HEAD exacto y autor/owner verificados, diff Application+test+documento+CHANGELOG.
- A1: casos xUnit positivos/negativos y comportamiento fail-closed, PASS sólo con CI requerida.
- A2: tenant A/B y pertenencia backend, revocación, recurso y caching sin autoridad cliente.
- A3: build y pruebas .NET 10 Application/Domain en GitHub Actions; MySQL/Angular/Playwright NO_APLICA al diff sin DB ni UI, no PASS simulado.
- A4: publicación dev fast-forward/CAS, checks backend, seguridad, Scope Lock y V5 procedencia `completed/success` exact SHA o equivalencia de blobs y controles posterior documentada, readback PLAN/COLA/BITACORA.
- A5: otra Task Javier distinta de DEV03 debe revisar SHA, pruebas y riesgos antes de sucesoras; no autoaprobación.

Rollback `git revert` normal del commit, respetando otros HEAD. No se alteran QA/main/PROD, proveedores, DNS, R2, migraciones ni Tasks.
