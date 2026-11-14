# ERP-N1.7 — Conteos físicos — Preflight

## 1. Objetivo

Definir el alcance técnico seguro de `ConteoInventario` para conteos generales, cíclicos, por ubicación, por categoría y ciegos, preservando `ExistenciaVariante` como autoridad de stock vivo y utilizando `AjusteInventario` como mecanismo formal de materialización de diferencias cuando corresponda.

## 2. Estado real dirigido

La inspección dirigida no encontró una implementación `ConteoInventario` existente en el repositorio. No hay dominio, persistencia, API ni frontend específico de conteos que deba migrarse o conservarse como autoridad legacy.

Las capacidades existentes relevantes son:

- `ExistenciaVariante` como autoridad de stock por `ProductoVariante + Almacen + Ubicacion`;
- `StockFisico`, `StockReservado`, `StockDisponible`, `StockTransito`, mínimos/máximos e invariantes ya consolidadas;
- `AjusteInventario` como documento formal `Borrador -> Confirmado -> Anulado` con snapshots bajo lock y reversión controlada;
- Kardex empresarial con origen tipado y `CorrelationId`;
- Almacenes y ubicaciones internas como topología física ya cerrada;
- transferencias internas ya certificadas, por lo que un conteo no debe confundir stock en tránsito con stock físicamente contado en una ubicación.

## 3. Autoridades y fronteras

### Autoridad de stock

`ExistenciaVariante.StockFisico` sigue siendo la única autoridad de cantidad físicamente presente. El conteo no introduce otra columna o agregado que compita como stock vivo.

### Documento de conteo

`ConteoInventario` debe ser evidencia temporal/auditable del proceso de conteo. Puede capturar snapshots y resultados, pero no se convierte en autoridad de disponibilidad.

### Ajuste posterior

Las diferencias aprobadas deben materializarse mediante `AjusteInventario` o un boundary equivalente que preserve las mismas garantías de lock, snapshot, auditoría, Kardex y reversión. No se permite escribir `ExistenciaVariante.StockFisico` directamente desde el controller/UI de conteos.

## 4. Alcance funcional propuesto

El módulo debe soportar al menos:

- conteo general por almacén;
- conteo cíclico por subconjunto de variantes/ubicaciones;
- conteo por ubicación;
- conteo por categoría con resolución a variantes físicas;
- conteo ciego, ocultando stock esperado al capturista;
- congelación lógica del universo contado mediante snapshot de líneas;
- captura progresiva de cantidades observadas;
- diferencias `Contado - Esperado`;
- revisión/aprobación antes de generar ajustes;
- generación trazable de uno o más `AjusteInventario` posteriores;
- estados de lifecycle auditables y fail-closed;
- paginación/filtros, frontend responsive y permisos relacionales.

## 5. Fuera de alcance N1.7

- cambiar la autoridad de `ExistenciaVariante`;
- reservas/overselling de N1.8;
- reabastecimiento automático;
- WMS avanzado, ondas, picking o slotting;
- manipulación directa de Producción;
- conteo automático basado en sensores externos;
- revaloración contable de inventario.

## 6. Modelo de dominio recomendado

### ConteoInventario

Campos mínimos sugeridos:

- `Id`, número/código;
- tipo de conteo;
- `AlmacenId` obligatorio;
- filtros/scope materializados al iniciar;
- estado;
- flags de conteo ciego;
- fecha de creación/inicio/cierre/aprobación;
- usuarios responsables;
- observaciones/motivo.

Lifecycle recomendado:

`Borrador -> EnProceso -> Cerrado -> Aprobado`

más `Cancelado` cuando corresponda.

Una vez iniciado, el universo de líneas debe quedar materializado para evitar que cambios posteriores de catálogo/ubicaciones alteren silenciosamente el alcance histórico.

### ConteoInventarioDetalle

Debe preservar como mínimo:

- `ProductoVarianteId`;
- `AlmacenId`;
- `UbicacionAlmacenId` nullable;
- stock físico esperado snapshot;
- cantidad contada nullable durante captura;
- diferencia derivada/materializada al cerrar;
- estado de revisión;
- snapshots descriptivos necesarios para histórico;
- referencia al ajuste generado cuando aplique.

La identidad física debe ser la misma del stock: variante + almacén + ubicación normalizada.

## 7. Concurrencia

Riesgo principal: que el stock cambie entre el snapshot inicial y la aprobación del conteo.

Regla recomendada:

1. el conteo captura un `StockEsperadoSnapshot` al materializar su universo;
2. al aprobar diferencias, se vuelve a bloquear la `ExistenciaVariante` autoritativa;
3. si el stock actual no coincide con la precondición esperada para aplicar la diferencia, la operación falla cerrado o exige reconciliación explícita;
4. nunca se aplica una diferencia calculada sobre un snapshot obsoleto sin volver a validar bajo lock.

No se debe bloquear un almacén completo durante horas; los locks deben concentrarse en el momento de materializar ajustes.

## 8. Stock reservado y en tránsito

El conteo físico observa existencia físicamente presente. Por defecto:

- `StockReservado` no cambia la cantidad física que se cuenta;
- `StockTransito` no debe tratarse como físicamente presente en destino hasta recepción;
- `StockDisponible` no es el valor a contar porque es derivado de físico - reservado.

Las diferencias deben compararse contra `StockFisico` de la clave física pertinente.

## 9. Conteo ciego

En modo ciego, la API/frontend de captura no debe exponer `StockEsperadoSnapshot` al rol capturista. La información puede persistir para reconciliación pero debe filtrarse en contratos/DTOs según permiso/etapa.

Las pruebas deben impedir filtraciones por DTO, endpoint de detalle o frontend.

## 10. RBAC recomendado

Módulo relacional nuevo `ConteosInventario` o equivalente, con permisos mínimos:

- `Ver`;
- `Crear`;
- `Editar`;
- `Iniciar`;
- `Capturar`;
- `Cerrar`;
- `Aprobar`;
- `Cancelar`;
- `GenerarAjuste` cuando se mantenga como acción separada.

Aprobación y generación de ajuste no deben degradarse a permisos genéricos de edición.

## 11. Auditoría y observabilidad

Auditar al menos:

- creación;
- inicio/materialización del universo;
- cambios de scope antes de iniciar;
- cierre;
- aprobación;
- cancelación;
- generación de ajustes;
- reapertura, si llegara a permitirse explícitamente.

Usar Correlation ID saneado por runtime y propagar una correlación determinística hacia ajustes/Kardex generados.

## 12. Persistencia e índices

Índices candidatos:

- `ConteoInventario(AlmacenId, Estado, FechaCreacion)`;
- número/código único estable;
- `ConteoInventarioDetalle(ConteoInventarioId)`;
- índice por `ProductoVarianteId, AlmacenId, UbicacionAlmacenId`;
- índice para líneas pendientes de captura/revisión.

Las FKs físicas deben ser restrictivas y validar que una ubicación pertenezca al mismo almacén.

## 13. API mínima

- listado paginado y filtrable;
- crear/editar borrador;
- iniciar/materializar líneas;
- detalle paginado;
- capturar cantidad por línea y carga controlada por lote si aplica;
- cerrar;
- aprobar;
- cancelar;
- generar/consultar ajuste resultante;
- endpoint de progreso/resumen de diferencias.

ProblemDetails y comportamiento fail-closed deben seguir el estándar del proyecto.

## 14. Frontend/UX

- wizard o formulario por etapas;
- selección de almacén y tipo de conteo;
- filtros de ubicación/categoría/variantes antes de iniciar;
- captura rápida de cantidades;
- soporte móvil/tablet por uso en piso/bodega;
- modo ciego real;
- progreso contado/pendiente;
- resaltado de diferencias después del cierre, no antes cuando el modo sea ciego;
- confirmaciones explícitas para cerrar/aprobar/cancelar;
- accesibilidad y navegación por teclado.

## 15. Riesgos P0/P1

### P0

- sobrescribir `StockFisico` directamente desde captura;
- aplicar diferencias contra snapshot obsoleto sin revalidación bajo lock;
- filtrar stock esperado en conteo ciego;
- mezclar stock tránsito con stock físico contado;
- generar ajustes duplicados por reintento.

### P1

- scope de conteo mutable después de iniciar;
- misma clave física duplicada dentro del mismo conteo;
- ubicación ajena al almacén;
- auditoría incompleta de aprobación/generación de ajuste;
- cierre con líneas pendientes sin política explícita.

## 16. Estrategia de pruebas

### Dominio

- lifecycle válido/inválido;
- invariantes de línea;
- conteo ciego no altera dominio, sólo exposición;
- cierre con pendientes fail-closed;
- no mutación parcial ante transición inválida.

### Persistencia

- FKs/índices/unicidad;
- ubicación del mismo almacén;
- migración reversible/rollback documentado;
- snapshot alineado.

### Aplicación/API

- filtros/paginación;
- materialización determinística de scope;
- captura idempotente;
- aprobación bajo concurrencia;
- generación única de ajuste;
- ProblemDetails.

### Seguridad

- permisos por acción;
- conteo ciego sin exposición de esperado;
- aislamiento por scope si aplica.

### E2E

Flujo mínimo:

crear -> iniciar -> capturar -> cerrar -> revisar diferencias -> aprobar -> generar ajuste -> verificar Kardex/stock.

Debe cubrir además cancelación y un conflicto de concurrencia.

## 17. Rollback

- código: commits de reversión en `Desarrollo`, sin force-push;
- esquema: migración down/forward-fix sólo con evidencia de que no se destruye histórico;
- datos: conteos históricos se conservan como auditoría;
- ajustes ya confirmados se revierten mediante lifecycle formal de `AjusteInventario`, no editando stock manualmente;
- Producción queda fuera de alcance.

## 18. Criterios de aceptación para N1.7.A

N1.7.A puede cerrarse cuando quede demostrado y documentado que:

- no existe autoridad legacy de conteos que deba migrarse;
- `ExistenciaVariante` sigue siendo la única autoridad física;
- el ajuste posterior reutiliza el mecanismo formal existente;
- riesgos de snapshot/concurrencia/modo ciego están definidos;
- B–H tienen frontera y estrategia de validación claras;
- no se modificó stock, esquema ni Producción durante el preflight.