# ERP-N1.2 — Almacenes empresariales

Fecha de cierre técnico: 2026-08-14  
Plan rector: `PLAN_MAESTRO_ERP_V5`  
Rama: `Desarrollo`  
Estado: **✅ CIERRE TÉCNICO COMPLETADO**

---

## 1. Objetivo y alcance final

ERP-N1.2 incorpora el maestro empresarial `Almacen` como hijo obligatorio de `Sucursal`, con soporte explícito para cinco clasificaciones operativas:

```text
Tienda      = 1
Bodega      = 2
Transito    = 3
Devolucion  = 4
Cuarentena  = 5
```

La jerarquía autoritativa queda:

```text
Sucursal 1 ── N Almacen
```

`Almacen.SucursalId` es la única relación organizacional persistida en N1.2. No se duplica `EmpresaId` en Almacén: el contexto futuro de empresa se deriva de Sucursal y la autoridad multiempresa/tenant se reserva para ERP-N6.

### Fuera de alcance deliberado

N1.2 **no** convierte Almacén en autoridad de existencias. Permanecen fuera de esta fase:

- ubicaciones internas/racks/bin: ERP-N1.3;
- existencias por almacén: ERP-N1.4;
- movimientos/transferencias por almacén: fases posteriores de inventario;
- migración de `ProductoVariante.Cantidad`: ERP-N1.4;
- semántica tenant/multiempresa y aislamiento por empresa: ERP-N6.

Esta separación evita introducir una segunda autoridad de stock o de tenant antes de que sus respectivos agregados estén diseñados y migrados.

---

## 2. Preflight y decisiones arquitectónicas

La auditoría N1.2.A confirmó que el baseline no contenía entidad, DbSet, tabla ni API legacy `Almacen`, `Bodega` o `Ubicacion`. Por tanto, la implementación es aditiva y no requiere backfill histórico.

Reglas fijadas en preflight:

1. todo Almacén pertenece obligatoriamente a una Sucursal;
2. crear o mover un Almacén exige Sucursal existente y activa;
3. reactivar un Almacén exige que su Sucursal continúe activa;
4. desactivar un Almacén es válido aun cuando la Sucursal permanezca activa;
5. edición ordinaria no cambia estado operativo;
6. activar/desactivar se realizan mediante operaciones separadas e idempotentes;
7. eliminación es lógica y auditable;
8. N1.2 no toca existencias ni movimientos de inventario;
9. N1.2 no introduce `EmpresaId` propio en Almacén.

Durante N1.2.B un runner concurrente introdujo temporalmente `EmpresaId` nullable en `Almacen`/DTOs. La reconciliación detectó que esa propiedad duplicaba la futura autoridad de empresa ya reservada en `Sucursal.EmpresaId`. Se corrigió forward-only en `85f2b845ca60d8e797425bd5b0f9a7d597a6cfa8` y se añadió una guarda de dominio que impide reintroducir `EmpresaId` en `Almacen`, `AlmacenDto`, `CreateAlmacenDto` y `UpdateAlmacenDto` antes de ERP-N6.

---

## 3. Dominio y contratos

Entidad principal:

```text
Almacen : AuditableEntity
- Id
- SucursalId                 requerido
- Sucursal                   navegación requerida
- Codigo                     requerido
- Nombre                     requerido
- Tipo : TipoAlmacen         requerido
- Activo                     default true
- Eliminado                  soft-delete
- FechaEliminacion
- EliminadoPorUsuarioId
- campos de auditoría heredados
```

Enum estable `TipoAlmacen`:

```text
Tienda = 1
Bodega = 2
Transito = 3
Devolucion = 4
Cuarentena = 5
```

Contratos HTTP/aplicación:

- `AlmacenDto`;
- `CreateAlmacenDto`;
- `UpdateAlmacenDto`;
- `TipoAlmacenDto`;
- `AlmacenFiltroDto`;
- `AlmacenPaginaDto`.

Los DTOs exponen `SucursalId`, código/nombre de Sucursal, código/nombre/tipo/estado del Almacén y trazabilidad básica. No exponen `EmpresaId` propio ni cantidades de stock.

---

## 4. Persistencia, migración e integridad

Configuración EF:

```text
backend/src/Infrastructure/Persistence/Configurations/AlmacenConfiguration.cs
```

Migración física:

```text
20260814192931_N1_2_AlmacenPersistencia
```

Snapshot:

```text
backend/src/Infrastructure/Migrations/AppDbContextModelSnapshot.cs
```

### 4.1 Tabla e índices

Tabla `Almacenes`:

- PK `Id`;
- FK requerida `SucursalId`;
- `Codigo varchar(40)`;
- `Nombre varchar(150)`;
- `Tipo int`;
- `Activo`;
- soft-delete y auditoría.

Relación física:

```text
FK_Almacenes_Sucursales_SucursalId
Almacenes.SucursalId -> Sucursales.Id
ON DELETE RESTRICT
```

Índices:

```text
UX_Almacenes_Codigo_Activo
IX_Almacenes_SucursalId
IX_Almacenes_Tipo_Estado
```

El código activo único usa una columna computada equivalente a:

```sql
IF(Eliminado = 0, UPPER(TRIM(Codigo)), NULL)
```

Esto permite reutilizar un código solamente después de una baja lógica y evita duplicados operativos activos.

### 4.2 Constraints

La migración exige:

- código no vacío;
- nombre no vacío;
- `Tipo BETWEEN 1 AND 5`.

### 4.3 Preflight y postcheck

El preflight falla cerrado si ya existe una tabla `Almacenes` no reconciliada y exige que `Sucursales` exista antes de crear la FK.

No existe backfill: el snapshot lógico previo es vacío porque no había histórico Almacén/Bodega.

El postcheck verifica:

- tabla `Almacenes`;
- los tres índices esperados;
- FK exacta a `Sucursales`;
- los tres checks físicos.

### 4.4 Rollback

`Down()` es fail-closed: si `Almacenes` contiene filas, el rollback se detiene antes de `DROP TABLE`.

Política final:

- Producción no fue modificada;
- no se autoriza un DROP improvisado con datos;
- ante datos reales, el rollback seguro es corrección forward o restauración controlada desde un respaldo compatible;
- una reversión destructiva solo es admisible en un entorno descartable y con tabla vacía.

### 4.5 Snapshot EF

Se utilizó EF Core 8.0.8 para generar el delta canónico y luego se reconcilió el snapshot condensado usado por el repositorio. El workflow temporal de generación tuvo `permissions: contents: read` y fue retirado antes del HEAD final de C.

Validación final de C:

```text
HEAD: bebafe3abb2ddc66448c805b107f8d1f8ee3f3e9
CI: 31834214669
Aplicar migraciones actuales MySQL 8.4: SUCCESS
Integración MySQL: SUCCESS
has-pending-model-changes: SUCCESS
Backend Release/unit: SUCCESS
Frontend/Docker/higiene: SUCCESS
```

---

## 5. Aplicación, servicio y API

Componentes:

```text
IAlmacenRepository / AlmacenRepository
IAlmacenService / AlmacenService
AlmacenValidators
AlmacenesController
```

API:

```text
GET    /almacenes
GET    /almacenes/activos
GET    /almacenes/tipos
GET    /almacenes/{id}
POST   /almacenes
PUT    /almacenes/{id}
PATCH  /almacenes/{id}/activar
PATCH  /almacenes/{id}/desactivar
DELETE /almacenes/{id}
```

### 5.1 Consultas

`GET /almacenes` soporta:

- búsqueda por código/nombre de Almacén y código/nombre de Sucursal;
- `SucursalId`;
- `Tipo`;
- `Activo`;
- paginación defensiva hasta 100 filas.

`GET /almacenes/activos` devuelve únicamente Almacenes activos cuya Sucursal también está activa.

`GET /almacenes/tipos` expone el catálogo enum estable para evitar listas hardcodeadas divergentes en frontend.

### 5.2 Reglas fail-closed

Crear:

- `SucursalId > 0`;
- Sucursal existente;
- Sucursal activa;
- tipo válido;
- código no duplicado.

Editar:

- puede modificar Sucursal/código/nombre/tipo;
- mover hacia otra Sucursal exige que la nueva Sucursal esté activa;
- no muta `Activo`.

Activar:

- exige Sucursal vigente activa.

Desactivar:

- operación separada;
- repetir el mismo estado no escribe ni audita de nuevo.

Eliminar:

- soft-delete;
- fuerza `Activo=false`;
- conserva trazabilidad.

---

## 6. RBAC, auditoría y seguridad

Nuevo módulo técnico:

```text
ModuloSistema.Almacenes = 29
```

Permisos relacionales seedables:

```text
Almacenes:Ver
Almacenes:Crear
Almacenes:Editar
Almacenes:Activar
Almacenes:Desactivar
Almacenes:EliminarLogico
```

Todos los endpoints usan `[Authorize]` + `RequierePermiso`. La autorización efectiva continúa dependiendo exclusivamente de grants persistidos `RolPermiso -> Permiso`; no se añadió bypass administrativo.

Auditoría de mutaciones:

```text
Entidad = Almacen
Modulo = Almacenes
Accion = Crear | Editar | Activar | Desactivar | EliminarLogico
ReferenciaId = Almacen.Id
```

La infraestructura global agrega usuario, IP, User-Agent y CorrelationId.

---

## 7. Observabilidad

`/almacenes` fue incorporado a `MedirRendimientoBusquedaFilter`.

La métrica registra:

- ruta;
- duración;
- P50/P95;
- muestras;
- longitud del término;
- cantidad de resultados;
- estado HTTP;
- CorrelationId.

No registra:

- término de búsqueda;
- nombre/código concreto;
- teléfono;
- correo;
- otros datos personales.

`CorrelationIdMiddleware`, `/health` y `/health/ready` continúan siendo transversales.

---

## 8. Frontend y UX

Frontend Angular:

```text
core/models/almacen.model.ts
services/almacen.service.ts
features/almacenes/almacenes-list.component.*
features/almacenes/almacen-form.component.*
features/almacenes/almacenes.routes.ts
```

### 8.1 Lista

Incluye:

- búsqueda server-side;
- filtro Sucursal;
- filtro tipo;
- filtro estado;
- paginación real;
- loading/error/vacío;
- reintento;
- acciones condicionadas por RBAC;
- activar/desactivar separado de editar;
- confirmación de soft-delete;
- tabla desktop;
- cards móviles sin overflow horizontal.

### 8.2 Formulario

Incluye:

- selector de Sucursal activa desde `/sucursales/activas`;
- selector de tipo desde `/almacenes/tipos`;
- código/nombre;
- validación accesible;
- loading/error;
- no modifica stock;
- no expone EmpresaId.

Al editar un Almacén cuyo padre histórico está inactivo, la Sucursal actual se conserva como opción visual marcada `inactiva · solo conservación`; el backend impide mover otro almacén hacia una Sucursal inactiva y también impide reactivar el Almacén bajo ella.

### 8.3 Rutas y menú

Rutas protegidas:

```text
/almacenes                  Almacenes:Ver
/almacenes/nuevo            Almacenes:Crear
/almacenes/:id/editar       Almacenes:Editar
```

Menú visible únicamente con `Almacenes:Ver`.

Durante QA se detectó que registrar `ALMACENES_ROUTES` mediante `provideRoutes()` las colocaba después del wildcard `**` de `app.routes.ts`. La corrección final usa:

```ts
provideRouter([...ALMACENES_ROUTES, ...routes])
```

por lo que Almacenes se resuelve antes del wildcard.

M10 sobre `3a1b8004f2120c4be6459bb46fd120eff8704fe9`:

```text
run 31835928799 — SUCCESS
API/MySQL: SUCCESS
Angular lint/build: SUCCESS
Playwright WCAG/teclado/responsive: SUCCESS
```

---

## 9. QA, regresión y CI dedicado

Workflow permanente:

```text
.github/workflows/n1-2-almacenes-ci.yml
ERP-N1.2 - Certificación Almacenes
```

Cobertura E2E:

1. acceso anónimo `401` y CorrelationId autenticado;
2. creación/normalización y auditoría;
3. duplicado fail-closed;
4. catálogo de cinco tipos;
5. filtros/paginación/activos;
6. desactivación idempotente sin auditoría duplicada;
7. Sucursal inactiva bloquea reactivación de Almacén;
8. edición cambia nombre/tipo sin mutar estado;
9. rutas/UI protegidas y mantenimiento visible;
10. responsive móvil sin overflow;
11. soft-delete oculta el registro y conserva auditoría.

### 9.1 Fallos reales encontrados y corregidos

#### Intento 1 — harness API/UI inconsistente

```text
run 31836552560
```

Resultado:

- 6 pruebas API: PASS;
- login UI: FAIL;
- soft-delete: no ejecutado por serialización.

Causa: el workflow levantaba API en `5006`, mientras el frontend de desarrollo consumía `http://localhost:5005`.

Corrección forward-only:

```text
3049cfdf637eb1c1d2fb0be7f9881e517a3cf13f
```

El workflow quedó alineado a API `5005`.

#### Intento 2 — ruta Almacenes después del wildcard

```text
run 31836970704
```

Resultado:

- 6 pruebas API: PASS;
- login UI: PASS;
- navegación `/almacenes`: FAIL por no encontrar el encabezado;
- soft-delete: no ejecutado por serialización.

Causa: `provideRoutes(ALMACENES_ROUTES)` agregaba las rutas después del wildcard `**` ya registrado.

Corrección forward-only:

```text
053152ae51de3617bf30a4e9987574c7879e3049
```

Rutas de Almacenes pasan antes del conjunto que contiene el wildcard.

#### Certificación final

```text
HEAD: 053152ae51de3617bf30a4e9987574c7879e3049
run: 31837394309
job: 94886619205
resultado: SUCCESS
Playwright: 8 passed / 0 failed / 0 skipped
```

El run final certificó:

- restore;
- build Release `-warnaserror`;
- 376 pruebas backend;
- API;
- migraciones MySQL 8.4;
- health/ready;
- npm ci;
- lint;
- build producción;
- Chromium;
- Angular;
- E2E N1.2 completo;
- publicación de evidencia.

---

## 10. Trazabilidad A–H

```text
A  preflight read-only
B  e451c5b7... -> e366198e... -> c91aaeed... -> 2ce9f58e... -> 85f2b845...
C  d457db74... -> 6b78fd83... -> d0fd6aec... -> 8523d41d... -> bebafe3a...
D  5a97bf3844069a565e1aecf39e4b8001c10f386b
E  3a1b8004f2120c4be6459bb46fd120eff8704fe9
F  30c7e9ff1dedf69eb860916b92b1d5bee0941084
G  f6f51bb6d0d5d1910e9561de30d934b30fa2d83e
   3049cfdf637eb1c1d2fb0be7f9881e517a3cf13f
   053152ae51de3617bf30a4e9987574c7879e3049
H  este documento + reconciliación TASKS/CHANGELOG/VAEP
```

---

## 11. Definition of Done

- [x] auditoría/preflight completados;
- [x] dominio y contratos definidos;
- [x] relación obligatoria a Sucursal;
- [x] cinco tipos operativos estables;
- [x] EmpresaId duplicada retirada y protegida por test;
- [x] persistencia EF y migración aditiva;
- [x] preflight/postcheck fail-closed;
- [x] rollback destructivo bloqueado con datos;
- [x] snapshot EF sin drift;
- [x] API CRUD/filtros/paginación;
- [x] activar/desactivar idempotente;
- [x] Sucursal inactiva bloquea crear/mover/reactivar;
- [x] soft-delete;
- [x] RBAC relacional seedable;
- [x] auditoría;
- [x] correlation/health;
- [x] observabilidad sin PII;
- [x] frontend administrable;
- [x] rutas/permisos UI;
- [x] responsive/accesibilidad M10;
- [x] CI dedicado permanente;
- [x] E2E específico 8/8;
- [x] fallos reales corregidos y documentados;
- [x] `main` y Producción intactos;
- [x] PR #2 permanece Draft/unmerged;
- [x] sin force-push ni ramas nuevas.

---

## 12. Continuidad

**ERP-N1.2 queda formalmente cerrado.**

El siguiente foco FINISH_FIRST elegible es:

```text
N1.3.A — Ubicaciones internas / auditoría y preflight
```

N1.3 deberá modelar la topología interna bajo Almacén sin adelantar todavía la autoridad de cantidades de N1.4. `Almacen` debe permanecer maestro de ubicación física/lógica, no stock agregado, hasta que N1.4 introduzca existencias normalizadas por Almacén/Ubicación.
