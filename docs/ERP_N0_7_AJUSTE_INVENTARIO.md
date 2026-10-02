# ERP-N0.7 — AjusteInventario formal

## Dictamen

**Estado:** `LISTO / CIERRE FORMAL`.

**SHA funcional certificado:** `cd5c1f058fc7a24fd477a4c9e8cda7cff4c99850`.

ERP-N0.7 queda cerrado después de completar dominio, persistencia, backend/API, frontend, RBAC/auditoría/observabilidad, regresión, CI y documentación. El preflight `docs/ERP_N0_7_AJUSTE_INVENTARIO_PREFLIGHT.md` permanece como antecedente histórico; este documento es la fuente canónica final del comportamiento implementado y de su recuperación.

## 1. Contrato canónico

ERP-N0.7 introduce `AjusteInventario` como agregado formal para corregir existencias sin escrituras directas ad hoc.

```text
Borrador -> Confirmado -> Anulado
```

Reglas:

- crear o editar un borrador no modifica existencias;
- confirmar materializa el cambio de stock;
- anular revierte operacionalmente el impacto sin borrar ni reescribir historia;
- un ajuste confirmado conserva snapshots suficientes para explicar y revertir el cambio;
- segunda confirmación o segunda anulación falla cerrada;
- productos con variantes deben identificar una variante concreta;
- no se admite cantidad objetivo negativa ni confirmación sin diferencia real.

## 2. Persistencia e integridad histórica

Persistencia incorporada mediante:

- `backend/src/Infrastructure/Migrations/20260814013600_N0_7_AjusteInventarioPersistencia.cs`;
- `backend/src/Infrastructure/Migrations/20260814014000_N0_7_AjusteInventarioVarianteIndex.cs`.

Al confirmar, cada detalle conserva snapshots de cantidad anterior/nueva, diferencia, costo unitario, impacto de costo, nombre, SKU, marca, modelo, color y talla.

Los movimientos del ajuste usan origen tipado `OrigenMovimientoInventario.DesdeAjusteInventario(ajuste.Id)`; `AjusteInventarioId` es la referencia relacional de autoridad.

## 3. Autoridad única de stock

La única autoridad de negocio que crea, confirma, anula y materializa stock es:

`backend/src/Application/Services/AjusteInventarioService.cs`

Concentra bloqueo pesimista, validaciones fail-closed, actualización de producto/variante, snapshots, movimiento tipado, auditoría crítica y reversión.

Durante N0.7.H se detectó que `InventarioAjusteService` todavía funcionaba como segunda autoridad histórica. El cierre se detuvo y esa arquitectura se corrigió antes de certificar el punto.

## 4. Compatibilidad legacy sin segunda autoridad

Los endpoints legacy `ajustes-stock` permanecen temporalmente por compatibilidad API, pero ya no escriben inventario por una ruta independiente.

`backend/src/Application/Services/InventarioAjusteService.cs` es un adaptador puro que depende únicamente de `IAjusteInventarioService` y delega en:

`AjustarStockCompatibilidadAsync(productoId, varianteId, request)`.

La compatibilidad legacy conserva `AjusteStockResultadoDto`, pero internamente ejecuta de forma atómica:

```text
Crear AjusteInventario
-> bloquear inventario
-> comprobar CantidadActualEsperada
-> Confirmar AjusteInventario
```

Todo ocurre dentro de una sola unidad transaccional del servicio formal.

### Concurrencia legacy

`CantidadActualEsperada` se compara contra el stock real después de adquirir el lock y antes de movimiento o mutación. Si la lectura del cliente quedó obsoleta, la operación falla cerrada y no cambia existencias ni emite movimiento.

## 5. API y RBAC

Permisos formales del módulo `Inventario`:

- lectura: `Inventario:Ver`;
- creación: `Inventario:Crear`;
- edición: `Inventario:Editar`;
- confirmación: `Inventario:Confirmar`;
- anulación: `Inventario:Anular`.

Los dos endpoints legacy permanecen autenticados y exigen `Inventario:Confirmar`; el bypass histórico por `Productos:Editar` fue retirado.

## 6. Auditoría, seguridad y observabilidad

`Confirmar` y `Anular`, las operaciones que materializan o revierten stock, usan `RegistrarEstrictoAsync` dentro de la misma transacción de base de datos. Una falla de auditoría crítica impide consolidar el cambio de stock.

Crear/editar borradores conservan auditoría tolerante porque no materializan inventario.

`CorrelationIdMiddleware`:

- acepta `X-Correlation-ID` válido de hasta 128 caracteres o genera uno;
- lo asigna a `HttpContext.TraceIdentifier`;
- lo devuelve en la respuesta;
- abre scope estructurado con CorrelationId, método HTTP y ruta.

La aplicación conserva además excepción global, cabeceras de seguridad, HTTPS/HSTS según entorno, CORS configurado, JWT, rate limiting de login y health/readiness certificados transversalmente.

## 7. Frontend y UX

La superficie N0.7 permite consulta, creación/edición de borrador, confirmación y anulación respetando el contrato del backend y sus permisos. La aceptación integral de `Desarrollo` cubre la aplicación completa con Playwright.

## 8. QA y regresión

Cobertura relevante:

- `backend/tests/Solqaryn.Tests/AjusteInventarioServiceTests.cs`;
- `backend/tests/Solqaryn.Tests/InventarioAjusteServiceTests.cs`;
- `backend/tests/Solqaryn.Tests/N07AjusteInventarioSeguridadRegressionTests.cs`.

La regresión protege snapshots, origen tipado, reversión histórica, doble confirmación/anulación fail-closed, compatibilidad legacy atómica, stale-write fail-closed, adaptador sin autoridad propia, permisos legacy y auditoría estricta.

## 9. Evidencia CI final

### CI principal

`Desarrollo - Compilación y pruebas` — run `31808933744` — **SUCCESS completo** sobre `cd5c1f058fc7a24fd477a4c9e8cda7cff4c99850`:

- backend Release y unitarias no integración;
- migraciones actuales e integración MySQL 8.4;
- variante/cargas/snapshot;
- SQL forward;
- frontend lint/build producción;
- Docker/aislamiento;
- higiene.

### Aceptación funcional integral

Run `31808933692` — **SUCCESS completo**:

- backend + MySQL temporal + migraciones;
- calidad/tipado frontend;
- Playwright integral;
- SMTP/PDF;
- publicación de evidencia.

### M13

Run `31808933833` — **COMPLETED / SUCCESS** sobre el mismo SHA funcional:

- frontend TypeScript/lint/build: SUCCESS;
- Docker/aislamiento/backup certificado: SUCCESS;
- backend/MySQL/migraciones/snapshot/upgrade histórico: SUCCESS;
- secretos/higiene/dependencias: SUCCESS;
- seguridad HTTP/autorización fail-closed: SUCCESS;
- runtime/Playwright integral: SUCCESS;
- SMTP/PDF/logs sin secretos: SUCCESS;
- `Dictamen automatizado M13`: SUCCESS, exigiendo todos los gates verdes.

## 10. Corrección descubierta durante N0.7.H

La revisión final evitó certificar una arquitectura con dos autoridades de stock. Cadena principal de corrección:

- `554c9f24902e12388c00e8ca093aa29b533c2ac1` — primer adaptador hacia flujo formal;
- `3416e47e811a2f7c7387bbdaf9964e745a0f6021` — regresión inicial de autoridad única;
- `28a0fe5a945c2071fe160bd208ca9cfc4a07013d` — compatibilidad atómica dentro de la autoridad formal;
- `d0bd3b18f092d189efea5ee69b229bce669387f5` — contrato formal de compatibilidad;
- `f26b7513cfb34ce9a9be54202b2363c1f19e712c` — legacy convertido en adaptador puro;
- `6e17376837e13fb70960da7b523785f54c23b04b` — regresión estructural;
- `7079263f86461bae136b509151da491d2b8bfcbe` — atomicidad + stale-write;
- `cd5c1f058fc7a24fd477a4c9e8cda7cff4c99850` — pruebas históricas del adaptador alineadas.

El run previo sobre `7079263f...` detectó un test antiguo que todavía construía `InventarioAjusteService` con dependencias eliminadas. Se corrigió forward-only en `cd5c1f05...`; el fallo quedó visible y no se atribuyó a infraestructura.

## 11. Trazabilidad A-H

- **N0.7.A** — auditoría y preflight.
- **N0.7.B** — dominio/contratos formales.
- **N0.7.C** — persistencia e integridad.
- **N0.7.D** — backend/API/reglas de negocio.
- **N0.7.E** — frontend/UX.
- **N0.7.F** — RBAC, auditoría, seguridad y observabilidad.
- **N0.7.G** — QA, regresión y CI.
- **N0.7.H** — documentación/certificación y eliminación de la segunda autoridad legacy descubierta en revisión.

Todos los puntos A-H quedan cerrados.

## 12. Rollback y recuperación

### Reversión funcional

No borrar `AjusteInventario`, sus movimientos ni restaurar manualmente snapshots como si fueran estado actual. La operación soportada es **Anular**: bloquea inventario, usa la diferencia histórica, calcula la reversión sobre stock actual, evita negativos, emite movimiento de reversión tipado, conserva historia y audita estrictamente.

### Rollback de código

Antes de Producción, volver únicamente a un SHA previamente certificado y compatible con el esquema. No usar force-push sobre `Desarrollo`.

### Rollback de esquema

No usar `Down` destructivo sobre una base con ajustes confirmados como recuperación normal. Requiere respaldo/restauración certificable, comprobar que no se destruirán datos N0.7, validar compatibilidad del código destino y preferir corrección forward cuando exista historia.

Producción permanece fuera de alcance sin autorización formal expresa.

## 13. Riesgo residual y continuidad

Los endpoints legacy todavía existen como superficie HTTP de compatibilidad, pero ya no son autoridad independiente. Su deprecación/eliminación física puede tratarse en ERP-N0.8.

Con `N0.7.H` certificado, el siguiente foco FINISH_FIRST elegible es `N0.8.A` conforme a VAEP.

## 14. Límites del cierre

Este cierre no autoriza merge a `main`, auto-merge del PR #2, despliegue a Producción, cambios de secretos/domínios/servicios productivos, force-push ni ramas adicionales.
