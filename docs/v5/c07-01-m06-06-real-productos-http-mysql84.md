# C07-01.M06.06 — integración incremental R2 de /productos (DEV)
PROJECT_SCOPE_LOCK=STRICT
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev

## A0. Objetivo y cambio

Una suite de Integration ejecuta el **ProductosController real** y el **GET /productos** bajo ASP.NET Core TestServer, validación de JWT firmado, CurrentUserService y servicios verdaderos de membresía y autorización IPermisoService sobre Oracle EF y MySQL 8.4 efímero. ProductoService y ProductoRepository son reales, ejecutan lectura relacional. Los doubles de IUnitOfWork, IAuditoriaService, IImageStorageService y IProductoVarianteService satisfacen dependencias constructoras de otras acciones de escritura nunca invocadas; no acreditan CRUD/archivos/auditoría. Se insertan físicamente productos A/B en MySQL 8.4 mediante un AppDbContext con marca comercial server-side VerifiedCommercialTenant; el GET real devuelve solo el producto propio de cada usuario firmado y oculta ID ajeno con 404.

A1/A2 dirigidas: token anónimo o manipulado => 401; JWT verdadero de A al seleccionar B y de B al seleccionar A => 403; A y B con membresía propia y grants vigentes => 200; membresía A revocada con JWT no expirado => 403; al reactivar membresía => 200; grant Productos.Ver revocado en base => 403, sin revocar el grant de B. Una cabecera X-Empresa-Id actúa exclusivamente como selector y no como autoridad.

A3 dirigida: esquema único aleatorio en mysql:8.4 loopback, base bootstrap exacta, versión SQL comprobada, inicialización Oracle EF real, limpieza EnsuredDeleted únicamente del esquema aislado. Pipeline GitHub-hosted exige TRX de la suite con al menos un test realmente ejecutado, 0 fail, 0 skipped, y validación exact-SHA Scope Lock/skill.

## Limitación explícita, sin certificación de fase

Este incremento, incluso con middleware de cuarentena real en la suite, todavía **NO** certifica escrituras reales de Producto y Variante, conflictos de versión, rollback transaccional de catálogo, navegador Angular conectado a API firmada, auditoría persistente, API productiva hospedada ni migraciones globales Oracle EF. Por ello mantener C07-01.M06.06 EN_PROGRESO y el namedRange V17 legítimo de DEV01. El objetivo E1045 de fase acumulada sigue abierto; agregar pruebas A0-A4 para estos faltantes y obtener SUCCESS exact-SHA antes de VALIDANDO/LISTO. A5 únicamente por una Task Javier distinta. No tocar QA/main/PROD/Aiven ni secretos.

## Impacto y rollback

Solo test de backend, workflow de CI dirigido, este documento y CHANGELOG_AI íntegro. No API/runtime/config productiva, BD, contrato DTO, feature flag o migración. Ante falla de CI, examinar TRX/log y corregir el defecto específico en un siguiente commit FF/CAS; nunca reclasificar FAIL/SKIPPED como PASS. Deshacer un incremento incorrecto mediante git revert seguro con HEAD actualizado y readback; sin force-push o limpieza histórica.

## Segundo incremento: pertenencia real de filas en MySQL 8.4

Inserción de dos productos de catálogo A/B en el esquema MySQL efímero bajo marca server-side. La respuesta de /productos obliga a contener solo el nombre propio y a no exponer el ajeno; GET /productos/{id} cruzado exige 404. Ahora se inserta en el pipeline TestServer el **UnscopedCommercialTenantGuardMiddleware real** tras UseAuthentication/UseAuthorization. Con el flag in-memory PrivateReadPilotEnabled=false rechaza incluso JWT válido con 503; al habilitarlo solo durante la prueba permite GET propio (200), bloquea selector ajeno, membresía o grant revocados (503), y el controller sigue ocultando filas ajenas (404). No se acredita POST/PUT, variantes, transacción/rollback, Angular/Chromium o servicios externos. No certificar el objetivo E1045 completo por un test HTTP aislado.

## Tercer incremento: cuarentena HTTP real fail-closed

El host de pruebas reproduce la ruta de seguridad del Program: UseRouting, middleware de excepciones, UseAuthentication, UseAuthorization, **UnscopedCommercialTenantGuardMiddleware**, y controllers reales. El flag de piloto es una configuración en memoria del TEST, inicialmente OFF. OFF exige 503, ON se habilita solo en el test y exige RBAC tenant real. Los denegados por el guard responden 503 TENANT_DATA_ISOLATION_REQUIRED (a diferencia del 403 del action-filter interno). El control A/B y los datos comerciales son efímeros. Ningún despliegue/flag real fue modificado.

## Reparación adversarial causal posterior a FAIL

El primer intento de instalar el middleware real en TestServer (SHA dab80beb) arrojó HTTP 500 en vez del 503 esperado para el flag de lectura OFF, aun después de compilar Release. Se trata como FAIL comprobado, no PASS. Hipótesis de infraestructura del host de prueba: InvokeAsync del middleware requiere `IPublicStoreHostTenantResolver` e `IPublicStoreProofReplayStore` disponibles mediante DI aunque el endpoint no consuma host público. Se registran dobles inertes **solamente** para esas dependencias ajenas a /productos y habilita consola de excepciones del host de pruebas. Es una prueba falsable: si 500 persiste, examinar la excepción concreta y corregir nueva causa, nunca cambiar 503 esperado por 500 ni desactivar filtros.

## Cuarto incremento: rollback de persistencia Oracle EF en MySQL 8.4 efímero

La misma prueba de integración crea un producto temporal de la empresa A bajo autoridad de tenant verificada en servidor, dentro de una transacción explícita de AppDbContext; exige que el registro exista antes del rollback y que desaparezca tras RollbackAsync usando un DbContext nuevo sobre MySQL aislado. Esta evidencia **solo prueba rollback físico de persistencia EF** y aislamiento del contexto de pruebas. Todavía NO acredita rollback de POST/PUT HTTP reales, sincronización de variantes, auditoría duradera o Angular/Chromium conectado a API autenticada. Mantener C07-01.M06.06 en EN_PROGRESO; registrar CI exact-SHA únicamente cuando termine el job.

## Quinto incremento: HTTP mutaciones negadas por piloto solo lectura con readback MySQL

En el mismo TestServer con JWT A firmado y dos tenants físicos, una vez habilitado **únicamente** `PrivateReadPilotEnabled`, se ejecutan POST /productos, PUT /productos/{idB} y DELETE /productos/{idA} reales. Los tres deben recibir 503 porque no existe autorización para piloto de escritura. Un segundo AppDbContext consulta MySQL 8.4 sin filtros y comprueba que ambos registros A/B persisten sin cambios y que el nombre creado por el intento POST no existe. Los mocks de escritura no son utilizados: esto acredita **negación real de mutaciones HTTP**, no aceptación positiva de CRUD, variantes ni auditoría. Sin cambios en configuración de producción o flags de escritura. Requiere CI del nuevo SHA y mantiene EN_PROGRESO/A5 pendiente.

## Sexto incremento: rollback conjunto Producto+Variante y actualización de precio

La integración Oracle EF / MySQL 8.4 efímero ahora extiende la transacción del producto temporal: agrega una variante con EmpresaId A y SKU exclusivo, cambia el precio del producto persistido A, exige ver ambos cambios dentro de la transacción y ejecuta RollbackAsync. Otro AppDbContext propio A comprueba que no existen ni el producto temporal ni la variante, y que el precio original de A volvió a 100; el producto B ya se verifica independientemente en la misma suite. Esto demuestra rollback físico real de tres operaciones y no sustituye aceptación positiva HTTP POST/PUT de Producto+Variante, auditoría persistente ni navegación Angular/Chromium con JWT. CI GitHub-hosted dirigida, Scope Lock, seguridad y procedencia V5 se acreditarán solo cuando el SHA nuevo finalice SUCCESS; H=EN_PROGRESO, owner DEV01 y lock V17 se mantienen hasta A0-A4 completas; A5 independiente después.
