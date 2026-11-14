# ERP-N1.4 — Existencias por variante

## 1. Alcance y autoridad

ERP-N1.4 establece `ExistenciaVariante` como autoridad de stock vivo por combinación de:

- `ProductoVarianteId`;
- `AlmacenId`;
- `UbicacionAlmacenId` opcional.

La existencia no duplica `SucursalId` ni `EmpresaId`: ambos se derivan transitivamente desde el almacén. La autoridad operativa de cantidad física es `ExistenciaVariante.StockFisico`; `ProductoVariante.Cantidad` queda únicamente como bridge/agregado de compatibilidad durante la transición.

`StockDisponible` no es una entrada independiente. El dominio lo deriva como `StockFisico - StockReservado` y rechaza estados inválidos antes de mutar la entidad: stock físico/reservado/tránsito/mínimo negativos, reservado mayor que físico y máximo menor que mínimo.

## 2. Modelo operativo

Campos de stock relevantes:

| Campo | Semántica |
| --- | --- |
| `StockFisico` | Cantidad física autoritativa en la existencia. |
| `StockReservado` | Cantidad comprometida que no está disponible para nuevas salidas. |
| `StockDisponible` | Derivado de físico menos reservado. |
| `StockTransito` | Cantidad en tránsito; no incrementa disponible hasta su materialización. |
| `StockMinimo` | Umbral operativo para alerta de stock bajo. |
| `StockMaximo` | Límite configurado opcional. |

Indicadores del dominio:

- `TieneStockBajo`: `StockDisponible <= StockMinimo`.
- `EstaAgotada`: `StockDisponible <= 0`.

Las operaciones de ajuste confirmadas/anuladas fueron migradas para trabajar sobre existencias físicas bajo locking de concurrencia, preservando claves por almacén/ubicación y evitando colapsar múltiples ubicaciones de una misma variante.

## 3. Backend y API

La superficie REST está protegida por autenticación y permisos relacionales del módulo `Inventario`:

| Método | Ruta | Permiso |
| --- | --- | --- |
| `GET` | `/existencias-variante` | `Inventario:Ver` |
| `GET` | `/existencias-variante/{id}` | `Inventario:Ver` |
| `POST` | `/existencias-variante` | `Inventario:Crear` |
| `PUT` | `/existencias-variante/{id}/configuracion` | `Inventario:Editar` |

El servicio valida que:

- la variante exista, esté activa y no eliminada;
- el almacén exista, esté activo y pertenezca a una sucursal activa;
- la ubicación opcional exista, esté activa y pertenezca al mismo almacén;
- no exista otra fila con la misma clave variante + almacén + ubicación;
- la persistencia confirme `SaveChangesAsync()` antes de emitir auditoría de éxito.

## 4. Seguridad, auditoría y observabilidad

La API exige `[Authorize]`; no existe bypass `AllowAnonymous` en los endpoints de existencias. Los permisos se validan con `RequierePermiso` y acciones relacionales `Ver`, `Crear` y `Editar`.

Las altas y cambios de configuración registran auditoría sobre la entidad `ExistenciaVariante`, con usuario de creación/actualización y referencia del registro. Si la persistencia no se confirma, la operación falla y no registra una auditoría falsa de éxito.

La observabilidad transversal conserva `X-Correlation-ID`/`TraceIdentifier` y la auditoría almacena el identificador de correlación junto con IP y User-Agent. Los endpoints `/health` y `/health/ready` continúan siendo los probes operativos de API y disponibilidad de base de datos.

## 5. Frontend y UX

N1.4.E incorporó la gestión de existencias al frontend con:

- modelo y servicio Angular para existencias;
- listado y ruta de navegación;
- formulario de alta/configuración;
- selector Producto → Variante;
- selector Almacén → Ubicación;
- permisos de UI para crear/editar;
- estados de loading, vacío y error;
- comportamiento responsive y controles de accesibilidad.

La certificación M10 correspondiente validó compilación backend, lint/build frontend y Playwright sobre el changeset de UI.

## 6. Evidencia técnica A–G

### N1.4.D — Aplicación, servicios y API

Cierre certificado por CI causal `#31896166358` sobre `394b4ded95cdbfdd5b9798519ee220d6f9b185bd`. Confirmar/Anular quedaron operando sobre `ExistenciaVariante.StockFisico` y preservando las claves físicas multiubicación.

### N1.4.E — Frontend y UX

Commits principales:

- `c08d41e6912640aa3bf60eeb9f499735d37691f5`;
- `74ee40fda65c1df96fde84fb81f8a59b6f027b2e`.

M10 `#31897833340`: `SUCCESS`.

### N1.4.F — RBAC, auditoría, seguridad y observabilidad

Commit principal:

- `c237f00d51be40b0070e35695aaffcfedca4bf44`.

Se incorporaron pruebas de autenticación/permisos y auditoría de `ExistenciaVariante`, incluyendo fail-closed cuando la persistencia no confirma el cambio.

### N1.4.G — QA, regresión y CI

Commits de cierre:

- `8d6e8baaeb50f06155e92acf1cd9e58fb3719358` — regresión de mutación atómica/umbrales;
- `f88d46a706a437b3e6944859a7e206e933d15969` — invariantes adicionales de stock físico/disponible;
- `0c1dc2178e083246594206783a6f6270ba597260` — alineación de regresión con la semántica autoritativa de stock bajo.

CI causal `Desarrollo - Compilación y pruebas #31904290239`: `SUCCESS` completo. Incluyó:

- Backend Release y pruebas unitarias;
- Docker y aislamiento;
- Frontend lint/build de producción;
- higiene del repositorio;
- historial de migraciones MySQL;
- preflight fail-closed;
- migraciones actuales;
- pruebas de integración MySQL;
- verificación de variante/cargas/snapshot;
- generación SQL forward.

## 7. Estrategia de rollback

El rollback de código se realiza exclusivamente sobre `Desarrollo` mediante reversión explícita de los commits causales, preservando la historia Git. No se autoriza force-push ni reescritura de historia.

Para datos/migraciones:

1. no ejecutar rollback destructivo automáticamente en Producción;
2. validar primero el impacto histórico y las referencias a existencias;
3. conservar la trazabilidad de `ProductoVariante + Almacen + UbicacionAlmacen`;
4. cualquier operación productiva requiere autorización separada y procedimiento operativo específico.

## 8. Restricciones de despliegue

Este cierre certifica el punto en `Desarrollo`; no autoriza:

- merge a `main`;
- auto-merge;
- cambios en Producción;
- ejecución de migraciones productivas;
- modificación de secretos, dominios o infraestructura productiva.

PR #2 debe permanecer abierto y Draft hasta autorización expresa.

## 9. Estado de cierre

A–G de ERP-N1.4 cuentan con evidencia funcional y CI causal. N1.4.H consolida esta documentación y debe cerrarse únicamente cuando este changeset documental quede publicado/certificado y el tablero VAEP registre la evidencia final y el siguiente punto elegible.
