# SOLQARYN V17 / V15 — C01-02.M03.03 capability authorization fence

**Proyecto:** solqaryn/Solqaryn en dev, PROJECT_SCOPE_LOCK=STRICT. **Unidad:** C01-02.M03.03 (PLAN fila 196). **Autor:** PRIMARY DEV 05 JAVIER (6adc2e55f47c81919f6374e561687076). **Riesgo:** R2, control tenant/RBAC. **Fuente:** Standard de Implementación de Automatizaciones, PLAN_MAESTRO/COLA/DAG_TECNICO; precedentes C01-02.M03.01–02 y C01-01.M06.06.

## Implementación y alcance real

`C0102CapabilityAuthorizationFence` es código .NET 10 Application, no mera documentación. Reutiliza `IdentidadUniversalAuthorizationGuard`, que reconsulta membresía, rol y permiso relacional a través de `IIdentidadUniversalSnapshotSource` del backend, antes de pedir catálogo y propiedad del recurso por `IC0102VerifiedCapabilityPolicyReader`. Esta interfaz de catálogo requiere implementación segura posterior en Infrastructure; **NO se registra DI de API ni endpoint nuevo**, y ninguna fuente se construye desde headers, query, DTO o claims de empresa del cliente.

Rechaza pre-I/O IDs inválidos, tenant B, sucursal no positiva, capabilities no canónicas, pretensiones de grants/policy o acciones de escritura/export/activación. La identidad actual se coteja con la política backend de tenant, actor, rol efectivo, recurso, sucursal, plan, rubro y allow/deny list, y reutiliza la intersección Domain `C0102CapabilityDomainRules.Evaluate`.

Un preflight nominal devuelve `AUTHENTICATED_CAPABILITY_RESOURCE_ADAPTER_REQUIRED`, **no** permiso efectivo. Todas las propiedades `Grants*`, escritura, exportación y override son falsas. El punto final de recurso debe revalidar autorización y ownership en el momento de efecto. No hay lectura de datos de negocio, EF nuevo, migración, API HTTP, grant JWT, activación de plan, transacción, worker ni acceso a MySQL desde este changeset.

## Matriz comprobable A0–A4

- **A0:** nuevos archivos Application, 12 tests xUnit, este documento y sección CHANGELOG autocontenida; sin cambios de Domain preexistente, API, control de acceso HTTP, EF, archivos/finanzas, frontend ni secretos.
- **A1:** asserts positivos de avance a adaptador autenticado pero sin acceso; negativos de null, identidad falsa, tenant A/B, recurso/actor alterados, sucursal, clave/case, roles revocados, RBAC, denylist, planes, evidencia confiable, fuente caída y cancelación. Los tests se cuentan PASS solo si GitHub Actions ejecuta la suite y registra 0 fallidos.
- **A2:** seguridad deny-by-default: un grant comercial no sustituye permiso relacional; se reconsulta membresía y rol; ningún actor puede elegir tenant o recurso vía cliente; prueba de A/B y revocación exigida en backend/security/authorization CI exact-SHA.
- **A3:** composición Application con el guard real existente y readers in-memory de prueba. **NO_APLICA** MySQL Category=Integration, HTTP, Angular y Playwright a un diff sin EF/endpoint/UI. Esto NO certifica que exista backend policy reader productivo o ejecución JWT HTTP.
- **A4:** commit único fast-forward/CAS, SHA y blobs exactos, Scope Lock y procedencia V5, Backend Release/xUnit, seguridad multiempresa y Authorization SUCCESS para SHA publicado (o descendiente con equivalencia probada); readback PLAN/COLA/DAG/BITACORA. Cualquier check requerido QUEUED/IN_PROGRESS/FAIL/CANCELLED deja VALIDANDO. A5 por otra Task Javier distinta, con revisión independiente real.

**Límites y rollback:** no se certifica autenticación API/HTTP final, ACL de base de datos ni permisos de producción. El adaptador persistente que construye `VerifiedResourcePolicy` y la autorización del endpoint se quedan como integración futura sujeta a sus propios gates. Reversión con git revert normal del único commit, sin force-push, datos productivos ni modificar BITACORA histórica.
