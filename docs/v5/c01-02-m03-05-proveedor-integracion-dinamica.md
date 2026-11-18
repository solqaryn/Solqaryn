# SOLQARYN V19 — C01-02.M03.05: Integración de proveedor de capabilities dinámica

Microtarea C01-02.M03.05, desarrollador DEV06 Javier TASK_ID 6af16bbad26881918c283abe06e1f32a, repositorio solqaryn/Solqaryn branch dev. Riesgo R2 (autorización/tenant). V17 lock 2000000198. Predecesora C01-02.M03.04 A5 correctiva independiente registrada sin borrar A5_FAIL histórico.

## A0 / Arquitectura

Implementa IC0102ReadThroughCapabilityEvidenceSource y C0102ReadThroughCapabilityProvider en Infrastructure, enrutados al contrato IC0102VerifiedCapabilityPolicyReader existente de Application, sin dependencias inversas. Es integración de lectura en proceso por solicitud: no hay worker, queue ni despacho asíncrono porque la autorización requiere revalidación actual a cada acceso. No se registra DI con fuentes de datos reales; no se altera API, JWT, MySQL/EF, roles, tenencia, frontend, secretos ni operaciones productivas. Un enlace live requerirá microtarea y pruebas adicionales.

El adaptador reconsulta el proveedor en CADA invocación, rechaza tenant/recurso/actor/rol/sucursal no concordantes, membresía inactiva, rol revocado, cancelación y errores opacos. Copia las cinco colecciones a FrozenSet ordinal independiente, sin HashSet mutable escapado. Nunca confía en banderas de autoridad suministradas por el proveedor: OwnershipReadFromTrustedPersistence, CurrentCapabilityCatalogReadFromBackend, ExplicitRelationalRbacGrant, PlanCurrent y RubroCurrent pasan a false. No genera permiso, efecto ni ALLOW en caché. Se conserva fail-closed.

## A1 / A2

Nueve casos xUnit: relectura dinámica sin ALLOW cacheado; rechazo A/B y recurso ajeno, actor/rol; revocación y sucursal; capturas FrozenSet resistentes a mutación; comparación ordinal; errores opacos; cancelación; ausencia de proveedor o IDs; colecciones faltantes. Pruebas sintéticas, no se afirma autenticación real ni grants de producción.

## A3 / A4

Tecnología tocada: código de Infrastructure .NET 10 y unit tests xUnit. Requeridos: build Release, xUnit pertinentes, SOLQARYN Seguridad multiempresa, verify-solqaryn-project-scope y V5 por SHA exacto, sin equiparar SKIPPED a PASS. No aplica MySQL 8.4, Chromium ni integraciones externas porque no se abrió conexión DB/API/browser ni se modificó la implementación live. Si el CI sigue ejecutándose, mantener VALIDANDO y propietario DEV06; cerrar LISTO solo con evidencia verdadera y readback Sheets/bitácora. A5 independiente corresponde a otra Task Javier.

## Rollback

Revert convencional de commit sobre dev con HEAD verificado, sin force push, sin tocar main/QA/PROD y sin borrar BITACORA.
