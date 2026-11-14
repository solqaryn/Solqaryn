# ERP-N1.9 — Series, lotes y vencimientos

## Estado

**DOCUMENTACIÓN CANÓNICA / N1.9.H EN CERTIFICACIÓN**

Baseline funcional certificado para QA:

```text
4b5a5c9a8b495fcef62464bf50010ac69117fe48
```

N1.9 introduce trazabilidad opcional por lote, número de serie y vencimiento sin convertir esas identidades en una segunda autoridad cuantitativa. `ExistenciaVariante` continúa siendo la autoridad agregada por `ProductoVarianteId + AlmacenId + UbicacionAlmacenId`.

## 1. Política opt-in

`ProductoVariante` persiste la política `ControlaLote`, `ControlaNumeroSerie`, `ControlaFechaVencimiento` y `DiasAlertaVencimiento`. La trazabilidad no es obligatoria para variantes que no la habilitan.

Reglas principales:

- vencimiento requiere control de lote;
- la activación de una dimensión nueva sobre stock físico existente falla cerrado y exige adopción/reconciliación explícita;
- no se puede desactivar lote con lotes activos ni serie con series activas;
- no se puede habilitar vencimiento si existen lotes activos sin fecha de vencimiento;
- la configuración idempotente no persiste ni audita de nuevo.

## 2. Dominio

### LoteInventario

Identidad por variante + código de lote normalizado, con fabricación/vencimiento opcionales y lifecycle activo/inactivo. Un lote no contiene un total de stock independiente.

### SerieInventario

Identidad serial única, ligada a `ProductoVariante` y opcionalmente a un lote compatible. El número de serie se normaliza, tiene longitud máxima de 120 caracteres y mantiene lifecycle controlado (`Disponible`, `Reservada`, `EnTransito`, `Vendida`, `Baja`).

## 3. Persistencia

N1.9.C incorpora las columnas opt-in en `ProductoVariante`, tablas `LotesInventario` y `SeriesInventario`, índices/constraints y FKs restrictivas. El snapshot EF y la migración canónica quedan alineados con el modelo runtime.

La migración es aditiva: no inventa lotes ni series históricas y no ejecuta backfill heurístico.

## 4. Aplicación y API

La superficie canónica es `TrazabilidadInventarioController`, ruta base `/trazabilidad-inventario`, autenticación obligatoria y RBAC relacional por operación.

| Método | Ruta | Propósito | Permiso |
|---|---|---|---|
| GET | `/trazabilidad-inventario/variantes/{id}/configuracion` | consultar política | `MovimientosInventario:Ver` |
| PUT | `/trazabilidad-inventario/variantes/{id}/configuracion` | configurar política | `MovimientosInventario:Editar` |
| GET | `/trazabilidad-inventario/lotes` | listar/filtrar lotes | `MovimientosInventario:Ver` |
| GET | `/trazabilidad-inventario/lotes/{id}` | detalle lote | `MovimientosInventario:Ver` |
| POST | `/trazabilidad-inventario/lotes` | crear lote | `MovimientosInventario:Crear` |
| PUT | `/trazabilidad-inventario/lotes/{id}` | editar lote | `MovimientosInventario:Editar` |
| POST | `/trazabilidad-inventario/lotes/{id}/desactivar` | desactivar lote | `MovimientosInventario:Anular` |
| GET | `/trazabilidad-inventario/series` | listar/filtrar series | `MovimientosInventario:Ver` |
| GET | `/trazabilidad-inventario/series/{id}` | detalle serie | `MovimientosInventario:Ver` |
| POST | `/trazabilidad-inventario/series` | crear serie | `MovimientosInventario:Crear` |
| POST | `/trazabilidad-inventario/series/{id}/baja` | dar de baja serie | `MovimientosInventario:Anular` |

## 5. Concurrencia e idempotencia

Las mutaciones de configuración, lote y serie se ejecutan dentro de transacciones. La variante y las identidades involucradas se bloquean en orden estable cuando corresponde. La unicidad persistente protege carreras concurrentes; si una identidad ya existe con el mismo payload, la operación puede resolverse idempotentemente, y si el payload difiere falla cerrado.

## 6. Seguridad, auditoría y observabilidad

N1.9.F exige auditoría estricta dentro de la misma transacción de negocio para cambios de política, alta/edición/desactivación de lotes y alta/baja de series. Si la auditoría no puede persistirse, la operación crítica no debe confirmarse.

La auditoría evita exponer `Codigo` de lote o `NumeroSerie` como secretos de negocio innecesarios en payloads. El `CorrelationId` procede del `TraceIdentifier` saneado por la plataforma, no del header bruto suministrado por el cliente. No existe `[AllowAnonymous]` en la superficie N1.9.

## 7. Frontend/UX

N1.9.E incorpora mantenimiento y consulta de configuración, lotes, series y vencimientos de forma opcional, respetando permisos y estados. La UX no sustituye las validaciones fail-closed del backend.

## 8. QA y evidencia

Baseline QA: `4b5a5c9a8b495fcef62464bf50010ac69117fe48`.

Gates confirmados sobre ese SHA:

```text
Desarrollo - Compilación y pruebas #32086058893  SUCCESS
Fase 8                              #32086058839  SUCCESS
M10                                 #32086058896  SUCCESS
M13                                 #32086058819  SUCCESS
```

La aceptación funcional integral `#32086058832` seguía ejecutándose al iniciar N1.9.H y debe quedar verde antes del cierre documental final.

Las regresiones N1.9.G cubren, entre otras: límites de vencimiento, atomicidad ante configuraciones inválidas, lifecycle serial, idempotencia, unicidad, metadatos EF, longitud de identidad serial y ausencia de mutaciones parciales.

## 9. Rollback y operación

Después de que una identidad sea referenciada, el rollback operativo es forward-fix o restauración controlada compatible; no se deben borrar lotes/series históricos para “volver atrás”. No se autoriza backfill inventado ni corrección manual de Producción desde este flujo.

## 10. Dictamen

N1.9 conserva una sola autoridad cuantitativa (`ExistenciaVariante`) y añade una capa de identidad trazable opt-in con persistencia relacional, concurrencia, RBAC y auditoría estricta. El cierre de N1.9.H requiere paquete documental completo y gates finales verdes sobre el HEAD documental.