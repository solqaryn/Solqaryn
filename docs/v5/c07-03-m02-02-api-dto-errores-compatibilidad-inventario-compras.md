# SOLQARYN V16 — C07-03.M02.02: contratos API, DTO, errores, validaciones y compatibilidad de inventario/compras

**Microtarea:** C07-03.M02.02, PLAN_MAESTRO fila 1089; owner PRIMARY DEV 05 JAVIER, `TASK_ID=6adc2e55f47c81919f6374e561687076`; `solqaryn/Solqaryn@dev`; PROJECT_SCOPE_LOCK=STRICT, V15 **riesgo R0 DOCUMENTAL**, 2026-11-15.

**Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`, PLAN/COLA/DAG_TECNICO. Resultado exigido: especificar API, DTO, errores, validaciones y compatibilidad, **no implementar código ni afirmar tests HTTP ejecutados**.

## 1. Compatibilidad y dependencias técnicas

`C07-03.M01.06`: BL01–BL16, RB01–RB10, SAL01–SAL12; `M01.03`: veinte archivos originales y cinco pruebas existentes inspeccionados **sin ejecutarlos**; `M01.04`: riesgos R01–R12 hipótesis y D01–D08 pendientes; `M01.05`: aceptación P01–P14 y N01–N14 futura. `M02.01`: doce entidades/relaciones E01–E12, dieciséis invariantes INV01–INV16 y diez REL propuestas. Preflight PLAN H1089=PENDIENTE/I1089 vacío; COLA G1089/O1089=SI; DAG F1050=VALIDADO/G1050=SI.

Mantener DTO y API existentes hasta auditoría del diff real: `backend/src/API/Controllers/AlmacenesController.cs`, `ReservasInventarioController.cs`, `TransferenciasInventarioController.cs`, `OrdenesCompraController.cs`, `RecepcionesCompraController.cs`, `ConteosInventarioController.cs`, `CosteoInventarioController.cs` y `ReportesInventarioReconciliacionController.cs` figuran en M01.03. Nombres de operaciones siguientes representan **contratos de aceptación candidatos**, no rutas ni nuevos endpoints ni evidencia de implementación. No alterar payload, versionado, permisos actuales, existencias, costos, MySQL, JWT/RBAC ni QA/main/PROD. `EmpresaId` siempre se resuelve en backend desde identidad validada y pertenencia a sucursal/almacén, no de un ID editable.

## 2. Doce operaciones contractuales OP01–OP12 (NO IMPLEMENTADAS AQUÍ)

| ID | Operación semántica candidata | Precondición backend y control negativo |
| --- | --- | --- |
| OP01 | ListarAlmacenes | tenant/sucursal/rol autorizado; no enumerar B |
| OP02 | ObtenerUbicaciones | almacén propio, sin NULL global |
| OP03 | ConsultarExistencias | filtrado por empresa, almacén y permisos |
| OP04 | ReservarInventario | disponibilidad atómica; sin sobrerreserva |
| OP05 | ConsumirOLiberarReserva | transición válida/owner, idempotencia |
| OP06 | CrearOrdenCompra | Idempotency-Key+fingerprint y RBAC |
| OP07 | RegistrarRecepcionCompra | saldo pendiente, sin recepción duplicada |
| OP08 | IniciarTransferencia | almacenes distintos, tenant correcto |
| OP09 | ConfirmarTransferencia | movimiento origen/destino transaccional |
| OP10 | RegistrarConteoBorrador | no aplicar ajuste sin autorización |
| OP11 | ObtenerReporteReconciliacion | filtro empresa y redacción de costo sin Finanzas |
| OP12 | ConsultarKardexCosteo | permiso por recurso, historia inmutable |

Los nombres OP son orientativos y deben mapearse a rutas existentes sin romper compatibilidad. No incluir ni ejecutar request HTTP desde esta microtarea.

## 3. Doce DTO contractuales DTO01–DTO12, no C# nuevos

| ID | Campos de contrato candidato | Regla |
| --- | --- | --- |
| DTO01 | AlmacenResumen(Id,SucursalId) | comprobar Sucursal.EmpresaId |
| DTO02 | UbicacionResumen(Id,AlmacenId) | parent autorizado |
| DTO03 | ExistenciaConsulta(AlmacenId,VarianteId) | nunca tenant desde caller sin verificar |
| DTO04 | ReservaSolicitud(VarianteId,Cantidad,Key) | cantidad >0, límite y concurrencia |
| DTO05 | ReservaTransicion(Id,Accion,Version) | CAS/estado/rol |
| DTO06 | OrdenCompraSolicitud(ProveedorId,Lineas,Key) | fingerprint y límites |
| DTO07 | RecepcionSolicitud(OrdenId,Lineas,Key) | suma <= pendiente |
| DTO08 | TransferenciaSolicitud(Origen,Destino,Lineas) | no mismo almacén ni tenant B |
| DTO09 | ConfirmarTransferencia(Id,Version) | rol, estado, ETag |
| DTO10 | ConteoBorrador(Id,Lineas,Version) | aprobación separada |
| DTO11 | ReconciliacionFiltro(AlmacenId,Cursor) | paginación y costo censurado |
| DTO12 | KardexVista(MovimientoId,Origen,Correlacion) | proyección mínima, sin secreto |

Los campos son guías de diseño, no modificaciones de DTO reales. Validar serialización, límites, overflow decimal y compatibilidad hacia atrás antes de cambios R2.

## 4. Doce errores seguros ERR01–ERR12

| ID | Situación futura | Resultado sin filtrar datos ajenos |
| --- | --- | --- |
| ERR01 | actor anónimo/JWT inválido | 401 |
| ERR02 | RBAC/rol revocado | 403 |
| ERR03 | almacén/sucursal de empresa ajena | 403/404 sin enumeración |
| ERR04 | jerarquía NULL legacy no verificable | 422/fail-closed |
| ERR05 | cantidad negativa, cero o overflow | 422 |
| ERR06 | stock insuficiente | 409/422, cero escritura |
| ERR07 | reserva consumida/liberada previamente | 409, no doble efecto |
| ERR08 | misma key y fingerprint divergente | 409 |
| ERR09 | recepción excede saldo | 409, no kardex |
| ERR10 | transferencia origen=destino/tenant B | 422/403 |
| ERR11 | ETag/versión obsoletos | 409 sin lost update |
| ERR12 | error de BD/proveedor/timeout | 503 mínimo sin SQL/secrets, rollback |

Estados HTTP son **contrato futuro para validación contra API existente**, no afirmación de comportamiento actual.

## 5. Dieciséis validaciones VAL01–VAL16

| ID | Invariante negativa obligatoria en futuro |
| --- | --- |
| VAL01 | JWT/membresía/empresa verificados por operación |
| VAL02 | tenant A no consulta ni escribe recursos de B |
| VAL03 | Sucursal/Almacén ambos autorizados, NULL no global |
| VAL04 | AlmacenId/UbicacionId de la misma jerarquía |
| VAL05 | cantidad positiva y dentro de precisión/límites |
| VAL06 | reserva y disponibilidad de la misma variante/almacén |
| VAL07 | concurrencia serializable/lock probado; no sobrerreserva |
| VAL08 | idempotency key no se reutiliza con payload distinto |
| VAL09 | orden, proveedor y recepción de mismo tenant |
| VAL10 | suma de recepciones no excede pendiente |
| VAL11 | transferencia origen distinto a destino y misma empresa |
| VAL12 | conteo no aplica ajuste automáticamente |
| VAL13 | movimiento/costeo con correlación y rollback atómico |
| VAL14 | costo se censura a rol sin Finanzas |
| VAL15 | paginación/cursor no cruza tenant |
| VAL16 | logs/error no incluyen PII, secretos, SQL o IDs de B |

## 6. Doce pares P/N T01–T12 FUTUROS_NO_EJECUTADOS

| ID | Positivo futuro | Negativo/error futuro |
| --- | --- | --- |
| T01 | listar almacenes propios | B no lista almacenes A |
| T02 | ubicación propia resuelve | NULL heredado no da acceso global |
| T03 | stock suficiente permite reserva | dos reservas simultáneas no duplican |
| T04 | transición de reserva válida | doble consumo falla |
| T05 | key+fingerprint idénticos dan resultado idempotente | fingerprint diferente da 409 |
| T06 | recepción parcial correcta actualiza saldo | replay no duplica movimiento |
| T07 | transferencia entre dos almacenes A | origen=destino o B se rechaza |
| T08 | conteo autorizado guarda borrador | sin rol no publica ajuste |
| T09 | usuario Finanzas ve costo autorizado | usuario sin Finanzas no ve costo |
| T10 | kardex muestra origen acotado | tenant B no lee transacciones A |
| T11 | version CAS actual modifica | ETag stale no altera historia |
| T12 | backend confirma transacción antes de UI éxito | timeout/5xx no confirma operación |

**12 pares descritos, 0/12 ejecutados**, tampoco 0 de 12 PASS: NO_MEDIDO. Pruebas obligatorias para código real R2: xUnit positivo/negativo/concurrencia, JWT/RBAC/tenant A/B, MySQL 8.4 efímero Category=Integration para transacciones/EF, logs, rollback; Angular/Playwright si cambia UI. No contar CI irrelevante ni mocks como prueba SQL real.

## 7. A0–A5, no regresión y reversión

- **A0:** sólo documento y nueva sección autocontenida CHANGELOG, coherentes con M01.06/M02.01 y el resultado M02.02.
- **A1:** 12 operaciones, 12 DTO, 12 errores, 16 validaciones, 12 pares futuros verificables; ninguna prueba HTTP ejecutada.
- **A2:** denegación tenant A/B, revocación JWT/RBAC, NULL legacy, redacción de costo, transacciones y auditoría descritas sin cambios runtime.
- **A3:** cotejo de inventario M01.03/M01.06, NO_APLICA build .NET, Angular, MySQL, HTTP, Playwright al diff R0 puramente Markdown.
- **A4:** publicación fast-forward/CAS `dev`, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success exact SHA, PLAN/COLA/DAG/BITACORA A:Z readback.
- **A5:** otra Task Javier distinta del autor revisa diff, requisitos y evidencia; A5 pendiente no habilita M03/QA/main/PROD.

**Rollback de documento:** `git revert` normal del SHA tras releer HEAD, sin forzar rama ni borrar BITACORA. Ninguna mutación de stock, recepciones, proveedores, cuentas o datos productivos.
