# SOLQARYN V5 — C07-02.M01.04 — Riesgos, supuestos y decisiones del storefront

**Autor:** PRIMARY DEV 01 — JAVIER; **fecha:** 2026-11-14. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C07-02.M01.04. Este documento es preparación M01, no implementación de checkout, HMAC, pedidos ni cobros. Los riesgos son hipótesis, no fallos demostrados.

## Evidencia observada en Git dev

- M01.01: `docs/v5/c07-02-m01-01-alcance-storefront-checkout.md` (blob `26310ed89f7510bc7e7c49b88ff1c2e3c315f49f`): NR01–NR12, sin certificación runtime.
- M01.02: `docs/v5/c07-02-m01-02-actores-interfaces-storefront-checkout.md`: A01–A12, I01–I12 y B01–B10, sólo contrato documental.
- `backend/src/API/Controllers/TiendaController.cs` (blob `39a23ddb94b24077bbce79de1b965781b70ca37a`): API pública, `checkout/validar`, límite de líneas y cálculo; validar no es reservar ni cobrar.
- `backend/src/API/Controllers/TiendaBindingController.cs` (blob `97fb0717baed839707dfd58cc98d9ce966fb8527`): `VerifiedPublicStoreTenant.Resolve`, cabecera `no-store`.
- `backend/src/API/Controllers/TiendaConfiguracionPublicaController.cs` (blob `ac31376356e7a8a7880fabbdeed1a990ac8289e4`): consulta pública restringida al `EmpresaId` verificado.
- `backend/src/API/Controllers/PlataformaTiendasController.cs` (blob `23f57d6ca9ed504d9223108eb4264d472389f2c1`): gestión autenticada y validación de `host/storeKey`.
- `backend/src/Domain/Entities/PublicStoreBinding.cs` (blob `dae03bcd963281d74e1d02e460934ab0783d39d5`): vincula host, StoreKey y EmpresaId.
- `backend/src/Application/Services/CatalogoPublicoService.cs` (blob `3d1ba38e0638ad75ad23e92c98120cc1b99660e4`): DTO de catálogo, cache opcional con tenantKey.
- `backend/src/API/Controllers/TiendaCuentaController.cs` (blob `ee7ab5fa9de80ba08a8173b88c27563bdddf7002`): login, sesión, direcciones y favoritos de cliente.
- `backend/src/Domain/Entities/TiendaCuentaCliente.cs` (blob `aa2bc1541afffd68aa980520178d2b6f0d5e7aad`): entidades de cuenta/sesión/dirección/favorito.
- `backend/src/Domain/Entities/PedidoVenta.cs` (blob `12c265e9b3dbd6d27883d52e3b2a0b05783eb0cf`): EmpresaId nullable legado; IdempotencyKey y fingerprint ERP, sin prueba de unión pública.
- `backend/src/API/Controllers/PedidosVentaController.cs` (blob `293839e33ced2fda5328eee8b42b2e7ae7a1fa50`): pedidos ERP autenticados con permisos e Idempotency-Key.
- `backend/src/API/Controllers/PagosOnlineController.cs` (blob `99962b2957f672a6dd435895daffaba7333f8a6b`): pago autenticado, autorización y clave idempotente, no PSP público certificado.
- `frontend/src/app/features/storefront/storefront.models.ts` (blob `de45fc1e9c0e3d1b98bd560b6e8006200d3a2b08`): tipos de checkout, carrito y recibo, no transacciones reales.
- `frontend/src/app/features/storefront/storefront.service.ts` (blob `ebf0d5d9287180fb3e6aae4f4190ee93afae11cc`): llamada a checkout/validar y endpoint configurable de tarjeta.
- `frontend/src/app/features/storefront/storefront.component.ts` (blob `1490eb5950eb3a9619046972ccf78bb80cab4bd9`): modos WhatsApp/tarjeta y Signals; no acredita móviles ni cobro.

Se inspeccionaron fuentes selectivamente, **no** todas las rutas de middleware, EF, proxy, PSP, pruebas o seguridad; la presencia de símbolos o blobs no prueba comportamiento. No se ejecutaron pruebas, pagos ni consultas productivas.

## Riesgos R01–R12 — potenciales, no verificados

| ID | Riesgo | Evidencia futura requerida |
| --- | --- | --- |
| R01 | Host/X-Forwarded-Host manipulable resuelve empresa errónea | Tests proxy confiable, host desconocido y binding fail-closed |
| R02 | StoreKey/EmpresaId provisto por navegador supera tenant A/B | Prueba negativa HTTP A/B por host y clave |
| R03 | Caché público comparte productos/costos entre empresas | Intercalado A/B y política de invalidación |
| R04 | Precio/IVA/SKU/stock del navegador se toman como verdad | Payload manipulado y recálculo servidor |
| R05 | Checkout validado se interpreta como pedido, reserva o pago | Verificar ausencia de efectos antes de transacción |
| R06 | Reintentos y concurrencia duplican pedido o cobro | MySQL efímero, idempotencia durable y doble solicitud |
| R07 | HMAC sin caducidad, nonce, ámbito o rotación permite replay | Negativas de firma/TTL/nonce/tenant |
| R08 | Redirect o callback no autenticado marca PAGADO | Firma de proveedor y conciliación de evento |
| R09 | Sesión revocada deja acceso a dirección o favorito ajeno | Expiración, logout, autorización por comprador/tenant |
| R10 | Variante ajena, inactiva o EmpresaId NULL se publica | EF A/B, legado NULL, variantes y publicaciones |
| R11 | Checkout móvil impide foco/error con teclado o red débil | Playwright a 320px y conexiones degradadas |
| R12 | DTO, logs o cache revelan PII, costos, JWT/HMAC | Auditoría de redacción, DTO y cache |

## Supuestos S01–S08 — falsables y pendientes

S01 la resolución de host confía sólo en proxies y binding aprobados; S02 el catálogo público nunca contiene costos o productos de B; S03 cada validación recalcula impuestos, precio y stock; S04 sesiones de cuenta se aíslan por comprador/empresa; S05 el pedido público dispone de puente transaccional ERP (no probado); S06 HMAC y callback cuentan con anti-replay durable (no probado); S07 la configuración de endpoint tarjeta usa una allowlist segura (no probado); S08 el carrito móvil resiste errores de red sin confirmar operaciones ficticias (no probado). Cada supuesto requiere prueba negativa causal antes de adoptarse.

## Decisiones D01–D08 — todas PENDIENTES

D01 trust boundary de Host/StoreKey y proxies; D02 distinción validación/reserva/checkout; D03 propiedad empresarial e idempotencia transaccional de pedido; D04 firma HMAC, TTL, nonce y rotación; D05 proveedor de pago, callback, reconciliación y recibo; D06 consentimiento, PII, retención y sesión; D07 política de cache/medios/SEO por tenant; D08 presupuesto de rendimiento móvil, gates y rollback. Ninguna queda aprobada por este documento.

## Oráculos futuros — NO_EJECUTADOS

**P01–P06:** P01 host legítimo A; P02 catálogo publicado A; P03 checkout recalcula precio y stock; P04 dirección propia autenticada; P05 pedido idempotente futuro; P06 errores móviles accesibles.

**N01–N12:** N01 host desconocido, N02 cabecera falsa, N03 storeKey B desde A, N04 producto B, N05 variante inactiva/NULL, N06 precio falsificado, N07 carrito excesivo, N08 stock agotado, N09 doble replay conflictivo, N10 HMAC vencido/nonce repetido, N11 callback sin firma/redirect engañoso, N12 cache/DTO revela datos de B. No se ejecutó ningún P/N.

## Alcance de auditorías, dependencias y recuperación

Preflight de C07-02.M01.04: PLAN_MAESTRO!H1049/I1049, COLA!G1049/O1049, DAG_TECNICO!F1010/G1010 y **70/70 preparaciones M01 LISTO** de C01-01–04/C02-01–04/C05-01–04/C06-02/C07-01. Autorización exclusiva de análisis documental; no M01.06, migraciones, JWT/RBAC, workers, cobros, QA/main/PROD.

A0 lectura de 14 fuentes de código y contratos; A1 coherencia documental de riesgos y oráculos no ejecutados; A2 amenazas host, tenant A/B, replay y PII identificadas, no certificadas; A3 builds, pruebas .NET/Angular, MySQL y Playwright **NO_APLICA** a dos Markdown, no PASS; A4 exige commit fast-forward, Scope Lock y verificador V5 exact-SHA completed/success más readback institucional; A5 requiere otra Task Javier y permanece PENDIENTE.

Impacto: sólo este documento y CHANGELOG_AI.md; ningún API/DTO, código, esquema, secreto, infraestructura o dato. Rollback mediante git revert normal y nuevo evento de BITACORA, nunca eliminación histórica.
