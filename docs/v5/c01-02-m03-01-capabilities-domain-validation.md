# SOLQARYN V5 — C01-02.M03.01 reglas Domain de capabilities y pertenencia

**ID:** C01-02.M03.01 (PLAN fila 194, DAG calculado, COLA G/O). **Autor:** PRIMARY DEV 04, Task `6aeacb7820ac8191b7d768a6f33dfa09`. Único repositorio `solqaryn/Solqaryn@dev`, Scope Lock STRICT.

## A0 — Alcance y dependencias

Se implementa `C0102CapabilityDomainRules` en Domain como política pura **fail-closed** y 13 pruebas xUnit sobre la intersección de plan, rubro, rol empresarial efectivo, tenant, sucursal, recurso y capacidad. Se reutiliza `ContextoTenantActual` construido a partir de membresía, no `Usuario.RolId` legacy. Se respetan los contratos documentales C01-02.M02.01–06 (modelo, API futuros, persistencia futura y contrato de salida) que exigen que un entitlement comercial **no conceda** un grant RBAC. El cambio no altera API, Application, DI, EF/MySQL, autorización JWT, datos, catálogo real de permisos ni planes efectivos.

## A1 — Validación positiva y negativa

El código comprueba el dueño empresarial del recurso, actor y rol del contexto, membresía y rol activos, bandera de revocación, sucursal propia, grant RBAC relacional explícito, vigencia de plan/rubro, intersección **ordinal** de listas autorizadas de plan, rubro, rol y empresa, y denylist con precedencia. Nulo, rol incorrecto, cliente de empresa B, sucursal ajena, entitlement comercial sin RBAC, matrices ausentes, plan expirado y capability no canónica devuelven denegación segura. Un resultado nominal devuelve `VERIFIED_PRECONDITIONS_ONLY_NO_RUNTIME_AUTHORIZATION` con todas las capacidades de efecto en `false`. Las políticas pasadas por parámetro **deben provenir de backend verificado**; ningún DTO, plan o rol enviado por el cliente es autoridad.

Se incluyen 13 tests xUnit, que únicamente se consideran ejecutados cuando GitHub Actions .NET 10 Release los aprueba en SHA causal.

## A2/A3 — Límites de seguridad e integración

La capa Domain no autentica JWT, no consulta grants persistidos, no asegura revocación en tiempo real, no aplica filtros EF ni concede acceso a recursos. Application y API deben recuperar y revalidar políticas versionadas, memberships, RBAC, propietario, sucursal y permisos usando infraestructura real antes de efectos. Sin migraciones, tablas, endpoints, React/Angular ni servicios externos; MySQL Category=Integration, HTTP/JWT/Playwright son `NO_APLICA` a este changeset, NO son PASS de runtime. Las pruebas de Domain negativas y CI de seguridad tenant ayudan a detectar regresiones pero no certifican aislamiento completo de API.

## A4/A5 — Gate y rollback

Para pasar VALIDANDO a LISTO_DEV se exige commit individual fast-forward/CAS `dev`, diff/blobs y checks GitHub Actions: xUnit+Backend Release, seguridad multiempresa/autorización, `verify-solqaryn-project-scope` y V5 procedencia `completed/success` en SHA exacto o equivalencia auditada. Una CI `queued/in_progress/cancelled/failed/skipped` nunca es PASS. A5 pertenece a otra de las cinco Tasks Javier, distinta de DEV04. Rollback mediante `git revert` normal con HEAD revalidado y eventos BITACORA aditivos, no reset/force-push, QA/main/PROD, cambios de secretos o base de datos.
