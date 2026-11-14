# SOLQARYN V5 — C07-02.M01.01 — Alcance y no regresión del storefront y checkout

**Autor:** PRIMARY DEV 03 — JAVIER; **fecha:** 2026-11-14; **autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C07-02, M01.01. **Tipo:** contrato documental preparatorio, no nueva implementación. La existencia de carrito o checkout previo no acredita flujo de pedido final, captura de pago, HMAC o protección anti-replay universales; todos los escenarios propuestos son **FUTUROS NO_EJECUTADOS**.

## 1. Objetivo y criterio de entrega

Definir límites auditables para tienda pública, catálogo, carrito, validación de checkout, creación futura de pedido, estados y confirmaciones, preservando ERP y el alcance comercial de cada empresa. Resultado exigido del bloque: **tienda pública, carrito, pedido, checkout, estados y confirmaciones**, con evidencia futura de **HMAC/host, anti-replay, tenant cruzado y flujo móvil**. Esta M01.01 fija objetivo, exclusiones, invariantes y estrategia de pruebas; **no** certifica ni implementa esas prestaciones. El comprador no puede convertir el precio, stock, EmpresaId, host o proveedor indicado por su navegador en autoridad del servidor.

## 2. Evidencia existente observada en dev (lectura selectiva, sin runtime)

| Superficie actual | Git blob SHA | Observación acotada y pendiente |
| --- | --- | --- |
| `backend/src/API/Controllers/TiendaController.cs` | `39a23ddb94b24077bbce79de1b965781b70ca37a` | API anónima `tienda`, endpoints públicos de bootstrap/productos/categorías y `POST checkout/validar`; recalcula precio/variante/stock vigente, documenta que **no reserva** inventario ni crea pedido ERP |
| `backend/src/API/Controllers/TiendaBindingController.cs` | `97fb0717baed839707dfd58cc98d9ce966fb8527` | `GET tienda/binding` usa `VerifiedPublicStoreTenant.Resolve(HttpContext)` y respuesta sin caché; la existencia de esta llamada no certifica toda resolución de host/forwarded headers |
| `backend/src/Domain/Entities/PublicStoreBinding.cs` | `dae03bcd963281d74e1d02e460934ab0783d39d5` | Asociación host/storeKey a una empresa; requiere pruebas de host no autorizado y fallback |
| `backend/src/Application/Services/CatalogoPublicoService.cs` | `3d1ba38e0638ad75ad23e92c98120cc1b99660e4` | Proyecciones y caché opcional con tenantKey; no inferir aislamiento A/B perfecto sin ensayos |
| `frontend/src/app/features/storefront/storefront.models.ts` | `de45fc1e9c0e3d1b98bd560b6e8006200d3a2b08` | Contratos de carrito, checkout validado, comprador y recibo; tipos UI no son reserva, pedido ni garantía de cobro |

La revisión no recorrió los pipelines completos HMAC, PSP, snapshots de pedido, consultas EF, fallbacks o middleware de proxy. Son **gaps de inspección** para M01.03 y fases posteriores, no incidentes confirmados. La ruta de `checkout/validar` presente no equivale a `pedido creado` ni autoriza cobros externos.

## 3. Matriz de alcance y ownership

| Flujo | Autoridad del diseño | Compatibilidad / exclusión actual |
| --- | --- | --- |
| Host a tienda | Resolver verificadamente host/storeKey/tenant A desde backend | Host desconocido, slug o cabecera manipulada = no selección arbitraria B |
| Catálogo visible | Sólo proyección publicada y DTO mínimo del tenant elegido | No exponer costo, PII, inventario privado, borradores ni otras empresas |
| Carrito | Cliente almacena intención de compra, IDs y cantidades; servidor revalida | Precio, descuentos, impuestos, variante y stock nunca se toman de cifras del navegador |
| Validación checkout | Cotización/validación efímera y revisable por cambios de stock | No proclamar reserva de stock, pedido ERP, factura, pago o éxito transaccional |
| Pedido futuro | Identidad de tenant + comprador + líneas server-side + identificador idempotente | No crear pedido, cobrar, activar PSP o sincronizar ERP en M01 |
| Confirmaciones futuras | Estado derivado de evidencia transaccional verificable | No convertir `redirect`, cierre de navegador, mensaje WhatsApp o demo en `PAGADO` |
| HMAC y antireplay futuros | Firma con alcance, caducidad, nonce, secret rotado y tiempo robusto | No declarar HMAC implantado ni reutilizar tokens hasta revisar código y CI causal |
| Accesibilidad móvil | Formulario y carrito adaptativos, foco, errores vinculados, red deficiente | No fingir mediciones Lighthouse/Playwright de un flujo que no se ha probado |

## 4. Invariantes de no-regresión NR01–NR12

- **NR01:** la resolución confiable de host/storeKey selecciona una sola empresa; nunca se confía ciegamente en Host/X-Forwarded-Host aportado por cliente.
- **NR02:** un pedido, carrito cotizado o caché de A nunca puede acceder a datos de B, incluso con IDs numéricos válidos.
- **NR03:** el precio, descuentos, impuestos, stock, SKU y disponibilidad se recalculan en backend al validar checkout.
- **NR04:** una variante inactiva o ajena no se resuelve automáticamente a otra variante ni a un producto sustitutivo sin confirmación.
- **NR05:** validar carrito **no** reserva unidades, no crea pedido ERP y no constituye captura de pago.
- **NR06:** cantidades y límites de líneas se validan; entradas inválidas se rechazan con error estructurado, sin efectos secundarios.
- **NR07:** firmas/credenciales HMAC, cuando su diseño sea aprobado, deben caducar, cubrir tenant/operación y prevenir replay en almacenamiento controlado.
- **NR08:** callbacks/confirmaciones futuras son idempotentes y sólo fuentes de pago verificadas podrán cambiar a estado pagado.
- **NR09:** reintentos, doble clic y desconexión no deben crear cobros o pedidos duplicados.
- **NR10:** costos internos, correo, teléfono, PII y tokens no se filtran en DTO públicos, caché, logs, URLs ni errores.
- **NR11:** compra móvil futura maneja teclado, foco, lector de pantalla, 320px, mensajes comprensibles y baja conectividad sin perder intención.
- **NR12:** facturas, pagos, pedidos y existencias históricos del ERP no se borran/recalculan mediante cambio preparatorio; rollback será aditivo y trazable.

## 5. Escenarios de aceptación FUTUROS NO_EJECUTADOS

| Par | Positivo requerido en fase técnica futura | Negativo requerido en fase técnica futura |
| --- | --- | --- |
| P01/N01 | Host vinculado A devuelve sólo catálogo publicado A | Host desconocido/spoofed no devuelve datos de B |
| P02/N02 | Carrito A con producto propio valida variante, SKU y cantidad | ID variante/producto B o inactivo no cotiza líneas A |
| P03/N03 | Servidor devuelve precio e impuesto recalculados al cotizar | Cliente altera precio/impuesto y el servidor lo rechaza/ignora |
| P04/N04 | Cotización refleja stock vigente antes del pedido | Stock agotado o cantidad superior al límite = conflicto, sin reserva |
| P05/N05 | Cuenta de líneas y normalización de cantidades son deterministas | Carrito vacío, líneas excesivas o cantidades inválidas = 4xx |
| P06/N06 | Binding firmado futuro contiene tenant, acción y expiración | Firma truncada, timestamp vencido o nonce repetido se rechaza |
| P07/N07 | Callback de pedido futuro tiene identidad idempotente | Duplicado/replay no crea doble pedido o doble pago |
| P08/N08 | Recibo futuro refleja pedido transaccional real | Redirección/WhatsApp/demo no se interpreta como transacción pagada |
| P09/N09 | Caché y galería se aíslan por host A y tenant | Consulta B no recibe cache/medios/costos de A |
| P10/N10 | Carrito móvil accesible con foco y errores asociados | 320px, teclado y lector no ocultan error ni impiden corregirlo |
| P11/N11 | Reintento tras desconexión mantiene estado seguro y verificable | Timeout no asume compra completada ni altera stock |
| P12/N12 | Auditoría futura conserva actor/tenant/operación con redacción | Logs/DTO nunca exponen HMAC secret, PII, JWT o costo |

Todos son contratos a implementar, ejecutar y evidenciar en fases posteriores. Una prueba `SKIPPED`, `CANCELLED`, `FAIL` o `IN_PROGRESS` no es PASS requerido.

## 6. Dependencias, impacto y cierre limitado a M01.01

- `DAG_TECNICO!A1007:J1007`: F=VALIDADO, G=SI para **preparación documental**. Origen de dependencias: doce familias C01/C02/C05, más C06-02 y C07-01, cada una M01.01–05 = **70/70 LISTO** verificadas contra PLAN_MAESTRO. El preflight no convierte esas preparaciones en implementación o promoción.
- **Cambios admitidos en esta microtarea:** sólo `docs/v5/` y sección completa de `CHANGELOG_AI.md`. Sin API/DTO, Angular, datos, migraciones, JWT/RBAC, HMAC, sesiones de PSP, DNS, infraestructura, workers o despliegues. No cambia arquitectura.
- **A0:** cotejo de evidencia Git y dependencias con 12 invariantes. **A1/A2:** contenido y escenarios futuros positivos/negativos revisados documentalmente, `NO_EJECUTADOS`. **A3:** compilaciones, DB, xUnit, Playwright y pasarelas `NO_APLICA` al diff Markdown. **A4:** publicar serializando HEAD, exigir Scope Lock y procedencia V5 `completed/success` sobre SHA exacto y readback PLAN/COLA/BITACORA. **A5:** otra Task Javier distinta del autor; no autoaprobación.
- **Rollback:** revert normal del commit y evento compensatorio idempotente nuevo, sin alterar BITACORA histórica ni QA/main/PROD. Los controles HTTP, MySQL, HMAC y pagos requieren futuras pruebas causales, no inferencia documental.
