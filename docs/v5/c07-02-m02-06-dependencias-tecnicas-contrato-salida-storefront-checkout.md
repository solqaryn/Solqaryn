# SOLQARYN V19 — C07-02.M02.06: contrato de salida M02 y dependencias técnicas de storefront y checkout

**Microtarea:** `C07-02.M02.06` · PLAN_MAESTRO fila 1057 · propietario DEV01 (`6adbb9fff1548191a1bd5b30d9e9984a`) · fecha 2026-11-18 · riesgo **R0 documental** · `solqaryn/Solqaryn@dev` · `PROJECT_SCOPE_LOCK=STRICT`.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), Skill canónica y PLAN/COLA/DAG vigentes. **Resultado exigido:** fijar las dependencias y el contrato de salida del lote M02 para tienda pública, carrito, validación de checkout, pedido futuro, estados y confirmaciones. No constituye implementación ni autorización comercial, financiera, de migración, QA, main o producción.

## 1. Fuentes, predecesores y condición de entrada

| Unidad | Documento y blob constatado en dev | Evidencia y límite |
| --- | --- | --- |
| C07-02.M01.06 | `c07-02-m01-06-gates-rollback-liberacion-storefront-checkout.md` · `122674cd481553a8bf2da3d420f379604211d9f1` | LISTO y A5 independiente; dieciséis gates, doce pares futuros |
| C07-02.M02.01 | `c07-02-m02-01-entidades-ownership-invariantes-storefront-checkout.md` · `f223040f96c6c45c02d8f1a37db86f2df2ab3a53` | LISTO y A5; E01–E16, INV01–INV20, sin nuevas tablas |
| C07-02.M02.02 | `c07-02-m02-02-api-dto-errores-validaciones-storefront-checkout.md` · `ac0aabf6a85ad893e1be0a8ed2291d7eb75e0be1` | LISTO y A5; ocho operaciones/DTO conceptuales, doce errores, dieciséis validaciones |
| C07-02.M02.03 | `c07-02-m02-03-persistencia-indices-migraciones-fixtures-storefront-checkout.md` · `6c0f0134683be361723e1f642977f605a2b108ed` | LISTO; A5 correctiva R0 independiente en HEAD `d91a2b95`, P2 histórico conservado, sin migración |
| C07-02.M02.04 | `c07-02-m02-04-configuracion-tipada-ambitos-feature-flags-storefront-checkout.md` · `806019414c42ee33d7e7f3d2eb7e964253335102` | LISTO y A5; dieciséis campos tipados, doce flags OFF e inertes |
| C07-02.M02.05 | `c07-02-m02-05-eventos-idempotencia-correlacion-limites-storefront-checkout.md` · `a1d598bff6b72fc972e6b39f4e652ae76d749a82` | LISTO y A5; doce eventos hipotéticos, catorce campos/invariantes |

Los archivos anteriores están bajo `docs/v5/`. DAG_TECNICO fila 1018 declara `F=VALIDADO/G=SI` con `C07-02.M01.01–06+A5`, `C07-02.M02.01–05+A5`, `C06-02.M01.06+A5` y `C07-01.M01.06+A5`. En el CLAIM real se verificaron `PLAN H1057=PENDIENTE/I1057` vacío y `COLA G1057/O1057=SI`, candado V17 `SQV17_C07_02_M02_06` (`2000001057`) y CAS `occurrencesChanged=1`. Son permisos **documentales**, no derechos para ejecutar M03 ni servicios reales.

## 2. Contrato de salida coherente y fronteras

1. **Tenant y propiedad de recursos.** El backend, no headers, slugs, cookies, HTML ni flags Angular, acredita Empresa, tienda, actor, permisos, membresía, roles, revocación y ownership. Cada entidad comercial futura, carrito, producto, pedido, descuento, caché o evento conserva aislamiento A/B. `EmpresaId=NULL` histórico se mantiene sin titular hasta reconciliación autorizada.
2. **Host de tienda confiable y corrección P2.** El host canónico *activo y verificado* tiene un solo titular global, incluso entre A/B y versiones activas. I01 propone `UNIQUE(clave_host_activo_global)` sin EmpresaId con NULL en registros inactivos; es **candidato, no DDL**. Requiere canonicalización/IDNA, trusted proxy, tratamiento de wildcard, verificación de titularidad y CAS; `Host`/`X-Forwarded-Host` arbitrarios o host ambiguo deniegan sin fallback de tenant. Mantener la crítica A5_FAIL histórica y la reparación R0 revisada independientemente.
3. **Catálogo y cotización honestos.** Mostrar únicamente variantes/productos publicados del tenant autorizado, sin costos internos ni caché cruzada. Stock, precio, descuentos, IVA y moneda se recalculan desde servidor. La ruta existente `POST tienda/checkout/validar` es validación/cotización: jamás prueba orden durable, reserva de stock, factura o cobro.
4. **Durabilidad, finanzas e inventario.** Orden futura y reserva requieren transacción, permisos, versión/CAS e idempotencia vinculada a Empresa+actor+operación+payload. Denegar exceso, overflow, stock agotado, replay, colisión de clave A/B, concurrencia y efectos parciales. No duplicar tablas/entidades existentes de ERP; no tocar ventas/IVA/stock en esta entrega.
5. **PSP/HMAC y confirmación.** Una futura notificación de pago exige proveedor habilitado por permiso específico, firma autenticada, nonce, TTL, versión, tenant, monto, idempotencia y conciliación server-side. Redirección de UI, callback sin firma o evento recibido no significa PAGADO. Nunca PAN, JWT, HMAC key, PII excesiva o secretos en documentación, logs, cache o frontend.
6. **Flags, estados y eventos futuros.** `ConfigTiendaV1`, `FeatureFlagsTiendaV1` y eventos E01–E12 sólo están especificados, no existen en runtime. Flags F01–F12 permanecen conceptualmente OFF y no elevan RBAC; versión inválida/host no confiable => fail closed. Idempotencia, causalidad y correlación no crean scheduler, outbox o worker durante M02.
7. **Contratos y compatibilidad.** Preservar arquitectura Domain ← Application ← Infrastructure y API, clientes Angular 22.2.1/Playwright, backend .NET 10 y Oracle EF Core 10/MySQL 8.4. Verificar interfaces, DTO, consumidores y rollback en el HEAD exacto antes de cualquier código M03; no introducir APIs ficticias por documento.

## 3. Matriz P/N de implementación futura — NO_EJECUTADOS

| ID | Positivo futuro | Negativo / frontera |
| --- | --- | --- |
| H01 | Host canónico activo y confiable selecciona tienda A | B reclama host de A, dos versiones activas, wildcard/IDNA ambiguo o host spoofed => DENY |
| H02 | Catálogo público A entrega producto/variante publicada A | B, borrador, costo o caché de otro tenant no visible |
| H03 | Cotización recalcula cantidad, precio, IVA y stock vigentes | cero, negativo, overflow, monto cliente y stock agotado rechazados |
| H04 | Validar checkout devuelve sólo cotización | timeout/validar no crea orden, pago, factura ni reserva |
| H05 | Idempotencia future acepta mismo actor/Empresa/operación y huella | payload cambiado, key B o doble comando no duplica efectos |
| H06 | Transacción futura con CAS confirma reserva autorizada | carrera A/B, sobreventa y fallo parcial revierten |
| H07 | JWT/RBAC/membresía actuales permiten consulta de pedido propio | token revocado, actor B/rol indebido o anónimo => DENY |
| H08 | Callback PSP auténtico firmado satisface TTL/nonce/tenant/monto | firma falsa, expiración/replay o PSP no habilitado no modifica |
| H09 | Pago/orden se confirma sólo tras commit y conciliación | redirect, callback no verificado o falta de commit no marca PAGADO |
| H10 | Flags/versiones válidos restringen una capacidad existente | configuración ausente, override de seguridad o versión ajena => OFF |
| H11 | Auditoría permitida aporta correlación opaca | logs contienen JWT/HMAC/PAN/PII de B => FAIL |
| H12 | UI móvil 320 px, teclado, foco y error de red son honestos | error/offline/retry no presentan confirmación falsa |

**Métricas de esta entrega documental:** 0/12 casos ejecutados en runtime, cobertura de APIs, xUnit, MySQL, PSP/HMAC y Playwright `NO_MEDIDO`. Son oráculos de aceptación, no tests PASS.

## 4. Puertas de salida M02 y de entrada técnica posterior

**R0 M02.06:** sólo si predecesores, owner y A5 están acreditados, Scope Lock/V5 en SHA exacto `completed/success`, diff y fuente coherentes, BITACORA/PLAN/COLA/DAG readback, y revisión independiente A5 de otra Task Javier. Un `LISTO` documental no constituye `A5_PASS` ni libera producción.

**M03 R2:** requiere candidato propio `PLAN H=PENDIENTE/I vacío`, `COLA G/O=SI`, `DAG F=VALIDADO/G=SI`, V17 lock CAS exclusivo, ausencia de escritores y análisis por capas/consumidores. La persistencia exige **MySQL 8.4 efímero Oracle EF 10**, tests `Category=Integration` realmente ejecutados, claves tenant/global, colisiones/concurrencia, migración/Down/restore y 0 fallos. API/backend requiere .NET 10 Release, xUnit positivos y negativos JWT/RBAC/tenant/revocación; Angular/UI requiere tipado/build y Chromium/Playwright cuando aplique. `QUEUED/IN_PROGRESS/CANCELLED/SKIPPED/FAILURE` no es PASS requerido. PSP/HMAC/contratación, datos productivos, Aiven real, QA/main/PROD, DNS y secretos siguen **NO_GO** sin autorización específica.

## 5. Calidad, trazabilidad y reversión

**A0:** revisión de seis fuentes M01.06/M02.01–05, blobs, corrección P2, propietario/alcance y diff sólo Markdown/CHANGELOG. **A1:** H01–H12 como especificación positiva/negativa futura, cero ejecuciones. **A2:** revisión documental de tenant A/B, host global, JWT/RBAC, HMAC/replay, stock, PSP y privacidad. **A3:** compilación/HTTP/MySQL/Playwright `NO_APLICA` a este cambio R0, jamás PASS fingido. **A4:** commit fast-forward serializado en dev con verificación Scope Lock, V5 y readback por SHA exacto; esperar CI externa sin falsear LISTO. **A5:** revisión independiente de otra Task Javier y derecho a rechazar; no autoprobación.

**Rollback:** revert selectivo de documentos con HEAD actual preservando cambios ajenos, sin force-push, historia reescrita ni borrado de BITACORA. Cualquier futuro cambio R2 sobre pedidos, stock, esquema o pagos exigirá restauración/reconciliación verificadas, no sólo desactivar un flag. Conservar todo el historial, autor, SHA y limitaciones verificables.
