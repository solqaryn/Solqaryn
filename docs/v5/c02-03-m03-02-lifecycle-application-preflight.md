# C02-03.M03.02 — Application tenant lifecycle bounded preflight

**Fecha:** 2026-11-18. **Repositorio:** solqaryn/Solqaryn. **Rama:** dev. **PROJECT_SCOPE_LOCK:** STRICT. **Riesgo:** R2 (JWT/RBAC, tenant, idempotencia). **Autor:** Task DEV05.

## A0 — Alcance real y límites

La microtarea implementa C0203TenantLifecycleApplicationService en la capa Application y su interfaz IC0203LifecycleEvidenceReader. El contrato admite exclusivamente intención (identificador del tenant, comando, estado y versión esperados, clave y huella). **Nunca admite que el cliente declare un actor de plataforma autenticado, JWT válido, pertenencia, autoridad o recibo durable**. Solicita evidencia a un lector *server-controlled*, con sesión y membresía actuales y búsqueda de recibo con tenant fence. No se suministra ni se registra ningún adaptador vivo en Dependency Injection: estas garantías son una interfaz que deberá cumplir el futuro componente Infrastructure, no pruebas de HTTP o MySQL.

El nuevo servicio utiliza V5TenantLifecycleDomainRules del C02-03.M03.01 (LISTO+A5 independiente). Aun si Domain devuelve VERIFIED_PRECONDITIONS_REQUIRE_APPLICATION_CAS, Application responde DURABLE_LIFECYCLE_CAS_ADAPTER_REQUIRED, nunca escribe ni concede autoridad. Para un recibo de repetición legítimo exige durable readback; si falta checkpoint, deniega nueva escritura. Para actor/tenant/huella que no coincida, devuelve conflicto. No cambia Empresa.Activa ni ejecuta migraciones, CAS SQL, restauración, grants, despachos, JWT HTTP ni producción. A5 de C02-02.M06.06 acreditada por DEV05 y A5 previa de C02-03.M03.01 completada antes del CLAIM, con PLAN/DAG/COLA revalidados.

## A1 y A2 — Pruebas

Pruebas xUnit adversariales GitHub-hosted verifican: intención positiva no autorizante; entrada nula, ID inválido y operación desconocida; bypass de autoridad desde cliente, DSN o cutover; ausencia de fuente, sesión desactualizada, falta de tenant fence y empresa B; JWT no verificado o revocado; snapshot obsoleto; huella inválida; replay persistente completado tras pasar de Active@7 a Suspended@8; replay incompleto con recuperación obligatoria; colisión por tenant; excepciones saneadas y cancelación. La respuesta nunca expone secretos ni devuelve AuthorizesTenantMutation o WritesToDatabase positivo. Estos tests in-process **no acreditan** JWT firmado real, RBAC con base de datos, servicio HTTP ni CAS transaccional real.

## A3 — Integración y seguridad

El alcance sólo incorpora Application, xUnit, CI y documentación. No se modifican DbContext, EF, migraciones, API, frontend, secretos o proveedores. MySQL, Playwright y HTTP son NO_APLICA **a este diff**: no se contabilizan como PASS. La futura infraestructura deberá validar sesión/JWT real, rol global, autorización tenant A/B, recibos durables, concurrent CAS y recuperación, todos con aislamiento verdadero antes de habilitar mutaciones. Riesgo productivo R3 permanece NO_GO.

## A4, A5 y rollback

Workflow v20-c02-03-m03-02-lifecycle-application.yml ejecuta dotnet restore, build net10 Release, pruebas xUnit dirigidas y verify-project-scope con GitHub-hosted; comprobar además backend Release, autorización, seguridad multiempresa, Scope Lock y V5 sobre SHA exacto. Un check QUEUED/IN_PROGRESS/CANCELLED/SKIPPED/FAILED no es PASS. Cuando el código esté completo y publicado mediante fast-forward CAS, mantener VALIDANDO con SHA/RUN si CI externa no concluye. Sólo pasar LISTO con A0–A4 causales y readback bajo V17; A5 independiente de otra Task Javier posterior. Sin QA/main/PROD.

Rollback: git revert normal del commit de esta unidad previa verificación de HEAD y otros escritores; no borrar BITACORA ni forzar fórmulas DAG.
