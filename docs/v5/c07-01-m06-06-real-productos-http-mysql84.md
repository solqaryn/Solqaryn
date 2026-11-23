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

## V23 DEV01 — reparacion causal de CI MySQL84 global (2026-11-21)

La suite obligatoria `Category=Integration` de GitHub-hosted en SHA `1685df2614fb92aef48ee84b64b5475b8bc5b1fc` ejecuto 150 tests: 149 PASS y 1 FAIL. El fallo real `C0402M0306DurableAuditMysql84Tests.ActualMysql84CommitsSanitizedAuditsAndDeniesUnsafeOperations` arrojo `EPHEMERAL_MYSQL84_REQUIRED` porque el job global no declaraba `C0402_M0306_MYSQL_TEST`. La suite dirigida C07-01.M06.06 si paso 1/1 con MySQL 8.4.13, JWT firmado y aislamiento A/B, pero ese PASS no sustituye la migracion global roja.

Se agrega exclusivamente la conexion de CI a mysql:8.4 efimero en loopback con esquema aislado `c0402_m0306_bootstrap`, siguiendo el patron existente. No se suprime ni omite ninguna prueba, no hay cambios al backend, datos reales, JWT, flags o produccion. Hipotesis falsable: el job global debe ejecutar la nueva prueba MySQL real y reportar 150/150 (o mas) PASS, 0 FAIL, 0 SKIP, con logs y TRX de ese SHA. A4 permanece PENDIENTE hasta CI completed/success, Scope Lock, V5, backend y seguridad del SHA exacto. La microtarea C07-01.M06.06 sigue EN_PROGRESO con owner DEV01 y lock V17: todavia faltan POST/PUT autenticados positivos Producto/Variante y verificaciones Angular/Chromium reales, segun alcance acumulativo E. No LISTO ni A5; rollback: git revert normal del changeset bajo HEAD nuevo, sin force-push.

## V23 DEV01 — correccion de bootstrap MySQL84 suite completa (2026-11-21)

La CI GitHub-hosted de SHA b1d14e3ba2f30acaa135a4e1a61533a468e1f98e finalizo FALLIDA en run 44974571372/job 137061542203: xUnit Category=Integration ejecuto 151 pruebas (149 PASS, 2 FAIL, 0 omitidas), no 151 PASS. La primera fallo en C0402M0306DurableAuditMysql84Tests con `Unknown database 'c0402_m0306_bootstrap'` antes de crear el esquema aleatorio aislado, porque admin.OpenAsync requiere una base de arranque existente. El servicio mysql:8.4 crea solo solqaryn_ci; la segunda falla fue una asercion signed-JWT HTTP en C0402M0306 que recibio reparaciones de prueba publicadas por DEV03/DEV04 en SHA posteriores. La prueba dirigida C04-02 2/2 SUCCESS de esos SHA NO sustituye la CI global.

Reparacion causal minimamente acotada: el valor C0402_M0306_MYSQL_TEST en .github/workflows/dev-ci.yml apunta a `solqaryn_ci` existente SOLO dentro del contenedor efimero. La clase de prueba crea un esquema `c0402_m0306_<GUID>` aislado mediante CREATE DATABASE, verifica InnoDB y 8.4, y lo elimina en finally. No se modifica codigo de produccion, JWT/RBAC, rutas, autorizaciones, filtros ni se omite ningun test. Hipotesis verificable: el siguiente SHA debe ejecutar Categoria Integration completa con TRX total=passed, failed=0, notExecuted=0, junto con Scope Lock/V5 y checks de backend/security aplicables. En espera de esos resultados, FAIL historico se conserva y A4 es PENDIENTE, no PASS.

Se mantiene C07-01.M06.06 EN_PROGRESO bajo owner DEV01 y V17 lock 2000001045. Quedan pendientes la integracion comercial completa POST/PUT de producto/variantes con JWT firmado y Angular Chromium conectado, sus negativos, auditoria y rollback end-to-end; no se concede VALIDANDO/LISTO, A5, QA/main/PROD. SKREF UI/motion/copy/performance son NO_APLICA al cambio CI backend; rollback con git revert normal en dev.

## V23 DEV01 — auditoría positiva de estado y aislamiento de empresa (21 noviembre 2026)

Se agrega una prueba REAL de dos transiciones comerciales: desactivar y reactivar un producto de la empresa A mediante ProductoService, UnitOfWork, AuditoriaService y AuditoriaRepository de producción, con Oracle EF Core/MySQL 8.4 efímero en GitHub-hosted. Cada transición confirma en un DbContext nuevo tanto el estado físico como el recibo estricto persistido (empresa, actor, acción y resultado). Luego se intenta desactivar ese producto desde el contexto servidor de la empresa B: debe devolver null, mantener el producto de A activo y no insertar recibo de B. El test es adversarial y fallaría si se rompe el filtro de tenancy o no se registran auditorías efectivas.

**Límite de aceptación:** el incremento prueba persistencia/auditoría de servicios con MySQL real, no POST/PUT positivos en HTTP ni Angular/Chromium conectado, y no habilita ninguna cuarentena de escritura. La suite dirigida y la migración global deben finalizar SUCCESS con ejecución real y 0 fallos/omitidas en el SHA publicado. Hasta entonces A1/A3/A4 pendientes; C07-01.M06.06 continúa EN_PROGRESO, propietario DEV01, candado V17 2000001045; sin LISTO ni A5. Si aparece fallo, diagnosticar logs del job exact-SHA antes de una reparación. Riesgo R2, rollback por git revert normal. SKREF-03/04/05/06/10/11/12 NO_APLICA: solo prueba y evidencia backend, no UI, motion, copy, amenaza nueva ni profiling; PROJECT_SCOPE_LOCK=STRICT y QA/main/PROD intactos.

## V23 DEV01 — integración incremental HTTP POST real con JWT, variante técnica y auditoría

En ASP.NET Core TestServer, con Oracle EF/MySQL 8.4 efímero, la solicitud firmada de tenant A se autoriza mediante un permiso Productos.Crear persistido y selector server-side. PrivateDraftPilotEnabled se enciende únicamente en configuración en memoria del test y se apaga tras comprobar la creación. Se ejecutan los componentes reales ProductosController, filtros RBAC, ProductoService, ProductoVarianteService, CatalogoProductoService, UnitOfWork y AuditoriaService. Otro DbContext exige producto A, variante técnica de A con precio 127, y recibo de auditoría estricto para el actor A. GET firmado consulta el producto. El JWT válido B, sin Crear, recibe 503 y MySQL no contiene su intento. Con el flag OFF, el JWT de A tampoco puede escribir.

**No se considera cierre de fase:** falta PUT positivo, variantes comerciales con seguridad HTTP y Chromium/Angular conectado a API JWT real con pruebas adversariales. A0-A4 finales y A5 independiente pendientes. El nuevo SHA deberá acreditar CI de MySQL84 Integration, Backend Release, Scope Lock, V5 y seguridad completados SUCCESS; queued o in_progress no son PASS. Permanecen EN_PROGRESO DEV01 y V17 namedRange 2000001045. No se cambia producción ni servicios externos ni feature flags productivos; rollback solo con git revert normal. SKREF UX/motion/copy/perf NO_APLICA para este cambio solo de test backend.


## V23 DEV01 — HTTP PUT auténtico, revocación RBAC e IDOR A/B (2026-11-22)

Incremento R2 de pruebas causales sobre `ProductosController.Update` de producción (sin modificar runtime): el TestServer usa JWT firmado, filtro de permiso Editar real, membresías/roles A/B en Oracle EF Core sobre MySQL 8.4 efímero y cuarentena `PrivateDraftPilotEnabled` habilitada solo en memoria. A actualiza el producto creado en el mismo caso de integración; un `DbContext` nuevo verifica Nombre y precio de la variante técnica, más recibo `AuditoriaService` persistido de la acción Editar. B posee Editar únicamente para su empresa y su intento de modificar el ID de A debe devolver 404 sin alterar la fila. La revocación del permiso Editar a A debe denegar un nuevo PUT con JWT aún vigente y sin alterar el estado físico. Toda aserción es exigible: no se certifican mocks, UI ni proveedores externos.

**Límites deliberados:** es una prueba .NET/MySQL HTTP real, sin cambios de producción ni activación de feature flags persistidos. Todavía faltan integración comercial Angular/Chromium con API y cobertura de variantes comerciales; la salida acumulativa M06.06 no se certifica. El candado propio V17 `2000001045`, estado EN_PROGRESO y owner DEV01 se conservan hasta A0-A4 globales; ninguna A5 propia. GitHub-hosted requiere TRX ejecutado y 0 failures/skips, Scope Lock/V5/backend/security exact SHA; si sigue QUEUED/IN_PROGRESS no es PASS. Rollback normal `git revert` del changeset; SKREF externas NO_APLICA porque la modificación es únicamente test backend y evidencia, sin UI/perf medida o threat model nuevo.

## V23 — Reparación causal de PUT HTTP aislado, sin alterar cuarentena real

- En el SHA `263c997b01b98ea9d1516f3a0980e8c51f455f41`, el job GitHub-hosted `137128706336` falló con `Expected: OK; Actual: ServiceUnavailable` al ejecutar `PUT /productos/{id}` (línea 798). La causa es un gate legítimo, no una falla de MySQL: `PrivateDraftPilotEnabled` habilita únicamente POST; el middleware real exige el opt-in independiente `PrivateCommercialFilesPilotEnabled` para PUT de producto, junto a JWT, RBAC y tenant actual.
- El test adversarial nuevo exige primero `503` al intentar PUT firmado sin el opt-in; verifica por lectura física de MySQL 8.4 que no hubo escritura; después habilita el opt-in **exclusivamente en configuración en memoria de TestServer**, ejecuta PUT positivo mediante el controlador real, prueba IDOR del tenant B, revocación del permiso Editar, auditoría persistente y restablece la bandera a `false`. No modifica la implementación de middleware ni flags de entornos.
- La evidencia no acredita todavía Angular/Chromium real conectado a API, hardware ni operación productiva. La prueba dirigida y el conjunto global MySQL deberán terminar SUCCESS en el SHA nuevo; hasta entonces `C07-01.M06.06` sigue EN_PROGRESO con owner DEV01 y candado V17 legítimo. A0–A4 de fase y A5 independiente aún abiertos; QA/main/PROD no autorizados.
- Rollback seguro: `git revert` del commit nuevo en `dev` con readback de HEAD y sin force-push.

## V23 — Reparación causal de negativo HTTP IDOR con formulario válido (DEV01, 22 noviembre 2026)

El job GitHub-hosted [137141628888](https://github.com/solqaryn/Solqaryn/actions/runs/45000783540/job/137141628888) del SHA `939e4dbec2f3c6f4c3b12e76483083a8e9af2317` finalizó **FAIL**: el PUT cruzado de tenant B contra producto A devolvió `400 BadRequest`, mientras la prueba esperaba `404 NotFound`. El POST/PUT autorizado A previo estaba completo, pero el negativo B omitía `CategoriaId`, `Cantidad` y `Costo`; se confundía rechazo de validación de entrada con rechazo real de ownership. Se corrige **solo el formulario adversarial**, copiando campos requeridos de un PUT positivo y manteniendo producto ajeno, JWT B real, ruta de controller real, respuesta exigida `404` y readback físico sin mutación de producto A. No cambiar el middleware, RBAC, guardas, respuestas esperadas ni configuración comercial. Un nuevo SHA deberá demostrar en GitHub-hosted la prueba real completa con TRX ejecutada, cero failures/notExecuted y controles Scope Lock/V5/backend/seguridad; nunca declarar PASS antes de su finalización.

El job global de migraciones del SHA anterior [137141701017](https://github.com/solqaryn/Solqaryn/actions/runs/45000783618/job/137141701017) también finalizó FAIL con 150/152 PASS y defectos `Unknown database c1402_m0606_bootstrap` y `GET /productos` 400 vs 404. El HEAD posterior `78331965` ya introduce reparación separada del bootstrap C14; las pruebas globales del nuevo SHA siguen siendo obligatorias, no se hereda SUCCESS de otros jobs. C07-01.M06.06 permanece **EN_PROGRESO** bajo owner DEV01 y lock V17 por verificarse, sin A0–A4 completos, faltando Angular/Chromium conectado a API firmada, matriz ERP/variantes, rollback y evidencia integral de fase. A5 independiente queda pendiente. No QA/main/PROD ni pagos/datos reales. Rollback: revert normal del changeset desde HEAD actualizado, conservando BITACORA y auditorías.

## V23 — Segunda hipotesis verificable de IDOR HTTP B contra A (DEV01, 22 noviembre 2026)

Los jobs GitHub-hosted [137156086590](https://github.com/solqaryn/Solqaryn/actions/runs/45005505224/job/137156086590) y [137156158607](https://github.com/solqaryn/Solqaryn/actions/runs/45005505231/job/137156158607) del SHA `4e8f045ac357c31d44a5e0de3a0939613c9ceb16` reprobaron el PUT B contra producto A: `400 BadRequest` frente al `404 NotFound` requerido; MySQL 8.4 global: 151 PASS de 152, un FAIL. La reparacion anterior habia completado los campos requeridos, pero enviaba `CategoriaId` de A bajo un JWT y empresa B, lo que podia producir 400 por validacion de categoria antes de evaluar ownership del producto. La nueva prueba usa categoria autentica de B (sembrada por su autoridad verificada y reconsultada de MySQL), JWT firmado de B y producto objetivo de A; conserva asercion estricta HTTP 404 y readback de cero mutaciones. No altera middleware, RBAC, pilotos, produccion o comprobaciones. Hipotesis pendiente de GitHub-hosted CI exact-SHA: un commit no certifica PASS. Mantener EN_PROGRESO, el candado V17 y owner DEV01 hasta A0-A4 reales completos de M06 y posterior A5 independiente. No QA/main/PROD. Rollback mediante `git revert` con HEAD actualizado, sin force-push.

## 2026-11-22 DEV01 — reparación de compilación CS1061 en prueba HTTP/JWT/MySQL C07-01.M06.06

- Evidencia causal: `dev` SHA `c5651b18fa9422e38242d40a60236b32fa2dde7a`, GitHub-hosted job `137181922606`, error `CS1061` en las líneas 742 y 748: `Categoria` no contiene `EmpresaId`; 12 checks requeridos fallaron y ninguna prueba de integración quedó acreditada por ese SHA.
- Solución mínima R2, sin cambiar runtime: resolver las dos categorías mediante `Producto.CategoriaId` asociado al `Producto.Id` conocido y `Producto.EmpresaId` verificado por MySQL. Las dos categorías siguen siendo diferentes, el JWT B usa la categoría del producto B y apunta a un producto A, y el test conserva HTTP 404 y ausencia de mutación para IDOR. No se agrega una columna falsa a `Categoria`, ni se relajan RBAC, validación o aislamiento.
- CI requerida: reconstrucción .NET 10 y tests Oracle EF/MySQL 8.4, seguridad/autorización, Scope Lock y procedencia V5 sobre nuevo SHA. Un check fallido/omitido/cancelado/en progreso no equivale a PASS; esta reparación elimina la falla de compilación observada, no certifica que IDOR 404 ni integración end-to-end ya pasen.
- `PLAN_MAESTRO!H1045` sigue `EN_PROGRESO`; owner histórico DEV01 y candado V17 2000001045 preservados. Pendientes Angular/Chromium realmente conectado, compatibilidad ERP/categoría/variantes, pruebas y A0–A4; A5 independiente por otra Task Javier. Rollback mediante `git revert` sin force-push. No QA/main/PROD.

## 2026-11-22 DEV01 — corregir HTTP 400 previo a verificacion de IDOR en PUT

- Evidencia nueva de GitHub-hosted en SHA `84ef63162d907b4f45a85d19a14e829bfd12d26e`: job dirigido `137187387296` y MySQL 8.4 global `137187460078` **FAIL**. El test real `C0701M0606ProductosRealHttpMysql84Tests.RealProductosGetUsesSignedJwtMysqlTenantABAndRevocation` esperaba 404 y obtuvo 400. Log causal: `ProductoVarianteService.ObtenerProductoAsync` lanza `BusinessRuleException("El producto no existe o fue eliminado.")` en la lectura de variantes previa al `ProductoService.UpdateAsync`; el middleware mapea esa excepcion a HTTP 400.
- Reparacion R2 minimamente acotada en `ProductosController.Update`: dentro de la transaccion, consultar `IProductoService.GetByIdAsync(id)` bajo los filtros actuales de tenant **antes** de `GetByProductoIdAsync`. Si no existe producto autorizado, el action retorna 404 por su salida existente; no se ejecutan operaciones de variantes ni mutaciones. Para un producto propio se conserva el flujo completo de validacion de formularios, variantes, transaccion, auditoria y permisos.
- El test adversarial existente conserva JWT real B, categoria propia B, objetivo producto A, asercion HTTP 404 y lectura independiente de MySQL que exige cero mutaciones. No modificar la expectativa, JWT/RBAC, filtros globales ni las banderas comerciales; pilotos privados solo en TestServer efimero. Requiere nuevo run GitHub Actions con .NET10/Oracle EF/MySQL8.4, security/auth y Scope Lock/V5 exact-SHA completos (FAIL/QUEUED/IN_PROGRESS/SKIPPED requerido nunca PASS).
- **No cerrar** `C07-01.M06.06` hasta evidencia acumulativa de fase (Angular/Chromium real conectado a API, tenant A/B, ERP/variantes, rollback/auditoria) y A0-A4 suficientes; A5 debe ser independiente. Preservar owner DEV01, V17 lock `2000001045`, `PLAN_MAESTRO!H1045=EN_PROGRESO`, BITACORA historica, QA/main/PROD intactos. Rollback mediante revert normal del commit verificado, sin force-push.

## V23 DEV01 — revocación real de Crear después de emitir JWT (incremento R2)

La prueba integrada `C0701M0606ProductosRealHttpMysql84Tests` añade una mutación negativa sobre el **POST de producción** en TestServer: después de una creación y edición positivas con token firmado, revoca físicamente en Oracle EF/MySQL 8.4 el grant `Productos.Crear` del rol A sin expirar ni cambiar su JWT. Envía un formulario válido bajo los flags de piloto **solo en memoria del test** y exige HTTP 503 fail-closed. Un `AppDbContext` independiente comprueba cero productos nuevos, idéntico número de recibos duraderos `Crear` del actor y ausencia de cambios al producto existente. La verificación usa los servicios RBAC reales, no un mock de autorizaciones.

Riesgo R2: no altera middleware, tokens, esquema, política de producción ni el estado de tenants reales. **A0** alcance dirigido, **A1/A2/A3** requieren xUnit y MySQL 8.4 efímero reales en GitHub-hosted, **A4** exige Scope Lock/procedencia y checks relevantes del SHA nuevo terminados SUCCESS; al publicar no se presume PASS. La fase E1045 aún carece de navegador Angular/Chromium conectado al API JWT y matriz ERP de ownership/rollback completa; conservar EN_PROGRESO, owner DEV01, lock V17 y A5 independiente posterior. Revertir el commit sin force-push si falla la hipótesis.


## V24 DEV01 - segunda identidad Angular JWT real y aislamiento navegador/API (22-11-2026)

Se amplia la prueba Playwright `frontend/e2e/c07-01-m06-06-browser-jwt-real.spec.ts`. En el mismo GitHub-hosted runner con Angular 22, ASP.NET Core 10 y MySQL 8.4 efimero, un usuario B distinto de A utiliza `/auth/login` real con JWT firmado. Debe obtener 200 en `/tenant-context/2` y `/productos`, una respuesta estructurada EF/MySQL y un layout sin body overflow a 390px. Reutilizar el mismo token B para seleccionar tenant A debe devolver 403/404 o cuarentena segura 503, sin filas del tenant A. No se usa `page.route`, dobles de JWT o MySQL ni se contabiliza un 503 como positivo.

**Evidencia A1-A4 aun pendiente para este incremento:** GitHub Actions V24 `C07-01.M06.06 - Angular JWT real MySQL 8.4 Chromium` debe completar 4/4 PASS, 0 failed/skipped y los checks causales Scope Lock/V5 en SHA exacto. La prueba dirigida previa `b877d1c642532f20ba9607c97084ae4652ccef8a` si obtuvo 1/1 JWT HTTP/MySQL 8.4 PASS, pero no certifica el test nuevo. C07-01.M06.06 sigue EN_PROGRESO/DEV01 con lock V17 2000001045. La matriz de producto/variante y ERP, auditoria durable y rollback extremo, cierre A0-A4 y A5 independiente siguen pendientes. No modificar QA/main/PROD.

Referencias originales consultadas: SKREF-03 `pbakaus/impeccable@2149fcce39a90bb409df5f16515f316a76dc6199` (responsive 390px, evidencia), SKREF-10 `angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9` (E2E en framework preexistente). SKREF-11 threat-model y SKREF-12 .NET perf NO_APLICA: este incremento es solo de pruebas, sin alteracion de runtime. Revert normal bajo HEAD/CAS si la prueba resulta incorrecta, sin force-push.

## V24 DEV01 — evidencia navegador con filas A/B físicas (2026-11-22)

- Antes, la prueba Chromium de JWT/tenant podía aprobar con catálogos vacíos de A/B. La suite ahora inserta **dos Productos físicos** con ownership A/B distinto en el mismo MySQL 8.4 efímero de GitHub-hosted; un readback SQL exige exactamente ambos registros.
- La página Angular `/productos` consume API ASP.NET real y JWT de usuario A y usuario B: cada respuesta HTTP debe contener su nombre propio y excluir explícitamente el nombre del otro tenant; las tarjetas móviles reales a 320/390 píxeles deben mostrar el propio y nunca el ajeno. Conservar negativos selector ajeno, firma JWT manipulada y cero mutaciones de UI.
- Alcance de prueba R2, solo workflow efímero + spec Playwright; no se editan controladores, proveedor, BD persistente ni configuración de producción. Referencias originales aplicadas: SKREF-10 Angular pruebas y SKREF-03 accesibilidad/visibilidad. No aplican SKREF-04/05/11/12 porque este diff no introduce animación, landing, threat model formal ni profiling.
- **A4 pendiente** hasta el job `C07-01.M06.06 - Angular JWT real MySQL 8.4 Chromium` y los checks pertinentes `completed/success` del SHA publicado. Una falla en bootstrap SQL o visibilidad debe diagnosticarse desde el job; nunca transformar SKIPPED/FAIL/IN_PROGRESS en PASS.
- No cerrar `C07-01.M06.06`: continúan pendientes matriz ERP/variantes, auditoría y rollback end-to-end acumulativos, revisión A5 independiente y gates naturales DAG/COLA. H=EN_PROGRESO, owner histórico DEV01 y su namedRange V17 siguen intactos.

## V24 DEV01 — revocación física de membresía tras carga real Angular (22-11-2026)

Incremento R2 para C07-01.M06.06: el navegador carga un producto físico del tenant A desde MySQL 8.4 efímero mediante API .NET 10 y JWT realmente firmado. Sin renovar el mismo JWT se revoca temporalmente la membresía A dentro del contenedor de CI (readback SQL), se exige denegación real de GET /productos, recarga segura de Angular sin catálogo privado y redirección de sesión. La prueba restaura la membresía en finally, exige readback SQL y control positivo posterior del mismo token. No altera secretos, permisos ni datos productivos.

Archivo de regresión: `frontend/e2e/c07-01-m06-06-browser-jwt-real.spec.ts`, suite Chromium GitHub-hosted `v24-c07-01-m06-06-browser-jwt-mysql84.yml`, sin mocks HTTP. Requiere 5/5 Playwright PASS, 0 FAIL/omisión, build frontend, MySQL y checks V5/ScopeLock en SHA exacto. Hasta su ejecución completa es CI_PENDING, NO PASS. Esta prueba no sustituye el cierre acumulativo de ERP, variantes, rollback end-to-end ni revisión independiente A5; PLAN sigue EN_PROGRESO con autor DEV01 y candado V17 propio.

## V24 DEV01 — aislamiento B durante revocación A y rollback SQL (22-11-2026)

Incremento R2 de regresión GitHub-hosted: el Playwright real de C07-01.M06.06 toma snapshots SQL de EmpresaId, Nombre, Cantidad y Precio sobre dos productos A/B físicos en MySQL 8.4 efímero. Revoca membresía A después de cargar Angular con JWT firmado, verifica denegación A sin mutación y login/API B con 200 y sólo producto B; `finally` restaura membresía A y coteja el snapshot antes/después. Sin `page.route`, tokens fabricados, flags productivos ni secretos reales.

El SHA anterior `d539a23af0f8195a878c98951ceb8bf17c660f2b` obtuvo 5/5 en [CI dirigida](https://github.com/solqaryn/Solqaryn/actions/runs/45053244598/job/137302782709), pero la aceptación global [137302782937](https://github.com/solqaryn/Solqaryn/actions/runs/45053244520/job/137302782937) falló 1/100: tema oscuro produjo tres elementos evaluables frente a cuatro mínimos. Esta brecha se conserva como FAIL, no se relaja contraste ni se certifica como PASS. El incremento nuevo requiere resultados GitHub-hosted exact-SHA: Playwright, Scope Lock, V5, backend y seguridad causal SUCCESS. E1045 aún necesita matriz de ERP/variantes/categorías/impuestos/atributos/rollback integral; mantener EN_PROGRESO DEV01, V17 lock 2000001045, sin LISTO/A5, QA/main/PROD. Revert normal de este changeset si falla.


## V24 — rechazo HTTP real de mutaciones y readback SQL en Angular CI

Se agrega un sexto caso adversarial a `frontend/e2e/c07-01-m06-06-browser-jwt-real.spec.ts` para la API ASP.NET 10 real con dos JWT firmados A/B y base Oracle EF/MySQL 8.4 efímera. Tras GET A=200, envía POST y PUT multipart y DELETE contra Productos; exige denegación 403/503 con piloto privado solo lectura, prueba DELETE cruzado B→A denegado y compara Id/EmpresaId/Nombre/Cantidad/Precio/Activo/Eliminado de las dos filas MySQL físicas antes y después. Niega creación no autorizada y verifica que la lectura B sigue funcionando sin exposición A. No se usan mocks, desactivación de controles ni escrituras en datos reales. El check GitHub-hosted nuevo debe acreditar 6/6 0 FAIL/0 SKIPPED y el Scope Lock/V5/backend/seguridad exact-SHA; antes de CI completa el incremento NO es PASS. Este caso no acredita escritura comercial positiva, ownership real de servicios/variantes/categorías/impuestos/atributos, rollback de POST/PUT ni auditoría durable; C07-01.M06.06 sigue EN_PROGRESO y A5 pendiente. Reversión permitida mediante git revert FF en dev preservando el histórico.


## V24 DEV01 - rollback atomico de variantes ante auditoria durable fallida

La ruta de servicio `ProductoVarianteService` antes confirmaba INSERT/UPDATE/estado/eliminacion de variantes en la transaccion y despues invocaba `RegistrarAsync`, que silencia errores. Este incremento mueve los cuatro recibos a `RegistrarEstrictoAsync` **dentro de la transaccion EF/MySQL**, incluido el espejo de `Producto`, para que fallo de auditoria revierta toda la unidad. En la suite integrada `C0701M0606ProductosRealHttpMysql84Tests` se exige rollback de desactivacion y eliminacion con auditoria inyectada fallida, comprobacion MySQL de estado/ownership sin recibo y caso positivo con recibo real durable de tenant A. Sin bypass de JWT/tenancy ni cambios en permisos, esquemas o datos reales.

Este es un incremento R2 de codigo y test; **GitHub-hosted CI del nuevo SHA es requisito y NO equivale a PASS hasta que concluya**. La prueba del servicio no habilita la API de escritura ni certifica variantes comerciales con JWT/HTTP, impuestos, servicios, atributos o rollback ERP completo. Conservar `PLAN_MAESTRO!H1045=EN_PROGRESO`, owner DEV01 y su V17, sin LISTO ni A5 y sin entornos QA/main/PROD. Ante falla se repara solo con evidencia causal; rollback del cambio por revert fast-forward si procede.


### V24 DEV01 - aislamiento de fixture de auditoria de variantes

La regresion de auditoria de variantes usa el producto de prueba aislado `auditedProductId` (ya creado en el escenario de auditoria durable), NO `productAId`, que conserva sus invariantes y precio para las posteriores pruebas HTTP negativas y positivas. El cambio es test-only; conserva los asserts de rollback y el esquema efimero. CI del nuevo SHA debe ejecutarse; ninguna prueba se declara PASS por anticipado.

## V24 DEV01 — reparacion causal del backend CI y guard de cancelacion (2026-11-22)

Origen observado: GitHub Actions para `b2396827bd2214837ea20ac9f07a8b8268b5d1aa` termino con `6388 PASS / 1 FAIL / 0 SKIP`. La unica falla de `ProductoVarianteServiceTests.CreateAsync_Normaliza_Sku_Y_Registra_Dimensiones` era una expectativa Moq obsoleta de `IAuditoriaService.RegistrarAsync`; el codigo de negocio llama a `RegistrarEstrictoAsync` dentro de la transaccion EF. Se corrige SOLO el contrato del test: verificar una llamada estricta y ninguna llamada tolerante, sin degradar el registro, el rollback ni las autorizaciones JWT/RBAC multiempresa. Los tests de auditoria transaccional MySQL 8.4 dirigidos ya pasaron en `13ce9521766c6fb3ff4377fe2a028476efbc4419`, pero ello no acredita todos los requisitos acumulados de C07-01.M06.06.

El workflow `erp-n0-1-ci.yml` tenia `concurrency.group` solo por ref y `cancel-in-progress: true`, que cancelo el job de una revision causal anterior. Ahora se aisla por `github.sha` y se desactiva la cancelacion; se mantienen intactos los pasos de MySQL 8.4, preflight y backfill. Riesgo R2: solo prueba y configuracion CI, sin runtime/producto/secretos/DB reales ni QA/main/PROD. A0: diff dirigido; A1: test de llamada positiva estricta y negativa tolerante; A2/A3: evidencia transaccional dirigida preexistente, sin pretender que un mock certifique MySQL; A4: GitHub-hosted del SHA nuevo pendiente hasta conclusion SUCCESS de checks pertinentes, Scope Lock/V5, backend Release y seguridad. H1045 continua EN_PROGRESO y owner DEV01, candado V17 2000001045 conservado. No LISTO ni A5 anticipados. Pendientes reales: cobertura ERP completa, frontend Angular/Chromium y todas las puertas acumuladas de M06.06. Rollback: git revert normal fast-forward de este changeset si fuese necesario.


## 2026-11-23 — evidencia adversarial R04: impuestos en cuarentena, tenant A/B

Incremento de riesgo R2 de la microtarea C07-01.M06.06 sobre `dev`. El inventario del código real comprobó que `Impuesto` y `ImpuestoRepository` no poseen alcance de empresa verificable, y que `ImpuestoService` no filtra lecturas por tenant. Esto no constituye por sí solo una filtración activa: `UnscopedCommercialTenantGuardMiddleware` impone cuarentena con dos empresas y no permite `/impuestos` a través de los pilotos comerciales actuales.

La prueba adversarial se inserta en `C0701M0606ProductosRealHttpMysql84Tests.RealProductosGetUsesSignedJwtMysqlTenantABAndRevocation`: reutiliza dos empresas creadas en Oracle EF/MySQL 8.4 efímero, JWT firmados y TestServer con guard real, y exige HTTP 503 más código `TENANT_DATA_ISOLATION_REQUIRED` en GET `/impuestos` y GET `/impuestos/1` para A y B. Fallará si una ruta privada fiscal queda abierta sin contrato de ownership; no desactiva guardas, no añade mocks de impuestos ni modifica runtime.

**Limitación explícita:** la prueba verifica exclusivamente denegación segura de la superficie fiscal aún no autorizada, no su funcionalidad futura ni cálculos, impuestos, atributos, precios o servicios end-to-end. Quedan pendientes la matriz ERP completa, pruebas de operaciones positivas cuando exista dueño fiscal explícito, regresiones de facturas/snapshots, controles de rol y fase A0-A4. A5 independiente solo después de un cierre legítimo. Cualquier CI QUEUED/IN_PROGRESS/FAIL/SKIPPED no es PASS. El estado PLAN H1045 debe seguir EN_PROGRESO, I1045 conserva DEV01 y namedRange 2000001045 permanece propio; ninguna sucesora se habilita manualmente.

Impacto: un test de integración y documentación de evidencia; sin migraciones, datos reales, cambios de permisos ni QA/main/PROD. Rollback: `git revert` ordinario bajo HEAD y candado revalidados. `CHANGELOG_AI.md` histórico de >1 MB no se altera ni reemplaza mediante lectura potencialmente truncada; queda esta documentación causal y BITACORA aditiva.


## V26 - cuarentena fiscal para verbos de mutacion (R2, evidencia incremental)

El controlador real `ImpuestosController` expone POST, PUT, PATCH y DELETE, pero `Impuesto` no contiene un contrato de propiedad `EmpresaId` verificado. Como resultado no se habilitan escrituras fiscales ni se fabrica una autoridad multiempresa. La prueba `C0701M0606ProductosRealHttpMysql84Tests` extiende su TestServer con JWT firmados de A y B y membresias reales MySQL 8.4 efimero: exige HTTP 503 `TENANT_DATA_ISOLATION_REQUIRED` en doce solicitudes negativas (seis rutas mutadoras por tenant: alta, edicion, desactivacion, borrado logico, borrado permanente y duplicacion). Se compara el numero fisico de filas `Impuesto` antes/despues mediante contextos EF separados.

Criterio causal: el job GitHub-hosted `v22-c07-01-m06-06-productos-real-http-mysql84.yml` del SHA publicado debe terminar `completed/success`, con TRX dirigido realmente ejecutado y sin FAIL/SKIPPED, mas Scope Lock exact-SHA y checks pertinentes. Una respuesta 400/401/403/404/500 no se normaliza a PASS. **No es prueba positiva de Impuestos**, no se modifican rutas ni flags de produccion, no desbloquea A5 ni finaliza M06.06. La matriz de ownership fiscal, servicios, categorias y atributos sigue pendiente y PLAN H1045 permanece EN_PROGRESO, owner DEV01, lock V17 2000001045. Rollback solo via git revert no destructivo y nueva verificacion de HEAD. `CHANGELOG_AI.md` supera el tamano seguro de lectura del conector: no se reconstruye ni se sobrescribe; evidencia causal en este documento y BITACORA aditiva.


### V26 DEV01 - R2 MySQL fiscal sentinel and full row snapshot (2026-11-23)

La prueba previa comparaba solo la cantidad de registros de `Impuestos`: una
modificacion de nombre, tasa, activo o borrado logico podia conservar el conteo y
evadir esa asercion. Se anade un impuesto centinela REAL por EF Oracle en el
MySQL 8.4 efimero, con codigo unico, y se comparan instantaneas ordenadas de
ID, Codigo, Nombre, Tasa, Activo y Eliminado antes y despues de las 12
solicitudes HTTP mutadoras con JWT firmados A/B y cuarentena estricta 503.
La prueba falla si un mutador altera el centinela sin cambiar el numero de
filas. No se elimina el chequeo de los 12 rechazos, la membresia vigente ni los
otros tests positivos/negativos. El fiscal catalogo global permanece sin contrato
tenant real y no se abre ningun endpoint. No hay modificaciones de runtime,
migracion, datos productivos ni QA/main/PROD.

A0: solo test y documento, WIP C07-01.M06.06 bajo candado V17 exclusivo
2000001045 y autor DEV01. A1-A3: exige el job dirigido GitHub-hosted real
de test de integracion EF/MySQL 8.4 con resultado completado SUCCESS,
0 fallos y 0 omitidos; no declarar aprobado mientras este queued/in_progress.
A4: Scope Lock, V5, backend/security y global MySQL pertinente del SHA
nuevo; un retry SKIPPED no cuenta como PASS. H1045 permanece EN_PROGRESO,
A5 pendiente de otro Javier; la certificacion fiscal positiva y el cierre
acumulativo ERP M06.06 siguen NO_GO. Revert normal fast-forward si la nueva
regresion detecta un problema, conservando historia.

### V26 DEV01 - selector de tenant falsificado en cuarentena fiscal (R2, 2026-11-23)

Hipotesis adversarial nueva: un JWT firmado de usuario de empresa A o B podria
acompanarse de `X-Empresa-Id` con el ID de la OTRA empresa. La cabecera es
un selector no confiable, nunca autoridad de acceso. La suite real
`C0701M0606ProductosRealHttpMysql84Tests` ahora combina cada JWT A/B con
su selector legitimo y con el selector falsificado. Para cada combinacion exige
GET fiscal 503 y `TENANT_DATA_ISOLATION_REQUIRED` en dos rutas, mas siete
mutadores fiscales 503 con dicho codigo: 8 GET y 28 mutaciones reales HTTP
en ASP.NET TestServer, JWT firmado y MySQL Oracle EF 8.4 efimero. El impuesto
centinela y el numero de auditorias deben quedar fisicamente identicos antes
y despues. La prueba falla ante una fuga de permiso inducida por cabecera.

Alcance limitado a prueba y evidencia; SIN cambio de controlador, schema,
autorizacion, tenant, runtime, dependencias ni configuracion comercial.
A0=diff test/doc sin secreto ni cambio productivo; A1/A2/A3 pendientes de CI
dirigida exact SHA GitHub-hosted con TRX 1/1 SUCCESS y 0 fallos/omitidos;
A4 pendiente de Scope Lock, backend, seguridad y migracion Oracle MySQL
cuando requieran causalidad. El SHA anterior `e044679d` tenia el job
dirigido, migraciones Oracle EF, backend, seguridad, V5 y Scope Lock SUCCESS
antes de este incremento, pero NO acredita el nuevo test. No certificar
LISTO ni A5: fiscal positivo con ownership y servicios/atributos de ERP
siguen sin integracion completa. `PLAN_MAESTRO!H1045=EN_PROGRESO`,
owner DEV01, V17 lock 2000001045. Rollback `git revert` normal si
aparece regresion, con readback previo de HEAD; QA/main/PROD prohibidos.

## V26 DEV01 — prueba física de cuarentena de categorías sin tenant

La entidad real \`Categoria\` es un catálogo global sin columna \`EmpresaId\`. Antes de conceder lectura o escritura privada es obligatorio establecer una pertenencia multiempresa verificable; una cabecera HTTP nunca confiere autorización. La suite existente añade 32 peticiones HTTP JWT firmadas a \`CategoriasController\` con empresas A/B reales y selectores propios/cruzados: GET listado, activas y detalle; POST, PUT, PATCH activar/desactivar y DELETE. Cada solicitud debe terminar 503 \`TENANT_DATA_ISOLATION_REQUIRED\`, sin exponer categorías. Un segundo DbContext Oracle EF/MySQL 8.4 efímero verifica por ID/nombre/descripción/estado que ninguna fila cambió y que los registros de auditoría del módulo permanecen idénticos.

Evidencia **negativa dirigida**, no aprobación de CRUD positivo de categorías, servicios, atributos ni fiscalidad multiempresa. No modifica runtime, banderas, DI ni datos comerciales reales. CI GitHub-hosted dirigida TRX real más Scope Lock/V5 exact-SHA: exigir SUCCESS antes de acreditar A0–A4 de este incremento. M06.06 sigue EN_PROGRESO con candado legítimo DEV01; falta aceptación acumulativa ERP, Angular/API completo y A5 independiente. Rollback con revert normal FF/CAS y readback; no QA/main/PROD.

## V26 DEV01 - R2 rutas anidadas de variantes protegidas (23 noviembre 2026)

Hipotesis nueva: la ruta privada `/productos/{productoId}/variantes` y sus subrutas de imagenes podrian eludir accidentalmente la cuarentena de producto por incluir mas segmentos URL. La prueba real del modulo `C07-01.M06.06` exige 32 solicitudes HTTP (JWT firmado A/B con selector propio y cruzado, ocho rutas GET/POST/PUT/PATCH/DELETE) a `ProductoVariantesController`, todas HTTP 503 `TENANT_DATA_ISOLATION_REQUIRED`. La prueba compara con un segundo DbContext Oracle EF/MySQL 8.4 fisico la instantanea COMPLETA de todas las variantes (ID, ProductoId, EmpresaId, SKU, cantidad, precio, costo, activo, eliminado) y el conteo de auditorias de variantes antes/despues. Incluye una variante A real sembrada con owner por contexto verificado y auditoria durable preexistente, para impedir falsos PASS por base vacia. No amplia permisos ni activa variantes comerciales.

A0: test y documento solamente, riesgo R2; dependencia de middleware tenant vigente y JWT firmado real del TestServer, sin cambios runtime. A1-A3: GitHub-hosted debe demostrar TRX completo con 1/1 PASS, 0 FAIL, 0 SKIPPED y MySQL 8.4. A4: verificar check exact-SHA Scope Lock/V5/backend/security y cualquier CI Oracle/authorization pertinente; no interpretar QUEUED/IN_PROGRESS/SKIPPED como PASS. Si la prueba falla, investigar respuesta/guard antes de cambiar controles. Limitacion: prueba negativa, no acredita operaciones positivas ni ownership completo de servicios, categorias, impuestos, atributos o experiencia Angular/ERP; `C07-01.M06.06` sigue EN_PROGRESO con owner DEV01, V17 lock 2000001045 y A5 pendiente independiente. `CHANGELOG_AI.md` no se reemplaza: el conector de lectura no devolvio su contenido completo aunque expuso blob SHA; modificarlo asi arriesgaria borrar historial. Evidencia de este incremento en este documento, GitHub y BITACORA aditiva. Rollback: git revert normal en dev tras releer HEAD, sin force push ni produccion.

## V27 DEV01 - contencion de clasificacion no vendible en SaveChanges (2026-11-23)

Hipotesis causal R2 nueva: el guard de ventas en AppDbContext solamente rechazaba
TipoInventario.InsumoAdministrativo=2. Una clasificacion desconocida (0/3/99),
incluyendo un futuro Servicio=3, podia atravesar la validacion EF directa de
VentaDetalle sin ser mercaderia admisible. El cambio **fail-closed** exige
TipoInventario.MercaderiaVenta=1 tanto para Producto rastreado como para consulta
persistida en el tenant, y conserva el mensaje previo de insumo administrativo.
No agrega Servicio al enum, no lo habilita en UI/ventas, ni cambia la cuarentena
de Categorias/Impuestos, rutas HTTP, DTO, schema o migraciones.

A0: R2, dos ramas de la misma validacion y cinco casos negativos xUnit
(0/3/99 rastreados, 3/99 persistidos) mas regresion previa de Insumo=2.
Las pruebas emplean EF InMemory, **no** prueban MySQL 8.4 fisico ni HTTP
firmado. A1-A4 dirigidos requieren GitHub-hosted dotnet Release/xUnit,
seguridad y Scope Lock/V5 exact-SHA y evidencia causal de CI completa.
A5 permanece independiente, sin LISTO de M06.06. La matriz positiva
Categoria/Impuesto ownership, Servicio con tratamiento propio, endpoints
JWT/RBAC/tenant A/B y experiencia Angular siguen pendientes, y deben ser
implementados en incrementos siguientes antes del cierre acumulativo.
No declarar PASS hasta leer los jobs. Rollback: git revert convencional
sobre HEAD nuevo tras verificar CAS. CHANGELOG_AI.md no se sobrescribe
porque el conector devuelve contenido vacio pese a un blob SHA valido;
se conserva integro, con evidencia adicional en documento/bitacora.

## 2026-11-23 — DEV01 V29: categoría privada positiva en piloto de lectura existente

- **Corrección arquitectónica real**: no falta columna física de propietario para `Categorias` ni `Impuestos`. La migración Oracle EF `20261013230000_CommercialTenantRemainingModules` ya crea `EmpresaId` nullable, índices y FK; `CommercialTenantRemainingModel` lo mapea como **shadow property**, y `CommercialTenantReadFilterModel` y `CommercialTenantOwnershipGraphWriteGuard` aplican aislamiento y asignación server-side. La ausencia de una propiedad CLR en `Categoria.cs` no demuestra ausencia de ownership físico. Se conserva en cuarentena el historial `NULL` sin atribución.
- **Implementación R2 limitada a dev**: se incorpora solamente GET exacto `/categorias`, `/categorias/activas` y `/categorias/{id positivo}` al **piloto de lectura ya existente** `MultiEmpresa:PrivateReadPilotEnabled` (OFF por defecto), con doble autorización SQL RBAC `Categorias.Ver`, membresía activa, selector no autoritativo y filtros EF tenant A/B. No se agrega autorización de POST/PUT/PATCH/DELETE, no hay nueva bandera, migración ni exposición por defecto.
- **A0/A1/A2/A3 a verificar en GitHub-hosted exact-SHA**: la prueba de integración amplía TestServer real JWT firmado, CategoriesController/CategoriaService reales, dos empresas y categorías en MySQL 8.4 efímero. Prueba SQL `EF.Property<int?>("EmpresaId")` A/B, GET positivo propio, GET ajeno 404, cross-tenant spoofing 503, revocación `Categorias.Ver`, piloto OFF 503 y ausencia de mutaciones. El readback negativo previo de categorías e impuestos permanece; el contrato positivo de Impuestos/Servicio/atributos/ERP no se declara aprobado.
- **Rollback**: git revert normal del commit bajo fast-forward/CAS después de verificar HEAD; `PrivateReadPilotEnabled` no se configura ni se habilita en producción. La entrega `C07-01.M06.06` sigue `EN_PROGRESO`, con owner DEV01 y candado V17 2000001045 hasta completar el alcance, checks reales A0–A4 y A5 independiente; no LISTO, QA/main/PROD ni secretos.

## 2026-11-23 — V29 hotfix compilación exact-SHA CS0128

- Diagnóstico real GitHub-hosted en SHA `cb44be3b3c0ad3f1976a3715b310869a3a664cd9`: job backend `137945139590` y gate de seguridad `137945138983` fallaron **antes de ejecutar los xUnit** por `CS0128` en líneas 1419 y 1421: nombres locales `categoryAId` y `categoryBId` duplicados dentro del método largo. No es fallo de aislamiento tenant ni de aserciones; sin PASS A1/A3.
- Reparación mínima: renombrar **solo las variables introducidas en el bloque V29** a `pilotCategoryAId` y `pilotCategoryBId` con sus usos en el mismo bloque. Sin cambios a API, middleware, tenant, flags, DB, fixtures o criterios. Publicar mediante segundo FF/CAS exact-HEAD y esperar compilación + prueba real en nuevo SHA; no relanzar manualmente el SHA fallido, no declarar listo.

## 2026-11-23 — DEV01 V30: GET fiscal privado tenant A/B

- Incremento funcional: `GET /impuestos` y `GET /impuestos/{id:int positivo}` entran SOLO en el piloto privado `PrivateReadPilotEnabled` preexistente, OFF por defecto. Mantiene JWT, membresía viva, selector coherente y doble `Impuestos.Ver`; datos aislados por la columna EF shadow `EmpresaId` física ya existente. No habilita mutaciones fiscales, flags nuevos, migraciones ni tratamiento de filas legacy.
- Integración dirigida pendiente de GitHub-hosted exact-SHA: crea impuestos A/B con propietario validado en MySQL 8.4 efímero y prueba GET list/detail positivos, ajeno 404, spoof 503, revocación RBAC, piloto OFF y ownership físico. No confundir este incremento con aprobación integral de servicios, atributos, mutaciones ERP, Angular ni cierre A0–A4.
- Continúa `C07-01.M06.06` EN_PROGRESO bajo owner DEV01 y V17 `2000001045`; A5 independiente posterior. Rollback: git revert normal mediante FF/CAS; prohibidos QA/main/PROD.


## V30 — GET fiscal activo, tenant real y seguridad fail-closed (2026-11-23)

Entrega funcional incremental: ruta `GET /impuestos/activos` en ASP.NET Core 10, bajo `PrivateReadPilotEnabled=true` (OFF predeterminado), JWT firmado, `Impuestos.Ver` SQL y selector de empresa validado desde servidor. Reutiliza servicio real y Oracle EF/MySQL 8.4 con propiedad EmpresaId shadow físicamente existente, filtra solo impuestos Activo y no eliminados. La suite dirigida exige positivos A/B, exclusión de registro inactivo, aislamiento ajeno, selector falso, permiso revocado y piloto OFF; ningún PASS se declara hasta CI exact-SHA. Escritores fiscales y ERP continúan en cuarentena.

Incidente causal CI: intento previo de GET de marcas fue inseguro ya que `Marca` no contiene ni mapea EmpresaId; GitHub-hosted falló con CS1061 antes de ejecutar xUnit. Se revierte íntegra su habilitación y se preserva cuarentena de marcas hasta un scope de migración autorizado. La aserción fiscal GET=503 antigua ya no aplica al piloto positivo existente; no se repone. Se preserva WIP C07-01.M06.06, owner histórico DEV01, lock 2000001045 y brechas de servicios/atributos/ERP/UI. No LISTO, no A5.

## V30 — Mutaciones fiscales de estado, contrato real y rollback atómico (23-11-2026)

En `C07-01.M06.06` se incorpora al runtime de ASP.NET Core 10 el piloto separado `MultiEmpresa:PrivateFiscalStatePilotEnabled`, apagado por defecto. Sólo PATCH exactos `/impuestos/{id}/activar` y `/impuestos/{id}/desactivar`, con identificadores positivos, pueden optar al nuevo flujo con JWT válido, empresa comprobada por membresía actual, permiso SQL `Impuestos.Activar/Desactivar` y filtro MVC independiente. No habilita POST/PUT/DELETE fiscales ni modifica shadow `EmpresaId`, migraciones, secretos, QA/main o producción.

`ImpuestoService` ejecuta actualización y auditoría crítica `RegistrarEstrictoAsync` dentro de la misma transacción Oracle EF/MySQL; un error de auditoría revierte el estado. La prueba de integración dirigida existente añade positivos A/B con JWT firmados, selectores falsos, IDs ajenos, revocación RBAC y rollback físico MySQL 8.4 comprobado desde contexto nuevo. Esta evidencia sólo acredita lo efectivamente ejecutado en GitHub-hosted exact-SHA, nunca sustituye integración de servicios, atributos, precios ni aceptación ERP/Angular. Mantener WIP DEV01 `EN_PROGRESO`, lock V17 2000001045 y A5 posterior independiente; no forzar DAG ni LISTO.


## V30 DEV01 — alta fiscal con recibo durable transaccional (23 noviembre 2026)

En `ImpuestoService.CreateAsync` se corrige el orden de persistencia anterior: una alta de impuesto no puede confirmar el registro comercial si el sink de auditoría falla, porque ambas escrituras se ejecutan dentro de `IUnitOfWork.ExecuteInTransactionAsync` y la auditoría se efectúa con `RegistrarEstrictoAsync`. Un error propaga excepción y la implementación Oracle EF/MySQL revierte la transacción. La semántica del código fiscal estable, los DTO, relaciones y los filtros de ownership existentes permanecen sin cambios; no se habilitan rutas adicionales ni flags de operaciones multiempresa.

La prueba xUnit de contrato verifica que el callback y la auditoría estricta se ejecutan y que la excepción del sink NO es absorbida. El mock NO demuestra rollback físico; exigir después CI GitHub-hosted en MySQL 8.4 efímero con aserción de cero filas/recibos al fallar el sink y aceptación positiva tenant A/B de las relaciones fiscales antes de declarar A3 completo. Esta entrega NO certifica servicios, atributos, mutaciones ERP globales, interfaz Angular ni cierre integral M06.06. Conservar H1045 EN_PROGRESO, I=DEV01, candado V17 propio 2000001045, A5 pendiente; sin QA/main/PROD. Rollback de código mediante revert ordinario serializado y verificación de invariantes.

## V30 DEV01 — edición fiscal sin confirmación parcial (2026-11-23)

**Scope** C07-01.M06.06, riesgo R2; cambio real en `backend/src/Application/Services/ImpuestoService.cs`. En `UpdateAsync` la obtención de impuesto, conservación del código fiscal estable, mutaciones de campos y relaciones, `SaveChangesAsync` y auditoría duradera `RegistrarEstrictoAsync` ahora forman una sola unidad transaccional EF Oracle/MySQL. Antes la edición llamaba al auditor tolerante a fallos después de confirmar los datos: la configuración podía cambiar sin recibo fiscal. El resultado existente no altera API/DTO, no recalcula documentos pasados ni habilita tenants o rutas nuevas. Ante fallo de auditoría estricta, la operación falla y la transacción real debe revertirse.

**Validación proporcional:** `ImpuestoCodigoEstableTests` ejecuta rechazo del cambio de código dentro de UoW simulado y propaga la falla del auditor durante edición; eso NO prueba rollback físico, JWT firmado o aislamiento tenant A/B. Requiere GitHub-hosted exact-SHA `Backend Release y pruebas`, Scope Lock/V5, y una integración adicional con MySQL 8.4 efímero que inyecte fallo del sink y lea la fila/relaciones nuevamente. Este incremento no equivale a A0–A4 de la fase ni autoriza `LISTO`/A5. Pendientes adicionales: altas fiscales positivas, servicios/atributos, autorización por tenant y aceptación ERP/Angular. Mantener owner, WIP V17 y condiciones fail-closed; reversión mediante commit `revert` fast-forward revisando HEAD.
