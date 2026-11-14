# ERP-N0.0 — Auditoría de cierre legacy

Fecha: 2026-08-10  
Plan: `PLAN_MAESTRO_ERP_V5`  
Rama auditada: `Desarrollo`  
Baseline auditado: `c7bdb7bf95ae80749adc6c517f9e1a67955d251a`  
Tipo de fase: **AUDITORÍA / PREFLIGHT — NO DESTRUCTIVA**

---

## 1. Objetivo

Inventariar las fuentes de verdad duplicadas y dependencias legacy que deben retirarse gradualmente antes de ampliar Solqaryn con ERP-N1–ERP-N9.

ERP-N0.0 no elimina columnas, tablas, endpoints, enums, DTOs ni compatibilidad histórica. Su propósito es fijar el mapa de impacto, el orden de sustitución y los riesgos del saneamiento.

Ámbitos obligatorios revisados:

1. `Producto` y `CatalogoProducto` legacy;
2. RBAC legacy;
3. referencias polimórficas críticas;
4. métodos de pago hardcodeados;
5. consumidores transaccionales que impiden una eliminación inmediata.

---

## 2. Dictamen ejecutivo

**ERP-N0.0 confirma deuda legacy activa y relevante, pero no demuestra un P0/P1 funcional abierto en el baseline auditado.**

La deuda principal no consiste únicamente en columnas antiguas sin uso. Varias estructuras legacy siguen participando en flujos runtime certificados:

- `ProductoService` escribe todavía campos legacy y agregados de `Producto`;
- `CatalogoProductoService` mantiene CRUD y validación sobre `CatalogoProducto`;
- `CatalogoProductoControllerBase` y los controladores de Marca/Modelo/Color/Talla siguen dependiendo de `ICatalogoProductoService`;
- `AppDbContext` usa `Producto.Cantidad` y `Producto.Costo` en valorización/reversión de compras;
- `RolPermiso` conserva simultáneamente claves relacionales y `Rol/Modulo/Accion` legacy;
- `PermisoService` todavía materializa y consulta matrices usando `ModuloSistema`/`AccionPermiso` además de `PermisoId`;
- movimientos de inventario conservan `ReferenciaTipo + ReferenciaId`;
- movimientos financieros conservan `ModuloOrigen + ReferenciaId` junto con FKs tipadas;
- métodos de pago siguen definidos mediante enum y listas hardcodeadas de frontend.

Por lo tanto, **no es seguro borrar directamente ninguna de estas estructuras**. El trabajo N0 debe seguir el patrón: preflight → dual-read controlado cuando sea necesario → backfill → cambio de autoridad → bloqueo de escritura legacy → reconciliación → eliminación posterior.

---

## 3. Arquitectura objetivo de N0

### Producto

`Producto` debe representar la familia comercial. `ProductoVariante` debe ser la autoridad operacional de SKU, barcode, costo, precio, existencia y atributos de variante.

### Catálogos

`Marca`, `Modelo`, `Color` y `Talla` deben ser entidades/tablas administrables independientes. `CatalogoProducto` no debe continuar como fuente runtime una vez migrados todos los consumidores.

### RBAC

Autoridad objetivo:

`Usuario -> RolId -> Rol -> RolPermiso -> Permiso`

Los enums `ModuloSistema` y `AccionPermiso` pueden seguir existiendo como identificadores técnicos de código si resulta útil, pero no deben constituir una segunda fuente persistida de autorización cuando `PermisoId` ya representa el permiso relacional.

### Referencias de origen

Los movimientos deben resolver su documento origen mediante FKs tipadas o agregados empresariales explícitos. Los campos de texto pueden conservarse como snapshot/diagnóstico si se justifica, pero no como autoridad referencial.

### Métodos de pago

Debe existir un catálogo administrable `MetodoPago` con código técnico estable. Los documentos históricos conservarán snapshot/código compatible para no mutar el pasado.

---

# 4. INVENTARIO — PRODUCTO Y CATÁLOGOS LEGACY

## F-N0-001 — `Producto` mantiene atributos y valores operativos duplicados

**Prioridad:** P2 / ALTA  
**Riesgo:** divergencia entre producto agregado y variante exacta.

| Archivo | Línea/componente | Dependencia actual | Sustituto | Estrategia |
|---|---|---|---|---|
| `backend/src/Domain/Entities/Producto.cs` | propiedades `Marca`, `Modelo` | snapshots/texto legacy dentro de Producto | `Marca`/`Modelo` normalizados vía `ProductoVariante` | conservar solo snapshot si existe justificación histórica; retirar autoridad operativa |
| `backend/src/Domain/Entities/Producto.cs` | `ColorId`, `TallaId`, `MarcaId`, `ModeloId` | FKs legacy hacia `CatalogoProducto` | IDs normalizados en `ProductoVariante` | backfill y cambio de consumidores antes de eliminar |
| `backend/src/Domain/Entities/Producto.cs` | `Cantidad`, `Costo`, `Precio`, `UmbralStockBajo` | valores agregados paralelos a variantes | `ProductoVariante` / futura `ExistenciaVariante` | convertir en derivados/read-model cuando proceda; eliminar escritura independiente |
| `backend/src/Application/Services/ProductoService.cs` | `CreateAsync` | escribe Marca/Modelo, IDs legacy, Cantidad/Costo/Precio | creación de familia + variante explícita | separar creación del producto base de la variante |
| `backend/src/Application/Services/ProductoService.cs` | `UpdateAsync` | sigue actualizando costo/precio y IDs legacy | edición de `ProductoVariante` | bloquear escrituras legacy una vez migrado frontend/API |
| `backend/src/Application/Services/ProductoService.cs` | `ValidarCatalogosAsync`, `ResolverMarcaModeloAsync` | depende de `ICatalogoProductoService` | servicios/repos normalizados Marca/Modelo/Color/Talla | reemplazo por contratos normalizados |
| `backend/src/Infrastructure/Persistence/AppDbContext.cs` | `CapturarSnapshotsValorizacion` / `RestaurarValorizacionAsync` | usa `Producto.Cantidad/Costo` y variante a la vez | valorización centrada en variante/existencia | migrar después de reconciliar históricos de compra |

### Observación crítica

`Producto.Cantidad/Costo` todavía no son simples columnas muertas: intervienen en la lógica de valorización y reversión de compras. Eliminarlas antes de sustituir ese algoritmo podría romper anulaciones históricas y cálculos de inventario.

---

## F-N0-002 — `CatalogoProducto` sigue siendo un subsistema runtime activo

**Prioridad:** P2 / ALTA  
**Riesgo:** dos modelos de catálogo simultáneos.

| Archivo | Línea/componente | Dependencia actual | Sustituto | Estrategia |
|---|---|---|---|---|
| `backend/src/Domain/Entities/CatalogoProducto.cs` | entidad completa | tabla polimórfica Marca/Modelo/Color/Talla | entidades normalizadas | mantener solo durante transición |
| `backend/src/Infrastructure/Persistence/AppDbContext.cs` | `DbSet<CatalogoProducto>` | persiste `CatalogosProducto` junto a `Marcas/Modelos/Colores/Tallas` | DbSets normalizados | retirar DbSet al final de N0.2 |
| `backend/src/Application/Services/CatalogoProductoService.cs` | CRUD completo | crea/edita/desactiva/elimina `CatalogoProducto` | servicios de catálogo normalizados | introducir servicios nuevos y migrar endpoints |
| `backend/src/Application/Services/CatalogoProductoService.cs` | `ValidarSeleccionProductoAsync` | valida atributos de producto contra catálogo genérico | validadores normalizados | sustituir antes de bloquear escritura legacy |
| `backend/src/API/Controllers/CatalogoProductoControllerBase.cs` | clase base | API genérica de catálogos | controladores/servicios normalizados | mantener contratos HTTP compatibles durante transición |
| `backend/src/API/Controllers/ColoresController.cs` y equivalentes | constructor/base | dependen de `ICatalogoProductoService` | servicio `Color/Marca/Modelo/Talla` | migración uno por uno |
| `frontend/src/app/features/catalogos-producto/*` | mantenimiento genérico | UI compartida sobre contrato legacy | componentes compartidos sobre API normalizada | reutilizar UI si conviene, pero cambiar fuente de datos |
| `frontend/src/app/app.routes.ts` | rutas `marcas/modelos/colores/tallas` | cargan feature `catalogos-producto` | API/servicios normalizados | no exige duplicar UI; sí sustituir backend legacy |

### Resultado

Las tablas normalizadas existen, pero `CatalogoProducto` continúa siendo autoridad de varios flujos. N0.2 debe migrar consumidores; no basta con eliminar la entidad.

---

## F-N0-003 — `ProductoVariante` existe pero todavía no es autoridad única

**Prioridad:** P2 / ALTA.

`ProductoVariante` ya concentra ProductoId, MarcaId, ModeloId, ColorId, TallaId, SKU, código de barras, cantidad, costo, precio, umbral e imágenes específicas. La brecha es de autoridad: `Producto` todavía mantiene campos equivalentes y algunos servicios actualizan ambos niveles.

### Estrategia

1. reconciliar cada producto con sus variantes;
2. identificar productos sin variante operativa;
3. crear variante técnica/inicial únicamente cuando el contrato funcional lo requiera;
4. bloquear nuevas escrituras de atributos de variante en `Producto`;
5. cambiar DTOs/reportes/búsquedas a variante;
6. convertir agregados de `Producto` en derivados temporales;
7. eliminar campos legacy solo después de migración certificada.

---

# 5. INVENTARIO — RBAC LEGACY

## F-N0-004 — `Usuario` conserva `Rol` enum junto con `RolId`

**Prioridad:** P2 / ALTA, sensible a seguridad.

| Archivo | Línea/componente | Dependencia | Sustituto | Estrategia |
|---|---|---|---|---|
| `backend/src/Domain/Entities/Usuario.cs` | `Rol` + `RolId` | dos representaciones del rol | `RolId -> Rol` | preflight de usuarios sin RolId/inconsistentes, backfill y bloqueo de escritura enum |

No eliminar `Rol` hasta demostrar que autenticación, claims, seeds, usuario inicial, scopes y pruebas de autorización ya resuelven exclusivamente `RolId`.

---

## F-N0-005 — `RolPermiso` conserva dos modelos simultáneos

**Prioridad:** P2 / ALTA.

| Archivo | Línea/componente | Dependencia | Sustituto | Estrategia |
|---|---|---|---|---|
| `backend/src/Domain/Entities/RolPermiso.cs` | `Rol`, `Modulo`, `Accion`, `RolId`, `PermisoId` | fila híbrida legacy/relacional | `RolId + PermisoId` | backfill obligatorio y constraints no-null cuando sea seguro |
| `backend/src/Application/Services/PermisoService.cs` | `UpdateMatrizAsync` | crea `RolPermiso` rellenando también `Rol=Vendedor`, `Modulo`, `Accion` | permiso relacional | hacer que `PermisoId` sea autoridad; mantener códigos técnicos en `Permiso` |
| `backend/src/Application/Services/PermisoService.cs` | `GetMatrizAsync`, `GetMisPermisosAsync` | reconstruye claves desde `Modulo/Accion` de `RolPermiso` | join `RolPermiso -> Permiso` | migrar lecturas antes de retirar columnas |
| `backend/src/API/Filters/RequierePermisoAttribute.cs` | `ModuloSistema/AccionPermiso` | filtro usa identificadores técnicos y delega en `IPermisoService` | puede mantenerse como contrato typed | no es por sí mismo un defecto si el servicio resuelve a permiso relacional |

### Matiz importante

No se recomienda eliminar automáticamente `ModuloSistema`/`AccionPermiso` del código. Pueden seguir siendo códigos técnicos estables y seguros para atributos y catálogo base. El objetivo es eliminar su duplicación persistida como segunda autoridad en `RolPermiso`.

---

## F-N0-006 — Bypass/validaciones específicas de Administrador

**Prioridad:** P3 / REVISIÓN DE DISEÑO.

Se observaron reglas `EsAdministrador` en permisos y finanzas. Parte de ellas es deliberada: el administrador obtiene acceso total implícito y ciertas revisiones financieras están reservadas a administrador.

### Acción N0

Inventariar cada bypass y clasificarlo como:

- regla empresarial deliberada; o
- autorización hardcodeada que debería expresarse mediante permiso (`Aprobar`, `Cerrar`, `Reabrir`, `Administrar`, etc.).

No sustituir una regla empresarial válida solo por uniformidad estética.

---

# 6. INVENTARIO — REFERENCIAS POLIMÓRFICAS

## F-N0-007 — Movimiento de inventario usa `ReferenciaTipo + ReferenciaId`

**Prioridad:** P2 / ALTA.

| Archivo | Línea/componente | Dependencia | Sustituto | Estrategia |
|---|---|---|---|---|
| `backend/src/Domain/Entities/MovimientoInventario.cs` | `ReferenciaTipo`, `ReferenciaId` | origen sin FK fuerte | CompraId, VentaId, AjusteInventarioId, TransferenciaId, RecepcionId, DevolucionId según documento | introducir FKs nullable + regla de exactamente un origen aplicable |
| `backend/src/Application/Services/InventarioAjusteService.cs` | `AjustarAsync` | genera `AjusteProducto`/`AjusteProductoVariante` como texto y referencia el producto/variante | documento `AjusteInventario` | crear cabecera/detalle, confirmar/anular y enlazar movimiento al documento |

### Hallazgo adicional

Existe servicio de ajuste seguro/concurrente, pero no una entidad documental `AjusteInventario` registrada como agregado en `AppDbContext`. El ajuste actual registra directamente el movimiento y auditoría. N0.7 debe formalizarlo sin perder la lógica de concurrencia ya existente.

---

## F-N0-008 — Movimiento financiero conserva origen dual

**Prioridad:** P2 / ALTA.

| Archivo | Línea/componente | Dependencia | Sustituto | Estrategia |
|---|---|---|---|---|
| `backend/src/Domain/Entities/MovimientoFinanciero.cs` | `ModuloOrigen`, `ReferenciaId`, `CompraId`, `VentaId`, `FacturaId` | dos formas de representar origen | FKs tipadas + tipo/código solo como snapshot si aplica | backfill y constraint de origen coherente |
| `backend/src/Application/Services/FinanzasService.cs` | `RegistrarMovimientoManualAsync` | asigna `ModuloOrigen = "Manual"` | origen manual explícito/nullable con categoría | distinguir movimiento manual de movimiento documental sin ID ficticio |

Debe evitarse un estado donde `ModuloOrigen` diga una cosa y las FKs indiquen otra.

---

# 7. INVENTARIO — MÉTODOS DE PAGO

## F-N0-009 — `MetodoPago` está hardcodeado en dominio y frontend

**Prioridad:** P2 / MEDIA-ALTA.

| Archivo | Línea/componente | Dependencia | Sustituto | Estrategia |
|---|---|---|---|---|
| `backend/src/Domain/Enums/MetodoPago.cs` | enum `Efectivo/Transferencia/Tarjeta/Otro` | catálogo rígido compilado | entidad `MetodoPago` | introducir tabla con `Codigo` estable y compatibilidad enum temporal |
| `backend/src/Domain/Entities/Compra.cs` | `MetodoPago` | enum persistido en compra | `MetodoPagoId` + snapshot/código histórico | migración con mapping 1:1 inicial |
| `backend/src/Domain/Entities/Venta.cs` | `MetodoPago` | enum persistido en venta | `MetodoPagoId` + snapshot | igual |
| `backend/src/Domain/Entities/FacturaPago.cs` | `MetodoPago` | enum persistido por pago | `MetodoPagoId` + snapshot | preservar pagos históricos |
| `backend/src/Domain/Entities/MovimientoFinanciero.cs` | `MetodoPago?` | enum nullable | FK/código normalizado | backfill |
| `backend/src/Application/Services/FinanzasService.cs` | parse `Enum.TryParse<MetodoPago>` | valida contra enum compilado | repositorio/catálogo activo | aceptar código estable administrable |
| `frontend/src/app/features/facturas/factura-pagos.component.ts` | `metodosPago = ['Efectivo','Transferencia','Tarjeta','Otro']` | lista duplicada en UI | endpoint de catálogo | cargar activos, respetar metadata `RequiereReferencia/RequiereBanco/...` |

### Contrato sugerido

`MetodoPago`: Id, Codigo, Nombre, Tipo, Activo, RequiereReferencia, RequiereBanco, PermiteCambio, Orden, Metadata.

Los códigos iniciales deben mapear exactamente a los cuatro valores históricos antes de permitir nuevos métodos.

---

# 8. CONSUMIDORES QUE BLOQUEAN EL BORRADO INMEDIATO

Los siguientes consumidores deben migrarse antes de cualquier DDL destructivo:

1. `ProductoService` — alta/edición/auditoría de Producto.
2. `CatalogoProductoService` — CRUD y validación de catálogos.
3. controladores de Marca/Modelo/Color/Talla — contrato de mantenimiento.
4. frontend `catalogos-producto` y formularios de Producto.
5. `AppDbContext` — snapshots de valorización de compras y reversión.
6. servicios/repositorios de Compra/Venta/Inventario que consolidan stock/costo de Producto.
7. `PermisoService`, repositorios RBAC, seeds y resolución de scope.
8. `InventarioAjusteService` y repositorio de movimientos.
9. `FinanzasService`, entidades Compra/Venta/FacturaPago/MovimientoFinanciero.
10. pruebas unitarias, integración, MySQL y Playwright que fijan contratos actuales.

---

# 9. PREFLIGHT OBLIGATORIO PARA N0.1–N0.8

Antes de modificar esquema:

### Producto/Variante

- productos sin variantes;
- productos con más de una variante y valores agregados divergentes;
- SKU/barcode duplicados;
- atributos legacy sin correspondencia normalizada;
- cantidad Producto vs suma de variantes;
- costo/precio Producto vs política derivada;
- históricos de compra/venta/factura que dependan de snapshots.

### Catálogos

- mapping `CatalogosProducto -> Marcas/Modelos/Colores/Tallas` por ID;
- modelos sin marca o con padre inválido;
- nombres duplicados normalizados;
- productos/variantes apuntando a IDs incompatibles.

### RBAC

- usuarios sin `RolId`;
- `RolId` inexistente/inactivo;
- `RolPermiso` sin `PermisoId`;
- duplicados `RolId + PermisoId`;
- discrepancias entre `Modulo/Accion` y la fila `Permiso` apuntada;
- roles/seeds/claims que todavía leen `Usuario.Rol`.

### Referencias

- inventario con tipos de referencia desconocidos;
- IDs huérfanos;
- movimientos financieros con texto/FKs contradictorios;
- movimientos automáticos sin documento origen demostrable.

### Métodos de pago

- todos los valores históricos deben mapear a código normalizado;
- valores fuera de enum o serializaciones antiguas;
- reportes/exportaciones que dependan del texto exacto.

Cualquier discrepancia no reconciliable debe abortar fail-closed la migración correspondiente.

---

# 10. ORDEN DE IMPLEMENTACIÓN RECOMENDADO DENTRO DE ERP-N0

1. **N0.1 — Producto legacy:** inventario detallado + reconciliación + autoridad de variante.
2. **N0.2 — CatalogoProducto:** APIs/repos/servicios normalizados y migración de consumidores.
3. **N0.3 — ProductoVariante:** constraints y autoridad única operativa.
4. **N0.4 — RBAC:** backfill `RolId/PermisoId`, lecturas relacionales, retiro de autoridad enum.
5. **N0.5 — MetodoPago:** tabla/códigos, backfill, frontend administrable.
6. **N0.6 — Referencias tipadas:** inventario/finanzas con reglas de origen coherente.
7. **N0.7 — AjusteInventario:** documento formal, estados, detalles, confirmación/anulación.
8. **N0.8 — Limpieza final:** bloqueo de escritura, verificación, DDL seguro y retiro físico de legacy que haya quedado sin consumidores.

N0.8 no puede borrar una estructura mientras exista un consumidor runtime, una FK histórica no migrada o una prueba que demuestre dependencia válida.

---

# 11. PRUEBAS Y GATES REQUERIDOS

Cada subfase N0 debe mantener como mínimo:

- build Release backend;
- pruebas backend;
- build producción frontend;
- lint/TypeScript;
- migración desde cero;
- upgrade desde esquema representativo anterior;
- pruebas MySQL reales/descartables;
- autorización RBAC fail-closed;
- regresión de compras/ventas/inventario/facturas;
- Playwright relevante;
- reconciliación de datos antes/después;
- no pérdida de snapshots históricos;
- regresión M0–M13 verde;
- P0/P1 = 0.

---

# 12. RIESGOS PRINCIPALES

### R1 — Romper anulaciones de compra

Eliminar `Producto.Cantidad/Costo` antes de reemplazar la lógica de snapshots/reversión puede invalidar anulaciones certificadas.

**Mitigación:** migrar algoritmo de valorización primero y probar históricos.

### R2 — Catálogos con IDs incompatibles

Las entidades normalizadas y `CatalogoProducto` pueden tener espacios de ID distintos.

**Mitigación:** tabla/mapa de equivalencia explícito; nunca asumir identidad por nombre.

### R3 — Pérdida de autorización

Hacer no-null `RolId/PermisoId` o retirar enums sin backfill puede dejar usuarios sin acceso o ampliar acceso por error.

**Mitigación:** matriz comparativa pre/post por rol y suites de autorización.

### R4 — Movimiento sin trazabilidad

Migrar referencias polimórficas sin identificar todos los `ReferenciaTipo/ModuloOrigen` existentes puede generar huérfanos.

**Mitigación:** inventario de valores distintos y reconciliación antes de FK.

### R5 — Ruptura de históricos por métodos de pago

Renombrar/desactivar un método administrable no debe reescribir documentos pasados.

**Mitigación:** código/snapshot histórico inmutable.

---

# 13. ELEMENTOS NO CLASIFICADOS COMO DEFECTO

No se deben eliminar por confundirlos con legacy:

- snapshots históricos de cliente/proveedor/producto/variante;
- totales confirmados de documentos;
- códigos técnicos estables de permisos si solo sirven para resolver `Permiso`;
- auditoría JSON;
- datos documentales inmutables;
- lógica de administrador deliberada mientras esté explícitamente justificada y probada.

---

# 14. ACCIONES REALIZADAS EN ERP-N0.0

- lectura del modelo actual en `Desarrollo`;
- lectura de servicios/APIs representativos y consumidores runtime;
- clasificación de deuda Producto/Catálogo/RBAC/Referencias/Métodos de pago;
- definición de riesgos, sustitutos y estrategia;
- definición del preflight y orden de implementación.

## Acciones NO realizadas

- no se modificó código productivo;
- no se modificaron entidades;
- no se ejecutaron migraciones;
- no se modificó MySQL/Aiven;
- no se modificó Render/Vercel/Cloudinary;
- no se modificó `main`;
- no se fusionó PR #2;
- no se tocó Producción.

---

# 15. CRITERIO DE CIERRE ERP-N0.0

**ERP-N0.0 — COMPLETADA a nivel de auditoría estática/preflight del baseline indicado.**

El resultado demuestra que ERP-N0 debe ejecutarse de manera incremental. La primera implementación segura es **ERP-N0.1 — saneamiento de Producto legacy**, comenzando por reconciliación de `Producto` vs `ProductoVariante` y mapa de consumidores, sin DDL destructivo inicial.

ERP-N0 global **NO está completada**: N0.1–N0.8 siguen pendientes.

---

## 16. Próximo checkpoint

**ERP-N0.1 — Producto legacy / autoridad de ProductoVariante.**

Primer entregable técnico recomendado:

1. consultas/preflight de reconciliación;
2. matriz por campo legacy y consumidor;
3. política exacta de derivación de Cantidad/Costo/Precio;
4. plan de backfill;
5. cambios pequeños de lectura/escritura;
6. pruebas de compra/venta/anulación antes de retirar cualquier columna.
