# C00-01.M03.02 - Contrato de aplicacion: replay idempotente de orden de compra

**Unidad V5 FINAL:** implementacion de servicio/caso de uso con limites claros. **Autor:** PRIMARY DEV 01 JAVIER, TASK_ID 6adbb9fff1548191a1bd5b30d9e9984a. **Rama:** dev exclusivamente.

## Cambio causal y fronteras

El servicio existente `OrdenCompraService.CreateAsync` calcula el SHA-256 del DTO canonico y consulta la clave de idempotencia. El dominio valida el digest de 64 caracteres ASCII hex y lo normaliza a minuscula (M03.01). Antes de este cambio `ResolverReintento` comparaba texto ordinal; un registro historico de huella hex en mayuscula, con el mismo contenido de 32 bytes, producia falso conflicto. Ahora se valida la huella guardada, se decodifican ambos SHA-256 y se comparan 32 bytes mediante `CryptographicOperations.FixedTimeEquals`. Huellas almacenadas ausentes, truncadas o no hexadecimales fallan cerradas como conflicto. Un payload distinto no se acepta.

La modificacion afecta solo `backend/src/Application/Services/OrdenCompraService.cs` y dos pruebas xUnit en `N22OrdenCompraServiceRegressionTests.cs`; no altera API, DTO, JWT, RBAC, consultas por EmpresaId, repositorios, indices, migraciones, archivos, proveedores, frontend ni autorizacion existente. Una clave no representa identidad de tenant y la ruta HTTP debe conservar su autorizacion por separado. No se prueban datos productivos ni se exponen fingerprints en logs.

## A0–A4 y evidencia exigida

- **A0 prepublicacion:** inspeccion del diff, ausencia de secretos y dependencias nuevas, complejidad acotada, semantica de conflicto y permisos; reproducir `git diff --check` y analisis estatico pertinente cuando haya checkout.
- **A1 funcional:** xUnit replay positivo de registro legacy con digest equivalente en mayuscula, replay negativo con digest no hexadecimal y pruebas anteriores de payload diferente. Probar la ruta real `CreateAsync` con Moq: no crea nuevas ordenes ni transacciones ante replay.
- **A2 seguridad:** fail-closed ante huella malformada; comparacion de 32 bytes en tiempo constante; no cambiar aislamiento de EmpresaId/JWT/RBAC. Ejecutar regresiones de Seguridad multiempresa del mismo SHA.
- **A3 integracion:** `solqaryn-core-ci.yml` ejecuta restore/build/dotnet test no-Integration .NET 10, `dev-ci.yml` clasifica `backend/src/**` y ejecuta MySQL 8.4 si se habilita impacto. Solo cuentan checks, jobs y logs completos exact-SHA; skipped no es pass.
- **A4 posintegracion:** verificar fast-forward, cambios de archivos, checks de build/seguridad/MySQL aplicables, readback PLAN/COLA/BITACORA, rollback por git revert aditivo.
- **A5:** pendiente de Task distinta del autor para QA/main/PROD. Nunca autopromover.

**Aceptacion:** SHA-256 hex equivalente de otra caja no genera duplicacion; huella corrupta se rechaza antes de escritura; diferencia de payload sigue en conflicto; no se relajan permisos. Si CI no termina en esta corrida, conservar VALIDANDO y no declarar LISTO.
