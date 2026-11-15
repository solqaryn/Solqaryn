# SOLQARYN V17 — C07-02.M02.04: configuración tipada, ámbitos y feature flags seguros del storefront y checkout

**Unidad:** C07-02.M02.04 · **PLAN_MAESTRO:** fila 1055 · **autor:** PRIMARY DEV 05 / JAVIER_CHATGPT · **TASK_ID:** `6adc2e55f47c81919f6374e561687076` · **riesgo de esta entrega:** R0, contrato documental sin runtime. **Repositorio:** `solqaryn/Solqaryn@dev`; **PROJECT_SCOPE_LOCK:** STRICT; **fecha:** 2026-11-15.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), V15/V16/V17, Skill canónica, PLAN/COLA/DAG verificados por ID. Esta entrega se limita a definir configuración futura de tienda, carrito, validación de checkout y operaciones comerciales; no modifica código .NET/Angular, variables efectivas, secretos, JWT/RBAC, tablas/migraciones, proveedores de pago, DNS/host, pedidos, cobros, rutas ni datos.

## 1. Fronteras, dependencias y estado real

- `C07-02.M01.06` define gates G01–G16 y decisiones D01–D08 aún no resueltas; su A5 independiente habilita preparación M02, **no** checkout productivo ni liberación de M03.
- `C07-02.M02.01` define E01–E16 e invariantes INV01–INV20. `Empresa`, `Producto` y conceptos ERP existentes son referencias; `TiendaPublicada`, `HostTienda`, `CarritoCliente`, `OrdenComercial` y `IntentoPagoPSP` siguen siendo modelos **propuestos**, no entidades nuevas.
- La ruta preexistente `POST tienda/checkout/validar` representa validación/cotización, no pedido durable, inventario reservado, PSP reconciliado ni cobro; antes de cualquier implementación deberá comprobarse de nuevo contra HEAD.
- Contrato únicamente conceptual: `ConfigTiendaV1`, `PoliticaCarritoV1`, `PoliticaCheckoutV1`, `PoliticaPagoV1` y `FeatureFlagsTiendaV1` son **nombres propuestos**. No constituyen clases, DTO publicados, migrations ni endpoints.
- La resolución autoritativa de `EmpresaId`, host de confianza, membresía, roles, propiedad de recursos, precios e impuestos pertenece al backend; **nunca** a una bandera del navegador, slug, header o cookie aportados por el cliente.

## 2. Ámbitos y precedencia — reglas propuestas

| Ámbito | Ejemplos de datos futuros | Condición para aplicar | Rechazo/fallback seguro |
| --- | --- | --- | --- |
| Política de plataforma | límites duros máximos, prohibición de cobro sin proveedor, TTL máximo, mínimo TLS | versión validada y autorizada centralmente | `OFF` o error explícito; el tenant nunca puede relajar restricciones |
| Empresa | `EmpresaId`, tienda habilitada, moneda autorizada, límites inferiores | contexto autenticado o tienda pública resuelta por servidor, ownership estable | Empresa desconocida/revocada => denegar |
| Tienda/host publicado | `StoreKey`, hostname normalizado, estado de publicación y versión | binding persistido y comprobado contra proxy confiable | host ambiguo/no reconocido => 404 seguro sin tienda por defecto |
| Canal | público, operador interno, móvil; lenguaje y accesibilidad | canal permitido para Empresa, sin elevar rol | canal no válido => no resolver comercio |
| Operación | carrito, cotización, pedido, pago, callback | fase expresamente habilitada, gates y capacidad backend comprobados | `false` por defecto para operaciones durables |
| Sesión/actor | restricciones temporales y permisos efectivos | JWT/RBAC/membresía activos para operación privada; sesión pública acotada para carrito anónimo | token revocado/cross-tenant => denegar sin mutación |

**Orden de evaluación obligatorio futuro:** (1) restricciones globales no negociables, (2) tenant y host verificados por backend, (3) políticas de Empresa dentro de límites globales, (4) tienda/canal publicado, (5) permiso real de operación/actor, (6) flag de disponibilidad técnica. El resultado es la intersección restrictiva, **no** una precedencia que permita que un flag override el rechazo de seguridad. Para host desconocido no existe fallback al tenant A ni al primero del catálogo. Configuración ausente, inválida, deserialización fallida o versión no soportada => `DENY` para compra/pago y error seguro para visualización dependiente.

## 3. Contrato tipado propuesto (C01–C16)

| ID | Campo futuro tipado | Valor restrictivo inicial/validación | Ámbito/propósito |
| --- | --- | --- | --- |
| C01 | `ConfigVersion: int` | versión exacta allowlisted, rechazar 0/desconocida | plataforma/Empresa |
| C02 | `StorefrontEnabled: bool` | `false` hasta publicación y owner válidos | Empresa/tienda |
| C03 | `TrustedHostBindingId: opaque-id` | ausente => no resolver host; validar proxy+storeKey | tienda |
| C04 | `CatalogPublishedOnly: bool` | `true` inmutable para canal público | plataforma |
| C05 | `MaxCartLines: int` | rango futuro 1..100; superar techo => rechazar | Empresa |
| C06 | `MaxUnitsPerLine: int` | rango futuro 1..1000 sujeto a stock real; no reservar | Empresa |
| C07 | `CartTtlMinutes: int` | positivo y acotado por máximo de plataforma | Empresa |
| C08 | `QuoteTtlSeconds: int` | positivo, acotado, UTC; cotización expira | checkout |
| C09 | `ServerRepriceRequired: bool` | `true` inmutable; cliente no fija importes | plataforma |
| C10 | `CheckoutValidationEnabled: bool` | `false` hasta verificar handler actual y permisos | Empresa/operación |
| C11 | `DurableOrderCreationEnabled: bool` | `false` hasta M03, autorización y A0–A5 | operación |
| C12 | `StockReservationEnabled: bool` | `false` hasta transacción MySQL y rollback verificados | operación |
| C13 | `PaymentCaptureEnabled: bool` | `false` hasta PSP aprobado y conciliación real | operación |
| C14 | `WebhookVerificationRequired: bool` | `true` no relajable para cualquier PSP futuro | plataforma |
| C15 | `NonceReplayWindowSeconds: int` | positivo, acotado y solo si HMAC real autorizado | callback |
| C16 | `PublicResponseCacheScope: enum` | sólo `TENANT_HOST_VERSION`; jamás global | tienda/caché |

Los rangos numéricos son **propuestas de diseño**, no límites efectivos del producto ni defaults reales constatados. Antes de codificarlos deben pasar por requisitos comerciales, compatibilidad, tests de borde y versionado. No almacenar llave HMAC, JWT, PAN, contraseñas ni secretos en objetos de configuración, documentos, logs o variables públicas Angular.

## 4. Flags, estados y observabilidad (F01–F12)

| Flag futuro | Estado inicial | Prerequisito indispensable |
| --- | --- | --- |
| F01 `PublicCatalog` | `OFF` | binding host/tenant, publicación y caché A/B verificadas |
| F02 `AnonymousCart` | `OFF` | ownership de sesión, expiración y cuotas seguras |
| F03 `CheckoutQuote` | `OFF` | backend recalcula stock/precio/IVA, sin efecto durable |
| F04 `OrderSubmit` | `OFF` | transacción, idempotencia, CAS y prueba negativa |
| F05 `StockReservation` | `OFF` | MySQL 8.4 efímero, rollback y concurrencia sin oversell |
| F06 `PaymentRedirect` | `OFF` | PSP aprobado, consentimiento, importe autoritativo |
| F07 `PaymentCallback` | `OFF` | firma/TTL/nonce/replay, tenant e idempotencia verificados |
| F08 `OrderConfirmation` | `OFF` | evidencia server-side de orden/estado, nunca redirect solo |
| F09 `PublicSeoCache` | `OFF` | partición por empresa/host/versión y purga |
| F10 `MobileCheckoutUi` | `OFF` | Playwright 320px, teclado, foco y error de red |
| F11 `StorefrontTelemetry` | `OFF` | minimización PII; correlación sin JWT ni claves |
| F12 `GracefulRollback` | `OFF` | política por fase y recuperación sin pérdida de orden/stock |

Estos nombres son **inertes**: no se crea ni se activa flag alguno. `ON` jamás reemplaza una autorización JWT/RBAC o un gate de riesgo. Cada flag futuro requiere `version`, `ownerTenant`, `changedBy`, `changedAtUtc`, motivo y expiración/rollback auditables, sin guardar valores secretos ni payloads sensibles. Un estado no reconocido o ambigüedad de configuración se interpreta como `OFF`.

## 5. Compatibilidad, seguridad y riesgos

1. **Tenant A/B:** mismo nombre de tienda, storeKey, id de producto o bandera entre A y B no comparte caché, carrito ni resultado. Fallar cerrado si binding host/Empresa está ambiguo.
2. **JWT/RBAC:** para backoffice y mutaciones comprobar membresía activa, rol, revocación y propiedad de recurso en backend después de resolver tenant; UI/flag no es permiso.
3. **HMAC/replay:** toda recepción de callback futura requiere firma verificada con clave fuera del repo, nonce y ventana con reloj UTC, versión de clave, tenant/pedido/importes; duplicados no generan doble efecto.
4. **Finanzas/inventario:** `CheckoutQuote` no crea orden, reserva, pago, factura o evento contable. Cambios de precio/IVA/stock entre quote y commit exigen nueva verificación.
5. **Reversión:** apagar flag no borra órdenes/reservas/pagos ya durables; éstos requieren conciliación transaccional y auditoría antes de compensar.
6. **Compatibilidad:** versiones desconocidas no deben degradarse silenciosamente; no hay migración de datos, endpoint ni config efectiva en esta entrega.
7. **PII:** no registrar dirección, tarjeta, JWT, firmas, claves o IDs de B en telemetría pública, error body o caché.
8. **Dependencias:** implementar F04–F08 requiere M03 real, pruebas por tecnología y autorizaciones específicas; M02 documental no habilita QA/main/PROD.

## 6. Doce casos positivos/negativos FUTUROS — NO EJECUTADOS

| Caso | Aceptación futura | Rechazo futuro |
| --- | --- | --- |
| P01/N01 | host confiable A selecciona tienda A | Host spoofed o desconocido no selecciona tienda B |
| P02/N02 | config versionada de A se deserializa | versión inválida/missing deja flags OFF |
| P03/N03 | límite de líneas/unidades válido se acepta | 0, negativo, overflow, exceso se rechaza |
| P04/N04 | catálogo A muestra únicamente publicados A | borrador/costo/variante B nunca se filtra |
| P05/N05 | cotización recalcula precio, IVA y stock | importe forjado o producto agotado se rechaza |
| P06/N06 | validar checkout no muta DB | activar flag de quote no crea pedido/pago |
| P07/N07 | actor A con membresía activa edita su tienda | JWT revocado, rol insuficiente o B denegados |
| P08/N08 | misma clave idempotente mismo payload genera un efecto | replay concurrente y clave de B no duplican ni cruzan |
| P09/N09 | HMAC genuino y vigente en PSP autorizado se valida | firma inválida, vencida o nonce repetido rechazados |
| P10/N10 | callback auténtico reconcilia estado una vez | redirect/callback sin firma no marca PAGADO |
| P11/N11 | caché versionada por host/Empresa se aísla | caché o SEO A nunca retorna información de B |
| P12/N12 | UI móvil y reconexión muestran estado de servidor | offline/timeout no anuncian compra confirmada |

**Evidencia de ejecución de P01–P12/N01–N12 en esta entrega:** `0/12` ejecutados, resultado `NO_MEDIDO`. Son criterios de prueba futuros, no asserts, simulaciones ni validación funcional presente. Un cambio posterior de código R1/R2 exige pruebas negativas xUnit, categoría Integration con MySQL 8.4 aislado si toca EF, Playwright/Chromium si altera UI y checks exact-SHA Scope Lock/V5, además de A5 independiente.

## 7. A0–A5, gates y rollback del documento

- **A0 (documental):** alcance C07-02.M02.04, contratos tipados C01–C16, flags F01–F12, matriz de ámbitos, rechazo fail-closed, referencias M01.06/M02.01 y criterios futuros. Archivo único `docs/v5/c07-02-m02-04-configuracion-tipada-ambitos-feature-flags-storefront-checkout.md` y sección autocontenida de `CHANGELOG_AI.md`.
- **A1 (R0):** inspección consistente de tipos, límites, estados, positivos/negativos futuros. No afirma pruebas xUnit ejecutadas.
- **A2 (R0):** análisis estático tenant A/B, host, HMAC, replay, JWT/RBAC, PII, inventario y pagos. No afirma seguridad runtime validada.
- **A3 (R0):** `NO_APLICA` a HTTP, Angular/Playwright, xUnit, PSP, EF/MySQL y migraciones por diff documental; checks de runtime ajenos no son PASS propios.
- **A4:** sólo una vez publicado por fast-forward/CAS en `dev`, Scope Lock y procedencia V5 `completed/success` por SHA causal y PLAN/COLA/DAG/BITACORA con readback; nunca calificar QUEUED/IN_PROGRESS/CANCELLED/FAIL como PASS requerido.
- **A5:** otra Task Javier distinta de `6adc2e55f47c81919f6374e561687076` debe revisar original, diff, blobs, límites, riesgos y evidencias; puede emitir A5_FAIL. Esta entrega no se autoaprueba.

**Rollback R0:** conservar eventos y owner; corrección aditiva o `git revert` dirigido del commit documental si fallan sus criterios, sin reescribir historia ni borrar BITACORA. No activar flags reales, migraciones, PSP, workers, scheduler, otros entornos ni secretos. Al terminar, liberar el candado V17 determinista de la fila 1055 sólo tras readback causal; prerequisitos futuros permanecen sometidos a DAG/COLA y revisión A5 verdadera.
