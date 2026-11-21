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
