# SOLQARYN V17 — C07-02.M01.06: bloqueo, rollback y liberación condicionada del storefront y checkout

**Microtarea:** C07-02.M01.06; **PLAN_MAESTRO:** fila 1051; **autor:** PRIMARY DEV 02 — JAVIER_CHATGPT, TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Fecha:** 2026-11-15. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), mandatos V15/V16/V17. **Repos/entorno:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

## Resultado exacto y límites

Se documenta el **bloqueo, reversión y condición de liberación** del lote preparatorio M01 de storefront, carrito, validación de checkout y futuras transiciones pedido/cobro. Se fijan evidencias observables y rechazo seguro, **sin modificar** API, Domain, Application, Infrastructure, Angular, MySQL, JWT/RBAC, permisos, proveedor de cobro, host/DNS, worker, colas, secretos, rutas, datos reales ni configuraciones productivas.

**LISTO_DEV de este documento no acredita checkout productivo ni ejecución de pruebas futuras.** La ruta existente `POST tienda/checkout/validar` calcula y valida sin convertir automáticamente el resultado en pedido, reserva, pago o factura. HMAC/replay, stock transaccional, conciliación PSP y aislamiento por host requieren pruebas dirigidas posteriores. M02 sigue sometida a A5 independiente de **esta** unidad, DAG, COLA, owner exclusivo y preflight por ID; no abrir sucesora sólo por estado VALIDANDO o LISTO.

## Fuente causal M01 y autoridades operativas

El preflight institucional de esta unidad exige `PLAN_MAESTRO!H1051=PENDIENTE/I1051 vacío`, `COLA!G1051=SI/O1051=SI`, `DAG_TECNICO!F1012=VALIDADO/G1012=SI`, un solo escritor propio y candado atómico V17 `SQV17_C07_02_M01_06` (namedRangeId `2000001051`). La fórmula DAG contiene antecedentes documentales `C01-01..04/C02-01..04/C05-01..04/C06-02/C07-01 M01.01–05` y, adicionalmente, `C07-02.M01.01–05+A5`, `C06-02.M01.06+A5` y `C07-01.M01.06+A5`. Su `SI` habilita **sólo** esta revisión preparatoria; al escribir o al avanzar HEAD se relee el estado, nunca se asigna `SI` manual.

| ID | Documento institucional anterior | Blob Git verificado | Límite |
| --- | --- | --- | --- |
| M01.01 | `docs/v5/c07-02-m01-01-alcance-storefront-checkout.md` | `26310ed89f7510bc7e7c49b88ff1c2e3c315f49f` | invariantes NR01–NR12, pruebas futuras |
| M01.02 | `docs/v5/c07-02-m01-02-actores-interfaces-storefront-checkout.md` | `aecbb48a27b1276a1c6b6cda1e4f8260a7d284f0` | actores, interfaces, fronteras |
| M01.03 | `docs/v5/c07-02-m01-03-inventario-causal-storefront-checkout.md` | `72193a45ee5d3745f3ea51e02398a47b66d95853` | 12 fuentes y G01–G10, no vulnerabilidades certificadas |
| M01.04 | `docs/v5/c07-02-m01-04-riesgos-supuestos-decisiones.md` | `9271770c2901b5b4e19163e6121eea5a5d53758a` | R01–R12, S01–S08 y D01–D08 **PENDIENTES** |
| M01.05 | `docs/v5/c07-02-m01-05-criterios-aceptacion-storefront-checkout.md` | `dd87d1a7474f4effa47b6253cd23e8c826d2990c` | P01–P14 y N01–N14 **FUTUROS_NO_EJECUTADOS** |

La revisión A5 de M01.03–05 fue independiente de sus respectivos autores y se documentó mediante candados V17 y readback en PLAN/BITACORA. Esto **no cierra** las decisiones D01–D08, ni prueba los casos de seguridad o pago pendientes. Las referencias de M01.01–02 son documentos preparatorios y no autoridad para cobros reales.

## Dieciséis gates de salida y bloqueos comprobables

| Gate | Condición exigible | Rechazar cuando... |
| --- | --- | --- |
| G01 | Skill canónica, Scope Lock STRICT, rama dev, identidad Javier y SHA comprobados | difiere gobierno o no hay permiso |
| G02 | PLAN/COLA/DAG consistentes y todos los antecedentes técnicos y A5 requeridos verdaderos | algún antecedente sigue PENDIENTE/VALIDANDO/A5_FAIL |
| G03 | V17 named range exclusivo por fila; H/I y BITACORA con readback sin otro escritor | lock ocupado, CAS no confirmado, ownership contradictorio |
| G04 | Binding `host/storeKey/EmpresaId` reconocido **en backend** y proxy de confianza | cabecera host no confiable selecciona otro tenant |
| G05 | Producto, variante, precio y medio publicados del tenant A no mezclan B | ID de B, draft o costo privado expuesto desde A |
| G06 | Caché de catálogo/SEO segmentada e invalidada por host, tenant y versión | respuesta cacheada de A aparece en B |
| G07 | `checkout/validar` recalcula precio, impuesto, stock, límites de carrito y errores server-side | navegador impone precio o acepta stock agotado |
| G08 | Validación, reserva y pedido durable se distinguen; transacción e integridad verificables | pantalla o redirect se contabiliza como pedido/cobro |
| G09 | Idempotencia durable por tenant, actor, operación y huella con prueba concurrente | doble clic, replay o clave cruzada duplica operación |
| G10 | HMAC incorpora ámbito, payload, TTL, nonce, versión y rotación autorizadas | firma inválida, truncada, vencida o nonce repetido pasa |
| G11 | Callback PSP/sandbox autorizado verifica firma, evento, origen, estado y conciliación | callback no firmado o URL demo marca `PAGADO` |
| G12 | JWT/RBAC, membresía activa y revocación se validan en servidor en rutas privadas | cuenta sin permiso o tenant B accede a A |
| G13 | DTO/logs, caché, errores, recibos y URLs omiten JWT, secretos, costos y PII no autorizada | filtración, enumeración o contenido público sensible |
| G14 | Angular/Playwright móvil 320px, teclado, foco, errores, cambios de red y host | falsa confirmación o carrito perdido sin retorno seguro |
| G15 | Pruebas pertinentes por SHA: xUnit/seguridad y MySQL 8.4 efímero si EF; Scope Lock/V5 SUCCESS | required FAIL/CANCELLED/SKIPPED/IN_PROGRESS o pruebas simuladas |
| G16 | Autor y revisor A5 distintos; aprobación independiente documentada antes de liberar M02 | autoaprobación o A5 no acreditado |

Los gates G04–G15 son **criterios para implementaciones futuras**, no afirmaciones de que estén satisfechos por esta microtarea R0. Riesgos de competencia P0/P1, aislamiento, integridad financiera, HMAC/replay o PII bloquean certificación técnica hasta prueba genuina. No inventar PASS desde cobertura documental.

## Reglas de liberación por nivel

**Nivel 1 — esta M01.06 R0:** revisar M01.01–05, predecesores y decisiones; publicar exclusivamente este Markdown y una sección coherente de `CHANGELOG_AI.md` por fast-forward/CAS sobre HEAD vivo; validar Scope Lock y procedencia V5 `completed/success` sobre SHA causal; registrar EN_PROGRESO→VALIDANDO→LISTO con BITACORA A:Z, readback PLAN/COLA/DAG, y liberar candado V17 tras resultado verificado. xUnit/HTTP/MySQL/Playwright **NO_APLICA** al diff sólo documental, sin llamarlos PASS.

**Nivel 2 — A5 independiente:** otra de las cinco Tasks Javier, distinta de DEV02, contrasta contenido, SHA/blobs, diff, límites y checks; puede registrar A5_FAIL. Hasta A5_PASS, M02 no se habilita por inferencia.

**Nivel 3 — M02 y M03 futuras:** volver a consultar `PLAN H=PENDIENTE/I vacío`, `COLA G/O=SI` y `DAG F=VALIDADO/G=SI` para cada sucesora, reclamar una unidad bajo candado V17 y publicar por separado. Los contratos M02 son conceptuales; M03 código real R1/R2 necesita pruebas funcionales/negativas, tenant A/B, integración EF/MySQL cuando proceda, frontend/Playwright en UI, y rollback. Ningún documento promueve QA/main/PROD.

## Doce pares de pruebas futuras — NO_EJECUTADAS

| Caso | Positivo futuro | Negativo futuro |
| --- | --- | --- |
| P01/N01 | host A reconocido resuelve tienda A | host spoofed o desconocido no selecciona B |
| P02/N02 | catálogo A devuelve sólo productos publicados A | producto/variante B desde A falla |
| P03/N03 | variante/stock vigente cotiza correctamente | agotado/inactivo/legado NULL no cotiza |
| P04/N04 | precio, descuentos e IVA provienen de backend | cliente altera monto/impuesto sin efecto |
| P05/N05 | carrito válido respeta límites de líneas | cantidad cero/exceso/entrada corrupta se rechaza |
| P06/N06 | validar checkout no escribe pedido | timeout o validar no reserva stock ni paga |
| P07/N07 | clave idempotente repite respuesta única | doble submit/tenant cruzado no duplica pedido |
| P08/N08 | HMAC firmado cubre tenant/acción/TTL | firma falsa, expirada o nonce repetido se rechaza |
| P09/N09 | pedido futuro transaccional queda auditable | fallo parcial no crea cargo/pedido huérfano |
| P10/N10 | callback autorizado reconcilia PSP una vez | callback falso o repetido no marca pagado |
| P11/N11 | móvil 320px/teclado indica estado servidor | error/caída de red no confirma compra ficticia |
| P12/N12 | cache/SEO y logs muestran mínimo de A | A ve caché B, PII, JWT o secretos |

**Resultados actuales de estos pares:** `0 ejecutados / 12 definidos`; estado `NO_MEDIDO` para tasas de éxito runtime. Deben asociarse a GitHub Actions/Playwright/MySQL efímero reales por SHA de la futura implementación, no a esta revisión de texto.

## Rollback y recuperación proporcional

1. **Fallo documental/A5_FAIL:** conservar autor, SHA, evento y crítica; reparar en dev mediante commit aditivo y reevaluar Scope Lock/V5. Revertir selectivamente con `git revert` normal si corresponde; nunca borrar BITACORA o force-push.
2. **Concurrencia/claim:** ante pérdida de candado, duda de CAS o escritor ajeno, abortar exclusivamente la fila; no tocar owner ni forzar `COLA/DAG`.
3. **Código futuro con fuga tenant/PSP/idempotencia:** bloquear sólo la ruta riesgosa, proteger datos existentes, conservar prueba causal, rollback compatible y reconciliar pedidos/stock/pagos antes de reabrir; no deshabilitar JWT/RBAC, tests o analizadores.
4. **Migración futura:** backup verificado, `Down`/reversión probada primero sobre MySQL 8.4 **efímero**, comprobando esquema, saldos, referencias, transacciones y concurrencia.
5. **UI futura:** feature flag y reversión de componente con pruebas de foco, rutas, estados y ausencia de doble acción; no cancelar workflows que aún necesitan checks exact-SHA.
6. **CI pendiente:** permanecer VALIDANDO/owner original con run/SHA/check; CANCELLED/SKIPPED/FAIL nunca PASS, reintento dirigido sólo con hipótesis y equivalencia verificable.
7. **Seguridad:** secretos nunca en repositorio ni BITACORA, datos reales/proveedores/QA/main/PROD fuera de alcance, revisor A5 obligatorio.
8. **Auditoría final:** registrar EVENT_ID e IDEMPOTENCY_KEY únicos, SOURCE_SHA/TESTED_SHA, blobs, gates, PLAN/COLA/DAG/BITACORA readback y siguiente paso por ID.

**Cierre de alcance:** entrega documental de preparación M01. No constituye checkout productivo, certificación HMAC, pedido, reserva, PSP, cambio de política, migración o finalización del capítulo. El siguiente gate sigue condicionado a A5 independiente y fórmula institucional vigente.
