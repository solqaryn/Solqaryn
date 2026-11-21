# C01-02.M05.03 — Integración real MySQL 8.4 del proveedor no autorizante — V22

**Unidad del PLAN D208:** `Ejecutar integración de persistencia o proveedor cuando corresponda`. SOLQARYN `dev` únicamente, Scope Lock STRICT, riesgo R2. Actor DEV05 (`6adc2e55f47c81919f6374e561687076`). No se declara completa toda M05/M06.

## A0. Alcance causal

Se reutiliza **sin modificar runtime** el proveedor `C0102ReadThroughCapabilityProvider`, cuya interfaz `IC0102ReadThroughCapabilityEvidenceSource` toma evidencia de una fuente inyectada. La producción **no** tiene aún vinculado el adapter de catálogo/plan/rol/capabilities a MySQL/JWT/RBAC. Se añade una prueba `Category=Integration` que implementa una fuente SQL de ensayo y ejercita el proveedor real, no simula la persistencia con un Dictionary o EF InMemory. No añade DI, tabla, endpoint, migración ni autorización a la aplicación real.

Archivo prueba: `backend/tests/Solqaryn.Tests/Integration/C0102M0503EphemeralMysqlCapabilityProviderTests.cs`; fuente de aislamiento en `backend/src/Infrastructure/AutomationV5/C0102ReadThroughCapabilityProvider.cs`. El check existente de migraciones Oracle EF/variantes y cargas de `.github/workflows/dev-ci.yml` ya proporciona `C1502_M0503_MYSQL_TEST` para loopback MySQL 8.4 efímero. El test verifica host `127.0.0.1`, puerto 3306, root sólo CI, `solqaryn_c1502_m0503`, `SslMode=Disabled` y crea un esquema aleatorio propio `c0102_m0503_<GUID>`; lo destruye al terminar.

## A1/A2/A3. Pruebas MySQL físicas con denegación

Se crean dos filas SQL de recursos correspondientes a tenant A (101) y B (202); se reconsulta físicamente el proveedor en cada lectura, y se rechazan interconsultas A→B, B→A y recursos inexistentes. Se revoca SQL a A y se demuestra que la lectura siguiente falla **sin usar ALLOW cacheado**; B continúa aislada. Se reintroduce A con owner B, se exige denegación; restauración comprobada y DELETE posterior deben cambiar el resultado en tiempo real. Tras volver de la fuente física, el proveedor *intencionalmente* degrada `OwnershipReadFromTrustedPersistence`, `CurrentCapabilityCatalogReadFromBackend`, `ExplicitRelationalRbacGrant`, `PlanCurrent` y `RubroCurrent`: evidencia SQL de prueba NO equivale a grants reales, no concede recurso, exportación o activación.

Esto es **integración real de la fuente SQL de ensayo con el proveedor concreto y límites del scope D208**, no certificación de Oracle EF Core productivo, de membresía JWT backend, del catálogo vivo comercial o de SQL Aiven. La prueba exige que el runner ejecute `Category=Integration` y que xUnit finalice sin fallos; un job `SUCCESS` que saltó este caso no acredita A3. El job de migraciones puede tener FAIL ajenos de entorno; nunca se califican `PASS` ni se ocultan. Un fallo de CI requiere lectura del log y reparación específica.

## A4, A5 y reversión

Comprobar en GitHub Actions el SHA exacto, los filtros y el test real, además del backend .NET 10 Release, Scope Lock y V5 provenance. Registrar evidence por ID en PLAN/COLA/BITACORA; luego `VALIDANDO->LISTO` sólo con calidad A0-A4 proporcional. A5 independiente por otra Task Javier distinta del autor/reparador. Rollback: revert normal del commit de prueba en `dev`, nunca force-push ni cleanup externo, conservar histórico institucional. No QA/main/PROD, proveedores de pago, secretos ni base real.
