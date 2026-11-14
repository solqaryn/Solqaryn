# SOLQARYN V5 — C07-02.M01.03: inventario causal del storefront y checkout

**ID:** C07-02.M01.03, PLAN_MAESTRO fila 1048. **Autor:** PRIMARY DEV 06 JAVIER; TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-14. **Fuente única:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`). **Repositorio:** `solqaryn/Solqaryn`, rama `dev`, PROJECT_SCOPE_LOCK=STRICT.

## Método, estado y alcance

Inventario de lectura de contratos y código en el HEAD de dev verificado por SHA blob. La tarea M01.03 pide **leer evidencia disponible sin asumir certificación**: no activa cobros, reservas, nuevos pedidos, credenciales, HMAC/nonce, trabajadores, callbacks ni despliegues. El contrato M01.01 continúa **VALIDANDO** por checks obligatorios CANCELLED en su SHA (no PASS); M01.02 está **LISTO** con A5 independiente pendiente. El gate DAG F=VALIDADO/G=SI y COLA G/O=SI autorizan solo **preparación documental reactiva**; no liberan M01.06, módulos terminales, migraciones o QA/main/PROD. Los 70 antecedentes M01.01–05 de C01/C02/C05/C06-02/C07-01 constan en evidencia institucional previa, no sustituyen el readback vigente.

La lectura de estos doce archivos es **muestra trazable, no prueba de ausencia de código adicional ni auditoría de toda la plataforma**. Ningún hallazgo de seguridad se presenta como explotabilidad demostrada.

## Fuentes S01–S12: rutas, hashes y límites

| Fuente | Ruta | Blob SHA observado | Evidencia y límite |
| --- | --- | --- | --- |
| S01 | `docs/v5/c07-02-m01-01-alcance-storefront-checkout.md` | `26310ed89f7510bc7e7c49b88ff1c2e3c315f49f` | NR01–NR12 y contratos futuros; su CI obligatoria fue CANCELLED, por lo cual M01.01 continúa VALIDANDO |
| S02 | `docs/v5/c07-02-m01-02-actores-interfaces-storefront-checkout.md` | `aecbb48a27b1276a1c6b6cda1e4f8260a7d284f0` | A01–A12, I01–I12, B01–B10; LISTO documental, A5 de Task distinta pendiente |
| S03 | `backend/src/API/Controllers/TiendaController.cs` | `39a23ddb94b24077bbce79de1b965781b70ca37a` | GET público y POST checkout/validar; recalcula precio/variante/stock sin reservar ni crear pedido ERP |
| S04 | `backend/src/API/Controllers/TiendaBindingController.cs` | `97fb0717baed839707dfd58cc98d9ce966fb8527` | GET tienda/binding con VerifiedPublicStoreTenant y Cache-Control no-store; no valida cadena completa de proxy |
| S05 | `backend/src/Domain/Entities/PublicStoreBinding.cs` | `dae03bcd963281d74e1d02e460934ab0783d39d5` | Entidad Host, StoreKey y EmpresaId; exige pruebas host/empresa cruzados |
| S06 | `backend/src/Application/Services/CatalogoPublicoService.cs` | `3d1ba38e0638ad75ad23e92c98120cc1b99660e4` | Servicio de proyección/catálogo y cache tenantKey opcional; aislamiento runtime no probado aquí |
| S07 | `frontend/src/app/features/storefront/storefront.models.ts` | `de45fc1e9c0e3d1b98bd560b6e8006200d3a2b08` | Modelos de carrito, comprador y checkout validado; tipos no garantizan pedido, pago o idempotencia |
| S08 | `frontend/src/app/features/storefront/storefront.component.ts` | `1490eb5950eb3a9619046972ccf78bb80cab4bd9` | Superficie de tienda y carrito Angular; falta evidencia Playwright móvil dirigida |
| S09 | `frontend/src/app/features/storefront/storefront-identidad.service.ts` | `c7dfc61c0b24cba9287b28fab25d3ef45cc4e05f` | Selección/identidad visual de storefront, no fuente única de autoridad de tenant |
| S10 | `frontend/src/app/core/interceptors/auth.interceptor.ts` | `4bdf19135ed1932a99fd09ace1f900bcccc5b25b` | Middleware HTTP cliente no reemplaza RBAC y tenant server-side |
| S11 | `frontend/server/seo-utils.js` | `b0886e30cc85efa6f800e558d5284412d8c573fc` | hostFromRequest lee x-forwarded-host/host; requiere revisar proxy confiable antes de inferir seguridad |
| S12 | `frontend/api/sitemap.js` | `ee50880520c906178a7ebd31553f3ab6d28f6b2d` | Sitemap público existente; no prueba indexación correcta para pedidos o preview de cada tenant |

## Brechas G01–G10: evidencia aún necesaria (NO fallos certificados)

| ID | Pregunta causal pendiente | Evidencia mínima futura |
| --- | --- | --- |
| G01 | ¿Host, X-Forwarded-Host y storeKey se validan en proxy confiable y resolver backend? | Pruebas host legítimo/desconocido y host spoofed, tenant A/B |
| G02 | ¿Catálogo y productos separan publicación, propietario y EmpresaId? | Pruebas negativas de IDs/variantes B consultados desde A |
| G03 | ¿El cache tenantKey evita mezcla de precios, media y disponibilidad? | Test A/B con invalidación, expiración y cambio de tenant |
| G04 | ¿Checkout valida montos, tasas, descuento y stock en backend? | Pruebas de manipulación de payload, límites y existencias |
| G05 | ¿Existe transición durable de cotización a pedido con transacción? | Diseño, integración EF/MySQL efímera y rollback sin reservar durante validar |
| G06 | ¿Los reintentos y doble click tienen clave idempotente durable? | Prueba concurrente de doble envío y resultado único |
| G07 | ¿Firmas HMAC, TTL, nonce y replay tienen autoridad implementada? | Diseño aprobado, secretos fuera del repo, positivos/negativos exact-SHA |
| G08 | ¿Callback PSP verifica firma, origen y reconciliación antes de PAGADO? | Mocks e integración de proveedor autorizada; sin cobros live |
| G09 | ¿DTO, URLs y trazas omiten PII, costos internos y secrets? | Escaneo dirigido de API, logs y acceso por rol/tenant |
| G10 | ¿El carrito móvil, errores y SEO evitan regresiones de accesibilidad y caché? | Playwright 320px/teclado/lector, robots/sitemap y host A/B |

Ninguna brecha confirma una vulnerabilidad ni autoriza proveedor de pago. La ruta `checkout/validar` no es `pedido creado` ni `pagado`; los tipos Angular tampoco lo son.

## Oráculos P01–P06 y N01–N12 — FUTUROS NO_EJECUTADOS

**Positivos:** P01 binding A resuelve únicamente A; P02 catálogo A muestra solo entidades publicadas A; P03 checkout recalcula totales e IVA del servidor; P04 variantes y cantidades válidas reflejan stock corriente sin reserva; P05 pedido futuro idempotente persiste una vez con evidencia de transacción; P06 usuario móvil identifica y corrige errores sin perder el carrito.

**Negativos:** N01 host desconocido o suplantado no resuelve B; N02 ID producto de B en A no cotiza; N03 variante inactiva no se sustituye silenciosamente; N04 precio/impuesto falsificados no se aceptan; N05 stock agotado se rechaza sin reserva; N06 exceso de líneas/cantidades falla cerrado; N07 click duplicado no duplica pedido; N08 nonce repetido o firma vencida no pasa; N09 callback no firmado no marca PAGADO; N10 cache B no expone datos A; N11 logs/DTO no exponen costos, JWT, secretos o PII; N12 redirección/WhatsApp/demo no demuestra pago.

Todos requieren ejecución posterior en CI dirigida (xUnit/EF/MySQL/Angular/Playwright cuando la implementación los haga causales). Ninguna validación futura se cuenta como PASS ahora.

## Impacto, controles y reversión

- **A0:** cotejar fuente V5, 12 blobs reales, alcance documental, semántica de host/tenant y exclusiones; monitorizar cambio de HEAD y procedencia.
- **A1/A2:** coherencia de inventario y criterios positivos/negativos **documentados**, no ejecución HTTP ni prueba A/B runtime. JWT/RBAC, tenant, HMAC y secretos permanecen intactos.
- **A3:** compilaciones .NET/Angular, MySQL, navegador y PSP **NO_APLICA** al changeset exclusivo Markdown; no inferir PASS desde documento.
- **A4:** exigir publicación fast-forward con CAS de `dev`, Scope Lock y check `V5 - Sin encabezados vacios ni atribucion cruzada` en `completed/success` del SHA exacto, más readback de PLAN/COLA/BITACORA.
- **A5:** revisión independiente exclusivamente por otra de las cinco Tasks Javier autorizadas, preservando el owner de DEV 06. Ninguna autoprobación.
- **Archivos del cambio:** solo este `docs/v5/*.md` y sección autocontenida de `CHANGELOG_AI.md`; sin alteración de API, DTO, Angular, SQL, jobs, infraestructura ni arquitectura.
- **Rollback:** `git revert` normal del commit publicado con registro compensatorio aditivo, nunca force-push ni supresión de BITACORA histórica.
