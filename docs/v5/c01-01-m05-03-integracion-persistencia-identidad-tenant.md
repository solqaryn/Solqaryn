# SOLQARYN V5 — C01-01.M05.03: integración física de identidad y membresía tenant

**Responsable:** PRIMARY DEV 05 — JAVIER, TASK_ID `6adc2e55f47c81919f6374e561687076`. **Rama:** `solqaryn/Solqaryn@dev`. **Alcance:** prueba de integración MySQL 8.4 efímero, sin cambios en Domain/Application/API de producción, esquemas, secretos ni proveedores.

## A0: impacto, dependencias y diseño

PLAN_MAESTRO!H172=PENDIENTE libre, COLA G/O=SI y DAG F=VALIDADO/G=SI para la unidad M05.03, derivados de M04.01–06+A5 y C00-03.M06.06+A5. Se confirmó propiedad exclusiva por CAS y bitácora. La unidad prueba contratos preexistentes `IdentidadUniversalReglas.CrearContextoVerificado`, `ContextoTenantActual.ExigirEmpresa`, `UsuarioEmpresa.CambiarRol/Desactivar` y el mapeo EF/MySQL de las tablas existentes de usuarios, empresas, roles y membresías. No duplica archivos de las microtareas M05.01, .02, .04 y .05, bajo otros propietarios.

Se añade `backend/tests/Solqaryn.Tests/C0101M0503MySqlTenantContextIntegrationTests.cs` con `[Trait("Category","Integration")]`. Utiliza el mismo `AppDbContext`, proveedor Oracle `UseMySQL` y el helper `Phase7MySqlTestDatabase.InitializeFreshAsync` que las pruebas físicas existentes del proyecto. Permite conexiones únicamente a `localhost` o `127.0.0.1`, genera una base de nombre aleatorio y borra exclusivamente esa base efímera en `finally`. Nunca conecta a Aiven, Dev live o producción.

## A1–A3: resultados exigibles, no presumidos

El test crea dos empresas con roles tenant-owned distintos, un usuario global y dos membresías físicas. Limpia EF ChangeTracker y **relee filas reales desde MySQL**; verifica contextos de A/B correctos, denegación cruzada de recursos, denegación de membresía B utilizada para empresa A, rol cruzado tras modificación física y membresía B revocada tras escritura y nueva lectura. Esto no equivale a una autenticación HTTP/JWT real, autorización RBAC por permiso, prueba de rendimiento, recuperación ni aislamiento de todas las tablas.

Los resultados reales solo pueden marcarse PASS después de ejecutar en GitHub Actions el test MySQL efímero y los checks .NET/seguridad pertinentes del SHA publicado; un archivo de test no constituye evidencia de ejecución. Se preserva el contrato de migraciones y se usan identidades sintéticas, sin datos personales ni credenciales. No hay nueva tabla, índice ni migración.

## A4/A5, continuidad y rollback

Publicación con commit fast-forward CAS, diff revisado, Scope Lock y procedencia V5 exact-SHA `completed/success`. Si la CI de integración está `queued` o `in_progress`, conservar VALIDANDO con el mismo owner, nunca declarar LISTO. A5 debe realizarla otra Task Javier distinta de la autora, con inspección del test, evidencias y no concurrencia. Rollback por git revert normal del changeset, sin borrar historia, sin alterar producción ni QA/main/PROD.
