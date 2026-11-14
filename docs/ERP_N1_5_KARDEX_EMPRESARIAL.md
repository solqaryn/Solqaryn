# ERP-N1.5 — Kardex empresarial

## 1. Alcance y objetivo

ERP-N1.5 consolida el Kardex empresarial como historial auditable de movimientos de inventario. Cada movimiento conserva, cuando aplica, variante, almacén, ubicación, cantidades antes/movimiento/después, costo, fecha, documento origen, usuario y un `CorrelationId` durable.

El Kardex no reemplaza la autoridad de stock vivo de `ExistenciaVariante`; registra la evidencia histórica de las operaciones que modifican inventario y permite consultarla de forma paginada, filtrable y auditable.

## 2. Contrato de movimiento

La trazabilidad empresarial preserva:

- producto y `ProductoVarianteId`;
- `AlmacenId` y `UbicacionAlmacenId` cuando existe contexto físico real;
- cantidad anterior, cantidad del movimiento y cantidad posterior;
- costo y valor económico cuando corresponde;
- fecha y usuario responsable;
- origen tipado del documento (`Compra`, `Venta`, `Consumo`, `Ajuste` y demás orígenes soportados);
- `CorrelationId` durable para agrupar y rastrear una operación extremo a extremo.

No se inventan `AlmacenId` o `UbicacionAlmacenId` para operaciones históricas que no disponen de dimensión física determinista. Esas filas conservan compatibilidad nullable sin fabricar contexto.

## 3. Escritura canónica y correlación

N1.5.D consolidó `IKardexMovimientoWriter` como contrato canónico de escritura del Kardex. Compra, Venta y Consumo usan correlación determinística en sus transiciones relevantes, de modo que confirmar y anular son distinguibles y rastreables.

La persistencia de `CorrelationId` tiene longitud máxima controlada e índice orientado a consulta temporal. La migración correspondiente es reversible y fue validada en MySQL 8.4.

## 4. API de consulta

La consulta empresarial paginada mantiene:

- paginación server-side;
- orden estable por fecha/identificador;
- límites de `Page` y `PageSize`;
- filtros por variante, contexto físico, tipo, causa, origen, correlación y fechas;
- contratos de error compatibles con ProblemDetails del proyecto;
- aislamiento por `UsuarioScope` y permisos relacionales.

N1.5.G incorporó índices compuestos alineados con filtros y orden para evitar scans previsibles. La migración `20260816005000_N1_5_KardexQueryIndexes` mantiene rollback explícito.

## 5. Seguridad, RBAC, auditoría y observabilidad

La lectura del Kardex es fail-closed: un usuario no administrativo queda restringido por su `UsuarioScope`; la ausencia de scope resoluble no concede acceso implícito.

La observabilidad mantiene el identificador de correlación saneado a través del pipeline. La auditoría usa el identificador de trazabilidad validado por runtime y no persiste directamente un header inseguro.

N1.5.F incorporó regresiones para aislamiento por usuario, fail-closed de scope, propagación segura de correlación y auditoría consistente con el identificador saneado.

## 6. Frontend y UX

N1.5.E migró la UI del Kardex hacia la consulta paginada real del backend. La pantalla incluye filtros empresariales, selectores dependientes, paginación server-side, estados loading/vacío/error, tabla responsive, accesibilidad y permisos de UI.

Durante el cierre H, M13 además detectó una regresión histórica de `AjusteInventario`: la UI ya debía seleccionar una `ExistenciaVariante` física concreta y enviar `AlmacenId/UbicacionAlmacenId`. Se corrigió sin reintroducir doble autoridad; el E2E crea contexto físico real y usa interacción de teclado accesible para los `MatSelect`, evitando `force click` y validando la ruta accesible real.

## 7. Evidencia técnica A–H

### N1.5.A — Auditoría y preflight

Preflight canónico `d42aec2c6168d49000db8378d144b2ea3ab904fb`; CI Desarrollo `#31904962826` `SUCCESS`.

### N1.5.B — Dominio y contratos

Cierre `625ba5a3777e0b8ffb38ecba1ea3fa1956270029`; CI `#31905282056` `SUCCESS`.

### N1.5.C — Persistencia, migración y datos

Cierre `55dbaa334ac6bbf236f6b5f376a0dcd69f2d2354`:

- Desarrollo `#31911214659` `SUCCESS`;
- aceptación integral `#31911214717` `SUCCESS`;
- Fase 8 `#31911214610` `SUCCESS`;
- recuperación MySQL `#31911214658` `SUCCESS`.

### N1.5.D — Aplicación, servicios y API

Cierre `6f4a3cb8f7e854c40569db08fe2f9dd05aca126f`; Desarrollo `#31916568532` y Fase 8 `#31916568530` `SUCCESS`.

### N1.5.E — Frontend y UX

Cierre `dd510978a16675b1839998ab8156e63f469a0b78`; Frontend producción `#31917459336` y M10 `#31917459334` `SUCCESS`.

### N1.5.F — RBAC, auditoría, seguridad y observabilidad

Cierre `75196d80602dc1c0d715b6b52069267d0cf6282c`; CI `#31917739811` y RBAC relacional `#31917739885` `SUCCESS`.

### N1.5.G — QA, regresión y CI

Base funcional `4871da115e72d205513ea23aa9fe95c1e4818e6b`, tree `3472011576fbb6f2114fab15d16016e2c83d5d43`. La QA añadió índices compuestos del Kardex y migración reversible. Desarrollo `#31918223873` terminó `SUCCESS` completo con backend/unitarias, frontend, Docker, higiene, migraciones e integración MySQL.

La aceptación posterior detectó un fixture legacy de imágenes que interceptaba `/inventario/movimientos` en vez del endpoint paginado real `/inventario/movimientos/paged`; se corrigió en `bb2cc9753063a688796979897857d3f57364257a` y `294a63b38d10ed95c2def06fb5bbd04d7ecce0d5`.

M13 también expuso que su gate `has-pending-model-changes` era inválido mientras N1.4/N1.5 usan migraciones explícitas sin `ModelSnapshot` generado. Se reemplazó por validación fail-closed material del esquema, índices e historial de migraciones en `1ce43e0fd1de72e7b6108fda6e291ca47f2059ba`.

### N1.5.H — Documentación y certificación

El cierre funcional definitivo se certifica sobre `7a37998c8ff94299904135471d328c0d8b91c705`, tree `cb850fb53be739984946f36bf727050af865e024`.

Gates finales reconciliados:

- `Desarrollo - Compilación y pruebas #31923298004` — `SUCCESS` completo;
- `Desarrollo - aceptación funcional integral #31923298037` — `SUCCESS` completo, incluido Playwright;
- `Fase 8 - Validación completa automatizada #31923298063` — `SUCCESS` completo;
- `M13 - Auditoría integral y certificación final #31923298066` — `SUCCESS` completo, incluido Dictamen automatizado M13.

El fix final del E2E de ajustes abre los `MatSelect` por `focus + Enter`, por lo que el test valida navegación accesible real y deja de depender de un click de puntero interceptable por el `required-marker` de Angular Material. No se ocultó ninguna prueba ni se usó `force click`.

## 8. Rollback y operación

Rollback de código:

1. revertir explícitamente commits causales en `Desarrollo`;
2. no usar force-push ni reescritura de historia;
3. volver a ejecutar CI y contratos después de cada reversión.

Rollback de datos/migraciones:

1. no ejecutar rollback destructivo automáticamente en Producción;
2. validar dependencias de consultas e índices antes de retirar una migración;
3. conservar `CorrelationId` y movimientos ya persistidos;
4. cualquier cambio productivo requiere procedimiento y autorización separados.

## 9. Restricciones de despliegue

Este cierre certifica ERP-N1.5 exclusivamente en `Desarrollo`. No autoriza merge a `main`, auto-merge del PR #2, cambios en Producción, nuevas ramas, force-push ni modificación de secretos/infraestructura productiva.

PR #2 debe permanecer abierto y Draft.

## 10. Estado final

**ERP-N1.5 A–H queda formalmente cerrado y certificado.** No quedan P0/P1 conocidos atribuibles al Kardex empresarial. La continuidad FINISH_FIRST pasa a `N1.6.A — Transferencias — Auditoría y preflight`, sujeto a las dependencias registradas en VAEP.