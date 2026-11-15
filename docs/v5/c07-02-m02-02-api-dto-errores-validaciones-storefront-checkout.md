# SOLQARYN V17 — C07-02.M02.02: contratos conceptuales API/DTO/errores/validación de storefront

**Identidad:** `C07-02.M02.02` · PLAN_MAESTRO fila 1053 · PRIMARY DEV 03 JAVIER_CHATGPT · `TASK_ID=6adc2e4bd8a48191ac7d8641ed95a1a3`. **Origen:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`; V15 R0 documental y candado V17 `SQV17_C07_02_M02_02` fila 1053.

**Entrega exacta:** propuesta de operaciones, DTO, errores, validaciones y compatibilidad para tienda pública, carrito y checkout **sin modificar runtime**. Ningún identificador siguiente es controlador/ruta, tabla, contrato de PSP, permiso JWT o producto implementado. `POST tienda/checkout/validar` existente — citado por el inventario M01 — valida cotización, no crea pedidos ni pagos. No se habilitan cobros, migraciones, host, HMAC, worker, secretos o producción.

## Fuentes y fronteras de autorización

- `C07-02.M01.01–06` contiene NR, actores, fuentes, riesgos R01–R12, D01–D08 abiertas, G01–G16 y P/N futuros. La M01.06 (`446ceff7b528222eb36c1a8ed37abbe7eb2bb848`, blob `122674cd481553a8bf2da3d420f379604211d9f1`) recibió A5 auténtica de otro Task; esto habilita sólo documentación M02 según la fórmula vigente.
- `C07-02.M02.01` documenta 16 entidades conceptuales E01–E16, 20 invariantes y 12 pares futuros sobre tenant/host, catálogo, precio, stock, carrito, pedido, PSP e HMAC; su A5 propia aún requiere revisor independiente.
- Cualquier host, tienda, storeKey o `EmpresaId` enviado por navegador es contexto no confiable; el backend verifica origen proxy, configuración de tienda y ownership. JWT/RBAC aplican a superficies privadas. Precios, stock, impuestos y descuentos se recalculan desde fuente confiable.
- Los permisos y tipos a continuación son ejemplos de diseño; **no existen hasta corroborar código y aprobación**. HMAC/nonce/TTL/replay, PSP y transacciones deben probarse con componentes reales, no con esta redacción.

## Ocho operaciones conceptuales (NO endpoints)

| Operación propuesta | Entrada y efecto hipotéticos | Autorización y regla |
| --- | --- | --- |
| O01 ResolverTienda | `HostTienda`/storeKey -> contexto público mínimo | host confiable, tienda publicada, Empresa existente |
| O02 ListarCatalogoPublicado | filtros tipados y cursor -> productos públicos | Empresa host vinculada, estados publicados, sin costo/PII |
| O03 ConsultarVariante | producto/variante -> disponibilidad y precio servidor | variante en misma Empresa y publicación vigente |
| O04 CotizarCarrito | líneas/cantidades -> resumen calculado y vencimiento | recalcular IVA, stock, moneda y límite sin mutación |
| O05 ValidarCheckout | cotización/intento -> validación segura sin pedido | **no** reserva, orden, cobro, factura ni confirmación |
| O06 RegistrarIntentoIdempotente | clave/huella/actor -> resultado reproducible futuro | operación futura, tenant+actor+payload ligados, CAS |
| O07 ConsultarEstadoPedido | referencia de orden futura -> estado y correlación | JWT/membresía/propiedad para privado, tokens públicos limitados |
| O08 ProcesarCallbackVerificado | mensaje firmado de PSP -> conciliación futura | firma, nonce, TTL, origen, idempotencia; no habilitado |

## Ocho DTO propuestos (NO clases)

| DTO | Campos permitidos propuestos | Control de seguridad |
| --- | --- | --- |
| D01 `TiendaPublicaView` | id opaco, nombre visible, host validado, versión | nunca secretos/config privada |
| D02 `CatalogoPageQuery` | filtros tipados, cursor ligado a Empresa, limit | no SQL/JS libre, topes por tenant |
| D03 `VariantePublicaView` | producto, variante, precio publicado, disponibilidad resumida | sin costos, stock reservado ajeno o B |
| D04 `CarritoValidarRequest` | líneas, variantes, cantidades, idempotency candidate | owner/precio/IVA jamás autoritativos desde body |
| D05 `CheckoutCotizacionView` | subtotal, impuesto, total, moneda, vencimiento | etiquetar `NO_PEDIDO_NO_PAGO` explícito |
| D06 `IntentoOrdenRequest` | cotización, expectedVersion, idempotency key | futuro R2 transaccional; no se implementa aquí |
| D07 `EstadoOrdenView` | referencia opaca, estado, última actualización UTC | respuestas privadas con JWT/RBAC/tenant |
| D08 `CallbackPagoEnvelope` | firma, nonce, timestamp, referencia opaca | validar criptográficamente antes de procesar |

## Doce errores futuros y comportamiento seguro

| Código | Circunstancia | Respuesta segura propuesta |
| --- | --- | --- |
| X01 HOST_NO_CONFIABLE | host spoofed o proxy no verificado | denegar sin tienda por defecto |
| X02 TIENDA_NO_PUBLICADA | tienda inactiva/desvinculada | no revelar backend privado |
| X03 TENANT_CRUZADO | producto/carrito B desde tienda A | denegar 403/404 sin enumeración |
| X04 PRODUCTO_NO_PUBLICADO | borrador, variante B o deshabilitado | respuesta vacía/404 sin costo |
| X05 CANTIDAD_INVALIDA | cero, negativo, overflow o límite | 400, ninguna mutación |
| X06 COTIZACION_CADUCADA | precio/stock cambió o venció | exigir recotizar, no pedir cobro |
| X07 SIN_STOCK | stock insuficiente/no válido | denegar sin reservar inventario |
| X08 CONFLICTO_VERSION | CAS vencido o repetición inconsistente | 409 sin efecto parcial |
| X09 HMAC_INVALIDO | firma, TTL o nonce incorrectos | 401/403, no aceptar callback |
| X10 REPLAY_CALLBACK | PSP duplicado/repetido | idempotencia sin estado adicional |
| X11 ACCESO_PRIVADO_DENEGADO | JWT/rol/membresía revocada | denegación sin PII de pedido |
| X12 FALLA_DEPENDENCIA | DB/proveedor/timeout/cache | fail-closed genérico, nunca pagado |

Los códigos y HTTP son propuesta, no respuesta existente comprobada. Los errores nunca incluirán JWT, secretos HMAC, costos internos o PII excesiva.

## Dieciséis validaciones conceptuales

- **V01** validar host/storeKey bajo proxies de confianza y lista de tiendas publicadas.
- **V02** resolver Empresa desde backend; nunca confiar en `EmpresaId` de cabecera o navegador.
- **V03** validar estados públicos de producto/variante en tenant correcto.
- **V04** negar referencias cruzadas A/B a producto, carrito, pedido y pago.
- **V05** validar cantidades positivas, topes, precisión decimal y desbordamientos.
- **V06** recalcular precios, impuestos, descuentos y moneda de origen confiable.
- **V07** verificar stock actual, versión y expiración antes de cualquier comando durable.
- **V08** separar estrictamente validar/cotizar de reservar, ordenar o cobrar.
- **V09** ligar idempotency key a Empresa, actor, operación, payload y TTL.
- **V10** imponer `ExpectedVersion`/CAS a transiciones de estados futuras.
- **V11** verificar HMAC sobre payload/tenant/nonce/TTL y versión de clave sin exponerla.
- **V12** rechazar replay, duplicado, callback de PSP no reconocido o expirado.
- **V13** exigir autenticación, membresía, permiso y ownership en API privada.
- **V14** minimizar PII/logs/URLs y segregar caché por host, tenant, versión.
- **V15** limitar filtros, paginación, tasa y coste; error de búsqueda no abre permisos.
- **V16** UI 320px, foco, teclado, offline/retry muestran estados honestos sin confirmación falsa.

## Doce pares de pruebas FUTURAS, ninguna ejecutada

| Par | Positivo esperado | Negativo obligatorio |
| --- | --- | --- |
| P01/N01 | host confiable resuelve tienda A | host spoofed no resuelve B |
| P02/N02 | catálogo A lista productos publicados A | draft o producto B no aparece |
| P03/N03 | variante A muestra precio/stock confiables | precio cliente o variante B se rechaza |
| P04/N04 | carrito válido recotiza cantidades | cantidad 0/negativa/overflow falla |
| P05/N05 | validar checkout no muta | validar no crea orden, pago, reserva |
| P06/N06 | cambio de stock exige recotizar | cotización caducada no ordena |
| P07/N07 | idempotencia repite efecto único futuro | replay/tenant B no duplica pedido |
| P08/N08 | HMAC fresco y firmado pasa verificación futura | firma falsa/replay/TTL vencido falla |
| P09/N09 | callback PSP válido reconcilia una vez | callback falso no marca pagado |
| P10/N10 | JWT/rol activo lee pedido propio | B o rol revocado no lee |
| P11/N11 | cache por host/Empresa mantiene aislamiento | A no observa caché/PII de B |
| P12/N12 | móvil y teclado explican error/éxito real | offline/redirect no confirma pago ficticio |

**Ejecución actual: cero de doce pares; `NO_MEDIDO`**. El diseño no valida HMAC criptográfico, transacciones MySQL, API JWT ni navegador. La posterior fase R2 debe probar xUnit negativos tenant A/B, integración MySQL 8.4 EFÍMERA si cambia persistencia y Playwright/Chromium si cambia UI.

## Compatibilidad, arquitectura y rollback

El diseño debe coexistir con `POST tienda/checkout/validar`, identidad de productos/stock/Empresa, cartera ERP y dominio existente; no sobreescribir contratos hasta identificar consumidores y autorizar versión aditiva. Domain define invariantes; Application orquesta y autoriza; Infrastructure persiste/integra; API autentica; Angular presenta sin conceder privilegios. Mantener transacciones de pedidos/stock y auditoría financiera intactas.

**Decisiones aún abiertas D01–D08:** confianza proxy/host, stock/reserva, diferencia cotización-pedido-cobro, HMAC/rotación/replay, PSP y sandbox, moneda/IVA, cuotas/retención y reversión compatible. Nada se resuelve por esta microtarea.

**Reversión R0:** `git revert` normal del commit documental, tras HEAD/owner y controles; BITACORA no se borra, sin reset ni force-push. QA/main/PROD, datos reales, secretos, DNS y proveedores quedan fuera de alcance.

## A0–A5 y salida real

- **A0:** PLAN/COLA/DAG por ID y A5 M01.06, candado atómico `SQV17_C07_02_M02_02` id `2000001053`, una unidad por commit, diff dos Markdown.
- **A1:** coherencia de O01–O08, D01–D08, X01–X12, V01–V16 y doce P/N futuros, no pruebas runtime.
- **A2:** JWT/RBAC/tenant A/B, host spoofing, HMAC/replay/PSP, cash/stock, secretos y fail-closed revisados sólo estáticamente.
- **A3:** `NO_APLICA` .NET, API/HTTP, EF/MySQL, proveedor real, Angular/Playwright para diff sólo Markdown.
- **A4:** fast-forward/CAS dev, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` SHA causal; BITACORA A:Z + readback y comprobación de doc/CHANGELOG blobs; checks in_progress no PASS.
- **A5:** revisor distinto de la Task autora y de las otras cinco Javier; ningún LISTO_DEV habilita QA/main/PROD, pago, migración o fase técnica por sí solo.
