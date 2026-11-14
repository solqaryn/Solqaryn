# SOLQARYN V5 — C07-02.M01.02: actores, interfaces y límites del storefront/checkout

**Fecha:** 2026-11-14. **Autor:** PRIMARY DEV 03 JAVIER, Task ID 6adc2e4bd8a48191ac7d8641ed95a1a3. **Fuente:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C07-02.M01.02. Contrato de preparación documental: ninguna clase, endpoint, HMAC, cobro, pedido, migración, notificación o worker es implementado ni certificado por esta entrega. Los escenarios son **FUTUROS NO_EJECUTADOS**.

## Evidencia real de dev y limitaciones

| Fuente | Blob Git verificado | Evidencia y límite |
| --- | --- | --- |
| `docs/v5/c07-02-m01-01-alcance-storefront-checkout.md` | `26310ed89f7510bc7e7c49b88ff1c2e3c315f49f` | 12 invariantes y escenarios futuros; no prueba runtime |
| `backend/src/API/Controllers/TiendaController.cs` | `39a23ddb94b24077bbce79de1b965781b70ca37a` | API `tienda` pública y `POST checkout/validar`; servidor recalcula precio, variante y stock, no reserva inventario ni crea pedido ERP |
| `backend/src/API/Controllers/TiendaBindingController.cs` | `97fb0717baed839707dfd58cc98d9ce966fb8527` | `GET tienda/binding`, resolver verificado y no-store, no acredita todas las rutas proxy |
| `backend/src/Domain/Entities/PublicStoreBinding.cs` | `dae03bcd963281d74e1d02e460934ab0783d39d5` | Host/storeKey y empresa; no inferir host spoofing resuelto completamente |
| `backend/src/Application/Services/CatalogoPublicoService.cs` | `3d1ba38e0638ad75ad23e92c98120cc1b99660e4` | DTO y caché con tenantKey opcional, requiere test A/B real |
| `frontend/src/app/features/storefront/storefront.models.ts` | `de45fc1e9c0e3d1b98bd560b6e8006200d3a2b08` | Carrito, CheckoutValidado, comprador, recibo; TypeScript no certifica cobros |

**No comprobado todavía:** pipeline íntegro host/proxy, filtros multiempresa y EF, HMAC TTL/nonce, callback del proveedor de pago, persistencia de pedido idempotente y flujo Playwright móvil. No inventar existencia ni calidad de esas implementaciones.

## Actores A01–A12 — matriz de autoridad

| Actor | Identidad y salida permitida |
| --- | --- |
| A01: Visitante | Host permitido; ve oferta pública propia sin datos internos |
| A02: Cliente carrito | IDs/cantidades no confiables; obtiene cotización server-side |
| A03: Comprador | Datos mínimos, consentimiento, recibo no pagado por redirección |
| A04: Administrador tienda | Membresía vigente y RBAC para su empresa |
| A05: Editor productos | Publicación por tenant con permiso y auditoría |
| A06: Operador stock | Sucursal autorizada; no reserva por validar |
| A07: Gestor impuestos | Política de tasa y vigencia servidor |
| A08: PSP futuro | Callback autenticado/idempotente; no proveedor activo por documento |
| A09: Notificador futuro | Mensajes minimizados sin secretos con retry seguro |
| A10: Operador pedidos futuro | Estado aprobado por backend, sin salto cross-tenant |
| A11: Soporte | Permiso específico, lectura restringida y trazable |
| A12: Crawler | SEO público indexa sólo el host y catálogo publicados |

## Interfaces I01–I12 — presente frente a futuro

| Interfaz | Contrato y estado observado |
| --- | --- |
| I01: GET tienda/bootstrap | API presente; tienda validada sin revelar secretos |
| I02: GET tienda/productos y detalle | API presente; sólo publicados del tenant |
| I03: GET tienda/categorias | API presente; categoría sólo por alcance seguro |
| I04: GET tienda/binding | API presente, VerifiedPublicStoreTenant, no-store |
| I05: POST tienda/checkout/validar | API presente; recalcula, NO crea pedido ERP o reserva |
| I06: CatalogoPublicoService | Application presente; cache tenantKey, validar A/B |
| I07: PublicStoreBinding | Domain presente; host/storeKey deben resolver empresa única |
| I08: storefront.models.ts | UI types presentes; carrito no autoriza precio ni pago |
| I09: Pedido persistente futuro | No acreditado aquí; idempotencia/transacción |
| I10: HMAC y nonce futuros | No acreditados aquí; vencimiento, scope y replay |
| I11: PSP callback futuro | No acreditado; firma verificada, no doble pago |
| I12: Notificaciones futuras | No acreditadas; opt-in, idempotencia y minimización |

## Fronteras de confianza B01–B10

- **B01:** Host/X-Forwarded-Host recibido no selecciona tenant sin binding y proxy confiable.
- **B02:** IDs, storeKey, slug y EmpresaId enviados por navegador nunca son autorización.
- **B03:** Un comprador anónimo no hereda privilegios ERP ni de sucursal.
- **B04:** Precio, impuesto, SKU y stock del cliente son intención; el servidor recalcula.
- **B05:** Checkout validado no constituye venta ERP, reserva ni confirmación de pago.
- **B06:** Redirect/WhatsApp/demo no es evidencia suficiente para estado PAGADO.
- **B07:** HMAC TTL, nonce único y anti-replay siguen pendientes de diseño/código/CI.
- **B08:** Caché, imágenes y DTO públicos deben separar tenant A/B y costos privados.
- **B09:** Logs, errores, URL y trazas no exponen JWT, HMAC secret, contacto ni PII.
- **B10:** UX móvil 320px, teclado y foco es requisito futuro, no seguridad de API.

## Pruebas futuras — no ejecutadas

**Positivas P01–P06 (FUTUROS NO_EJECUTADOS):** P01 binding host A a tenant A; P02 catálogo visible A; P03 precio/stock recalculados backend al validar; P04 carrito y variantes autorizadas con límite; P05 pantalla móvil muestra errores claros/teclado/foco; P06 actualización de pedido real con idempotencia sólo cuando implementado.

**Negativas N01–N12 (FUTUROS NO_EJECUTADOS):** N01 host desconocido; N02 X-Forwarded-Host/slug falso; N03 producto/variante de B en carrito A; N04 precio/IVA forjado; N05 cantidad excesiva; N06 stock agotado; N07 doble click/replay; N08 HMAC vencido/nonce repetido; N09 callback sin autenticación; N10 caché A presentada a B; N11 costo/PII/secretos en DTO/log; N12 redirección confundida con pago. Estas son hipótesis de test, no findings ni pases de CI.

## Impacto, dependencias y auditoría A0–A5

- Fuente técnica `DAG_TECNICO!A1008:J1008`, F=VALIDADO/G=SI; `PLAN_MAESTRO!A1047:I1047` libre y `COLA!G1047/O1047=SI` al claim. Preparaciones C01/C02/C05/C06-02/C07-01 M01.01–05: **70/70 LISTO**. Sólo habilita M01 documental; no M01.06, pagos, HMAC, DB ni QA/main/PROD.
- **Cambio real:** únicamente este `docs/v5/*.md` y sección autocontenida `CHANGELOG_AI.md`, sin API/DTO/Angular/SQL/JWT/RBAC, dominios o cuentas de pago. Sin cambio de arquitectura.
- **A0:** seis blobs fuente más A01–A12/I01–I12/B01–B10. **A1:** actor/interfaz/resultado positivo como criterio futuro no ejecutado. **A2:** fronteras A/B, PII, host, HMAC, replay y negativas futuras no ejecutadas. **A3:** pruebas .NET/Angular/MySQL/Playwright NO_APLICA exclusivamente al diff Markdown, no PASS. **A4:** fast-forward, checks Scope Lock y V5 procedencia `completed/success` de SHA exacto y readback PLAN/COLA/BITACORA. **A5:** otra Task Javier distinta a la autora.
- **Rollback:** `git revert` normal del commit y nuevo evento compensatorio auditado, sin borrar BITACORA histórica.
