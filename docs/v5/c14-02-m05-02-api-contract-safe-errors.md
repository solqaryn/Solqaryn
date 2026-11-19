# C14-02.M05.02 — contrato API/DTO y errores seguros (incremento V20)

**ID:** C14-02.M05.02 · **Riesgo R2:** autorización, BOLA/IDOR, host, archivos y jobs · **Repositorio:** solqaryn/Solqaryn@dev · **Owner DEV05:** 6adc2e55f47c81919f6374e561687076 · **Scope Lock:** STRICT.

## A0: alcance, impacto y límites

El STANDARD V5 exige ejecutar el contrato API/DTO con errores seguros para la aceptación de seguridad. Este incremento añade una **suite xUnit ejecutable contra el código Application real** `C1402SecurityAcceptanceAuthorizationGate` y un workflow .NET 10 dirigido. No crea controlador, endpoint HTTP, DTO público, DI de autorización real, acceso a MySQL, migración, JWT, grants, secretos ni llamadas a proveedores. No se presume que la API futura propuesta en C14-02.M02.02 esté implementada.

El cambio afecta exclusivamente pruebas, workflow, este documento y CHANGELOG. El gate Domain/Application subyacente permanece idéntico. Consumidores API/Angular y esquema de datos no cambian. Rollback: git revert normal del commit, sin force-push ni alterar historia institucional.

## A1-A3: pruebas adversariales pertinentes

Ocho métodos xUnit nuevos cubren: prevalidación positiva que **sigue sin conceder acceso**, petición nula, identificadores/traversal/longitud/formato/SHA/superficie inválidos sin lectura externa, override tenant/RBAC y restore, recurso extranjero y SHA obsoleto, membresía revocada/tenant B/RBAC/JWT inválidos, host no confiable, enlace de archivo vencido, job sin grants/idempotencia, lector fallido con código estable y cancelación. El oráculo exige códigos exactos y banderas de autorización falsas, no resultados textuales autoafirmados. La ejecución GitHub-hosted es la única fuente de PASS.

A2 prueba denegación en el preflight Application con snapshots sintéticos. **No acredita autenticación JWT firmada, middleware HTTP real, sesión revocada de MySQL ni aislamiento tenant A/B de extremo a extremo**. A3 xUnit real Domain→Application y error del lector; MySQL/EF/Playwright/proveedores son NO_APLICA para el **diff test-only**, pero el requisito de aceptación comercial integral permanece **NO_ACREDITADO**.

## A4-A5: gates institucionales

Publicar en dev por fast-forward y revalidar SHA de commit, `verify-solqaryn-project-scope`, V5/procedencia, backend Release, seguridad multiempresa, autorización y job dirigido. `QUEUED`, `IN_PROGRESS`, `CANCELLED`, `SKIPPED` y `FAILURE` no constituyen PASS requerido. La microtarea permanece VALIDANDO hasta completar A0–A4 para el alcance exigido y contar con pruebas pertinentes de HTTP/JWT/RBAC/tenant y cero P0/P1 sin mitigación. A5 debe efectuarla otra Task Javier independiente del autor y del reparador, mediante lectura del código, requisitos y logs. Nunca habilitar QA/main/PROD ni abrir dependencias por una suite parcial.

## V21 — Reparacion transversal de cobertura JWT HTTP y persistencia real (19 noviembre 2026)

Se agrega una prueba adversarial **nueva** en `P1010BMultiEmpresaHttpJwtCertificationTests`: altera la firma del JWT HS256 legitimo y comprueba que `/tenant-context/empresas`, empresa A y B devuelven **401** incluso con `X-Empresa-Id` falso; el control positivo comprueba 200 para A con firma legitima y 403 para empresa ajena. Se ejecuta mediante TestServer/ASP.NET Core .NET 10, middleware JWT autentico y EF InMemory aislado, no por mocks del gate C14.

El workflow dirigido existente ahora ejecuta las 8 pruebas originales de DTO, las 7 pruebas de JWT HTTP (incluida la nueva) y la suite ya implementada `C1402M0503MySqlThreatScopeIntegrationTests` sobre servicio **MySQL 8.4 efimero** mediante proveedor Oracle EF, con verificaciones TRX separadas de conteos, fallos y omitidas, ademas del Scope Lock y build Release. Todo resultado queda **PENDIENTE** hasta la corrida GitHub-hosted exact-SHA y lectura de logs.

Alcance: incremento autentico de test para BOLA/tenant/JWT/host spoof y MySQL de laboratorio, **no** cobertura exhaustiva de archivos/jobs/backup/restore, autorizacion de cada endpoint real, ni prueba de cero P0/P1 global. No se creo endpoint, ruta, secreto, migracion ni adaptador productivo y no se habilita acceso real. `PLAN_MAESTRO!H` permanece VALIDANDO; A0-A4 completos y A5 de revisor distinto del DEV05 autor y DEV06 reparador siguen pendientes. Rollback: revert normal de este changeset en dev, tras verificar HEAD y candado.
