# SOLQARYN V5 — C06-04.M03.01: reglas reales Domain para portal de cliente

**Autor:** PRIMARY DEV 05 — JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Destino:** `solqaryn/Solqaryn@dev`, Scope Lock STRICT. **Fecha:** 2026-11-15. **Clase:** implementación de validadores de dominio + pruebas xUnit, no activación de un portal API.

## A0 — Entrada, arquitectura y exclusiones

PLAN_MAESTRO C06-04.M03.01 estaba PENDIENTE sin propietario; COLA G/O=SI y DAG F=VALIDADO/G=SI sólo después del A5 independiente de C06-04.M02.06. Claim exclusivo y readback documentados en BITACORA. Fuente de diseño M02.01–06 con JWT CUSTOMER separado de ERP, tenant A/B y cliente A1/A2; Domain no debe fabricar grants y Application/API conservan comprobaciones reales de identidad y resource ownership.

Se incorporan `backend/src/Domain/Security/C0604PortalClienteDomainRules.cs` y `backend/tests/Solqaryn.Tests/Domain/C0604PortalClienteDomainRulesTests.cs`, más este documento y CHANGELOG. Se emplean entidades reales `Cliente`, `TiendaCuentaCliente` y `TiendaSesionCliente` sin cambiar su esquema. No se incorporan migraciones, endpoints, flags, eventos, jobs, recuperación, documentos ni proveedores externos; no se escribe MySQL ni se cambia producción.

## A1/A2 — Invariantes reales y ensayos ejecutables

`ExigirSesionYPropiedad` coteja empresa identificada desde backend, cuenta identificada desde backend, `Cliente.EmpresaId` persistido, relación de cuenta con cliente, actividad, propiedad de sesión, token hash presente, revocación y expiración. Deniega registros legacy sin propietario. `ExigirRecursoPrivado` coteja empresa y cliente verificados contra el dueño del recurso, rechazando tenant B o cliente A2 aunque pertenezca a A. `ExigirVersionEsperada` rechaza versiones obsoletas antes de intentar mutación; el CAS atómico real debe materializarlo Infrastructure.

Se escriben xUnit dirigidas con casos positivos/negativos para A1, tenant B, cuentas cruzadas, legacy null, cliente/cuenta desactivada, sesión revocada/expirada/ajena, recurso ajeno y revisión de versión. Las pruebas no constituyen PASS hasta GitHub Actions. Esta clase es un guard puro, no login, no JWT ni verificación criptográfica del hash: **los ID de empresa/cuenta del lado solicitante deben provenir únicamente de autenticación y membresía servidor y una relectura física**, nunca de body, header, query, URL ni datos del cliente. Denegar mediante estas reglas tampoco prueba la efectividad del RBAC en HTTP.

## A3/A4 — Certificación causal

El alcance real es Domain + tests xUnit sin interacción EF/MySQL, HTTP ni Angular. Requiere compilación/pruebas backend y regresiones de seguridad en GitHub Actions sobre el commit, además de Scope Lock y procedencia V5 SUCCESS SHA exacto. Pruebas MySQL, Playwright y restore de otros scopes no sustituyen xUnit. Revisar HEAD, diff de cuatro archivos, compare/fast-forward, PLAN, COLA, DAG, BITACORA y no usar checks pending/cancelled/skipped como PASS. No prometer integraciones no implementadas.

## A5 y rollback

Solo otra Task Javier distinta de DEV05 puede emitir A5; el LISTO_DEV no concede QA/main/PROD. Si CI obligatoria está pendiente, el owner conserva VALIDANDO, y cualquier reparación posterior debe preservar HEAD y usar CAS; git revert normal como rollback documental/código, sin borrado de BITACORA histórica.
