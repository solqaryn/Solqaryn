# ERP-N1.7 — Conteos físicos

## 1. Estado certificado

ERP-N1.7 implementa conteos físicos empresariales sobre `ExistenciaVariante` como autoridad única del stock físico. El módulo soporta conteos generales, cíclicos, por ubicación, por categoría y ciegos; captura progresiva; cierre; aprobación; cancelación y generación posterior de un `AjusteInventario` formal cuando existen diferencias.

La autoridad de stock no cambia: `ConteoInventario` conserva evidencia, snapshots y resultados; nunca sustituye `ExistenciaVariante.StockFisico` ni modifica stock desde el controller o frontend.

## 2. Modelo de dominio

### ConteoInventario

Lifecycle canónico:

`Borrador -> EnProceso -> Cerrado -> Aprobado`

con `Cancelado` cuando la transición es válida.

El agregado conserva número, tipo, almacén, filtros de alcance, flags de modo ciego, fechas y usuarios de lifecycle, observaciones y vínculo con el ajuste generado.

### ConteoInventarioDetalle

Cada línea conserva la identidad física `ProductoVarianteId + AlmacenId + UbicacionAlmacenId`, el snapshot esperado, la cantidad contada, la diferencia y la referencia al ajuste resultante cuando aplique.

Las transiciones inválidas son fail-closed y no dejan mutaciones parciales.

## 3. Persistencia

La persistencia usa entidades y configuraciones EF dedicadas para cabecera y detalle, con FKs restrictivas, unicidad física por conteo, validación de ubicación perteneciente al almacén e índices para listado, estado y líneas de captura.

`ExistenciaVariante` permanece como fuente física. Los snapshots del conteo son históricos y no compiten como stock vivo.

## 4. Aplicación y API

El controller canónico está publicado bajo:

`/conteos-inventario`

Endpoints principales:

- `GET /conteos-inventario` — búsqueda paginada y filtrable.
- `GET /conteos-inventario/{id}` — detalle.
- `POST /conteos-inventario` — crear borrador.
- `PUT /conteos-inventario/{id}` — editar borrador.
- `POST /conteos-inventario/{id}/iniciar` — iniciar/materializar universo.
- `PUT /conteos-inventario/{id}/detalles/{detalleId}/captura` — captura individual.
- `PUT /conteos-inventario/{id}/detalles/captura-lote` — captura por lote.
- `POST /conteos-inventario/{id}/cerrar` — cerrar.
- `POST /conteos-inventario/{id}/aprobar` — aprobar.
- `POST /conteos-inventario/{id}/generar-ajuste` — generar `AjusteInventario` borrador desde diferencias.
- `POST /conteos-inventario/{id}/cancelar` — cancelar con motivo.

La API usa respuestas tipadas del proyecto, validación de contratos y comportamiento fail-closed.

## 5. Ajuste posterior

La aprobación del conteo no escribe directamente `StockFisico`. Las diferencias se materializan mediante `AjusteInventario`, conservando el lifecycle, locks físicos, snapshots, Kardex, auditoría y reversión ya definidos para ajustes.

La generación del ajuste es idempotente y falla si el conteo no cumple las precondiciones o si detecta vínculos parciales/inconsistentes.

## 6. Conteo ciego

La privacidad del conteo ciego es una garantía de seguridad del contrato, no sólo de UI.

Mientras un conteo ciego esté en `Borrador` o `EnProceso`, el API no expone `StockEsperado`, `Diferencia`, `CantidadConDiferencia` ni `DiferenciaNeta`. La cantidad efectivamente capturada puede permanecer visible sin permitir reconstruir el stock esperado.

La misma protección aplica a detalle y listado paginado, incluso si el conteo fue cancelado antes del cierre. Tras un cierre válido, la etapa de conciliación puede revelar snapshot y diferencias.

## 7. RBAC

El controller requiere autenticación y permisos relacionales de `MovimientosInventario` por operación:

- `Ver` para consultar.
- `Crear` para crear y generar ajuste.
- `Editar` para editar/capturar.
- `CambiarEstado` para iniciar.
- `Cerrar` para cerrar.
- `Aprobar` para aprobar.
- `Anular` para cancelar.

No existe degradación a `[AllowAnonymous]` ni autorización implícita por rol administrador.

## 8. Auditoría y observabilidad

Se auditan creación, edición, inicio, captura individual, captura por lote, cierre, aprobación, cancelación y generación de ajuste.

La auditoría registra entidad, referencia, estado y datos operativos relevantes. La correlación utiliza el `TraceIdentifier` saneado del runtime y no confía en un `X-Correlation-ID` bruto suministrado por el cliente.

Una falla del logging no debe romper la operación de negocio ya validada.

## 9. Frontend y UX

El frontend incorpora:

- listado responsive con filtros y paginación;
- formulario de alta/edición con catálogos activos de almacenes, ubicaciones, categorías y variantes;
- detalle del lifecycle;
- captura individual y por lote;
- captura dirty-only para no reenviar valores sin cambios;
- modo ciego consistente con el API;
- acciones de iniciar, cerrar, aprobar, cancelar y generar ajuste;
- navegación lateral protegida por permisos;
- feedback de loading, error y estados vacíos;
- cobertura Playwright del flujo principal y de acceso.

## 10. Concurrencia e integridad

Las diferencias se comparan contra el snapshot del conteo, pero la materialización de stock se delega al mecanismo formal de `AjusteInventario`, que vuelve a validar bajo lock la existencia física autoritativa.

No se bloquea un almacén durante toda la captura. El control de concurrencia se concentra en el momento de aplicar el ajuste.

## 11. QA y regresión

La cobertura de N1.7 protege, entre otros, estos escenarios:

- lifecycle válido e inválido;
- no mutación parcial ante fallos de transición;
- cierre con líneas pendientes;
- captura individual y por lote;
- atomicidad del lote ante una línea inválida;
- aceptación explícita de cantidad física `0`;
- validación de `DetalleId` y cantidades negativas;
- paginación y filtros;
- generación única/idempotente del ajuste;
- conteo aprobado sin diferencias;
- vínculos parciales de ajuste;
- permisos por endpoint;
- auditoría de lifecycle y captura;
- privacidad ciega en listado y detalle antes del cierre;
- imposibilidad de reconstruir el stock esperado a partir de diferencia y cantidad contada;
- revelación controlada después del cierre;
- E2E de acceso, creación, captura y lifecycle.

## 12. Evidencia de certificación

HEAD de cierre de QA previo a documentación:

`7bba45d13a3fe0579285ed273062f66b2796893f`

Gates causales verificados en `SUCCESS`:

- `Desarrollo - Compilación y pruebas #31995868136`.
- `Desarrollo - aceptación funcional integral #31995868251`.
- `Fase 8 - Validación completa automatizada #31995868120`.
- `M13 - Auditoría integral y certificación final #31995868144`.
- `M10 - UI UX empresarial y accesibilidad #31995868110`.

Los workflows históricos ajenos al alcance de N1.7 que puedan continuar fallando no se usan como evidencia causal de este punto.

## 13. Rollback

- Código: revertir commits de N1.7 en `Desarrollo`, nunca force-push.
- Esquema: usar migración inversa/forward-fix sólo con evidencia de preservación de histórico.
- Conteos históricos: conservar como evidencia auditable.
- Ajustes ya confirmados: revertir mediante el lifecycle formal de `AjusteInventario`; no editar stock manualmente.
- Producción: fuera del alcance de esta fase.

## 14. Cierre

N1.7 queda funcionalmente compuesto por dominio, contratos, persistencia, aplicación/API, frontend/UX, RBAC/auditoría/seguridad y QA automatizado. La certificación final de N1.7.H debe registrar este documento, el ADR, el runbook, evidencia de CI y checkpoint operativo antes de continuar al siguiente punto elegible de ERP-N1.
