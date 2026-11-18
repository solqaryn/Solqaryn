# C02-03.M03.01 — Reglas de dominio de ciclo de vida tenant (sin autorización ni mutación)

**Fecha:** 2026-11-18 · **Proyecto:** SOLQARYN dev · **Scope Lock:** STRICT · **Clasificación:** R2 (tenancy, JWT/RBAC y concurrencia), pero el diff es una regla Domain pura de precondiciones. **Owner:** DEV03.

## A0 — Cambio implementado y sus interfaces

Nueva clase `V5TenantLifecycleDomainRules` en Domain, sin dependencias a Infrastructure, API ni proveedores. Contiene siete estados del contrato M01 (`Requested`, `Provisioning`, `Active`, `Suspended`, `Archived`, `LogicalDeleted`, `RepairableFailure`) y ocho comandos, todos evaluados contra una fotografía confiable obtenida por backend y una solicitud sin capacidad de autorizar. El retorno afirmativo **`VERIFIED_PRECONDITIONS_REQUIRE_APPLICATION_CAS`** exige verificar y ejecutar CAS, idempotencia persistente, auditoría, revocación y controles de permisos/tenant de nuevo en Application/API antes de cualquier cambio real. Todos los veredictos declaran `AuthorizesTenantMutation=false`, `WritesToDatabase=false`, `AuthorizesGlobalPermission=false`, `AuthorizesPhysicalDeletion=false` y `AuthorizesProductionCutover=false`.

No modifica `Empresa.Activa`, `Empresa.Eliminado` NotMapped, UsuarioEmpresa, JWT, RBAC, migraciones, SQL/EF, HTTP, workers, permisos, configuración real, secrets, RRULE, QA/main/PROD. No se emiten señales ni se crea una nueva máquina de estado durable. La configuración está **apagada por defecto** si no hay prueba de flags confiables y permisos de plataforma explicitados. No basta un admin de tenant para cambiar empresas.

## A1 y A2 — Pruebas adversariales reales de código Domain

La suite `V5TenantLifecycleDomainRulesTests` ejecuta xUnit net10 GitHub-hosted con estados y transiciones permitidas para revisar únicamente **precondiciones**; deniega operador no global, JWT no verificado/revocado, inyección de tenant B, ownership sin verificar, revisión obsoleta, bandera apagada, bypass de auditoría, DSN/host cliente, borrado físico, cutover, clave o huella inválida, estado desconocido, transiciones ilegales, falta de revocación, retención, restore y checkpoint. Diferencia replay idéntico de colisión por payload/empresa/actor y de recuperación incompleta: ninguna variante muta ni escribe. Casos positivos no habilitan conexiones ni producen respuesta HTTP positiva. La prueba es falsable: si el guard devuelve autorización directa o permite un bypass, los asserts fallan.

**Garantías no demostradas:** no se invoca API con JWT firmado, no se ejercita autorización/filtrado real en MySQL, no se prueba CAS transaccional concurrente, revocación efectiva de tokens, baja lógica real ni recuperación de infraestructura. Estos gates corresponden a M03.02–06/M05 y permanecen abiertos; clasificar un `bool` de fotografía como confianza probada no sería correcto.

## A3 — Integración aplicable

La clase y pruebas sólo usan memoria/código Domain y xUnit. No tocan EF, MySQL, migraciones, Angular/Chromium ni servicios; esas integraciones son **NO_APLICA a este diff** y no se atribuyen como PASS. El futuro Application/Infrastructure debe comprobar snapshots sobre autoridad y persistencia reales y evaluar A/B, concurrencia, rollback y recuperabilidad.

## A4 — CI y trazabilidad exigibles

Nuevo workflow `.github/workflows/v20-c02-03-m03-01-lifecycle-domain.yml`, con .NET 10 GitHub-hosted, build Release y pruebas xUnit dirigidas sobre SHA exacto; adicionalmente Scope Lock, V5/procedencia, backend Release, seguridad/authorization pertinentes. `QUEUED`/`IN_PROGRESS`/`FAILED`/`SKIPPED` no se cuentan PASS. Al publicar pasar a VALIDANDO con SHA y run; sólo cerrar LISTO_DEV con A0–A4 causales completados y CAS V17/readback, conservando owner. `A5_PENDIENTE` corresponde a otra Task Javier independiente.

## Dependencias y rollback

Preflight: `C02-03.M02.01–06+A5`, `C02-02.M06.06+A5`, PLAN H374 PENDIENTE sin owner, COLA G/O=SI, DAG F=VALIDADO/G=SI. No se forzaron formulas ni se produjo infraestructura. Dependientes C02-03.M03.02–06 requieren gates actuales de PLAN/COLA/DAG, no basta que esta clase compile. Reversión de este incremento: `git revert` normal del commit fast-forward después de verificar HEAD, sin destruir datos ni borrar BITACORA. No es rollback de DB. Sin autorización R3, QA/main/PROD.
