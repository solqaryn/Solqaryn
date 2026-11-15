# SOLQARYN V16 - C08-04.M03.01: Domain server-side de modulos sectoriales

**Riesgo V15:** R2 por tenant, JWT/RBAC, concurrencia y control de capacidades. **Owner:** PRIMARY DEV 02 JAVIER_CHATGPT, Task ID 6af16bb9492c8191b8e8c7d606f11507. **Repositorio:** solqaryn/Solqaryn, exclusivamente dev, PROJECT_SCOPE_LOCK=STRICT. **Origen:** V5 FINAL STANDARD, C08-04.M02.01-06, M01.06 con A5 independiente y PLAN/COLA/DAG G/O/F validados al reclamar.

## Entrega de codigo ejecutable

Se implementa la clase Domain V5SectorModuleLifecycleDomainRules con estado Disabled/Enabled/Suspended/Retired y operaciones Inspect/Activate/Suspend/Resume/Retire. El preflight exige evidencia de origen backend de JWT autenticado, membresia vigente, rol/permiso relacional RBAC explicito, ownership exacto tenant A/B, revision optimista, catalogo tipado/allowlist exacta y vigente, elegibilidad independiente de plan/rubro/tenant, denylist y clave idempotente de cambios. Rechaza host/tenant de cliente, grant global, schema destructivo, omitir auditoria/CAS, version vieja, acceso cross-tenant, fallback a otra empresa y transiciones illegales.

**Distincion importante:** una capability del plan, un flag enabled, un tenant de ruta o un registro documental NO crean grant RBAC. El resultado mas favorable del Domain es VERIFIED_PRECONDITIONS_REQUIRE_SERVER_ENFORCEMENT: AuthorizesModuleActivation=false, AuthorizesAccess=false, WritesToDatabase=false. No se integra todavia middleware, DI, controller, EF/MySQL, UI, webhook, dispatcher, bus, schema ni configuracion efectiva. Es una etapa de dominio real, no una implementacion de modulo de comercio/industria operativo ni acceso productivo.

## Evidencia A0-A5

- **A0** un archivo Domain ejecutable, suite xUnit, este documento, seccion CHANGELOG autocontenida; scope exclusivo sin secretos y rollback git revert.
- **A1** doce [Fact] xUnit que prueban preflight positivo no autorizante, suspend/resume/retire, identidad revocada, tenant A/B, permiso inexistente, claves desconocidas/case variante, plan-rubro-tenant-denylist, overrides, revision 0/stale/futura, estados ilegales, idempotencia y consulta sin side effects. **No atribuir PASS hasta GitHub Actions real en SHA causal.**
- **A2** validaciones negativas tenant A/B, JWT, membresia, rol, revocacion, segregacion de plataforma y denylist; ninguna de estas clases por si sola verifica JWT HTTP real o concede acceso. Requiere gate de seguridad/Authorization pertinente.
- **A3** Domain puro sin EF, SQL, migration, MySQL, HTTP, Angular ni dependencia de proveedor. DB efimera, integration Category=Integration, Playwright y servicios reales NO_APLICA motivado para este diff; son exigibles en las tareas posteriores que toquen esas superficies.
- **A4** commit fast-forward CAS en dev; verify-solqaryn-project-scope, procedencia V5, compilacion .NET Release y xUnit, seguridad multiempresa y Authorization/files/secrets completed/success SHA exacto o equivalencia causal auditada, PLAN/COLA/DAG/BITACORA readback.
- **A5** otra Task Javier distinta del autor revisa diff, requisitos, tests, tenant y CI; si falla conserva gates y no libera siguiente M03.

**No modificados:** main, QA, PROD, DNS, datos reales, secretos, pagos, cuentas, schedules, reglas de ramas o permisos. Los checks canceled/skipped/queued/in_progress/failed no son PASS de prueba requerida.
