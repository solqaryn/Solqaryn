# SOLQARYN V5 — C08-02.M01.05 — Criterios de aceptación de mesas, QR y autoservicio

**Autor:** PRIMARY DEV 06 — JAVIER, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Fecha:** 2026-11-14. **Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, C08-02.M01.05. **Naturaleza:** contrato de aceptación documental; **todos los escenarios de runtime están FUTUROS NO_EJECUTADOS**.

## 1. Propósito y secuencia de evidencia

La salida exigida para C08-02 es mesas, QR, menú, pedidos internos y autoservicio con rechazos reales de **QR falso, mesa ajena y cierre de pedido inseguro**. Este documento convierte en oráculos medibles los invariantes NR01–NR14 de M01.01, actores A01–A14/contratos I01–I14/fronteras F01–F09 de M01.02, evidencia de siete blobs y brechas G01–G10 de M01.03, y riesgos R01–R14/supuestos S01–S08/decisiones D01–D08 PENDIENTES de M01.04. La matriz no autoriza crear API, tables, JWT policies, QR real, nuevos pagos, migraciones, webhooks ni producción.

**Regla de verdad:** documentar el escenario no lo ejecuta. Un resultado PASS futuro requiere artefacto, fixture, comando CI, SHA exacto, estado `completed/success` y verificación de efectos en DB/API/UI. Los checks de Scope Lock/CHANGELOG de esta entrega Markdown no prueban ningún caso P/N.

## 2. Matriz P01–P14 / N01–N14 — FUTUROS NO_EJECUTADOS

| ID | Positivo observable a ejecutar | Negativo obligatorio a ejecutar | Evidencia causal requerida |
| --- | --- | --- | --- |
| P01/N01 | QR vigente y autorizado A devuelve proyección pública de mesa y sede A | QR falso, alterado, expirado, revocado o de tenant B no devuelve datos ni efectos | HTTP/API + fixtures A/B + resultado 4xx |
| P02/N02 | Menú visible muestra productos publicados y precios canónicos | Producto oculto o de otra sede no se muestra ni se fuerza por query | HTTP contrato menú + MySQL fixture |
| P03/N03 | Usuario A con rol mesero crea pedido válido mesa A | MesaId/SucursalId de B o rol revocado rechazan operación sin persistencia | JWT/RBAC + MySQL before/after |
| P04/N04 | Pedido retorna total calculado por servidor | Cliente altera precio, IVA o descuento sin modificar total canónico | xUnit + HTTP DTO manipulado |
| P05/N05 | Reintento idéntico con misma clave retorna misma operación | Doble submit concurrente no crea 2 pedidos; payload diferente con misma clave falla | MySQL transacción/idempotencia |
| P06/N06 | Cocina de A recibe y avanza sólo pedido de A | Cocinero de B o caja sin permiso no ve/muta preparación A | Auth A/B + separación roles |
| P07/N07 | Estado permitido Borrador→aceptado/preparación/servido registra auditoría | Transición inválida o con versión antigua rechaza sin cambio | xUnit máquina de estados + conflictos |
| P08/N08 | Cajero con rol válido cierra pedido autorizado exactamente una vez | Replay cierre o doble clic no duplica factura, stock, cobro ni saldo | MySQL/Pago fixture efímero, no PSP real |
| P09/N09 | Fallo transaccional deja pedido, factura y stock coherentes | Interrupción entre commit y notificación no confirma cierre parcial | integración MySQL + rollback/replay |
| P10/N10 | Factura y devolución respetan reglas ERP vigentes | Cierre no genera pago/saldo sin autorización comercial | regresión de ventas/facturas/cuentas |
| P11/N11 | QR revocado deja de resolver antes de nuevos pedidos | QR histórico/copiado o sesión revocada no revive acceso | TTL/rotación/revocación negativa |
| P12/N12 | Auditor autorizado ve trazabilidad mínima de tenant A | Logs/errores no muestran JWT, PII, claves QR ni pedidos de B | revisión logs & pruebas de privacidad |
| P13/N13 | UI móvil accesible muestra éxito solo tras respuesta servidor | Offline/timeout/401/409 no muestra paid/confirmado falso | Angular/Playwright Chromium accesibilidad |
| P14/N14 | Reconciliación segura de cancelación/reversión conserva auditoría | Reintento de cancelar/cerrar terminal no reabre ni duplica movimientos | xUnit transiciones + MySQL A/B |

**Gates sobre la matriz:** P03/P06/N03/N06 requieren JWT/RBAC y pertenencia A/B reales; P05/P08/P09/N05/N08/N09 exigen transacción MySQL efímera; P13/N13 necesitan prueba visual E2E; P01/N01 requieren verificación futura de firma/expiración sin incluir secretos. Ningún `SKIPPED`, `CANCELLED` o `in_progress` de una suite requerida equivale a PASS.

## 3. No funcionales NFR01–NFR08 y señales de rechazo

| ID | Propiedad | Oráculo mínimo futuro |
| --- | --- | --- |
| NFR01 | Seguridad multitenant | Recursos, errores, datos y trazas A/B separados en API/DB, no solo Angular |
| NFR02 | Mínimo privilegio | Mesero/cocina/caja/auditor roles y sucursal revalidados cada operación |
| NFR03 | Idempotencia | Crear/cerrar/cancelar con replay, fingerprint y mutación exactamente una vez |
| NFR04 | Concurrencia | Versiones/locks evitan doble cierre y pérdida de líneas o stock |
| NFR05 | Privacidad | Anonimización, caché, logs y respuestas públicas sin PII o secretos |
| NFR06 | Accesibilidad | Teclado, foco, mensajes de error, lector pantalla y viewport 320 px |
| NFR07 | Observabilidad | Correlación request, tenant, sucursal, actor, estado y resultado saneados |
| NFR08 | Reversibilidad | Fallo técnico revierte efectos sin datos interempresa, con auditoría durable |

Errores por entidad inexistente y entidad ajena deben evitar filtrar existencia o PII. Aceptar respuestas 403 o 404 según política homogénea definida en M02, nunca permitir una lectura a B por ser válido el identificador. Descartar respuestas recibidas tardíamente si la UI ya cambió de mesa o tenant. El acceso público al menú no concede estado ni permisos privados.

## 4. Evidencia por capa y pruebas necesarias

| Capa / frontera | Evidencia mínima requerida para etapas ejecutables | Situación en M01.05 |
| --- | --- | --- |
| Domain .NET 10 | xUnit de estados, montos canónicos, idempotencia y no-regresión ERP | PENDIENTE; sin nueva implementación |
| Application | Casos de uso con contexto tenant/sede y validación de cada transición | PENDIENTE; sin servicio nuevo |
| Infrastructure/MySQL 8.4 | Fixtures A/B, índice, unicidad, transacción, replay y rollback | PENDIENTE; no se migró ni se probó DB |
| API/JWT/RBAC | 401/403/404/409 con tokens/roles vigentes/revocados y filtrado de errores | PENDIENTE; sin endpoints nuevos |
| Angular 22.2.1 | UI de QR/menú/mesas, permisos server-side y accesibilidad responsive | PENDIENTE; sin componente |
| Playwright/Chromium | Flujos móviles, expiración, offline, foco y lector pantalla | PENDIENTE; sin navegador ejecutado |
| Observabilidad | Correlation ID, actor/sede/tenant y ausencia de secretos/PII | PENDIENTE; sin logs runtime |
| DevOps/CI | Scope Lock, diff, tests dirigidos exact-SHA y A5 independiente | Solo comprobación documental pertinente ahora |

Los archivos reales `PedidoVenta`, `PreparacionPedidoVenta`, `PedidoVentaService`, `PedidoVentaRepository`, `PedidoVentaConfiguration`, `FacturaPago` y el modelo Angular de pedido comercial son precedentes **parciales**, no certificación de restaurante. El inventario M01.03 contiene hashes exactos que deberán releerse si cambia HEAD.

## 5. Prohibiciones, condiciones de salida y reversión

**NO_GO futuros:** QR sin revocación/firma verificable; mesa B accesible desde A; JWT/RBAC basado sólo en guard Angular; estado terminal sin idempotencia; cierre con cobro/stock no transaccional; D01–D08 sin decisión y prueba causal; P/N pendientes presentados como PASS; migración, pago o infra productiva no autorizados. No liberar M01.06 o etapas M02/M03 por esta sola redacción; aplicar sus propios DAG, A0–A4 y A5.

**Impacto hoy:** único archivo bajo `docs/v5` y sección aditiva `CHANGELOG_AI.md`. Sin cambios de Domain, Application, Infrastructure, API, DTO, cliente Angular, MySQL, secretos, cuentas, DNS, webhooks, QA, main o PROD.

**A0:** validar gobierno, PLAN/COLA/DAG, owner exclusivo y ausencia de encabezados vacíos. **A1:** confirmar 14 positivos y 14 negativos y ocho NFR en el documento como diseño, no ejecución. **A2:** amenazas tenant A/B, QR falso, roles, replay, cierre y privacidad cubiertas contractualmente, sin test de aislamiento runtime. **A3:** referencias M01.01–04 y arquitectura compatibles; .NET/MySQL/Angular/Playwright `NO_APLICA` para este diff exclusivamente Markdown. **A4:** publicar fast-forward y comprobar exact-SHA Scope Lock y procedencia V5 con `completed/success`, más readback PLAN/COLA/BITACORA. **A5:** revisión documental independiente de otra Task Javier; autor no puede revisarse.

**Rollback:** revert Git ordinario del commit documental y evento compensatorio de auditoría. No alterar BITACORA histórica ni convertir aceptación futura en permiso de ejecución real.
