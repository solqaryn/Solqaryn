# SOLQARYN V5 — C07-02.M01.05: aceptación observable de storefront y checkout

**Microtarea:** C07-02.M01.05, PLAN_MAESTRO fila 1050. **Autor:** PRIMARY DEV 06 JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-14. **Repositorio:** `solqaryn/Solqaryn`, rama `dev`, PROJECT_SCOPE_LOCK=STRICT. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`.

## Resultado delimitado

Definir criterios positivos y negativos, fuentes de autoridad y evidencia medible futura para tienda pública, carrito, pedido, checkout, estados y confirmación. Esta microtarea **no implementa ni ejecuta esos escenarios**. No transforma validación en reserva, compra, factura ni cobro. El flujo vigente `POST tienda/checkout/validar` recalcula precio/variante/stock pero **no reserva inventario ni crea pedido ERP**; el resto del proceso comercial futuro requiere código, CI y autorización específicos. No existen certificados aquí de HMAC, antireplay, PSP, pedido durable o flujo Playwright.

## Fuentes trazables del HEAD (blobs verificados)

| Fuente | SHA Git blob | Alcance de lectura |
| --- | --- | --- |
| `docs/v5/c07-02-m01-01-alcance-storefront-checkout.md` | `26310ed89f7510bc7e7c49b88ff1c2e3c315f49f` | Doce invariantes NR01–NR12, criterios futuros; **VALIDANDO** por CI obligatoria CANCELLED |
| `docs/v5/c07-02-m01-02-actores-interfaces-storefront-checkout.md` | `aecbb48a27b1276a1c6b6cda1e4f8260a7d284f0` | A01–A12, I01–I12, B01–B10; LISTO documental con A5 independiente pendiente |
| `docs/v5/c07-02-m01-03-inventario-causal-storefront-checkout.md` | `72193a45ee5d3745f3ea51e02398a47b66d95853` | Doce fuentes S01–S12, brechas G01–G10 sin certificar defectos runtime |
| `backend/src/API/Controllers/TiendaController.cs` | `39a23ddb94b24077bbce79de1b965781b70ca37a` | Checkout/validar actual sin persistir pedido ni reservar stock |
| `backend/src/API/Controllers/TiendaBindingController.cs` | `97fb0717baed839707dfd58cc98d9ce966fb8527` | Binding verificado y cabecera no-store, no asegura proxy completo |
| `backend/src/Application/Services/CatalogoPublicoService.cs` | `3d1ba38e0638ad75ad23e92c98120cc1b99660e4` | Proyección/caché con tenantKey; requiere A/B dirigido |
| `frontend/src/app/features/storefront/storefront.models.ts` | `de45fc1e9c0e3d1b98bd560b6e8006200d3a2b08` | Contratos TS no son prueba de ejecución de pago |

M01.04 está en trabajo bajo otra Task Javier al preparar este documento; sus riesgos o decisiones no se dan por aprobados aquí. El gate `DAG_TECNICO!F1011/G1011=VALIDADO/SI` y `COLA!G1050/O1050=SI` permite documentación preparatoria, **no libera** M01.06 ni IAM, pagos, DB, QA/main/PROD. La condición `VALIDANDO` de M01.01 no se confunde con PASS de CI.

## Matriz P01–P14 / N01–N14 — pruebas FUTURAS NO_EJECUTADAS

| Par | Positivo futuro observable | Negativo futuro obligatorio | Oráculo/fixture requerido |
| --- | --- | --- | --- |
| 01 | **P01** Host A registrado resuelve A mediante backend de confianza | **N01** Host desconocido o X-Forwarded-Host falsificado no selecciona B | binding, host y proxy; backend, sin confiar en navegador |
| 02 | **P02** Productos publicados A proyectan sólo A | **N02** IDs de B/ocultos no aparecen en catálogo A | filtro de publicación y tenant A/B |
| 03 | **P03** Variante válida A y stock actual se cotizan | **N03** Variante ajena, inactiva o stock agotado es rechazada | validación backend y prueba de variante B |
| 04 | **P04** Servidor calcula precio/IVA/total y devuelve valores normalizados | **N04** Precio/IVA/descuento inyectado por cliente no altera el total | DTO checkout vs fuente server-side |
| 05 | **P05** Cantidades y líneas válidas producen errores/resultado deterministas | **N05** Carrito vacío, cantidad 0/negativa o exceso de líneas es rechazado | límites actuales y errores estructurados |
| 06 | **P06** Cambio real de stock exige nueva cotización sin reservar | **N06** Validación obsoleta no congela inventario ni genera pedido | flujo de validación vs transacción posterior |
| 07 | **P07** El mismo idempotency key devuelve estado único estable | **N07** Doble clic y reintento con la misma clave no duplican pedido | futura persistencia durable y concurrencia |
| 08 | **P08** HMAC cubre tenant, acción, payload, versión y expiración aprobados | **N08** Firma truncada, acción ajena o secreto inválido se rechaza | implementación aprobada y pruebas criptográficas |
| 09 | **P09** Primer uso dentro de TTL se acepta una vez | **N09** Nonce repetido, expirado o de B falla cerrado | almacenamiento atomicidad y TTL |
| 10 | **P10** Pedido consistente refleja líneas, moneda y tenant aceptados | **N10** Fallo parcial no crea venta/cargo sin reconciliación | EF/MySQL efímera y rollback |
| 11 | **P11** Webhook validado por firma e idempotencia reconcilia estado real | **N11** Callback sin firma/replay o PSP de B no marca PAGADO | sin usar proveedor live, sandbox autorizado separado |
| 12 | **P12** Recibo refleja estado persistido, no solamente navegación | **N12** Redirect, WhatsApp o mensaje demo no acredita pago | consulta fuente transaccional de verdad |
| 13 | **P13** 320px, teclado y lector permiten corregir carrito/checkout | **N13** Error oculto o pérdida de intención tras red lenta no se acepta | Playwright y accesibilidad futura dirigida |
| 14 | **P14** Host A muestra sólo SEO público y datos mínimos de A | **N14** Caché cruzada, preview indexado, costo/PII/token en DTO/log falla | A/B, robots/sitemap y redacción |

## Plan de ejecución y criterios de evidencia futuros

1. **Aislamiento:** ejecutar fixture tenant A/B (empresas, productos, variantes, host y caché) y denegaciones server-side; autorización real en backend, jamás en interceptores UI.
2. **Validación de checkout existente:** ejecutar API xUnit positivos y negativos, mutación de precio/impuesto, límites de líneas, faltantes y stock; demostrar que validar **no** escribe pedidos ni stock. Documentar status y contrato de error, sin suponer resultados.
3. **Pedido/PSP futuros:** solo tras código y autorización específicos, ejecutar pruebas EF/MySQL efímeras, concurrencia/idempotencia, firma/nonce TTL y simulaciones de callbacks. Sin transacciones reales, secretos, PSP live ni Webhooks instalados desde este documento.
4. **Frontend/SEO:** cuando exista flujo objetivo, ejecutar Playwright/Chromium 320px, teclado/foco, estado de red y caché host/tenant; no inferir conformidad por definición de pruebas.
5. **Resultado verificable:** cada escenario necesita enlace a run, SHA exacto, entradas/esperado/obtenido, versión de fixtures, y separación PASS/FAIL/SKIPPED/CANCELLED/IN_PROGRESS. P01–P14 y N01–N14 quedan todos `NO_EJECUTADOS` en esta M01.05.

## Seguridad, impacto, A0–A5 y reversión

- **A0:** revisión de contratos M01.01–03, código real en siete fuentes y matriz de 14 pares. Cambios solo a esta página `docs/v5/` y `CHANGELOG_AI.md`.
- **A1:** criterios funcionales definidos y medibles; **NO** xUnit ni HTTP ejecutados. **A2:** requisitos negativos A/B, firma, replay, JWT/RBAC, redacción de PII, costos y secretos, sin afirmar hardening implementado.
- **A3:** compilaciones .NET/Angular, MySQL, Playwright y PSP `NO_APLICA` al diff solo Markdown; no convertir estados en curso en PASS.
- **A4:** publicar fast-forward serializado con CAS sobre HEAD vigente; exigir `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` en `completed/success` del SHA publicado, más readback PLAN/COLA/BITACORA.
- **A5:** únicamente otra Task de las cinco Javier distinta del TASK_ID autor; A5 pendiente y no habilita QA/main/PROD.
- **No cambia:** controladores, servicios, contratos DTO runtime, Angular, dependencias, esquemas/migraciones, JWT/RBAC, empresas, secretos, DNS, infraestructura, workers, webhooks, proveedores ni arquitectura.
- **Rollback:** `git revert` normal y evento compensatorio aditivo con evidencia, sin reescribir historia Git ni BITACORA.
