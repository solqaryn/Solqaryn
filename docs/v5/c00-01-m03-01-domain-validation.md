# C00-01.M03.01 — Reglas de dominio y validación server-side

**Autor:** PRIMARY DEV 06 — JAVIER; TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Plan:** V5 FINAL `C00-01.M03.01`; ejecución únicamente `solqaryn/Solqaryn@dev`, protocolo V13 A0–A5, `PROJECT_SCOPE_LOCK=STRICT`.

## Alcance real

Se fortalece `OrdenCompra.EstablecerIdempotencia` — contrato de dominio existente, documentado en `C00-01.M02.05`. Previamente sólo se verificaba longitud 64, por lo que caracteres no hexadecimales podían entrar como huella "SHA-256"; además una huella idéntica en otra caja podía rechazarse por comparar texto sin normalizar antes de persistir. El contrato nuevo exige 64 caracteres ASCII hexadecimales, normaliza a minúsculas y permite repetir el mismo fingerprint válido, pero deniega sustituir clave o huella de una orden existente.

**Superficie:** `backend/src/Domain/Entities/OrdenCompra.cs`; `backend/tests/Solqaryn.Tests/V5C0001M0301OrdenCompraDomainValidationTests.cs`. No se alteran API, DTO, JWT, RBAC, EmpresaId, SQL, migraciones, datos, secretos, proveedores, recursos externos ni interfaz Angular. Se preserva el contrato actual de idempotencia y la validación de claves por el servicio de aplicación. Sin escritura o consulta sobre tenants reales.

## Aceptación y pruebas causales

- **Positiva:** huella SHA-256 hex válida en mayúsculas se almacena canónicamente y el replay de la misma clave y huella en minúsculas es aceptado.
- **Negativas:** huellas con caracteres `G`, `z` o dígitos Unicode, longitudes 0/63/65, y reuso de clave con huella distinta son rechazados sin alterar el estado; modificar la clave de una orden persistida tampoco está permitido.
- **A0:** revisar el diff exacto, secreto agregado = ninguno, nulabilidad, simplicidad, API pública y ausencia de dependencias nuevas.
- **A1–A2:** pruebas positivas y negativas de la guardia real de dominio con xUnit; no se sustituye autorización tenant por idempotencia.
- **A3:** compilar `backend/Solqaryn.sln` y ejecutar xUnit de la clase añadida en .NET 10, más regresiones del backend y pruebas de seguridad del runner GitHub Actions en SHA exacto. `dev-ci.yml` clasifica modificaciones a `backend/src` como alto impacto, por lo que exige comprobar efectivamente el job MySQL 8.4. CI genérica verde no sustituye ese job.
- **A4:** comprobar el commit fast-forward y HEAD exacto, jobs/steps/logs, diff, rollback y readback de `PLAN_MAESTRO`, `COLA` y `BITACORA`. `A5_PENDIENTE` se registra para otra Task; A5_PASS exige revisor independiente.

**Límite:** no declarar `.NET`, MySQL, runtime HTTP ni aislamiento A/B en PASS hasta contar con ejecución causal de GitHub Actions; los checks de SHA previo no son evidencia. Aunque el job MySQL de CI pase, no prueba una base productiva. **Rollback:** revertir exclusivamente el commit de esta microtarea con `git revert` no destructivo tras revalidar HEAD. Ninguna promoción QA/main/PROD está autorizada.
