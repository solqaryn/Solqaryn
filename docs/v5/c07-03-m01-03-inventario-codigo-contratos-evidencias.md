# SOLQARYN V5 — C07-03.M01.03 — Inventario causal de código, contratos y evidencias ERP

**Autor:** PRIMARY DEV 05 — JAVIER | **Fecha de lectura:** 2026-11-14 | **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, bloque C07-03, fase M01.03. **Naturaleza:** inspección documental de artefactos existentes, no implementación funcional ni certificación runtime.

## 1. Objetivo y delimitación

Inventariar código actual, contratos, pruebas y brechas verificables para preservar almacenes, compras, costeo, reservas de inventario, transferencias, conteos y trazabilidad. El inventario parte de las condiciones de M01.01 (NR01–NR12) y de las fronteras/actores de M01.02 (A01–A12, I01–I12, B01–B10). La existencia del archivo, prueba o atributo **no acredita ejecución exitosa de la operación ni cobertura de toda la plataforma**. El alcance se limita a archivos originales del HEAD de `dev` y consultas estáticas de sus blobs; las pruebas documentadas son **existentes, no ejecutadas durante M01.03**.

## 2. Inventario con SHA de blobs de Git

| ID | Archivo original | Blob SHA | Hallazgo parcial, sin inferir certificación |
| --- | --- | --- | --- |
| E01 | `backend/src/Domain/Entities/Almacen.cs` | `b420419660dd15e5c90b15dc10f7459edd967966` | Ownership por Sucursal, excepción sin jerarquía |
| E02 | `backend/src/Domain/Entities/ReservaInventario.cs` | `9e097bbf04d5c2e0bca61426ea673b2306efe7d9` | Transiciones con precondiciones para reservar/consumir/liberar |
| E03 | `backend/src/Domain/Entities/TransferenciaInventario.cs` | `b2ded478a0083d9e5d8ed6fd10c974409e2b6ac6` | Topología almacenes origen/destino distintos y estados |
| E04 | `backend/src/Domain/Entities/MovimientoInventario.cs` | `a1093f36aea78e6851fe4b364e4eb710832a7e61` | CorrelationId, origen tipado y almacén nullable histórico |
| E05 | `backend/src/Domain/Entities/OrdenCompra.cs` | `211e21ce0210add8e5e88f79c7237fd673270fdc` | IdempotencyKey/Fingerprint y estados de aprobación |
| E06 | `backend/src/API/Controllers/ReservasInventarioController.cs` | `3cbddb32ea5696028eea5ac5f78e3f78a5971e89` | [Authorize] y permisos de lectura/creación/transiciones |
| E07 | `backend/src/API/Controllers/TransferenciasInventarioController.cs` | `2a3c0ed7cea58cc09f610124b53ed47b43ab44ac` | Permisos y auditoría selectiva en endpoints |
| E08 | `backend/src/API/Controllers/OrdenesCompraController.cs` | `c57e143da4a0b5b6fba79a7b12a192d1c5b95d44` | POST con Idempotency-Key obligatorio |
| E09 | `backend/src/API/Controllers/RecepcionesCompraController.cs` | `3c100f3907a10b5159d8bebc6c8d192057c1847d` | Saldo de orden y Idempotency-Key de recepción |
| E10 | `backend/src/API/Controllers/ConteosInventarioController.cs` | `f995bf6938fb4546168e52bf9c3c443a99f11843` | Captura de conteos y ajuste borrador, confirmación posterior |
| E11 | `backend/src/API/Controllers/CosteoInventarioController.cs` | `9a5ffd9852ecb1c89d5a47d30d21b3453b01a615` | Política de costeo y permiso de modificación |
| E12 | `backend/src/API/Controllers/ReportesInventarioReconciliacionController.cs` | `b0ad8f5143fd34beca236c57beaabbf9941a9393` | RequireEmpresaIdAsync, scope guard y censura del costo |
| E13 | `backend/src/API/Controllers/AlmacenesController.cs` | `abcc1e615eb4f3d75704562ae8c82cf603839c2d` | CRUD de almacenes sujeto a permisos |
| E14 | `backend/src/API/Controllers/UbicacionesAlmacenController.cs` | `60ba7144d1c26db758679f2209450d7a5b2e9eac` | CRUD y listados de ubicaciones dentro del alcance |
| E15 | `backend/src/API/Controllers/MovimientosInventarioController.cs` | `8dd4e9c1ea3f56141de71f7aeefb2c0a8de171c2` | Listado/paginación de movimientos |
| E16 | `backend/src/Application/Services/OrdenCompraService.cs` | `233291032c7437f3db6cf61c648b4ee234e03e87` | Servicio con key/constraint, unidad de trabajo y transacción |
| E17 | `backend/src/Application/Services/RecepcionCompraService.cs` | `364f9a580742767887ca3b0c166872291d052e99` | Servicios de saldo, ubicaciones, existencias y kardex |
| E18 | `backend/src/Application/Services/ReservaInventarioService.cs` | `c8efe2fc5becc0edcca7981e710106004611f350` | IExistenciaVarianteConcurrencyService y ExecuteInTransactionAsync |
| E19 | `backend/src/Application/Services/TransferenciaInventarioService.cs` | `8d8987225bb1eaf43f4d1744af5b8eb09eb9be0c` | Transacciones y estado editable borrador |
| E20 | `backend/src/Infrastructure/Services/ReporteInventarioService.cs` | `7fb1d696125a7fa8d9afd0f0f621293259c9a774` | Filtro de `Almacen.Sucursal.EmpresaId` en consultas observadas |

**Interpretación:** E16–E20 indican controles de arquitectura y llamadas transaccionales, pero no se ha trazado toda interacción de repositorios, locks, isolation level, migrations, FKs y casos de rollback. E12 tiene censura condicional de campos de costo en el controlador; la cobertura de todos los canales de exportación exige revisión transversal independiente.

## 3. Pruebas existentes observadas, no ejecutadas en esta inspección

| ID | Ruta de prueba | Blob SHA | Propósito que se aprecia en el código |
| --- | --- | --- | --- |
| T01 | `backend/tests/Solqaryn.Tests/InventarioConcurrencyTests.cs` | `9d7dc5bb592d6aeb9febd6b24c52a309fbca26cc` | Concurrencia ventas/stock; usa helper MySQL, requiere ejecución dirigida |
| T02 | `backend/tests/Solqaryn.Tests/N22OrdenCompraSecurityAuditRegressionTests.cs` | `5bf2a9849f2bcb0d35238a9e7f3d92fdc1a2140e` | Auditoría estricta de aprobación/cancelación de orden |
| T03 | `backend/tests/Solqaryn.Tests/N18ReservaInventarioSecurityAuditTests.cs` | `301c2387462298d5f7f7068116bd915bde20bdf7` | Atributos Authorize/permiso exacto de endpoints de reservas |
| T04 | `backend/tests/Solqaryn.Tests/MovimientoInventarioTenantScopedReadTests.cs` | `2b0c49278255386675a4e758e522428f78b4514f` | Caso A/B de lectura kardex y denegación de solo PublicStoreTenant |
| T05 | `frontend/src/app/core/services/reporte-inventario-reconciliacion.service.spec.ts` | `e3af94a71d89aa2cd3ef2c87004f46414c091e9f` | HttpClientTesting de paging/origen; no prueba integración HTTP real |

El árbol Git contiene también pruebas N16 transferencias, N17 conteos, N18 reservas, N22 órdenes y pruebas de conciliación; no se inventaría su estado exitoso a partir de nombres. Las suites requeridas de API, MySQL, concurrencia y multitenancy deben ejecutarse en CI causal de futuros cambios ejecutables. En este changeset no se alteran rutas, DTO, SQL ni tests.

## 4. Brechas y controles dirigidos siguientes (G01–G10)

| Brecha | Riesgo a comprobar | Evidencia requerida para cierre futuro |
| --- | --- | --- |
| G01 | Recurso de empresa B usado desde token A | Dos tenants y datos A/B en endpoints y repositorios reales |
| G02 | NULL histórico interpretado como global | Prueba negativa con producto/movimiento legado y filtro empresa |
| G03 | Stock duplicado por reserva/compra/venta concurrente | MySQL efímero, reintentos paralelos y stock final reconciliado |
| G04 | Transferencia partida entre almacenes | Simulación de fallo intermedio, rollback y movimiento correlacionado |
| G05 | Reintento de orden/recepción con payload distinto | Misma key con distinto fingerprint: conflicto, sin doble mutación |
| G06 | Costeo y documentos históricos alterados por regla nueva | Snapshots, valor fiscal y movimientos estables antes/después |
| G07 | Consulta/exportación revela costos a usuario sin Finanzas | Contrato backend y payload censurado incluso en paginación |
| G08 | Conteo genera ajuste automático no autorizado | API/RBAC y flujo de aprobación de ajuste tras conteo |
| G09 | Caché/consulta sin tenant o cross-branch | Prueba de sucursal/almacén y ausencia de colisión A/B |
| G10 | UI presenta éxito después de un rollback | Angular/Playwright con error 4xx/5xx, foco y feedback seguro |

Estas **brechas son preguntas de certificación, no vulnerabilidades verificadas**. Resolverlas exige un plan de pruebas positivo/negativo y autoridad por fase, sin activar secretos, pagos, infraestructura, workers o eventos.

## 5. Evidencia de no-regresión y reversión

- A0: diff y hashes de documentos/código existentes. A1: coherencia de fuentes e identificación explícita de límites de prueba. A2: aislamiento tenant/RBAC/PII revisado como **obligaciones de futuro**; ningún código de seguridad alterado. A3: pruebas .NET/Angular/MySQL **NO_APLICA a esta inspección documental** y no se contabilizan PASS. A4: publicar dos Markdown por commit fast-forward CAS, verificar Scope Lock y procedencia del CHANGELOG exact-SHA, readback GitHub/PLAN/COLA/BITACORA.
- A5: revisión independiente por otra Task Javier **PENDIENTE**. Toda evidencia de M01.03 puede alimentar la evaluación de riesgos M01.04 y aceptación M01.05, pero no habilita M01.06, M03 ni QA/main/PROD por inferencia.
- Rollback: `git revert` del commit documental, con revisión previa de consumidores y sin reescritura ni modificación de BITACORA histórica.

**Resultado:** inventario trazable de veinte fuentes de código, cinco pruebas preexistentes y diez brechas a evaluar. Ningún test ejecutable fue corrido en esta microtarea; no se publicaron APIs, esquemas, cambios de configuración, pagos o existencias reales.
