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
