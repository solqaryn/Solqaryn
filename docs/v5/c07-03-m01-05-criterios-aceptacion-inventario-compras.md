# SOLQARYN V5 — C07-03.M01.05 — Criterios de aceptación de inventario, compras y operaciones

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 01 — JAVIER (TASK_ID `6adbb9fff1548191a1bd5b30d9e9984a`). **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C07-03.M01.05. **Clase:** criterios documentales para ejecución futura; **ningún caso de esta matriz se ejecutó ni se certifica en esta entrega**.

## 1. Fuentes y alcance verificable

La aceptación futura deberá preservar almacenes, compras, costeo, reservas, inventario, transferencias, conteos, trazabilidad y reconciliación, sin cambiar hoy existencias o datos productivos. Contratos inspeccionados:

| Fuente de preparación del mismo bloque | Git blob comprobado | Alcance acotado |
| --- | --- | --- |
| `docs/v5/c07-03-m01-01-alcance-inventario-compras-operaciones.md` | `200792a55ed6fced30ae5d5e4cb45104f7210507` | NR01–NR12, alcance y exclusiones |
| `docs/v5/c07-03-m01-02-actores-interfaces-inventario-compras-operaciones.md` | `f8ac2a714841adf3804ad24374e03162b0d69a7e` | A01–A12, I01–I12 y B01–B10 |
| `docs/v5/c07-03-m01-03-inventario-codigo-contratos-evidencias.md` | `382bbd606993c0103399a22a6056415f84e88a80` | Veinte referencias a código E01–E20 y cinco archivos de pruebas existentes; no se ejecutaron aquí |
| `docs/v5/c07-03-m01-04-riesgos-supuestos-decisiones.md` | `5b0649b4ef0e0c5f14ca4075fd0672a067bca5ff` | Doce riesgos, ocho supuestos, ocho decisiones PENDIENTES; P01–P08/N01–N14 futuros |

Estos son orígenes de criterios, **no evidencias de que un caso técnico haya pasado**. Los tests preexistentes requieren ejecución dirigida cuando una microtarea de código los necesite. Los criterios se aplicarán a Backend Domain/Application/Infrastructure/API, EF Core/MySQL efímero, RBAC/tenancy y Angular/Playwright según alcance realmente implementado, nunca a partir de mocks exclusivamente positivos.

## 2. Condiciones generales de aprobación causal FUTURA

- **CG01 — datos:** dos empresas A/B activas y un caso NULL legacy, sucursales y almacenes propios/ajenos, producto/variante con stock disponible/reservado, unidades y costo verificables. Usar fixtures efímeros, no producción.
- **CG02 — permisos:** tres perfiles por tenant (lectura, operador y aprobador), un perfil sin permiso de costos, membresía revocada. El frontend no sustituye la verificación del servidor.
- **CG03 — concurrencia:** doble POST con misma key/payload y con misma key/payload diferente; capturar transacciones, índices, retry, saldos y auditoría. No asumir que ejecutar tests unitarios de Domain prueba locks SQL.
- **CG04 — observabilidad:** cada caso registra SHA exacto, nombre de test, resultado real, base de datos/fixture, consulta previa/posterior, logs redactados, idempotencia y correlación. Si skipped/cancelled/failed/in_progress una prueba requerida, NO PASS.
- **CG05 — rollback:** restauración por revert/migración reversible que preserve registros legítimos; un fallo a mitad de escritura no debe dejar movimiento sin contrapartida, doble afectación de existencia o costo histórico alterado.
- **CG06 — UI:** sólo si se implementan interfaces, Playwright con teclado/foco, 320px, estado loading/error, red degradada y rechazo de acciones sin permiso; no cargar tests móviles por trabajo exclusivamente documental.

## 3. Matriz P01–P14 / N01–N14 — criterios futuros NO_EJECUTADOS

| Par | Positivo futuro y observación de aprobación | Negativo futuro y denegación observable | Capa/fixture |
| --- | --- | --- | --- |
| P01 / N01 | A lee sólo almacenes de sus sucursales, listado paginado | A consulta `AlmacenId` de B: 403/404, sin datos ni PII | API + EF/MySQL A/B |
| P02 / N02 | Operador A ubica variante en almacén A autorizado | Ubicación de otra sucursal u orphan parent rechazado sin mutación | Domain/API/EF A/B |
| P03 / N03 | Reserva A válida reduce disponibilidad sin alterar stock físico | Reservar más de disponible o variante NULL legacy: conflicto y 0 efectos | Reserva + MySQL real |
| P04 / N04 | Consumir una reserva activa produce una afectación única | Dos consumos simultáneos no duplican movimiento ni descuentan dos veces | Transacción/concurrencia |
| P05 / N05 | Liberar/expirar reserva reestablece disponibilidad exacta | Repetir liberar/expirar una reserva consumida no genera saldo positivo artificial | Máquina de estados + SQL |
| P06 / N06 | Orden propia se crea una vez con Idempotency-Key/fingerprint | Misma key con payload distinto: conflicto y ninguna orden adicional | API + índices MySQL |
| P07 / N07 | Recepción parcial actualiza saldo pendiente y correlación | Retry concurrente con misma clave no registra dos recepciones ni kardex duplicado | Compras/recepción SQL |
| P08 / N08 | Transferencia aprobada mueve origen→destino con dos referencias rastreables | Intentar recibir sin despacho, con destino B o sin permiso: denegar | API/RBAC/SQL |
| P09 / N09 | Reconciliación de transferencia cerrada deja sumas correctas | Segundo despacho/recepción no acredita destino por segunda vez | Invariantes/existencia |
| P10 / N10 | Conteo autorizado deja borrador trazable, confirmación explícita genera ajuste | Confirmar sin rol, sin estado correcto o con mismatch: no mutar existencias | API y estado transaccional |
| P11 / N11 | Capa de costo e historial permanecen tras compra/movimiento válido | Recalcular/editar pasado sin permiso o introducir costo inválido: rollback y snapshots intactos | Costeo/EF/MySQL |
| P12 / N12 | Auditor autorizado con permiso Finanzas ve costo sólo en su tenant | Operador sin Finanzas no ve costo en API, export, logs ni cache | RBAC/censura A/B |
| P13 / N13 | Kardex/movimiento conserva origen, fecha, CorrelationId y empresa | Inyectar fallo entre saldo y kardex revierte todo sin entradas huérfanas | Fault injection + SQL |
| P14 / N14 | Vista inventario muestra loading/error, foco y operación confirmada por servidor | Timeout/doble clic/offline no muestra éxito ni duplica movimientos; acceso sin rol bloqueado | API + Angular/Playwright |

**Semántica:** 2xx no basta para PASS; hace falta comparar cantidades y documentos antes/después, auditoría y no-regresión por tenant. Para escenarios negativos se aceptará el código 4xx definido por contrato y probado, no errores 5xx ni fuga de datos. **Todos P01–P14 y N01–N14 están NO_EJECUTADOS** en M01.05.

## 4. Trazabilidad de riesgos, responsables y evidencia pendiente

- **R01/R02/R08** → P01/P02/N01/N02/N03: aislamiento A/B y NULL legacy; revisión de servicio y filtro EF (no sólo atributos `[Authorize]`).
- **R03/R04/R05/R10** → P03–P09/N04–N09/N13: reservas, recepción, transferencias, compensación y prueba con MySQL efímero.
- **R06/R07** → P10/P11/N10/N11: conteo, costo y conservación de snapshots.
- **R09/R11** → P12/P13/N12/N13: redacción y correlación sin PII ni referencias ajenas.
- **R12** → P14/N14: confirmación real de servidor y errores accesibles.
- Las decisiones D01–D08 del documento M01.04 siguen **PENDIENTES**; una matriz de aceptación no aprueba automáticamente diseño de tenant, SQL, contabilidad o nuevos jobs.

## 5. A0–A5, impacto, dependencias y rollback

- **A0:** contratos M01.01–04, 20 fuentes y cinco archivos de tests previamente inventariados; criterios vinculados a riesgos sin simular ejecución. **A1:** 14 escenarios positivos + 14 negativos con resultado observable especificado, `NO_EJECUTADOS`. **A2:** tenant A/B, NULL, RBAC, permisos de costo y concurrencia como futuras pruebas, no PASS. **A3:** `NO_APLICA` compilaciones .NET, MySQL, Playwright y pruebas integradas para diff de Markdown únicamente; no se contó como SUCCESS de runtime. **A4:** commits FF y controles Scope Lock + V5 changelog provenance `completed/success` contra el SHA publicado, con readback en PLAN/COLA/BITACORA. **A5:** otra Task Javier autorizada distinta del autor.
- **Dependencia:** `PLAN_MAESTRO!H1086/I1086`, `COLA!G1086/O1086`, `DAG_TECNICO!F1047/G1047=VALIDADO/SI` de preparación. C01-01–04/C02-01–04/C05-01–04/C07-01 M01.01–05 = 65 preparaciones; no libera M01.06, DB, IAM, trabajadores ni QA/main/PROD.
- **Impacto real:** documento `docs/v5` y una sección autocontenida en `CHANGELOG_AI.md`. No cambia Domain, Application, Infrastructure, API, Angular, SQL, JWT/RBAC ni derechos de usuario, secretos, pagos, eventos o servicios externos.
- **Reversión:** `git revert` no destructivo con evento compensatorio en BITACORA. Nunca modificar 26 columnas de eventos históricos; no reescribir Git.
