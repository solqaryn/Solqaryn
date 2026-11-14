# ERP-N1.3.A — Ubicaciones internas — Auditoría y preflight

Fecha: 2026-08-14  
Proyecto: Solqaryn  
Repositorio: `jmejia31/Solqaryn`  
Rama: `Desarrollo`  
Baseline inspeccionado: `d25752bfc7a9727e1fc531ed849b8fd61344bc24`  
Estado: **PRELIGHT COMPLETADO — SIN CAMBIOS FUNCIONALES**

---

## 1. Objetivo

Definir el contrato técnico de ERP-N1.3 antes de implementar código o persistencia. N1.3 incorpora `UbicacionAlmacen` para modelar la topología interna de un `Almacen`: pasillo, estante, rack, sección, bin y otras ubicaciones físicas/lógicas equivalentes.

N1.3 **no** introduce existencias, cantidades, reservas ni movimientos por ubicación. Esa autoridad pertenece a ERP-N1.4 y fases posteriores.

---

## 2. Evidencia de auditoría dirigida

La inspección del baseline confirmó:

- no existe entidad `UbicacionAlmacen`;
- no existe entidad o contrato denominado `Ubicacion` aplicable a inventario;
- no existen referencias de código a `rack` como concepto de almacenamiento;
- `Almacen` ya existe y depende obligatoriamente de `Sucursal`;
- `Almacen` documenta expresamente que ubicaciones internas pertenecen a ERP-N1.3;
- `ProductoVariante` todavía conserva `Cantidad` y `UmbralStockBajo`; N1.3 no debe modificar esa autoridad legacy;
- el Plan Maestro reserva ERP-N1.4 para `ExistenciaVariante` por `ProductoVariante + Almacen (+ Ubicacion opcional)`.

Conclusión: **N1.3 es aditivo y no requiere backfill histórico de ubicaciones**.

---

## 3. Jerarquía autoritativa

La topología objetivo queda:

```text
Sucursal
└── Almacen
    └── UbicacionAlmacen
        └── UbicacionAlmacen hija (opcional, recursiva)
```

Una ubicación pertenece a **un solo Almacén**. La jerarquía interna puede representar, por ejemplo:

```text
Bodega Central
└── Pasillo A
    └── Rack A-01
        └── Estante 03
            └── Bin A-01-03-02
```

La autorreferencia permite representar pasillo/estante/rack/sección/bin sin crear una tabla distinta para cada nivel físico.

---

## 4. Dominio propuesto para N1.3.B

Entidad propuesta:

```text
UbicacionAlmacen : AuditableEntity
- Id
- AlmacenId                  requerido
- Almacen                    navegación requerida
- UbicacionPadreId           nullable
- UbicacionPadre             navegación opcional
- Codigo                     requerido
- Nombre                     requerido
- Tipo                       TipoUbicacionAlmacen
- Activa                     default true
- Eliminado                  soft-delete
- FechaEliminacion
- EliminadoPorUsuarioId
```

Enum propuesto y estable:

```text
Pasillo   = 1
Estante   = 2
Rack      = 3
Seccion   = 4
Bin       = 5
Otra      = 6
```

No se incorporan campos especulativos de capacidad, volumen, peso, picking, temperatura o stock en N1.3.

---

## 5. Invariantes obligatorias

### 5.1 Pertenencia

- `AlmacenId > 0`.
- El Almacén debe existir.
- Crear, mover o reactivar una ubicación exige Almacén activo.
- No se duplica `SucursalId` ni `EmpresaId` en `UbicacionAlmacen`; ambos contextos se derivan por `UbicacionAlmacen -> Almacen -> Sucursal`.

### 5.2 Código

- `Codigo` requerido y normalizado `Trim + UpperInvariant`.
- Un código activo debe ser único **dentro de su Almacén**, no globalmente.
- El mismo código puede existir en almacenes distintos.
- Tras soft-delete puede reutilizarse según la misma estrategia de código activo computado ya utilizada en Sucursal/Almacén.

### 5.3 Jerarquía interna

- `UbicacionPadreId` es opcional.
- Una ubicación no puede ser su propio padre.
- El padre debe pertenecer al mismo Almacén.
- El padre debe estar activo para crear/mover/reactivar un hijo.
- Deben rechazarse ciclos directos o indirectos.
- Mover una ubicación no puede convertir a uno de sus descendientes en ancestro.

### 5.4 Estado y eliminación

- Activar/desactivar es una operación separada de editar.
- Repetir el mismo estado debe ser idempotente y no duplicar auditoría.
- Desactivar una ubicación con descendientes activos debe fallar cerrado; no se realizará cascada silenciosa de estado.
- Eliminar lógicamente una ubicación con descendientes no eliminados debe fallar cerrado.
- No habrá `DELETE CASCADE` desde Almacén ni desde ubicación padre.

---

## 6. Persistencia propuesta para N1.3.C

Tabla prevista:

```text
UbicacionesAlmacen
```

Relaciones:

```text
UbicacionesAlmacen.AlmacenId -> Almacenes.Id                RESTRICT
UbicacionesAlmacen.(AlmacenId, UbicacionPadreId)
    -> UbicacionesAlmacen.(AlmacenId, Id)                    RESTRICT
```

La FK compuesta propuesta evita físicamente que un hijo apunte a un padre de otro Almacén y deja preparada la misma clave `(AlmacenId, Id)` para que N1.4 pueda exigir que una `ExistenciaVariante` y su ubicación opcional pertenezcan al mismo Almacén.

Índices previstos:

```text
UX_UbicacionesAlmacen_Almacen_Codigo_Activo
IX_UbicacionesAlmacen_AlmacenId
IX_UbicacionesAlmacen_Padre
IX_UbicacionesAlmacen_Tipo_Estado
```

Checks previstos:

```text
Codigo no vacío
Nombre no vacío
Tipo BETWEEN 1 AND 6
UbicacionPadreId IS NULL OR UbicacionPadreId <> Id
```

Los ciclos indirectos se validarán en la capa de aplicación porque un `CHECK` local no puede recorrer el grafo jerárquico.

---

## 7. Preflight, migración y rollback

Dado que no existe histórico de ubicaciones:

1. el preflight físico debe fallar cerrado si ya existe una tabla `UbicacionesAlmacen` no reconciliada;
2. debe exigir `Almacenes` antes de crear la FK;
3. no hay backfill;
4. el postcheck debe verificar tabla, FKs, índices y checks exactos;
5. `Down()` debe fallar cerrado si la tabla contiene filas;
6. Producción permanece fuera de alcance.

Rollback seguro con datos: corrección forward o restauración controlada desde respaldo compatible; no se autoriza `DROP TABLE` improvisado.

---

## 8. Contrato de aplicación/API previsto

Superficie propuesta:

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

Filtros mínimos:

- búsqueda por código/nombre;
- `AlmacenId`;
- `UbicacionPadreId`/raíz;
- `Tipo`;
- `Activa`;
- paginación server-side.

Para UX se podrá presentar la administración desde `/almacenes/:almacenId/ubicaciones`, aunque el recurso HTTP siga siendo independiente.

---

## 9. RBAC, auditoría y observabilidad previstos

Nuevo módulo propuesto:

```text
ModuloSistema.UbicacionesAlmacen = 30
```

Permisos mínimos:

```text
UbicacionesAlmacen:Ver
UbicacionesAlmacen:Crear
UbicacionesAlmacen:Editar
UbicacionesAlmacen:Activar
UbicacionesAlmacen:Desactivar
UbicacionesAlmacen:EliminarLogico
```

Auditoría:

```text
Entidad = UbicacionAlmacen
ReferenciaId = UbicacionAlmacen.Id
Accion = Crear | Editar | Activar | Desactivar | EliminarLogico
```

La búsqueda deberá incorporarse a la métrica segura P50/P95 sin registrar términos ni PII y conservar CorrelationId/health transversales.

---

## 10. Frontend/UX previsto

La UI debe incluir:

- selector de Almacén activo;
- vista jerárquica o lista con padre visible;
- filtros por Almacén, padre, tipo y estado;
- formulario con código, nombre, tipo y ubicación padre opcional;
- solo padres del mismo Almacén;
- padres inactivos no seleccionables para nuevas relaciones;
- conservación visual del padre histórico al editar si quedó inactivo;
- estados loading/error/vacío;
- responsive sin overflow;
- acciones condicionadas por RBAC;
- ninguna cantidad/stock en este módulo.

---

## 11. Interacción con ERP-N1.4

N1.3 debe dejar preparado, pero **no implementar**, el siguiente contrato:

```text
ExistenciaVariante
- ProductoVarianteId
- AlmacenId
- UbicacionAlmacenId nullable
- stock físico/reservado/disponible/tránsito/mínimo/máximo
```

Regla futura esencial: si `UbicacionAlmacenId` no es nulo, la ubicación debe pertenecer al mismo `AlmacenId` de la existencia.

N1.3 no migra `ProductoVariante.Cantidad`, no crea existencias y no cambia movimientos.

---

## 12. Riesgos principales

1. **Cruce de almacenes en jerarquía** — mitigación: FK compuesta + validación de aplicación.
2. **Ciclos jerárquicos** — mitigación: traversal fail-closed antes de guardar/mover.
3. **Desactivación/eliminación de padres con hijos activos** — mitigación: rechazo explícito, sin cascada silenciosa.
4. **Doble autoridad de contexto** — mitigación: no duplicar SucursalId/EmpresaId.
5. **Adelantar stock N1.4** — mitigación: prohibir cantidades/reservas/movimientos en N1.3.
6. **Códigos globalmente restrictivos** — mitigación: unicidad por Almacén.
7. **Jerarquía demasiado rígida** — mitigación: autorreferencia + enum de tipo, sin columnas físicas por nivel.

---

## 13. Criterios de aceptación N1.3

- ubicación normalizada bajo Almacén;
- jerarquía recursiva sin cruces/ciclos;
- tipos Pasillo/Estante/Rack/Seccion/Bin/Otra estables;
- códigos activos únicos por Almacén;
- soft-delete y estados idempotentes;
- FK/constraints/índices fail-closed;
- API + filtros/paginación;
- RBAC relacional y auditoría;
- frontend responsive/accesible;
- observabilidad sin PII;
- CI/E2E dedicado;
- cero modificación de stock/ProductoVariante.Cantidad;
- `main`/Producción/PR #2 merge/auto-merge intactos.

---

## 14. Secuencia autorizada B–H

```text
N1.3.B  Dominio y contratos
N1.3.C  Persistencia, migración y datos
N1.3.D  Aplicación, servicios y API
N1.3.E  Frontend y UX
N1.3.F  RBAC, auditoría, seguridad y observabilidad
N1.3.G  QA, regresión y CI
N1.3.H  Documentación y certificación
```

**N1.3.A queda técnicamente listo.** El siguiente punto elegible es `N1.3.B — Ubicaciones internas / Dominio y contratos`.
