# SOLQARYN V17/V15 — C05-01.M03.04 adaptador aislado de design tokens

**ID:** C05-01.M03.04, PLAN_MAESTRO fila 737. Autor: PRIMARY DEV 06 — JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. Único ámbito: `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Riesgo **R2** por multiempresa, RBAC, sesión, revisión y CSS seguro. Candado V17: `SQV17_C05_01_M03_04` / `2000000737`.

## A0 — requerimiento, arquitectura y diff

Entrega una implementación **ejecutable y aislada** de `C0501DesignTokenIsolatedSnapshotAdapter` en Infrastructure .NET 10. Reutiliza las reglas de Domain `C0501DesignTokenDomainRules`, el servicio Application de M03.02 y el fence de autorización M03.03 sin modificarlos. Constructor exige actor/tenant capturados positivos y SHA fuente canónico de 40 hex minúsculas; congela el conjunto de claves aprobado con comparador ordinal y no comparte la colección mutable del llamador. La evaluación exige igualdad de actor/tenant observados con el contexto sintético, tenant declarado en intent, revisión, epoch y SHA, y deniega un catálogo de otra empresa antes de invocar Application. No hay ruta HTTP, DI real, EF, migraciones, MySQL, JWT vivo, CSS aplicado ni nuevo proveedor. No introduce tablas ni indices, no concede grants. El resultado positivo solo dice `AUTHENTICATED_THEME_ADAPTER_REQUIRED`.

**Archivos:** nueva clase Infrastructure, xUnit dirigidos, este Markdown y sección autocontenida en CHANGELOG_AI.md. No modifica Domain, Application, API, frontend ni dependencias. La fuente de verdad para autorización real permanece en backend autenticado y persistencia vigente; un snapshot de prueba jamás representa acceso concedido.

## A1 — prueba funcional real exigida en CI

15 `[Fact]` xUnit en `C0501DesignTokenIsolatedSnapshotAdapterTests`: ruta sintética positiva no autorizante; intención nula; actor/tenant discordantes; tenant B declarado; revisión y sesión obsoletas; SHA cambiado/ausente; RBAC/membresía revocados; JWT o permisos no actuales; catálogo ajeno/sin propietario; clave ordinal y catálogo obsoleto; mutación posterior del conjunto allowlist; CSS inseguro y petición de mutación; comparador case-insensitive; cancelación y constructor inválido. Son casos ejecutables; **no** se llaman PASS antes de observar GitHub Actions del SHA exacto.

## A2 — seguridad y tenant

Denegación por defecto sobre tenant A/B, actor, revisión, session epoch, SHA ordinal y catálogo; clonado inmutable de allowlist evita TOCTOU de colección externa. Resultado nunca concede tenant, RBAC, JWT, acceso a CSS ni persistencia. Revalidación de autorización real en producción depende de una implementación posterior con fuentes confiables, no de este adaptador sintético.

## A3 — integración proporcional

La composición Infrastructure → Application → Domain es ejecutable **in-process** con pruebas dirigidas. `NO_APLICA` MySQL 8.4/Category=Integration, EF, HTTP/JWT firmado, Angular/Playwright, proveedores externos, accesibilidad WCAG y métricas de producción porque ningún fichero de esas superficies se modifica ni se conecta. No equiparar mock o compilación con un servicio real.

## A4 — publicación y trazabilidad

Exigir commit fast-forward y CAS en `dev`, diff de una sola microtarea, blobs y SHA de source, Backend Release, backend build, seguridad multiempresa, autorización, `verify-solqaryn-project-scope` y procedencia V5 `completed/success` en SHA causal o descendiente equivalente. Un check `QUEUED/IN_PROGRESS/CANCELLED/SKIPPED/FAIL` nunca es PASS. Registrar CLAIM, VALIDANDO, LISTO únicamente con estados verdaderos, BITACORA idempotente A:Z, readback PLAN/COLA/DAG y liberar candado una vez tras readback. CI externa genuina permite VALIDANDO sin certificar LISTO.

## A5 y rollback

**A5_PENDIENTE** hasta revisión independiente por otra Task Javier distinta de DEV06, con lectura de código, requisitos, tests y evidencias. P0/P1 activos no se omiten. Rollback de este diff: `git revert` normal con HEAD verificado; no realiza rollback de datos porque no escribió datos. No QA, main, PROD, secretos, hosting, webhooks ni servicios de pago.
