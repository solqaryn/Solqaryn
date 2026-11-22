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

## Septimo incremento: selectores ambiguos y aislamiento de precio fisico

El mismo test integrado de **ProductosController**, JWT firmado y Oracle EF/MySQL 8.4 efimero envia a GET /productos tres selecciones invalidas: cabecera X-Empresa-Id con A+B, empresa cero y texto no numerico. Todas deben devolver 503 bajo el guard comercial real, sin respuesta con datos de A ni B. El readback MySQL independiente despues de negar POST/PUT/DELETE comprueba ahora que nombres, no-eliminacion y precio original de ambos productos siguen intactos (100 en A y B), junto con el rollback fisico previamente probado del producto, variante y precio de A.

**Limite causal:** pruebas dirigidas en TestServer/CI, no escritura comercial positiva real via HTTP, no navegador Angular conectado a API JWT/DB, no auditoria durable, ni migraciones o despliegue productivo. No retirar la cuarentena de mutaciones. Mantener EN_PROGRESO, owner DEV01, candado V17 2000001045 y A5 independiente despues de A0-A4 completos. El workflow existente se dispara al cambiar el test y debe probar TRX con 1 test ejecutado, 0 FAIL/0 SKIP, Scope Lock y V5 exact-SHA. Rollback de este incremento: git revert normal del commit nuevo preservando evidencia previa.


## V22 2026-11-21 - Reparacion causal CI MySQL84 global (no cierre de M06.06)

- Fallo reproducido en GitHub-hosted dev-ci (`e3b2d3d6cb5227f7b3bfe2a3ca85ecf7be9f54e7`, job `136951951963`): 148 tests Category=Integration, 146 PASS y 2 FAIL; ambos `C0601M0306LandingReviewAuditMysqlTests` fallaron `EPHEMERAL_MYSQL84_REQUIRED` antes de su ejecucion SQL.
- Hipotesis comprobada: el test lee `C0601_M0306_MYSQL_TEST` y exige loopback/root/3306; el workflow global no exportaba esa variable, a diferencia de otras suites. Se agrega solo a la configuracion efimera mysql:8.4 del job de migraciones, sin relajar controles de test ni cambiar runtime.
- Verificar en el SHA publicado con GitHub Actions que `Migraciones EF Oracle, variantes y cargas masivas en MySQL 8.4` finalice `completed/success`, que los 148+ tests Integration se ejecuten sin FAILED/SKIPPED, que Scope Lock/V5 sean SUCCESS exact-SHA y que el proceso siga aislado. Si CI corre o falla, mantener `C07-01.M06.06` EN_PROGRESO con lock propio V17, sin acreditar LISTO/A5.
- Evidencia negativa: job original `https://github.com/solqaryn/Solqaryn/actions/runs/44938997980/job/136951951963`. No prueba POST/PUT transaccionales positivos ni Angular API real; no hay certificado de fase.

## Octavo incremento R2 — auditoria critica y rollback de alta de Producto

El servicio comercial `ProductoService.CreateAsync` deja de usar el registro tolerante a fallo al crear un producto y llama `IAuditoriaService.RegistrarEstrictoAsync`. El controlador real `POST /productos` ya envuelve esa operacion en `IUnitOfWork.ExecuteInTransactionAsync`; ante error al persistir auditoria, la excepcion debe revertir tambien el INSERT de Producto y evitar altas sin rastro. No altera filtros RBAC, tenant, flags, DTO ni contratos publicos.

La suite `C0701M0606ProductosRealHttpMysql84Tests` incorpora un caso adversarial **dentro de la prueba integrada**: usa `ProductoService`, `ProductoRepository`, `CategoriaRepository`, `UnitOfWork` y `AppDbContext` reales en el mismo esquema efimero Oracle EF/MySQL 8.4. Un doble del colaborador de auditoria simula un fallo de persistencia irreversible, exige la llamada estricta y verifica en un DbContext nuevo que no se confirmo el producto. El doble solo simula la falla; el INSERT/rollback y readback de MySQL son autenticos. GitHub-hosted debe completar el TRX y checks exact-SHA antes de acreditar A1/A3; no es evidencia de audit log positivo ni de UI/API POST firmado.

Alcance R2 y rollback: cambio minimo del servicio + test real + documentacion + CHANGELOG intacto. La fase `C07-01.M06.06` sigue EN_PROGRESO bajo su lease existente: aun faltan HTTP positivo Producto/Variante, evidencia de auditoria durable exitosa, navegador Angular/Chromium conectado y cierre independiente A5. No habilita el piloto de escritura, migraciones, pagos, QA/main/PROD; ante defecto usar commit revert normal tras leer HEAD, nunca force-push.

## Reparación P1 — rollback estricto ante fallos de auditoría en otras escrituras (DEV)

`ProductoService.UpdateAsync`, `CambiarEstadoAsync` y `DeleteAsync` deben invocar `RegistrarEstrictoAsync` dentro de la transacción del controlador: si el registro de auditoría devuelve error, el cambio en MySQL debe revertirse. Se añaden tres escenarios adversariales en `C0701M0606ProductosRealHttpMysql84Tests`: edición, desactivación y eliminación lógica, con fallo de auditoría inyectado y comprobación de estado original desde un `AppDbContext` nuevo sobre MySQL 8.4 efímero. Estos escenarios no prueban persistencia positiva de auditoría ni habilitan operaciones HTTP privadas; conservan la cuarentena fail-closed. Exigir xUnit Integration en GitHub-hosted exact-SHA, `Backend Release`, seguridad y Scope Lock/V5 completed/success antes de acreditar A1–A4. Si el CI falla, conservar EN_PROGRESO, revisar el log y reparar; A5 independiente y LISTO únicamente al completar el alcance real sin QA/main/PROD.

## V23 — auditoría positiva durable (incremento de prueba R2)

La integración añade una prueba **positiva** con `AuditoriaService`, `AuditoriaRepository`, `ProductoService`, `UnitOfWork` y `AppDbContext` reales contra MySQL 8.4 efímero en GitHub Actions. Inserta un producto autorizado con `VerifiedCommercialTenant` server-side y registra su auditoría estricta en la **misma transacción EF**. Un nuevo contexto verifica producto, empresa, actor, referencia, acción y resultado guardados físicamente. Complementa, no reemplaza, las pruebas negativas de rollback por fallo de auditoría.

Limitación explícita: la demostración pertenece al servicio y persistencia de la suite; no activa ni certifica HTTP POST/PUT/variantes detrás del piloto de lectura deshabilitado, ni navegación Angular real, ni auditoría de todas las rutas. Se requiere CI dirigida y global pertinente **SUCCESS del nuevo SHA** antes de acreditar A1/A3 de este incremento. La microtarea `C07-01.M06.06` conserva `EN_PROGRESO`, owner DEV01 y lock V17; falta completar integración de fase, A0–A4 y A5 independiente. No se altera seguridad/QA/main/PROD.


## V23 2026-11-21 — JWT firmado con issuer, audience y expiración inválidos (R2)

Se amplía el caso integrado de `ProductosController` en TestServer con tres negativos adversariales sobre la **ruta real GET /productos**: token con firma válida pero emisor distinto, audiencia distinta y token expirado. Cada solicitud lleva selector A válido; debe devolver **401** por validación JwtBearer, no 503 por cuarentena. El cuerpo no puede revelar productos de A ni B. Los tokens de prueba usan sólo la llave efímera del laboratorio; el test conserva MySQL 8.4, JWT real, RBAC y tenant A/B, sin cambiar producción ni la cuarentena de escritura. El resultado en GitHub Actions del SHA nuevo permanece **PENDIENTE** hasta prueba exact-SHA: no declarar PASS por el código añadido.

**Límite:** estos rechazos no prueban POST/PUT positivos, variantes comerciales con API firmada, Angular/Chromium conectado ni autorizan LISTO. La microtarea permanece EN_PROGRESO y el candado propio V17 se conserva; A5 debe ser independiente. Ante defectos, reparar la causa sin relajar JwtBearer y revertir por commit normal si es necesario.
