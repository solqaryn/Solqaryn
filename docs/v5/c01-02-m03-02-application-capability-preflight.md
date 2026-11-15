# SOLQARYN V16 — C01-02.M03.02: preflight Application de capabilities y permisos por tenant

**Unidad:** C01-02.M03.02 (PLAN fila 195). **Autor:** PRIMARY DEV 02 JAVIER / TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Repositorio:** solqaryn/Solqaryn@dev con PROJECT_SCOPE_LOCK=STRICT, V15 riesgo **R2**. **Fuente:** Standard de Implementación de Automatizaciones V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), DAG dinámico y Domain `C0102CapabilityDomainRules.cs` de la anterior M03.01.

## Alcance real

Se agrega `backend/src/Application/AutomationV5/C0102CapabilityApplicationPreflight.cs` y `backend/tests/Solqaryn.Tests/Application/C0102CapabilityApplicationPreflightTests.cs`. La capa Application **NO** registra endpoint/DI, consulta DB, autentica JWT, autoriza lectura de recursos, ejecuta workers, crea grants, actualiza plan o asigna rol; solo valida **intención** de consulta y la intersección Domain **con política ya verificada por backend**.

La Domain M03.01 `C0102CapabilityDomainRules.Evaluate` comprueba actor/rol efectivo, propietario de recurso, tenant, sucursal, RBAC relacional explícito, plan, rubro, allowlist/capabilities y denylist. Application agrega fronteras de consistencia de tenant/sucursal solicitados, rechaza intentos de grant/policy en cliente, escritura/export/activación y delega validación Domain sin convertir resultado nominal en autorización. Nunca utilizar el ID de empresa del cliente como fuente de `ContextoTenantActual`.

## Contrato y estados fail-closed

- `ClientIntent(RequestedTenantEmpresaId, RequestedBranchId, CapabilityKey, IncludesClientGrantOrPolicy, RequestsWriteExportOrActivation)`: solicitud de lectura lógica, **no autoridad**.
- `ReviewOnly(ContextoTenantActual?, VerifiedPolicy?, ClientIntent?)` devuelve `ReviewResult(Code, RequiresLiveBackendAuthorization)`. Las cuatro capacidades `GrantsAccess`, `ReadsOrWritesResource`, `CreatesCapabilityGrant`, `MayOverrideTenant` son **siempre false**.
- Ausencia de intent o contexto, tenant B, branch distinto/nulo indebido, policy/grant inyectada, export o modificación solicitan **denegación** antes de Domain. Ninguna respuesta nominal elude revocación/roles/ownership/plan/rubro y autorización backend en tiempo real.
- No se confunde entitlement comercial con autorización relacional, ni un snapshot `VerifiedPolicy` estático con sesión JWT validada. La futura M03.03+ deberá cargar grants persistidos y revocación desde fuente autorizada, auditar, probar HTTP 401/403 y diferenciar A/B en CI.

## Calidad V15, pruebas y limitaciones

**A0:** archivos nuevos Application, xUnit, documento y sección autocontenida de CHANGELOG, sin tocar Domain preexistente, API, EF/MySQL, frontend/Angular, IAM runtime, secretos ni configuración CI. **A1:** 12 métodos xUnit: caso positivo no autorizante y negativos de null intent/context/policy, tenant simulado, branch arbitrario, role/owner cruzado, revocación, RBAC ausente, plan vencido, denylist y capability no canónica. El conteo escrito NO es ejecución hasta que GitHub Actions confirme xUnit Release por SHA causal. **A2:** seguridad multiempresa A/B, JWT/RBAC/tenancy, privilegios y revocación siguen fail-closed: la entrega no concede acceso API. **A3:** pruebas EF/MySQL, HTTP, proveedores y Playwright NO_APLICA por preflight sin integración; sí se requiere CI de backend, Release, seguridad y autorización para este código Application. **A4:** exigir publicación fast-forward/CAS exact-HEAD dev, `verify-solqaryn-project-scope`, V5 provenance, backend Release/xUnit y seguridad/autorización `completed/success` por SHA o equivalencia de blobs demostrada y readback PLAN/COLA/DAG/BITACORA. CANCELLED/SKIPPED/IN_PROGRESS nunca se contabilizan como PASS de job requerido. **A5:** revisión de Task Javier distinta del autor, evidencia de diff, pruebas y seguridad, nunca autoaprobación.

**Rollback:** git revert normal del commit (sin force-push, alteración de datos, secrets o QA/main/PROD); ningún dato o tabla que restaurar. La integración real API, JWT, auditoría y revocación requiere etapa futura independiente autorizada y pruebas de MySQL efímero cuando se añadan adaptadores.
