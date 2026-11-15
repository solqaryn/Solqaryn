# SOLQARYN V17 — C07-02.M02.01: modelo conceptual, propiedad e invariantes de storefront y checkout

**Microtarea:** `C07-02.M02.01`. **Responsable:** PRIMARY DEV 03 / JAVIER_CHATGPT, `TASK_ID=6adc2e4bd8a48191ac7d8641ed95a1a3`. **Fuentes:** PLAN_MAESTRO fila 1052; STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; `PROJECT_SCOPE_LOCK=STRICT`; `solqaryn/Solqaryn@dev`. **Clasificación:** R0 documental sin runtime.

**Propósito exacto:** definir entidades, ownership, relaciones y reglas de invariantes para el modelo conceptual de tienda pública, carrito, validación de checkout, pedidos futuros, estados y confirmaciones. La evidencia final del lote M02 seguirá requiriendo pruebas *reales* de host/HMAC/replay, aislamiento tenant A/B y flujo móvil en las fases que implementen código. **Esta microtarea no crea ni modifica entidades .NET, DTO, controladores, rutas Angular, EF/MySQL, pedidos, pagos, HMAC, PSP, inventario, datos o proveedores.**

## 1. Autoridades, hechos previos y límites

- `C07-02.M01.01–05` documentan objetivo y no-regresiones NR01–NR12, actores/interfaces, fuentes, riesgos R01–R12, decisiones D01–D08 pendientes y casos P01–P14/N01–N14 `FUTUROS_NO_EJECUTADOS`.
- `C07-02.M01.06` (SHA fuente `446ceff7b528222eb36c1a8ed37abbe7eb2bb848`, documento blob `122674cd481553a8bf2da3d420f379604211d9f1`) establece 16 gates G01–G16, 12 pares P/N futuros y rollback. Su A5 PASS independiente por DEV03 está registrado institucionalmente; `DAG_TECNICO` habilita sólo la preparación actual con COLA G/O=SI, no una compra real.
- El código existente `POST tienda/checkout/validar` se describe como validación/cotización, no como pedido durable, pago, reserva de inventario o confirmación. Esta observación de M01 debe corroborarse contra código fresco antes de implementar.
- Ningún host, slug, storeKey, header, cookie, cliente ni guard UI confiere automáticamente autoridad: backend debe resolver confianza de proxy, tenant, actor y recurso. No se presume PSP activado ni contrato de pagos válido.
- Diferenciar `ACTUAL_VERIFICADO_POR_M01` (conceptos de Empresa, productos, stock, flujo de validar) de `PROPUESTO_NO_IMPLEMENTADO` (modelo específico de tienda/checkout). Esquemas propuestos no son migraciones autorizadas.

## 2. Dieciséis conceptos y sus propietarios

| ID | Concepto | Propietario y relación conceptual | Naturaleza y protección |
| --- | --- | --- | --- |
| E01 | Empresa | Raíz de aislamiento; todas las relaciones transaccionales incluyen empresa | Existente; jamás exposición cross-tenant |
| E02 | TiendaPublicada | 1:N por Empresa, identificada mediante host/storeKey validado servidor | Propuesto; host inválido no elige tienda arbitraria |
| E03 | HostTienda | Asociación verificable host + tienda + versión + estado | Propuesto; no confiar en Host reenviado sin proxy confiable |
| E04 | ProductoPublicado | Referencia a producto/variante del tenant, estado y snapshot publicable | Concepto derivado; no publicar drafts ni costo |
| E05 | VarianteDisponible | Producto, unidad, precio e inventario asociado a Empresa | Concepto derivado; precio de cliente no es autoridad |
| E06 | CarritoCliente | Sesión/actor de tienda ligada a Empresa y expiración | Propuesto; cliente anónimo no accede a carritos ajenos |
| E07 | LineaCarrito | Producto/variante, cantidad y versión de cotización | Propuesto; no reserva stock al añadir |
| E08 | CotizacionCheckout | Snapshot efímero de validación de líneas, precio/impuesto y tiempo | Propuesto; no equivale a pedido, cobro o inventario reservado |
| E09 | IntentoCheckout | Operación idempotente propuesta con hash, actor y vencimiento | Propuesto; no activa pedido por este documento |
| E10 | OrdenComercial | Agregado durable futuro con número, empresa, estado y líneas | Propuesto; no crear hasta autorización de fase técnica |
| E11 | ReservaStock | Relación futura orden/variante/cantidad dentro de transacción | Propuesto; no reservar sin CAS/DB real |
| E12 | IntentoPagoPSP | Orden/importe/moneda/proveedor y referencia tokenizada | Propuesto; no guardar PAN ni afirmar pago |
| E13 | EventoPagoVerificado | Evidencia de callback HMAC/nonce/idempotencia reconciliada | Propuesto; rechazo por defecto de notificación sin firma |
| E14 | TransicionEstadoOrden | Estado previo/nuevo, actor, versión y causalidad por Empresa | Propuesto; no pasar PAGADO por URL/redirect |
| E15 | EventoAuditoriaComercial | actor, tenant, correlación, resultado y timestamp sin secretos | Propuesto; no sustituir facturación/auditoría existente |
| E16 | PoliticaTienda | reglas por Empresa: TTL, límites, tasas, accesibilidad y reversión | Propuesto; configuración no equivale a permiso de seguridad |

**No introducir clases duplicadas:** `Empresa`, producto, variante, stock, pedido y cliente pueden existir en el sistema y conservar su identidad canónica; cualquier integración posterior debe mapearse al modelo exacto del repositorio, nunca crear un segundo ERP por esta tabla.

## 3. Relaciones y límites de confianza

| Relación conceptual | Regla obligatoria futura | Prueba adversarial exigida |
| --- | --- | --- |
| Empresa -> TiendaPublicada -> HostTienda | host/storeKey verificado por backend; empresa estable | host spoofed/CNAME ajeno no elige B |
| TiendaPublicada -> ProductoPublicado | publicación explícita por Empresa | producto no publicado o de B no visible desde A |
| ProductoPublicado -> VarianteDisponible | disponibilidad y precio final desde fuente de verdad | precio falso/variante agotada no aceptada |
| CarritoCliente -> LineaCarrito | cada línea en la misma Empresa, cantidades acotadas | ID de producto B, negativos y overflow |
| CarritoCliente -> CotizacionCheckout | validación sin escritura durable; snapshot con vencimiento | no se genera orden, pago, reserva en sólo validar |
| CotizacionCheckout -> IntentoCheckout | clave idempotente ligada a tenant, actor y payload | replay/carrera no duplica efecto |
| IntentoCheckout -> OrdenComercial | comando futuro transaccional y estado explícito | fallo parcial deja cero pedido confirmado |
| OrdenComercial -> ReservaStock | transacción atómica y rollback de stock | oversell o rollback incompleto rechazado |
| OrdenComercial -> IntentoPagoPSP | importe y moneda fijados por servidor, tokens seguros | redirect no acredita cobro ni pagado |
| IntentoPagoPSP -> EventoPagoVerificado | HMAC+TTL+nonce+reconciliación de proveedor | callback falso, repetido o expirado rechazado |
| OrdenComercial -> TransicionEstadoOrden | CAS/versionado, allowlist de transiciones | saltos de estado sin autoridad y doble entrega rechazados |
| Todo -> EventoAuditoriaComercial | mínimo actor/empresa/causa/tiempo y correlación | logs jamás contienen JWT, HMAC key o PII excesiva |

## 4. Veinte invariantes testables futuras

- **INV01:** toda entidad tienda/transacción resuelve una sola Empresa desde servidor, no desde cabecera manipulable.
- **INV02:** ningún usuario o visitante de A ve producto, precio privado, carrito, reserva u orden de B.
- **INV03:** host desconocido o reenviado por proxy no confiable es denegado, no enrutado a tienda por defecto.
- **INV04:** storeKey duplicado, inactivo o de B no otorga autoridad sobre A.
- **INV05:** sólo producto publicado y variante disponible con propietario válido entra en catálogo.
- **INV06:** precio, descuentos, impuestos, stock y moneda se recalculan en servidor; el cliente no fija montos.
- **INV07:** cantidad cero, negativa, excesiva, NaN o fuera de límites falla antes de mutar cualquier dato.
- **INV08:** `checkout/validar` no crea orden, reserva, pago, factura o evento de conciliación.
- **INV09:** snapshots cotizados expiran; cambio de precio/stock exige revalidación antes de paso durable.
- **INV10:** doble envío y replay por clave+payload en misma Empresa retorna efecto único.
- **INV11:** idempotency keys iguales entre tenants diferentes nunca comparten respuesta ni pedido.
- **INV12:** firma HMAC futura une tenant, orden, monto, nonce, TTL, algoritmo y versión; rotación es autorizada.
- **INV13:** callback inválido, vencido, repetido o de proveedor no verificado nunca cambia estado a PAGADO.
- **INV14:** estado de pedido cambia sólo por transición allowlisted y versión actual CAS con audit trail.
- **INV15:** agotamiento de stock o fallo parcial se revierte sin asiento huérfano, oversell o cobro inventado.
- **INV16:** JWT/RBAC/membresía vigente/propiedad verifican rutas privadas; guard Angular no es control suficiente.
- **INV17:** tokens, firmas, PII, costos privados y claves no aparecen en URLs, logs, error bodies ni caché pública.
- **INV18:** caché, SEO y contenidos públicos se separan por Empresa, host y versión; A no usa caché de B.
- **INV19:** la UI 320px, teclado, foco y error de red no muestran confirmación de pago/orden sin prueba server-side.
- **INV20:** ninguna implementación de pedidos, cobros, migraciones, workers o QA/main/PROD queda autorizada por M02 documental ni por A5 sin permisos de fase.

## 5. Doce pares futuros positivos/negativos; cero ejecutados

| Caso | Positivo FUTURO_NO_EJECUTADO | Negativo exigido en CI de implementación |
| --- | --- | --- |
| P01/N01 | host confiable resuelve Tienda A | host spoofing o host de B denegado |
| P02/N02 | catálogo muestra sólo ProductoPublicado A | variante B/draft no visible en A |
| P03/N03 | precio+IVA+stock cotejados servidor | monto/IVA inventados desde navegador rechazados |
| P04/N04 | carrito A acepta cantidades válidas | cero/exceso/overflow y B fallan |
| P05/N05 | validar checkout devuelve cotización sin mutaciones | sólo validar no crea orden/reserva/pago |
| P06/N06 | idempotencia devuelve respuesta idéntica sin duplicar | replay y carrera no duplica pedido |
| P07/N07 | transacción de orden/stock hipotética revierte ante fallo | fallo parcial no deja reserva o saldo |
| P08/N08 | HMAC legítimo/fresco y tenant correcto se acepta | firma falsa/vencida/replay/tenant B se rechaza |
| P09/N09 | callback PSP real verificado reconcilia única vez | callback sin firma o URL demo no marca pagado |
| P10/N10 | usuario permitido consulta orden propia | JWT revocado/rol indebido/tenant B se deniega |
| P11/N11 | caché y SEO muestran sólo contenido A | caché cruzada B, PII/secretos expuestos fallan |
| P12/N12 | UI móvil con teclado y error muestra estado honesto | offline/redirect sin backend no confirma compra |

**Métricas ejecutadas en esta unidad:** `0/12` pares y `NO_MEDIDO` de runtime. HMAC, pagos, pedidos, stock, MySQL, browser y JWT funcional exigen implementación, un SHA de código y CI independiente; no llamar PASS a pruebas propuestas.

## 6. Riesgos, decisiones y rollback

| Decisión pendiente | Requisito de autorización futura | Límite de esta unidad |
| --- | --- | --- |
| D01 binding host/storeKey y proxies | modelo de confianza revisado | no DNS/hosting ni rutas efectivas |
| D02 stock transaccional y reserva | fuente de verdad y rollback MySQL efímero | no DDL, no movimientos de stock |
| D03 orden/cotización/pago separados | dominio/finanzas y control contractual | no crear venta, factura, cobro |
| D04 firma HMAC, TTL, nonce y rotación | threat model/autorización seguridad | no generar ni almacenar secreto |
| D05 PSP y conciliación | contrato autorizado/sandbox permisible | no integrar proveedor real |
| D06 cuotas, precio, moneda, IVA | negocio y prueba de límites | no asumir reglas tributarias |
| D07 expiración de carrito e idempotencia | política por Empresa y retención | no crear worker/event dispatcher |
| D08 compatibilidad ERP y auditoría | versiones/consumidores/rollback verificados | no romper rutas ni inventario |

**Rollback R0:** un `git revert` normal y dirigido del commit documental si fallan sus criterios, sin reset/force-push, sin borrar BITACORA ni inventar A5; recalcular el DAG si se modifica evidencia.

## 7. A0–A5, seguridad y arquitectura

- **A0:** requisitos M02.01, M01.01–06+A5, PLAN H/I, COLA G/O, DAG F/G y exclusividad V17 lock `SQV17_C07_02_M02_01` id `2000001052`, revisión diff dos archivos Markdown.
- **A1 R0:** cotejo de E01–E16, INV01–INV20, vínculos y P01/N01–P12/N12 FUTUROS; ninguno es test ejecutado.
- **A2 R0:** lectura estática de host/storeKey tenant A/B, JWT/RBAC, HMAC nonce/replay, PSP, PII, caché, idempotencia y fallo seguro; no se acredita seguridad implementada.
- **A3 R0:** `NO_APLICA` .NET, HTTP, EF/MySQL, PSP, Angular, Playwright: el diff no altera runtime, ni realiza compra/cobro.
- **A4:** commit fast-forward/CAS dev con este documento y sección autocontenida CHANGELOG; `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` sobre SHA propio (o equivalencia causal), BITACORA idempotente y readback PLAN/COLA/DAG; jamás contar pending como PASS.
- **A5:** revisión independiente por una de otras cinco Tasks Javier, distinta de DEV03; sólo después de su evento auténtico recalcular sucesoras. R0 no autoriza migración, QA/main/PROD, credenciales ni proveedores.

**Impacto de capas:** Domain/Application/Infrastructure/API/Angular y esquemas MySQL no cambian; clientes consumidores no modificados; Scope Lock y política seguridad siguen intactos. La única entrega es un modelo conceptual sujeto a decisiones y revisión independiente.
