# ERP-N2.9 — Evaluación de proveedores

## Estado y alcance

ERP-N2.9 materializa una evaluación objetiva por recepción de compra ya materializada. El alcance certificado conserva hechos observables de la orden y la recepción: proveedor, orden, recepción, fecha esperada UTC, fecha real UTC y cantidades ordenada, aceptada, dañada y sobrante.

No se definen ni se infieren scoring, ranking, pesos, umbrales, SLA comerciales ni fórmulas de calificación que no estén expresamente implementadas. Esas extensiones requieren una decisión funcional posterior y no forman parte de N2.9.

## Autoridad funcional

La generación exige una `RecepcionCompra` existente en estado `Recibida` y con `FechaRecepcionUtc`. La `OrdenCompra` asociada debe existir, tener `FechaEsperadaUtc` y un `ProveedorId` válido. La identidad del proveedor se deriva de la orden persistida y no de datos suministrados libremente por el cliente.

La operación es idempotente por recepción: si ya existe una evaluación para `RecepcionCompraId`, se actualizan los hechos observables; si no existe, se crea. Las cantidades se derivan de `OrdenCompra.Detalles` y de los totales materializados de la recepción.

## Persistencia

La migración `20260823042000_N2_9_EvaluacionProveedorPersistencia` crea `EvaluacionesProveedor` con FKs `Restrict` hacia `Proveedores`, `OrdenesCompra` y `RecepcionesCompra`, cantidades `decimal(18,4)`, checks no negativos e índices para recepción, orden y proveedor+fecha de recepción.

La migración incorpora guardas pre/post y `Down()` fail-closed: no permite retirar la tabla mientras existan evaluaciones. El snapshot EF queda actualizado mediante Part22. Los scripts preflight/postcheck de N2.9 verifican precondiciones, estructura, FKs e integridad sin inventar históricos.

## API y RBAC

Controller: `EvaluacionesProveedorController`.

- `GET /evaluaciones-proveedor` — autenticación + `Compras/Ver`.
- `GET /evaluaciones-proveedor/{id}` — autenticación + `Compras/Ver`.
- `POST /evaluaciones-proveedor/recepciones/{recepcionCompraId}/generar` — autenticación + `Compras/Crear`.

La generación retorna `201 CreatedAtAction`. No existe endpoint de scoring/ranking ni mutación manual de métricas.

## Auditoría y seguridad

La generación/actualización se ejecuta dentro de unidad transaccional y registra auditoría estricta mediante `IAuditoriaService.RegistrarEstrictoAsync`. Un fallo en la autoridad de recepción/orden/proveedor o en las reglas de negocio falla cerrado antes de producir una evaluación válida.

## Frontend

El frontend de Compras consume el listado, detalle y generación de evaluación mediante los contratos HTTP reales. Las rutas/acciones quedan protegidas con permisos runtime; la UI representa fechas y cantidades observables y no fabrica un score inexistente.

## Evidencia causal

- N2.9.C persistencia: `69419edf2ccb62b7d5849d242ca723f6d64b9ee5`; Development `32617575595`, Acceptance `32617575668`, Fase 8 `32617575661`, M13 `32617575687`, Recovery MySQL `32617575639` — SUCCESS.
- N2.9.D Application/API: `ca03082ff6bdbb587a58ee65052dd3b70df47957`; Development `32622074034`, Acceptance `32622073980`, Fase 8 `32622073999`, M13 `32622074016`, Recovery MySQL `32622073966` — SUCCESS.
- N2.9.E Frontend/UX: `1d7c10a9ee0132032716144ad726c3261522868f`; Development `32626602367`, Acceptance `32626602450`, Fase 8 `32626602397`, M13 `32626602394`, Recovery MySQL `32626602428` — SUCCESS.
- N2.9.G QA/regresión/CI: `19db085b630814b814f8c877010cc83f665b27a3`; Development `32627965927`, Acceptance `32627965884`, Fase 8 `32627965969`, M13 `32627965880` — SUCCESS.
- N2.9.H pre-cierre/control-plane: `16f1c70c5d09babe68915f8eeee97cf96b4fa755`; Development `32629015701`, Acceptance `32629015690`, Fase 8 `32629015682`, M13 `32629015708` — SUCCESS.

## Fuera de alcance y continuidad

No se toca `main`, Producción, secretos, despliegues ni merge del PR #2. El cierre de ERP-N2 no habilita ERP-N3 hasta superar el gate formal `GATE-N2` definido por el Plan Maestro/COLA.