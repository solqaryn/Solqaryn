# ERP-N0.8 — Migraciones y limpieza — Preflight

## Dictamen de N0.8.A

**Estado de esta microtarea:** preflight técnico listo para certificación operativa.

**Rama:** `Desarrollo`.

**Baseline técnico inspeccionado:** `610ebbf9e0d4e65e1861bf5ff7917dd925a8c86d`.

**Dependencias VAEP verificadas:** `N0.5.15 = LISTO`, `N0.6.H = LISTO`, `N0.7.H = LISTO`.

ERP-N0.8 no es una autorización para borrar columnas o tablas. Su función es consolidar la deuda de compatibilidad que los puntos anteriores dejaron deliberadamente viva, demostrar qué puede retirarse, qué debe migrarse primero y qué debe conservarse como evidencia histórica.

La regla de seguridad del punto es:

```text
preflight -> backup/restauración verificable -> migración/backfill -> reconciliación -> cambio de consumidores -> postcheck -> retiro físico
```

Ningún `DROP`, rename destructivo ni eliminación de contrato es elegible si existe un consumidor runtime, una dependencia histórica o un dato no reconciliado.

## 1. Fuente ejecutable del preflight

Script:

`backend/scripts/preflight-erp-n0-8-migraciones-limpieza.sql`

El primer changeset `916447b9f9d6ee0fc732ccd688807563962ff9fe` incorporó el inventario base. La revisión de N0.8.A detectó que esa versión no incluía `Productos` ni los campos críticos `ReferenciaTipo/ReferenciaId` y `ModuloOrigen`; por tanto no era suficiente para cerrar A.

La corrección `610ebbf9e0d4e65e1861bf5ff7917dd925a8c86d` amplía el script para:

- inventariar las tablas realmente involucradas;
- separar deuda de compatibilidad de snapshots históricos;
- enumerar autoridades relacionales/tipadas esperadas;
- detectar regresión de estructuras que N0.2/N0.4 ya eliminaron;
- preservar `Roles.EsAdministrador` como metadato deliberado, no como bypass;
- listar FKs, índices, historia EF, triggers y vistas;
- declarar explícitamente que un `PASS` del script significa únicamente que el inventario pudo ejecutarse en un contexto válido, **no** que sea seguro ejecutar un `DROP`.

El script es de solo lectura y no contiene DDL/DML destructivo.

## 2. Estado real consolidado

### 2.1 Producto y ProductoVariante

`ProductoVariante` es la autoridad operativa ya certificada para SKU, código de barras, stock, costo, precio, umbral y dimensiones.

Sin embargo, `Producto` todavía conserva físicamente:

- `Marca`;
- `Modelo`;
- `Cantidad`;
- `Costo`;
- `Precio`;
- `UmbralStockBajo`;
- `ColorId`;
- `TallaId`;
- `MarcaId`;
- `ModeloId`.

Estas propiedades no son autoridad operativa nueva, pero todavía no son columnas muertas.

Bloqueo confirmado actual:

- `CompraService` continúa actualizando la proyección `Producto.Cantidad/Costo` al confirmar compras;
- `AppDbContext.PrepararValorizacionComprasAsync`, `CapturarSnapshotsValorizacion` y `RestaurarValorizacionAsync` siguen usando `Producto.Cantidad/Costo` para snapshots y reversión segura de compras;
- el contrato familiar/frontend todavía expone proyecciones de Producto por compatibilidad.

**Conclusión:** N0.8 no puede retirar todavía esas columnas. Primero debe trasladar completamente la valorización/reversión hacia variante/snapshots ya persistidos, demostrar equivalencia histórica y retirar consumidores del contrato legacy.

### 2.2 CatalogoProducto legacy

N0.2 ya eliminó correctamente:

- entidad persistente `CatalogoProducto`;
- `DbSet<CatalogoProducto>`;
- configuración EF legacy;
- tabla `CatalogosProducto`;
- escritura espejo.

Los nombres públicos `CatalogoProductoService`, interfaces, DTOs y base de controller permanecen como fachada nominal de compatibilidad sobre maestros normalizados.

**Conclusión:** `CatalogosProducto` debe seguir **ausente**. Su reaparición es regresión/drift y debe bloquear N0.8. Los nombres públicos no se eliminan por simple coincidencia de texto; se retiran solo cuando no existan consumidores de contrato.

### 2.3 RBAC

N0.4 ya retiró físicamente:

- `Usuarios.Rol`;
- `RolPermisos.Rol`;
- `RolPermisos.Modulo`;
- `RolPermisos.Accion`;
- `RolPermisos.Permitido`.

La autoridad es:

`Usuario.RolId -> Rol -> RolPermiso.PermisoId -> Permiso`.

`Roles.EsAdministrador` permanece como metadato/invariante de bootstrap. No constituye bypass de autorización y **no es candidato automático de limpieza**.

**Conclusión:** N0.8 debe comprobar ausencia de las columnas legacy, no recrearlas ni volver a migrar RBAC.

### 2.4 MetodoPago

La autoridad relacional está implantada en Venta, FacturaPago y MovimientoFinanciero, pero queda deuda física/contractual:

- `Venta` conserva `MetodoPago` enum + `MetodoPagoId`;
- `FacturaPago` conserva `MetodoPago` enum + `MetodoPagoId` y snapshots de código/nombre;
- `MovimientoFinanciero` conserva `MetodoPago` enum + `MetodoPagoId`;
- **`Compra` conserva únicamente `MetodoPago` enum y su configuración EF lo persiste como string; no existe todavía `MetodoPagoId` en la entidad/configuración.**

`CompraService` sigue parseando el enum desde DTO y, al confirmar, propaga ese enum al `MovimientoFinanciero` automático.

Esto impide retirar de forma segura las columnas enum legacy del resto del sistema: antes debe existir una autoridad relacional equivalente para Compra y deben migrarse sus DTOs/servicio/repositorio/históricos.

Los snapshots `MetodoPagoCodigoSnapshot`/`MetodoPagoNombreSnapshot` de FacturaPago representan historia inmutable y **no deben borrarse por confundirse con doble autoridad**.

**Conclusión:** N0.8 debe completar la migración relacional de Compra y luego retirar los campos enum persistidos únicamente después de backfill, postcheck y consumidores cero.

### 2.5 Origen de MovimientoInventario

N0.6/N0.7 introdujeron físicamente las columnas tipadas:

- `CompraId`;
- `VentaId`;
- `ConsumoInsumoId`;
- `AjusteInventarioId`.

La escritura relacional moderna usa `AddConOrigenTipadoAsync` y esos IDs son la autoridad de base de datos.

No obstante, el modelo EF `MovimientoInventario` todavía expone `ReferenciaTipo/ReferenciaId`, y `MovimientoInventarioRepository`:

- genera esos snapshots en cada escritura tipada;
- inserta las FKs tipadas mediante SQL explícito;
- lee FKs tipadas mediante ADO.NET/raw SQL;
- conserva fallback `ReferenciaTipo/ReferenciaId` para providers no relacionales/pruebas.

**Conclusión:** antes de retirar `ReferenciaTipo/ReferenciaId`, N0.8 debe consolidar el contrato tipado dentro del modelo/persistencia o demostrar otra arquitectura equivalente, migrar las pruebas/fallbacks y obtener consumidores legacy cero. Borrar primero rompería tests y compatibilidad, aunque la base ya tenga FKs tipadas.

### 2.6 Origen de MovimientoFinanciero

La autoridad relacional actual usa:

- `CompraId`;
- `VentaId`;
- `FacturaId`.

`ModuloOrigen/ReferenciaId` permanecen documentados y configurados como snapshot de auditoría/correlación.

**Conclusión:** N0.8 no presupone que deban eliminarse. Debe decidir explícitamente entre:

1. conservarlos como snapshot auditivo estable, con invariantes que impidan contradicción; o
2. retirarlos si se demuestra que auditoría/correlación ya dispone de un sustituto completo y no hay consumidores.

No se autoriza un `DROP` por estética de normalización.

### 2.7 AjusteInventario y endpoints legacy

`AjusteInventario` ya es la única autoridad de negocio para el ciclo Borrador -> Confirmado -> Anulado.

Los endpoints legacy:

- `POST /productos/{productoId}/ajustes-stock`;
- `POST /productos/{productoId}/variantes/{varianteId}/ajustes-stock`

siguen presentes como superficie HTTP compatible, pero delegan en el servicio formal y ya no constituyen una segunda autoridad de stock.

**Conclusión:** N0.8 puede deprecar/retirar esta superficie solo después de confirmar consumidores frontend/externos cero. Su existencia no bloquea la integridad de inventario mientras siga siendo un adaptador puro.

## 3. Matriz de decisión

| Área | Estado actual | Acción N0.8 |
|---|---|---|
| `CatalogosProducto` tabla | Eliminada en N0.2 | Debe permanecer ausente; aparición = drift |
| RBAC `Rol/Modulo/Accion/Permitido` persistido | Eliminado en N0.4 | Debe permanecer ausente |
| `Roles.EsAdministrador` | Metadato deliberado | Conservar |
| Producto legacy/proyecciones | Físicas y aún consumidas | Migrar consumidores + reconciliar + retirar solo al final |
| `ProductoVariante` | Autoridad operativa | Conservar |
| Compra `MetodoPago` enum | Autoridad aún persistida | Migrar a `MetodoPagoId` antes de retirar enum |
| Venta/FacturaPago/MovimientoFinanciero `MetodoPago` enum | Compatibilidad junto a FK | Backfill/postcheck + retirar tras Compra/consumidores |
| Snapshots de MetodoPago | Historia inmutable | Conservar |
| MovimientoInventario FKs tipadas | Autoridad DB | Conservar y consolidar mapping/contrato |
| `ReferenciaTipo/ReferenciaId` inventario | Snapshot/bridge + fallback tests | Retirar solo tras consumidores/fallback cero |
| MovimientoFinanciero FKs tipadas | Autoridad | Conservar |
| `ModuloOrigen/ReferenciaId` financiero | Snapshot auditoría/correlación | Mantener salvo prueba explícita de retiro seguro |
| Endpoints `ajustes-stock` | Fachada HTTP a autoridad formal | Deprecar/retirar solo con consumidores cero |

## 4. Alcance de ERP-N0.8

Incluye:

- reconciliar deuda física y contractual residual de ERP-N0.1–N0.7;
- preflight de esquema/datos e historia EF;
- backup/restauración verificable antes de migraciones destructivas;
- backfill determinista y fail-closed;
- eliminación de consumidores legacy antes de eliminar columnas;
- postchecks de equivalencia e integridad;
- rollback/forward recovery documentado;
- pruebas de migración sobre MySQL efímero/Desarrollo, nunca Producción;
- reconciliación de DTOs/API/frontend únicamente cuando la limpieza lo requiera.

No incluye:

- nuevas funciones ERP-N1+;
- refactors cosméticos sin deuda demostrada;
- borrar snapshots históricos válidos;
- reabrir N0.2/N0.4 ya cerrados;
- eliminar `EsAdministrador` por uniformidad;
- tocar `main`, Producción, secretos, dominios o infraestructura productiva;
- merge/auto-merge del PR #2.

## 5. Descomposición recomendada para B–H

N0.8 cruza varias autoridades y no debe implementarse como un changeset gigante.

### N0.8.B — dominio y contratos

Resolver únicamente contratos necesarios para la limpieza, previsiblemente:

- autoridad relacional `MetodoPago` en Compra;
- consolidación tipada del origen de `MovimientoInventario` dentro del modelo/contrato cuando proceda;
- definición explícita de qué propiedades de Producto seguirán siendo read-model/snapshot durante la transición.

### N0.8.C — persistencia, migración y datos

Debe subdividirse antes de editar si mantiene los tres concerns independientes:

- **C1 Producto**: preflight de equivalencia, traslado de valorización/reversión, reconciliación y retiro físico seguro;
- **C2 MetodoPago**: `Compra.MetodoPagoId`, backfill por código estable, constraints/postcheck y retiro enum físico cuando todos los consumidores estén migrados;
- **C3 Orígenes inventario**: mapping/constraints/postcheck y retiro de `ReferenciaTipo/ReferenciaId` cuando no exista fallback runtime;
- cualquier decisión sobre `ModuloOrigen/ReferenciaId` financiero debe ser una microtarea separada y solo si existe evidencia para retirarlo.

Una migración destructiva no debe mezclar Producto + MetodoPago + origen de movimientos en un único changeset.

### N0.8.D — aplicación/API

Migrar repositorios/servicios/DTOs y retirar bridges solo después de que el esquema de transición exista.

### N0.8.E — frontend/UX

N/A para estructuras internas si no hay contrato afectado; obligatorio si se retiran campos de Producto, MetodoPago enum o endpoints legacy consumidos por Angular.

### N0.8.F — seguridad/auditoría/observabilidad

No reabrir RBAC. Validar que la limpieza no pierda trazabilidad histórica ni correlation/auditoría.

### N0.8.G — QA/CI

Ejecutar migraciones desde historia representativa, preflight/postcheck, integración MySQL, contrato/API, frontend y E2E cuando aplique. Ningún fallo se oculta mediante exclusión de suites.

### N0.8.H — documentación/certificación

Cerrar únicamente cuando no queden dobles autoridades operativas ni columnas marcadas para retiro sin una decisión explícita documentada.

## 6. Estrategia de backup y rollback

### 6.1 Antes de cualquier cambio destructivo

Requisitos mínimos:

1. drill M11 de backup/restauración vigente y verde;
2. snapshot lógico de los datos que serán transformados, con PK y tipos explícitos compatibles con `sql_require_primary_key=ON`;
3. conteos/checksums o reconciliación 1:1 antes y después;
4. historia EF exacta registrada;
5. script de preflight con bloqueos en cero para la microtarea concreta.

### 6.2 Rollback

No asumir que `Down()` puede reconstruir identidades/catálogos/históricos después de un `DROP`.

Para limpiezas no reversibles determinísticamente:

- migración `forward-only` fail-closed;
- restauración desde backup certificado como ruta de retorno al esquema anterior;
- preferir corrección forward si ya existen datos nuevos escritos con el esquema nuevo.

Producción permanece fuera de alcance.

## 7. Riesgos principales

### R1 — romper anulaciones históricas de compra

Probabilidad/impacto: alto si se eliminan `Producto.Cantidad/Costo` antes de migrar `AppDbContext`.

Control: trasladar la restauración a snapshots/variante y probar compras confirmadas/anuladas con historia representativa.

### R2 — perder semántica de MetodoPago en Compra

Compra todavía no posee FK relacional. Un DROP del enum antes de backfill haría el documento irreconciliable.

Control: migrar por código estable, fail-closed ante valores desconocidos y preservar snapshots documentales.

### R3 — eliminar referencias legacy antes de migrar fallbacks

`MovimientoInventarioRepository` todavía usa `ReferenciaTipo/ReferenciaId` en provider no relacional y como snapshot de escritura.

Control: consumidores cero + pruebas tipadas + postcheck antes del DROP.

### R4 — borrar snapshots confundidos con deuda

Snapshots de Producto/MetodoPago/origen pueden ser necesarios para explicar documentos históricos.

Control: clasificación KEEP explícita en preflight y revisión por propósito, no por nombre.

### R5 — reabrir RBAC/catalogo ya saneados

Control: `N0.8_EXPECTED_ABSENT` debe fallar si reaparecen `CatalogosProducto` o columnas RBAC retiradas.

### R6 — migración gigante

Control: subdividir N0.8.C/D por concern y certificar causalmente cada tramo.

## 8. Matriz de pruebas requerida

### Preflight

- ejecutar `preflight-erp-n0-8-migraciones-limpieza.sql` en MySQL 8.x de Desarrollo/efímero;
- verificar estructuras esperadas ausentes;
- registrar autoridades presentes/ausentes;
- inventariar snapshots/FKs/índices/triggers/vistas/historia EF.

### Producto

- equivalencia `Producto` proyección vs variantes;
- confirmación/anulación de compra preserva stock/costo histórico;
- producto simple y comercial;
- transición de variante técnica;
- reportes y DTOs sin lectura operacional legacy.

### MetodoPago

- Compra backfill enum -> catálogo por código estable;
- valores desconocidos bloquean;
- Venta/FacturaPago/MovimientoFinanciero conservan FK y snapshots;
- documentos históricos siguen mostrando el método correcto tras editar/desactivar catálogo.

### Orígenes

- cada movimiento documental posee exactamente una FK tipada aplicable;
- mismatch legacy/tipado falla cerrado durante transición;
- una vez retirado el bridge, ninguna decisión usa texto legacy;
- manual financiero sigue representable sin FK ficticia.

### Regresión transversal

- build backend con warnings como error;
- unitarias;
- integración MySQL;
- snapshot EF sin drift;
- SQL forward/idempotencia cuando aplique;
- frontend lint/build;
- aceptación Playwright si cambia contrato;
- M13 final.

## 9. Criterios de salida de N0.8.A

A puede marcarse `LISTO` cuando:

- [x] dependencias N0.5.15/N0.6.H/N0.7.H están cerradas;
- [x] existe preflight SQL read-only;
- [x] el preflight incluye Producto, MetodoPago, orígenes, RBAC ya retirado, snapshots, FKs, índices, EF history, triggers y vistas;
- [x] alcance/fuera de alcance están definidos;
- [x] consumidores y bloqueos actuales están identificados;
- [x] riesgos y rollback están definidos;
- [x] matriz de pruebas está definida;
- [x] no se ejecutó DDL/DML destructivo;
- [x] Producción/main/merge/secretos permanecen intocados;
- [ ] el changeset técnico `610ebbf9...` debe reconciliarse con CI causal antes de cerrar el estado operativo en VAEP.

## 10. Siguiente punto

Cuando el CI causal del preflight técnico quede reconciliado, `N0.8.A` puede cerrarse y queda elegible `N0.8.B`.

N0.8.B no debe empezar borrando columnas. Debe resolver primero los contratos de autoridad que todavía impiden una migración segura, especialmente Compra/MetodoPago y el origen tipado de MovimientoInventario.
