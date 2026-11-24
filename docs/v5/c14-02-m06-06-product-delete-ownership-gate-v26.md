# C14-02.M06.06 — orden de autorización antes de borrar producto (V26 DEV06)

## Hipótesis causal

El controlador productivo `ProductosController.Delete` invocaba el borrado de la variante técnica antes de comprobar que `IProductoService.GetByIdAsync(id)` pudiera leer el producto en el ámbito de la empresa autorizada. Aunque el repositorio de variantes actualmente valida el scope por separado, el orden permitía depender de esa segunda barrera y podía revelar un fallo de variante distinto al 404 de producto inexistente o ajeno. Se refuerza el orden existente en `Update`: primero se prueba visibilidad tenant-scoped y sólo entonces se modifican variantes.

## Incremento real

Se añade el preflight `GetByIdAsync` *dentro* de la transacción de `Delete`, antes de `EliminarTecnicaConProductoAsync` y `DeleteAsync`. Un ID no visible devuelve 404 sin invocar ninguna mutación. Para un producto visible se conserva la misma secuencia de eliminación en la transacción. Las tres pruebas xUnit nuevas ejecutan el **método real** del controlador para verificar denegación sin efectos, secuencia positiva y aborto tras fallo de limpieza. Son dobles Moq exclusivamente de las interfaces del servicio; no se etiquetan como pruebas de MySQL ni como un HTTP/JWT real.

## Riesgo, gates y límites

- R2 seguridad BOLA/IDOR y consistencia de producto/variante. A0 diff de un método de API, tres xUnit, este documento y CHANGELOG sin nuevos contratos ni migraciones.
- A1 sólo tras GitHub-hosted .NET10 Release con 3/3 tests nuevos ejecutados, 0 FAIL/0 SKIP; A2 se prueba la barrera de secuencia sin conceder grants, no un JWT externo.
- A3 **PARCIAL**: es controlador productivo auténtico, pero servicios falsos. Faltan HTTP con JWT firmado y permisos reales, ownership del producto verificado contra MySQL 8.4 efímero y prueba de rollback de la transacción real.
- A4 Scope Lock/V5 y CI relevante `completed/success` del SHA real o descendiente con blobs y dependencias equivalentes; un job pendiente/cancelado/omitido no es PASS.
- **No se certifica** la ausencia global de P0/P1, ni BOLA/IDOR de archivos/jobs/cache/backups/sesiones, ni C14-02.M06.06 LISTO/A5. PLAN continúa VALIDANDO y el owner histórico DEV06 no cambia. A5 independiente por Task Javier distinta del autor/reparador, sólo cuando A0–A4 realmente estén completas.
- Rollback: `git revert` ordinario del commit con nueva comprobación de HEAD y ausencia de escritores; no reset ni fuerza.

## Cobertura posterior obligatoria

Ejecutar GET/DELETE `/productos/{id}` con verdadero `RequierePermiso`, JWT firmado A/B, sesiones/revocación y repositorio EF Oracle MySQL8.4 efímero. Verificar producto A visible, producto B ajeno 404 sin cambios ni diagnóstico de variante, restauración de transacción y sin ninguna mutación bajo permiso revocado. Luego ampliar la matriz de seguridad global según C14-02.M06.06, sin confundir el preflight aislado anterior con aprobación final.

## Reparación V27 — transacción parcial ante DeleteAsync=false

Hipótesis verificada por inspección del controlador real y de `UnitOfWork`: después del preflight tenant-scoped, `EliminarTecnicaConProductoAsync` puede modificar una variante, pero `IProductoService.DeleteAsync` puede devolver `false`; la `UnitOfWork` anterior hacía COMMIT sin excepción, dejando una eliminación técnica parcial. La corrección convierte el resultado `false` en error explícito dentro de la transacción, de modo que la `UnitOfWork` productiva ejecute ROLLBACK y no anuncie éxito. Un producto no visible continúa devolviendo 404 antes de mutar.

El nuevo xUnit adversarial ejecuta el método real `ProductosController.Delete`, comprueba la secuencia de limpieza seguida de `false`, exige que la transacción no alcance el camino de commit y mantiene los tres casos anteriores. Este test usa una `IUnitOfWork` simulada: **no es prueba de rollback real MySQL 8.4**, ni de filtro RBAC/JWT/HTTP productivo. A3 de `C14-02.M06.06` sigue incompleto hasta comprobar el controlador, autorización efectiva, empresas A/B y la transacción real Oracle EF/MySQL efímera; no certificar LISTO/A5, P0/P1 global ni QA/main/PROD sólo por compilar. Rollback del changeset: `git revert` normal del commit bajo control de rama dev.

## V29 — prueba física de rollback del controlador (23 noviembre 2026)

Se añade una aserción dirigida en `C0701M0606ProductosRealHttpMysql84Tests`, reutilizando el esquema Oracle EF/MySQL 8.4 **efímero**, la variante existente del tenant A y un segundo producto legítimo del tenant B. El método **real** `ProductosController.Delete` utiliza **real** `UnitOfWork`/transacción MySQL; la frontera `IProductoVarianteService` inyectada ejecuta una modificación **SQL real** y `IProductoService.DeleteAsync` devuelve `false` para reproducir el fallo causal. Se exige la excepción `PRODUCT_DELETE_ATOMICITY_ROLLBACK_REQUIRED` y readback desde otro `DbContext` que conserva la variante y los productos de A/B; el test falla si el cambio parcial se confirma.

**Límite explícito:** el método de eliminación de variante y el resultado negativo del servicio de producto están inyectados para provocar el caso de fallo. No constituye una ejecución productiva completa de `EliminarTecnicaConProductoAsync`, ni una petición HTTP `DELETE /productos/{id}` con JWT real, ni prueba de escrituras habilitadas bajo la cuarentena P0, ni certifica el cierre global C14-02.M06.06 (archivos/jobs/caché/backups/sesiones). Exigir CI con ejecución verdadera de `Category=Integration` en MySQL 8.4, el Scope Lock y V5 del SHA nuevo. Solo entonces clasificar esta **prueba parcial**; no marcar LISTO ni A5 sin integrar la prueba completa autorizada y cerrar P0/P1. El servicio productivo de variantes, los gates privados y los permisos quedan sin cambios. Rollback seguro del commit: `git revert` normal, no force-push.

## V31 — regresión causal de autoridad vigente y matriz de evidencia (24 noviembre 2026)

La fuente `5147d74354a798de576b0d6ae0e4831d0f625505` acreditó el Fact nominal DELETE mediante GitHub-hosted run 45351565693/job 138299164553: MySQL 8.4.13, 160/160 integraciones, cero fallos/omisiones; manifiesto individual Passed. El diff de nueve commits hasta `60cb78d438d137b77650a77990044786c2bd0ec9` modifica `UsuarioScopeService` y contratos compartidos de autoridad. No extrapolar esa prueba antigua al nuevo contexto. Se amplía el mismo Fact real con dos estados persistidos independientes, membresía desactivada y actor bloqueado, manteniendo el JWT firmado anterior, esperando la denegación 503 del piloto y verificando que producto, variante y auditoría A/B no cambian. Se restaura cada estado en la base efímera y se conservan los casos positivos, revocación de permiso, stock y rollback SQL. No cambia runtime, autorización, esquema, flags ni filtros de CI.

| Superficie | Evidencia real disponible y límite |
| --- | --- |
| DELETE BOLA/IDOR / spoofing JWT | Fact nominal histórico Passed; nueva regresión de autoridad pendiente del SHA publicado. |
| Host y replay | `PublicStoreHostMySqlIsolationIntegrationTests` blob 5a7db3db149aedebc3194e0668077ec71262c527: middleware/Oracle MySQL A/B y replay concurrente; candidato en suite Integration, sin manifiesto nominal recuperado; no certifica host productivo. |
| Sesión pública | `PublicStoreAccountSessionMySqlIsolationIntegrationTests` blob 3fc67da7d26b0372a188ef798c9f0b91398f8409: 200 propio, 401 cruzado y 503 desconocido; falta evidencia nominal y alcance privado completo. |
| Backup | Job 138299013007 fuente 5147d743: restore aislado PASS, 141 tablas, 18 migraciones, 571 FK, 984 índices; no restore productivo (R3). |
| Archivos, jobs, caché | Los preflights y dobles no acreditan almacenamiento/proveedor ni worker durable autenticado; requieren matriz causal de runtime por superficie. |

A0: diff de test/documentación y CHANGELOG, sin cambios en capas, API, datos reales ni dependencias. A1–A4 de esta regresión pendientes de CI exacta; A5 independiente distinta de autor/reparadores. No declarar P0/P1=0, no cerrar M06.06 ni liberar DAG. Rollback: revert normal del changeset; no tocar QA/main/PROD.
