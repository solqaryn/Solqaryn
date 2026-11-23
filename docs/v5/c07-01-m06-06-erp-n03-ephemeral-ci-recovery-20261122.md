# C07-01.M06.06 — correccion causal ERP-N0.3 GitHub-hosted (2026-11-22)

## Evidencia de fallo previa
Commit base: 37f13974f32c532fea0b96589411a353ccc895cb. La ejecucion de autoridad-variante 45081335159, job 137389941421, termino con 49 FAIL / 6494 PASS / 0 SKIPPED en 6543 pruebas. El log evidencia 29 configuraciones locales por microtarea que el workflow no exportaba; los fallos de tipo EPHEMERAL_MYSQL84_REQUIRED / MYSQL84_EPHEMERAL_TEST_CONNECTION_REQUIRED ocurren aunque el contenedor MySQL 8.4 exista. El test de falta de tenant tambien falla por esta misma precondicion ausente. El check dirigido C07-01.M06.06, job 137389941413, paso 1/1 y no habilita por si solo el cierre.

## Reparacion real
El workflow ERP-N0.3 mantiene dotnet restore, build Release -warnaserror y dotnet test de TODA Solqaryn.sln, sin quitar ninguna prueba o relajar asserts. Agrega 29 variables de conexiones apuntando EXCLUSIVAMENTE a 127.0.0.1:3306 MySQL 8.4 efimero en GitHub-hosted; crea bases bootstrap locales para las suites que requieren nombres exactos; aumenta redo temporal a 512 MiB; concede hasta 60 minutos para CI. Concurrency cambia a exact github.sha y cancel-in-progress=false para preservar pruebas de otros commits.

## Seguridad y alcance
Credenciales del servicio efimero de CI, no secretos productivos. No modifica JWT/RBAC real, API comercial, tablas de Aiven ni despliegues QA/main/PROD. No altera BITACORA historica ni libera gates DAG. Validar check-run exact-SHA despues de publicacion y leer logs, TRX y errores; WAIT_CI/FAIL no son PASS. R2: rollback operativo git revert normal y restitucion de version anterior del workflow bajo CAS. El propietario DEV01 conserva H1045 EN_PROGRESO, I historico y candado V17 2000001045; A0-A4 de fase comercial y A5 permanecen pendientes.

## Reparacion causal posterior: contratos de schema MySQL (2026-11-22)

La ejecucion GitHub-hosted ERP-N0.3 `45086881088` (job `137407425360`) concluyo `8 FAIL / 6535 PASS / 0 SKIPPED` de 6543 casos; build Release con `-warnaserror` correcto. Los siete fallos C04-04 son aserciones de precondicion: las suites sustituyen literalmente `Database=c0404_bootstrap` por un schema aleatorio y el workflow anterior suministraba `Database=solqaryn_n03_ci`, de modo que la cadena aislada nunca se construia. El octavo `C01-04.M03.06` lanzo `LOOPBACK_MYSQL84_ONLY` porque exige `Database=solqaryn_ci` y recibia `solqaryn_n03_ci`.

Este changeset corrige **solo** los ocho valores de entorno en `.github/workflows/erp-n0-3-ci.yml` y crea las dos bases bootstrap descartables en el contenedor MySQL 8.4 de Actions. No cambia aserciones, filtros, runtime comercial, permisos JWT/RBAC, pagos ni secretos productivos. Se mantienen las **6543 pruebas sin filtro** y la validacion real fisica MySQL 8.4. La reparacion tiene hipotesis y evidencia distintas de la anterior (49 fallos por variables ausentes). El nuevo SHA debe lograr CI completa antes de acreditar A4; ninguna ejecucion pendiente, fallida o omitida constituye PASS.

`C07-01.M06.06` conserva WIP/owner historico DEV01 y lock V17 `2000001045`. La salida acumulativa ERP aun requiere su propia matriz integral y A5 independiente. Rollback: git revert normal del unico commit en `dev` y revalidacion CAS/CI, sin force-push. `CHANGELOG_AI.md` no se reescribe en este cambio debido a la imposibilidad de verificar su contenido de mas de 1 MB a traves del conector sin arriesgar su historia; documentacion de reparacion queda completa en este archivo y BITACORA.

## Reparacion causal: dotnet-ef global fuera de PATH (2026-11-22)

En GitHub-hosted, run `45092068294`, job `137423634910`, SHA `c3ed2895641a2a64b6f170c75d41522d8a227b7f`: Release `-warnaserror` paso y xUnit/MySQL 8.4 ejecutaron **6544 PASS, 0 FAIL, 0 SKIPPED**. El job termino FAILURE (exit 1) despues, no por tests: la instalacion global `dotnet-ef 10.0.12` termino satisfactoria pero el paso inmediatamente siguiente `dotnet ef database update 20260811013917_N0_2_RetirarCatalogoProductoLegacy` devolvio `Run dotnet tool restore to make the dotnet-ef command available`. La causa es la resolucion del comando global desde `dotnet ef`, no una migracion fallida ni una base de produccion.

Correccion quirurgica: tras instalar el dotnet-ef global, comprobar que el ejecutable existe y mostrar su version con `"$HOME/.dotnet/tools/dotnet-ef"`; invocar esta ruta absoluta en los **tres** pasos `database update` N0.2, N0.3 y `migrations has-pending-model-changes`. Se conserva la integridad de 6544 pruebas, MySQL 8.4 descartable, build Release, preflight/postcheck y cancel-in-progress=false; no se relajan gates, SQL, JWT/RBAC, tests ni validaciones. La evidencia de 6544 tests **NO** supone PASS del workflow completo: requiere completar EF y todas las aserciones en nuevo SHA. Rollback normal con `git revert`, tras verificar HEAD y sin force-push. Se mantiene el owner DEV01, H1045 EN_PROGRESO y candado V17 2000001045; A0-A4/A5 globales de la fase aun pendientes, QA/main/PROD excluidos.

## Reparación causal adicional: N0.2/N0.3 legacy excluidas del EF10 Oracle (2026-11-22)

El run real `45097973250` (job `137441963149`) del SHA `fdfd716783ca9c9c6143b53bba64be1a3cd75141` terminó **FAILURE**: xUnit/MySQL 8.4.13 ejecutó 6544/6544 PASS, dotnet-ef 10.0.12 funcionó, pero `Crear esquema N0.2` falló con `The migration '20260811013917_N0_2_RetirarCatalogoProductoLegacy' was not found`. Los pasos de N0.3/postcheck/snapshot fueron SKIPPED, no PASS.

La causa ya no es PATH: el runtime vigente usa `Solqaryn.Infrastructure.Migrations` (Oracle EF10), mientras `backend/src/Infrastructure/Migrations/*.cs` de agosto 2026 son historia EF8/Pomelo expresamente excluida del proyecto Infrastructure. El ensamblado Oracle contiene migraciones desde octubre y no la N0.2/N0.3 legacy. `erp-n0-4-ci.yml` documenta y verifica exactamente esta frontera. Reintroducir las migraciones legacy o insertar registros históricos manuales no es una solución válida.

Se reemplazan **solo los pasos inviables del workflow N0.3** por evidencia real proporcional al backend vigente: inventario del ensamblado Oracle (ausencia legacy), `DatabaseBootstrap` en modo `fresh` sobre MySQL 8.4 efímero, comprobación SQL de columnas/FK/índices de variante por tenant, rechazo negativo real de una variante sin padres (error FK 1452), snapshot EF sin drift, y se conserva íntegra la suite de xUnit **sin filtro** previa. No se aplica, no se finge y no se certifica la migración histórica N0.3; el gate revisado certifica el esquema Oracle **actual** únicamente cuando GitHub-hosted exact-SHA termina todos sus pasos SUCCESS. Un fallo en la prueba real queda FAIL y obliga nueva hipótesis, nunca bypass.

Esta es reparación de CI R2 de `C07-01.M06.06`, no un cierre funcional integral: aún falta contrastar matriz positiva/negativa del alcance ERP de productos/servicios/variantes/categorías/impuestos/atributos, ownership y rollback transaccional del flujo HTTP/JWT/tenant A/B y obtener A5 independiente. `PLAN_MAESTRO!H1045=EN_PROGRESO`, owner histórico DEV01 y candado V17 2000001045 deben conservarse. No QA/main/PROD, datos Aiven ni secretos. Rollback mediante git revert ordinario del commit tras revalidar HEAD y lock, sin force-push.

## V26: instrumentacion fisica fail-fast (2026-11-22)

El job GitHub-hosted 137460739850 / run 45103916134 / SHA aa343553ca3857d29fff73dce3edabfe3f800863 termino FAILURE: 6577/6577 xUnit PASS, frontera Oracle EF10 PASS, bootstrap MySQL 8.4.13 fresh PASS (139 tablas, pending=0) y prueba fisica etapa 10 exit1 sin identificar asercion. El snapshot posterior fue SKIPPED. No inferir el subfallo.

El workflow mantiene restore y build Release -warnaserror, traslada el inventario Oracle, bootstrap y contrato fisico a un schema de CI separado solqaryn_n03_physical_ci, y ejecuta la suite completa sin filtros sobre el schema original solqaryn_n03_ci solo si el preflight pasa. Los 7 conteos producen N03_ASSERT/EXPECTED/ACTUAL; cualquier discrepancia imprime indices y FK reales sin exponer filas. La prueba negativa exige error MySQL 1452 exacto y cero huérfanos, no un error SQL generico. No cambia migraciones Oracle, tests de negocio, JWT/RBAC ni gates.

Este changeset no certifica PASS hasta que GitHub-hosted exact nuevo SHA confirme todos los checks requeridos; si falla, el label N03_FAIL, EXPECTED/ACTUAL y el catalogo de indices/FK permiten reparar la causa observada. H1045 queda EN_PROGRESO con DEV01, namedRange V17 2000001045, A5 independiente pendiente; sin QA/main/PROD/Aiven ni forzar DAG. Rollback: git revert ordinario tras CAS. No se reescribe CHANGELOG_AI.md (>1 MB, lectura incompleta del conector), preservando historia como indica esta nota.

## V26: correccion causal MySQL 1364 antes de FK (2026-11-22)

El preflight instrumentado en SHA 543ed64a72078e8094de4a8275e031cb50481844 / run 45109293202 / job 137477700419 terminó FAILURE en etapa 10. Las seis aserciones SQL de baseline, ausencia legacy, nueve columnas, cuatro índices, FK Empresa y tablas padre tuvieron exactamente EXPECTED=ACTUAL. La prueba negativa obtuvo `N03_FAIL=orphan_fk_error EXPECTED=ERROR_1452 ACTUAL=ERROR 1364`: **la inserción no alcanzó la restricción FK**. La suite xUnit completa y snapshot EF quedaron SKIPPED, no PASS en ese SHA; ese skip es consecuencia del fail-fast, no una aprobación.

La entidad `ProductoVariante : AuditableEntity : BaseEntity` incluye `FechaCreacion` y `FechaActualizacion` no anulables; el INSERT SQL de la prueba omitía ambas, lo que puede producir 1364 por campo obligatorio antes de evaluar las FK. Corrección concreta del fixture negativo: suministrar `UTC_TIMESTAMP(6)` para ambos, conservar IDs padre inexistentes y exigir **solo** error físico MySQL 1452 y ausencia de fila. Se imprime el inventario de columnas NOT NULL sin default de la tabla física para diagnóstico si surge otra precondición. No se cambia el modelo Oracle, no se relaja la política de seguridad ni se alteran datos de negocio. La suite completa permanece obligatoria después del preflight.

Nuevo commit necesita GitHub-hosted SUCCESS exact-SHA del job N03, Scope Lock/V5 y pruebas aplicables; cualquier fallo mantiene H1045 EN_PROGRESO, owner DEV01, V17 lock 2000001045 y A5 pendiente. No certifica cierre de la fase ni QA/main/PROD.


## V26: endurecimiento de la cuarentena fiscal con datos físicos y peticiones válidas (2026-11-23)

Riesgo R2, únicamente test de integración: el caso C0701M0606 usa el ID real de un Impuesto centinela persistido en MySQL 8.4 efímero, en lugar de asumir `/impuestos/1`. Recorre POST, PUT, PATCH activar/desactivar, DELETE lógico/permanente y duplicación con JWT firmados de A y B. Los cuerpos de POST/PUT/duplicación son válidos para evitar que un 400 de model binding enmascare una omisión del aislamiento fiscal. Cada respuesta debe fallar cerrada con 503 `TENANT_DATA_ISOLATION_REQUIRED`.

El readback compara antes/después **todas** las filas fiscales y el número de recibos de auditoría de Impuestos; no se atribuye al test un aislamiento fiscal positivo, pues `Impuesto` carece de `EmpresaId`. No se habilitan escrituras fiscales ni se alteran migraciones, middleware, autorización, producción o Aiven. La fase M06.06 sigue EN_PROGRESO con owner DEV01 y candado V17 2000001045; faltan ownership fiscal real, alcance positivo Servicios/Categorías/Atributos y aceptación ERP integral, A0-A4 de fase y A5 independiente. GitHub-hosted exact SHA es obligatorio antes de acreditar el test; rollback por revert ordinario con CAS. `CHANGELOG_AI.md` no se reescribe por conector al superar 1 MiB, preservando íntegro su historial.

