# SOLQARYN V16 — C02-02.M03.02 Application preflight shared/dedicated

**Riesgo:** R2 tenancy, RBAC, placement, posible migración futura y concurrencia. **Owner/autor:** PRIMARY DEV 06 Javier, Task 6af16bbad26881918c283abe06e1f32a. **Repo:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT. **Plan:** V5 FINAL STANDARD de Implementación, fila PLAN 339 y gates COLA/DAG.

## A0 Objetivo y alcance

Implementar el caso de uso de prevalidación Application M03.02 encima de las reglas Domain C02-02.M03.01 existentes. El lector IC0202PlacementEvidenceReader representa una obligación para un adaptador autenticado futuro: debe obtener estado JWT/membresía, roles relacionales, ownership de asignación, alias de binding de catálogo confiable, revisión CAS y evidencias reales de backup/restore y consistencia. Esta tarea NO implementa dicho lector contra DB ni agrega conexión, router, EF/MySQL, esquema, migración, backup, API, secret, proveedor, DNS o cutover real.

## A1 Pruebas ejecutables

Doce pruebas xUnit con asserts positivos, negativos, límites y errores para inspección segura, propuesta dedicated no autorizante, SHA/versión/enum, DSN cliente/dual write/DDL, tenant B y ownership, evidencia antigua/nula, JWT/revocación/roles, lectura no confiable/fence, binding de catálogo, ausencia de restore/verificación de datos, CAS/idempotencia y cancelación. No declarar PASS hasta ejecución GitHub Actions del SHA.

## A2 Seguridad R2

Tenant autenticado y dueño persistido coinciden exactamente. No se acepta DSN del cliente ni alias no confirmado por el servidor. Se exige JWT y rol actuales, revisión, catalogo y guard de restauración declarados desde un lector BACKEND (que esta unidad NO implementa). Cada resultado favorable solicita AUTHENTICATED_STORAGE_ADAPTER_REQUIRED y conserva AuthorizesConnection/AuthorizesMigration/AuthorizesCutover/AuthorizesProduction=false. No se realiza ninguna operación que pueda mezclar tenants, datos ni credenciales.

## A3 Superficies sin cambios

Se reutiliza V5SharedDedicatedPlacementDomainRules, sin tocar Domain, EF, SQL, MySQL 8.4, API, Angular, Playwright o acceso externo. Integration Category=Integration/restore MySQL y pruebas HTTP son NO_APLICA a este diff exclusivamente Application+unit test, NO equivalen a ejecución ni certificación de la migración. Cuando se implemente el adaptador Infrastructure/DB será obligatorio CI efímera MySQL 8.4 y rollback real.

## A4 Evidencia y reversibilidad

Commit único en dev FF/CAS, Scope Lock, V5 CHANGELOG y pruebas backend Release, seguridad multiempresa, Authorization en SHA exacto o descendiente con blobs inalterados y dependencia equivalente. Si faltan, mantener VALIDANDO/owner y pasar otro scope independiente. Readback PLAN/COLA/DAG/BITACORA. Rollback por git revert normal del changeset, nunca force push, no QA/main/PROD ni cambios productivos.

## A5

Revisión independiente por otra Task Javier distinta del autor, con posibilidad de A5_FAIL. LISTO_DEV no autoriza migración, QA ni producción.
