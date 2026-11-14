# ERP-N1.3 — Ubicaciones internas de almacén

Fecha de cierre técnico: 2026-08-14  
Plan rector: `PLAN_MAESTRO_ERP_V5`  
Rama: `Desarrollo`  
Estado: **✅ CIERRE TÉCNICO COMPLETADO**

---

## 1. Objetivo y alcance final

ERP-N1.3 incorpora el maestro jerárquico `UbicacionAlmacen` para modelar la topología física interna de cada Almacén: pasillos, estantes, racks, secciones, bins y ubicaciones equivalentes.

La jerarquía autoritativa queda:

```text
Sucursal 1 ── N Almacen 1 ── N UbicacionAlmacen
                              └── jerarquía padre/hija opcional
```

`UbicacionAlmacen.AlmacenId` es la única relación organizacional persistida por este agregado. `SucursalId` y `EmpresaId` no se duplican: ambos contextos se derivan transitivamente desde `Almacen` y `Sucursal`.

### Fuera de alcance deliberado

N1.3 **no** convierte la ubicación en autoridad de stock. Permanecen fuera de esta fase:

- `ExistenciaVariante` por Almacén/Ubicación: ERP-N1.4;
- migración de `ProductoVariante.Cantidad`: ERP-N1.4;
- transferencias y movimientos por ubicación: fases posteriores de inventario;
- reservas, picking, packing y reglas WMS avanzadas: fases posteriores;
- aislamiento tenant/multiempresa: ERP-N6.

Esta separación impide introducir una segunda autoridad de cantidad antes del diseño y migración formal de existencias.

---

## 2. Preflight y decisiones arquitectónicas

La auditoría N1.3.A quedó documentada en:

```text
docs/ERP_N1_3_UBICACIONES_PREFLIGHT.md
```

El baseline no contenía entidad, tabla ni consumidor legacy `UbicacionAlmacen`, `Ubicacion` o `Rack`. La implementación pudo ser aditiva.

Decisiones fijadas:

1. toda Ubicación pertenece obligatoriamente a un Almacén;
2. el padre es opcional;
3. un padre debe pertenecer al mismo Almacén;
4. un padre operativo debe estar activo;
5. se prohíben ciclos directos e indirectos;
6. una ubicación con hijas no puede moverse a otro Almacén;
7. una ubicación con hijas activas no puede desactivarse;
8. una ubicación con hijas no eliminadas no puede eliminarse lógicamente;
9. activar/desactivar son operaciones separadas e idempotentes;
10. N1.3 no persiste cantidades, existencias ni movimientos.

---

## 3. Dominio y contratos

Entidad principal:

```text
UbicacionAlmacen : AuditableEntity
- Id
- AlmacenId                  requerido
- Almacen                    navegación requerida
- UbicacionPadreId           opcional
- UbicacionPadre              navegación opcional
- Codigo                     requerido
- Nombre                     requerido
- Tipo : TipoUbicacionAlmacen
- Activa
- Eliminado                  soft-delete
- FechaEliminacion
- EliminadoPorUsuarioId
- auditoría heredada
```

Tipos estables:

```text
Pasillo
Estante
Rack
Seccion
Bin
Otro
```

Contratos de aplicación/HTTP:

- `UbicacionAlmacenDto`;
- `CreateUbicacionAlmacenDto`;
- `UpdateUbicacionAlmacenDto`;
- filtros y paginación server-side;
- catálogo dinámico de tipos.

Los DTOs exponen contexto de Almacén y padre para UX, pero no exponen `SucursalId`, `EmpresaId` ni cantidades propias.

---

## 4. Persistencia, migración e integridad

Configuración EF:

```text
backend/src/Infrastructure/Persistence/Configurations/UbicacionAlmacenConfiguration.cs
```

Migración física:

```text
20260814211647_N1_3_UbicacionAlmacenPersistencia
```

La persistencia incluye:

- FK requerida a `Almacenes` con `RESTRICT`;
- relación autorreferente jerárquica;
- clave alternativa `(AlmacenId, Id)`;
- FK compuesta `(AlmacenId, UbicacionPadreId)` para impedir padres de otro Almacén;
- código operativo único dentro del Almacén;
- índices de consulta por Almacén, padre, tipo y estado;
- checks físicos compatibles con MySQL 8.4.

### 4.1 Self-parent en MySQL 8.4

MySQL 8.4 no permite que un `CHECK` referencie la columna `AUTO_INCREMENT` utilizada como `Id`. La invariante `UbicacionPadreId <> Id` se preservó físicamente mediante triggers fail-closed de inserción/actualización, en lugar de relajar la regla de dominio.

La validación final de C confirmó:

- generación EF válida;
- snapshot sin drift;
- build y unitarias verdes;
- historial completo aplicado sobre MySQL 8.4;
- triggers e índices postcheck presentes;
- helpers temporales retirados antes del cierre.

---

## 5. Aplicación, servicio y API

Componentes principales:

```text
IUbicacionAlmacenRepository / UbicacionAlmacenRepository
IUbicacionAlmacenService / UbicacionAlmacenService
UbicacionAlmacenValidators
UbicacionesAlmacenController
```

Superficie API:

```text
GET    /ubicaciones-almacen
GET    /ubicaciones-almacen/activas
GET    /ubicaciones-almacen/tipos
GET    /ubicaciones-almacen/{id}
POST   /ubicaciones-almacen
PUT    /ubicaciones-almacen/{id}
PATCH  /ubicaciones-almacen/{id}/activar
PATCH  /ubicaciones-almacen/{id}/desactivar
DELETE /ubicaciones-almacen/{id}
```

Las consultas soportan búsqueda, Almacén, padre, raíz, tipo, estado y paginación defensiva.

Las mutaciones validan fail-closed:

- Almacén existente y operativo;
- padre existente, activo y del mismo Almacén;
- código no duplicado dentro del Almacén;
- tipo válido;
- ausencia de ciclos;
- protección de descendientes en movimientos, desactivación y baja lógica.

---

## 6. RBAC, auditoría, seguridad y observabilidad

Módulo RBAC:

```text
UbicacionesAlmacen
```

Permisos por endpoint:

- lectura: `Ver`;
- creación: `Crear`;
- actualización: `Editar`;
- activación: `Activar`;
- desactivación: `Desactivar`;
- eliminación lógica: `EliminarLogico`.

Todas las mutaciones registran auditoría mediante `IAuditoriaService`, con entidad `UbicacionAlmacen`, referencia y acción relacional. La auditoría de éxito se emite únicamente cuando la persistencia confirma la operación.

La superficie reutiliza la infraestructura transversal ya certificada:

- autenticación/autorización global;
- `CorrelationIdMiddleware`;
- manejo global de excepciones/ProblemDetails;
- headers de seguridad;
- health/live y health/ready.

Las pruebas de N1.3.F congelan los 9 contratos de autorización del controller y las acciones de auditoría de las mutaciones.

---

## 7. Frontend y UX

Se incorporó módulo Angular dedicado:

```text
frontend/src/app/features/ubicaciones-almacen/
```

Incluye:

- listado responsive con paginación y filtros server-side;
- filtros por Almacén, padre/raíz, tipo y estado;
- formulario de alta/edición;
- selector de Almacén activo;
- selector de padre opcional restringido al mismo Almacén;
- preservación del contexto histórico al editar;
- acciones visibles según RBAC;
- rutas registradas antes del wildcard;
- acceso `Ubicaciones` en el menú principal protegido por `UbicacionesAlmacen:Ver`.

No existe ningún campo frontend de cantidad/stock en este módulo.

---

## 8. Evidencia de commits y CI

Commits/hitos principales:

```text
N1.3.A  a8e6ec0c...  preflight y diseño
N1.3.B  683e137c...  dominio y contratos
N1.3.C  b229ac2e...  persistencia/migración final
         1ace9e78...  regresión de metadatos EF
         eb163d7f...  corrección de prueba EF
N1.3.D  4d2cc04b...  aplicación, servicios, API y DI
N1.3.E  91f878ef...  frontend/UX y menú RBAC
N1.3.F  4a6be386...  regresiones RBAC/auditoría
```

Evidencia causal destacada:

- N1.3.D: run `31843085895`, Backend Release y pruebas job `94903923345` — **SUCCESS**;
- N1.3.E: run `31846161956`, Frontend producción job `94912936660` — **SUCCESS**;
- N1.3.F/G: run `31846485117` — **SUCCESS** agregado:
  - higiene `94913888918`;
  - backend Release/unitarias `94913888850`;
  - frontend producción `94913888865`;
  - Docker/aislamiento `94913888808`;
  - MySQL 8.4 + migraciones + integración `94913888844`.

El job MySQL ejecutó migraciones actuales, suite `Category=Integration`, verificación de snapshot/variantes/cargas y generación SQL forward sin regresiones.

Baseline funcional certificado previo a la reconciliación documental H:

```text
4a6be38683f03fc2076f18a71115480c930ba79b
```

---

## 9. Resultado final y siguiente foco

ERP-N1.3 queda técnicamente completo y certificado como **topología interna de almacenes**, sin introducir stock prematuramente.

Siguiente foco obligatorio del Plan Maestro:

```text
N1.4.A — ExistenciaVariante — Preflight y diseño
```

ERP-N1.4 será responsable de introducir la autoridad de existencias por Almacén/Ubicación y de diseñar la transición desde `ProductoVariante.Cantidad` sin doble autoridad.
