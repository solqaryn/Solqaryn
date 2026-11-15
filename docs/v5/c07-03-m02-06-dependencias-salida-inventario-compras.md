# SOLQARYN V16 — C07-03.M02.06 dependencias técnicas y salida documental de inventario/compras

**Microtarea:** C07-03.M02.06, PLAN_MAESTRO fila 1093. **Autor/owner:** PRIMARY DEV 04 JAVIER, `TASK_ID=6aeacb7820ac8191b7d768a6f33dfa09`; solo `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Clasificación V15:** R0 documental, sin migraciones, operaciones, runtime o datos modificados.

**Fuente institucional:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`) y PLAN_MAESTRO, COLA, DAG_TECNICO y BITACORA. **Resultado exigido:** fijar dependencias técnicas y criterios de salida de M02 para preservar almacenes, compras, reservas de inventario, costeo y trazabilidad, dejando los riesgos y gates de implementación real sin liberar artificialmente.

## 1. Prerrequisitos A5 y control de selección

Para el claim exclusivo, la fuente dinámica devolvió PLAN `H1093=PENDIENTE` e `I1093=vacío`; COLA `G1093=SI/O1093=SI`; DAG `F1054=VALIDADO/G1054=SI`. Precedentes formales del DAG:
- `C07-03.M01.01–06+A5` en fase preparatoria, sin convertir documentos en pruebas runtime.
- `C07-03.M02.01–05+A5` auditados separadamente antes de abrir M02.06: modelo de entidades e invariantes, API/DTO/errores, persistencia/índices/migración conceptual, opciones tipadas y flags inertes, eventos/idempotencia/correlación futuros.
- `C07-01.M01.06+A5` como requisito transversal. El DAG vigente confirmó su gate; si cambia, releerlo antes de cualquier sucesora.

En particular, `C07-03.M02.04` tuvo fallo previo de Scope Lock por nombre conceptual de una opción y fue reparado en SHA `c992a81bc165083c6a46dcd6a558434b14bccae0` (prefijos españoles en documento) sin apagar checks. Su A5 independiente comprobó la reparación y preservó el fallo histórico; **el SHA original fallido no se declara PASS**. `C07-03.M02.05` recibió A5 independiente documentado sobre SHA `597bf5fff593f6a21b77a53775a18347d1cb9cd9`. Una aprobación documental no prueba que ya exista una transacción MySQL segura.

La arquitectura real ASP.NET Core 10 Domain <- Application <- Infrastructure, API composición, Oracle MySQL EF Core 10 en backend y Angular 22.2.1 como cliente se conservan. El servidor deriva tenant con JWT, membresía, empresa, sucursal, almacén, rol y recurso. Nunca `EmpresaId`, `Host`, query, parámetro, slug o header del navegador otorgan propiedad. Las fuentes de stock, compra, recepciones y kardex ya existentes deben reutilizarse sin duplicar modelos.

## 2. Dieciséis gates de salida G01–G16 — criterios, NO pruebas de runtime

| Gate | Condición contractual futura o documental | Rechazo o evidencia requerida |
| --- | --- | --- |
| G01 | Único plan V5, identidad Javier, repo/rama dev, Scope Lock STRICT | NO_GO sin identidad, permisos o SHA verificados |
| G02 | M01.01–06 LISTO+A5 por otra Task y BLOQUEOS BL01–BL16 no omitidos | No interpretar inventario de documentos como prueba de producto |
| G03 | M02.01–05 LISTO+A5 real, autor/revisor distintos y SHA/blobs causales | Si A5_FAIL/pendiente, no habilitar sucesora técnica |
| G04 | DAG F1054 VALIDADO/G1054 SI, COLA G1093/O1093 SI al claim | No cambiar fórmulas G/O ni reservar múltiples WIP |
| G05 | Entidades/relaciones M02.01 ligadas a esquema existente, tenant A/B | No crear modelos paralelos de stock/compra |
| G06 | Contratos OP/DTO/ERR de M02.02 con roles y ownership | No confiar en EmpresaId, sucursal o almacén del cliente |
| G07 | Persistencia M02.03 P/IDX/MIG referencial, índices no aplicados | No ejecutar EF migration ni DDL real por documento |
| G08 | Configuración M02.04 OPT/VAL/flags propuesta OFF/fail-closed | No habilitar escritura por flags, rol revocado o NULL legado |
| G09 | Eventos M02.05 EV/IDEM/COR/LIM futuros sin emisor | No activar dispatcher, webhooks, outbox ni jobs |
| G10 | Saldo, reservas, recepciones y transferencias conservan atomicidad | MySQL 8.4 efímero Category=Integration exigible al tocar EF/stock R2 |
| G11 | Costos, proveedores, PII y auditoría mínimos por permisos | Sin rol Finanzas los costos quedan censurados |
| G12 | Pruebas positivas, negativas, fronteras y concurrencia para M03 | Un mock/doc/green build no prueba flujo MySQL/HTTP |
| G13 | Rollback probado ante parcialidad antes de promoción real | No cambiar existencias ni kardex si falla transacción |
| G14 | PLAN/COLA/DAG + BITACORA idempotente 26 columnas por transición | Razas de owner/HEAD => abortar CAS sin doble cierre |
| G15 | Diff únicamente documento+CHANGELOG, Scope Lock y V5 exact SHA SUCCESS | CANCELLED/SKIPPED/IN_PROGRESS/FAIL nunca PASS requerido |
| G16 | A5 independiente futura de M02.06 antes de habilitar M03 cuando DAG exija | LISTO_DEV_R0 no autoriza QA/main/PROD ni migración |
La **salida de esta unidad R0** consiste en este documento, una sección completa de CHANGELOG, fast-forward/CAS en dev, Scope Lock y procedencia V5 exact SHA y readback PLAN/COLA/DAG/BITACORA. **No consiste** en activar M03, crear un webhook/worker, ejecutar una migración ni emitir un evento de inventario. La siguiente microtarea técnica debe acreditar su propia elegibilidad y controles R2.

## 3. Ocho rutas de reversión RB01–RB08 y propiedad

| ID | Recuperación futura o de documento | Condición de seguridad |
| --- | --- | --- |
| RB01 | Revertir el commit documental con git revert normal bajo HEAD actual | BITACORA se conserva aditiva; no force-push |
| RB02 | Invalidar temporalmente cualquier decisión de flag futuro no acreditada | Sin cambiar runtime/configuración actual |
| RB03 | En implementación futura, conservar stock/saldo anterior ante fallo de reserva | Test de doble consumo y rollback MySQL real |
| RB04 | Restituir coherentemente transferencia origen/destino ante error | No dar éxito hasta commit verificado |
| RB05 | Rechazar recepción parcial duplicada y reconciliar kardex | Reintento idempotente sin doble movimiento |
| RB06 | Bloquear lectura/costo no autorizado por rol/tenant | Prueba negativa A/B y revocación |
| RB07 | Restaurar señal/versión previa y evidencia del SHA | Prueba de regresión exact SHA y readback |
| RB08 | Recalcular sucesores sólo tras gates verdaderos | A5_FAIL histórico permanece hasta revisión real |
Reversión de **este change R0**: `git revert` aditivo del SHA documental tras comparar HEAD y exclusividad. No force-push, reset, alteración del plan, mutación de datos reales, reescritura histórica de BITACORA ni cambios de schedules. Para operación R2 de stock, el rollback real exige transacción y prueba MySQL 8.4 efímera, no una promesa de esta matriz.

## 4. Doce pares de aceptación P01/N01–P12/N12 — FUTUROS_NO_EJECUTADOS

| ID | Positivo que tendrá que demostrarse | Negativo/frontera que deberá fallar |
| --- | --- | --- |
| P01/N01 | Reserva válida empresa A con stock suficiente | dos reservas concurrentes no sobreconsumen |
| P02/N02 | Almacén/sucursal A autorizado | tenant B/NULL legado no accede a A |
| P03/N03 | Orden compra con idempotency key consistente | misma key con otro fingerprint se rechaza |
| P04/N04 | Recepción parcial acumula pendiente correcto | replay no duplica kardex/costo |
| P05/N05 | Transferencia válida entre almacenes A | destino B u origen igual destino se bloquea |
| P06/N06 | Conteo solo borrador hasta aprobación | rol sin permiso no crea ajuste real |
| P07/N07 | Costos con rol Finanzas autorizado | sin Finanzas se censuran en API/exportación |
| P08/N08 | Membresía vigente permite acción | revocación durante retry no reautoriza |
| P09/N09 | Eventos solo tras commit transaccional | fallo previo a commit no emite éxito |
| P10/N10 | CAS versión vigente modifica una vez | stale update/retry no pisa versión |
| P11/N11 | Rollback seguro preserva existencias y auditoría | rollback incompleto o pérdida de historia falla |
| P12/N12 | CI exact SHA de integración MySQL8.4 con 0 FAIL | SKIPPED/CANCELLED o Category!=Integration no acredita DB |
**Ejecución por esta microtarea: 0/12 pares; MySQL, backend xUnit, HTTP, Angular, Playwright, datos productivos y proveedor NO_APLICA al diff R0.** Al implementar Domain/Application/Infrastructure futuros se exigirán build .NET Release y xUnit real, negativos tenant A/B, rol revocado, balance concurrente, contratos de error, y si hay EF/MySQL, `Category=Integration` efectivamente ejecutada sobre MySQL 8.4 **efímero** con logs y 0 FAIL. `Category!=Integration` y un job de migraciones verde sin test no certifican DB. Una UI no es autoridad de existencia de inventario ni de costo.

## 5. Dependencias posteriores, no-regresión y riesgos abiertos

Los contratos M02.01–05 definen **propuestas**, no capacidades activas. Antes de entrar en M03 revisar nuevamente: PLAN H=PENDIENTE/I vacío de esa unidad; COLA G/O=SI; DAG F=VALIDADO/G=SI; A5 exigidas sobre SHA/blobs aplicables; seguridad y ownership; archivos, tests y migraciones específicas; CI requerida no cancelada y libre de conflictos. `VALIDANDO` no equivale a `LISTO+A5` ni libera por sí solo M03.

No desactivar IAM/JWT/RBAC, roles de Finanzas, validación empresa/sucursal, transacciones, audit logs ni límites de costo. Null legacy es rechazo o cuarentena, no accesibilidad global. Preservar reservas, existencias, ajustes y recepciones ya operativas y nunca escribir en Aiven, QA/main/PROD o DNS. Gasto, migración productiva, brokers, webhooks, Tasks y event dispatch permanecen NO_AUTORIZADOS.

Resultados de pruebas de rendimiento/carga, recuperación de stock real y conciliación de existencias en este documento = `NO_MEDIDO`; no fabricar tasa de defectos ni aprobar finanzas por contrato textual. Cualquier hallazgo P0/P1 bloqueará el scope técnico específico y requerirá reparación con pruebas causales y revisión A5 distinta.

## 6. A0–A5, evidencia y handoff

- **A0:** fuente V5, M01 y M02.01–05, DAG/PLAN/COLA, dieciséis gates, ocho rutas RB y doce pares P/N definidos; diff exactamente este Markdown y una sección autocontenida de `CHANGELOG_AI.md`.
- **A1:** oráculos de positivos, negativos, fronteras, error y concurrencia revisables pero **0 tests runtime ejecutados**.
- **A2:** aislamiento tenant A/B, JWT/RBAC, revocación, roles de costo, idempotencia, rollback y NULL legacy contractuales; enforcement backend no modificado.
- **A3:** cotejo con precedentes M01/M02; build .NET, MySQL 8.4, EF, Angular, navegador, proveedor y stock reales `NO_APLICA` al R0 Markdown.
- **A4:** publicación fast-forward/CAS en dev, checks `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` en SHA causal, eventos BITACORA A:Z idempotentes y readback PLAN/COLA/DAG.
- **A5:** otra Task Javier distinta del autor revisa criterios, SHA y blobs y puede emitir FAIL. Ni LISTO_DEV ni A5 de este documento concede acceso QA/main/PROD. No se modifican fórmulas G/O para adelantar trabajo.

**Handoff:** Esta unidad cierra solamente las dependencias documentales M02.06; los implementadores futuros deberán verificar de nuevo el DAG y pruebas R2 auténticas para cualquier modificación de stock, compras, costos o datos.
