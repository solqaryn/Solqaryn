# SOLQARYN V16 - C07-03.M01.06: bloqueo, rollback y salida de inventario, compras y operaciones

**ID de plan:** C07-03.M01.06, fila 1087. **Actor:** PRIMARY DEV 02 JAVIER_CHATGPT, Task ID 6af16bb9492c8191b8e8c7d606f11507. **Riesgo:** R0, exclusivamente documental. **Ambito:** solqaryn/Solqaryn rama dev, PROJECT_SCOPE_LOCK=STRICT. **Autoridad:** STANDARD DE IMPLEMENTACION DE AUTOMATIZACIONES V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA.

## 1. Objetivo y frontera de esta entrega

Cerrar la preparacion de C07-03 M01 con un contrato de bloqueos, reversion y condiciones comprobables para una **futura** implementacion y no-regresion de almacenes, sucursales, compras, recepciones, reservas, movimientos, transferencias, conteos, costos y reconciliacion. No se agrega codigo ejecutable, rol, migracion EF, MySQL, API, worker, flag, secreto, interfaz ni alteracion de existencias.

Los cinco predecesores M01.01-05 se revisan por sus documentos, contratos y A5 independientes. Fuentes de evidencia:
- M01.01: docs/v5/c07-03-m01-01-alcance-inventario-compras-operaciones.md; no-regresion NR01-NR12.
- M01.02: docs/v5/c07-03-m01-02-actores-interfaces-inventario-compras-operaciones.md; A01-A12, I01-I12, fronteras B01-B10.
- M01.03: docs/v5/c07-03-m01-03-inventario-codigo-contratos-evidencias.md; evidencia de 20 referencias de codigo y 5 archivos de test, no ejecucion.
- M01.04: docs/v5/c07-03-m01-04-riesgos-supuestos-decisiones.md; R01-R12 hipoteticos, S01-S08 no certificados, D01-D08 pendientes.
- M01.05: docs/v5/c07-03-m01-05-criterios-aceptacion-inventario-compras.md; P01-P14 y N01-N14 son oraculos **NO_EJECUTADOS**.

A5 independiente en la fase documental significa aprobacion del artefacto, no que transacciones, endpoint HTTP, JWT/RBAC, aislamiento MySQL A/B, stocks o costos esten certificados.

## 2. Condiciones de BLOQUEO BL01-BL16

| Gate | Bloqueo causal y evidencia exigible |
| --- | --- |
| BL01 | Usuario no autenticado, JWT caducado o empresa inactiva: prohibir acceso y mutacion |
| BL02 | Membresia/rol revocado o permiso por accion no verificado en backend: denegar, no confiar en UI |
| BL03 | Empresa A solicita sucursal, almacen, costo o existencia B: rechazo seguro sin enumerar B |
| BL04 | Almacen sin sucursal, herencia NULL o propietario no verificable: fallo cerrado, sin fallback a tenant default |
| BL05 | Reserva excede disponibilidad o se consume dos veces: exigir prueba de carrera y reconciliacion |
| BL06 | Orden de compra reintentada con misma key y payload divergente: conflicto idempotente, cero efecto duplicado |
| BL07 | Recepcion parcial/repetida con saldo incorrecto: abortar hasta prueba SQL transaccional real |
| BL08 | Transferencia entre almacenes invalidos, mismo almacen o tenant ajeno: no despachar ni recibir |
| BL09 | Reenvio de despacho/recepcion duplica movimiento o kardex: bloquear efecto y auditar intento |
| BL10 | Conteo genera ajuste confirmado sin autorizacion/estado correcto: conservar borrador, no alterar stock |
| BL11 | Costeo historico cambia sin politica probada, redondeo o snapshot inmutable: NO_GO financiero |
| BL12 | Reporte/exportacion revela costos o PII a rol sin Finanzas o tenant B: denegar y redactar |
| BL13 | Fallo entre saldo, movimiento, kardex y capa de costo deja estado parcial: requiere rollback atómico demostrado |
| BL14 | Timeout, red offline o doble clic lleva a UI a informar exito no confirmado por servidor: bloquear declaracion de exito |
| BL15 | CI pertinente del SHA esta queued/in_progress/cancelled/skipped/failed o no ejecuta MySQL Category=Integration exigible: NO PASS |
| BL16 | Falta owner exclusivo, A0-A4 causal, A5 independiente, DAG VALIDADO/SI o permiso ambiental: no liberar sucesora ni QA/PROD |

Un bloqueo puede ser local a su superficie; no sustituye los estados institucionales ni habilita cambiar COLA/DAG a SI manualmente.

## 3. Recuperacion y rollback RB01-RB10 (procedimientos FUTUROS)

| Ref | Fallo previsto | Respuesta de recuperacion exigida |
| --- | --- | --- |
| RB01 | JWT/rol/membresia revocados | Revalidar autoridad backend, anular nuevas operaciones y registrar auditoria redactada |
| RB02 | Intento tenant A hacia B | Rechazar, no modificar B y comprobar ausencia de fuga en respuestas, cache y logs |
| RB03 | Datos legacy NULL o jerarquia rota | Cuarentena del recurso, reparar con decision y migracion propia, no exponer globalmente |
| RB04 | Doble reserva/consumo por concurrencia | Reconciliar reservas y saldos dentro de transaccion ensayada, sin añadir stock artificial |
| RB05 | Retry de orden/recepcion con clave repetida | Comparar fingerprint, devolver resultado original valido o conflicto sin segundo movimiento |
| RB06 | Transferencia parcialmente confirmada | Revertir transaccion indivisible o aplicar compensacion autorizada comprobando origen y destino |
| RB07 | Conteo o ajuste no autorizado | Conservar borrador, bloquear posteo y preservar historico de aprobacion |
| RB08 | Error de costeo o conciliacion | Conservar snapshot, validar historial con fixtures MySQL efimeros y plan de restauracion aprobado |
| RB09 | Error de UI, red o cache | Mostrar estado de error o pendiente y consultar servidor; nunca emitir exito o doble escritura |
| RB10 | Fallo CI, Scope Lock, revisiones o release | Mantener VALIDANDO/owner, corregir causa con commit normal, revalidar SHA y conservar BITACORA |

Ninguno de RB01-RB10 se ha ejecutado aqui. El rollback **de este documento** es exclusivamente un git revert no destructivo con HEAD/CAS y evento BITACORA nuevo. Un git revert de Markdown no restaura MySQL ni revierte existencias.

## 4. Liberacion documental SAL01-SAL12

| Gate | Condicion |
| --- | --- |
| SAL01 | M01.01-05 LISTO y A5 de Task Javier distinta de sus respectivos autores |
| SAL02 | Fuentes y blobs de contratos revisados, sin atribuir pruebas runtime ejecutadas |
| SAL03 | BL01-BL16 especificados como negativos bloqueantes y respuestas observables |
| SAL04 | RB01-RB10 contienen estrategia y limitacion de rollback |
| SAL05 | NR01-NR12, R01-R12, S01-S08 y D01-D08 preservados sin falsos cierre de decisiones |
| SAL06 | P01-P14/N01-N14 siguen NO_EJECUTADOS hasta fase de pruebas autorizada |
| SAL07 | Tenant A/B, NULL legacy, roles/revocacion y coste financiero no quedan implicitamente abiertos |
| SAL08 | No DDL/MySQL, endpoints, JWT, Angular, DNS ni cambios productivos por este M01.06 |
| SAL09 | CHANGELOG AI tiene encabezado y viñetas autocontenidas, sin intercalado |
| SAL10 | Git dev commit fast-forward/CAS, Scope Lock y control V5 completed/success del SHA causal |
| SAL11 | Estado LISTO solo tras A0-A4 con readbacks de PLAN/COLA/DAG/BITACORA |
| SAL12 | A5 de esta M01.06 queda pendiente de **otra** Task Javier; M02 y runtime requieren sus propios gates |

## 5. Auditorias y no-regresion posterior

**A0:** documento y seccion de CHANGELOG solamente. Revisar BL01-16, RB01-10 y SAL01-12; ausencia de secretos, permisos, ejecutables y mezcla de cambios de otros scopes.

**A1:** cobertura documental de casos positivos y negativos, sin confundir P01-P14/N01-N14 con xUnit, HTTP, Playwright ni transacciones ya ejecutadas. **A2:** matrices JWT/RBAC, empresa A/B, almacen/sucursal, costo, privacidad, revocacion y fallback fail-closed.

**A3:** cotejo con las cinco preparaciones M01 anteriores y sus evidencias. Compilacion .NET, Angular, MySQL efimero, fixtures e integracion HTTP=NO_APLICA a este diff Markdown-only; seran obligatorias en las futuras microtareas que modifiquen esas superficies.

**A4:** publicacion directa y serializada en dev, SHA exacto, diff de dos documentos, checks verify-solqaryn-project-scope y V5 - Sin encabezados vacios ni atribucion cruzada completed/success, BITACORA A:Z y readback H/I/K, COLA y DAG. Cancelled/Skipped/Queued/In progress no son PASS.

**A5:** independiente de otra de las cinco Tasks Javier, con revision del documento, autor, SHA, diff, reglas y pruebas aplicables. LISTO DEV o A5 documental no significan aprobacion de QA/main/PROD.

**Estado de esta entrega:** contrato de salida documental. No se modificaron saldos, reservas, gastos, pagos, servidor ni datos reales.
