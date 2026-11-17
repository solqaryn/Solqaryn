# SOLQARYN V19 — C09-04.M05.01: regla reproducible de adaptadores de mensajeria

Proyecto: solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. Scope unico C09-04.M05.01, PLAN fila 1430, owner DEV05, candado V17 2000001430. Riesgo R2: idempotencia, revocacion intra-lote y privacidad multitenant.

## A0 — Impacto real

Se amplian pruebas xUnit del `OutboxTenantDeliveryWorker` existente, sin modificar codigo de produccion, contratos API, EF, esquema MySQL ni la ruta Angular. El Worker sigue sin ser HostedService ni estar registrado como transporte SMTP, WhatsApp o proveedor adicional. Las pruebas son una barrera de reglas previa a cualquier futuro adaptador; **no certifican adaptadores conectados**.

## A1 — Oraculos ejecutables

- Positivos existentes: autorizacion tenant y mensaje, una sola entrega confirmada.
- Negativo de replay: dos eventos del mismo tenant con identica clave idempotente son rechazados antes del primer despacho/confirmacion.
- Negativo de revocacion tardia: dos mensajes de claves distintas, el gate permite el primero y revoca el segundo; exactamente un efecto, una confirmacion, un fallo reprogramado con mensaje persistido generico `DELIVERY_FAILED`.
- Fronteras ya cubiertas por el mismo archivo: empresa 0, DateTime no UTC, cancelacion y lote contaminado entre empresas A/B.

## A2–A3 — Limites y verificacion

xUnit usa repositorio y dispatcher dobles; la logica de `OutboxRetryProcessor` y worker es real. No se ejecutan MySQL, HTTP, JWT firmado, consentimiento servidor, proveedores externos ni integracion de webhooks. Por ser test-only, MySQL y transporte externo son NO_APLICA causal al diff, **no PASS**. Ningun secreto, mensaje real, webhook o dato productivo se utiliza.

## A4–A5

GitHub Actions en runner .NET 10 debe ejecutar tests Release filtrados por clase exacta, ademas de Scope Lock, V5, backend y seguridad requeridos por SHA original. CHECKS QUEUED/IN_PROGRESS/FAILED/SKIPPED no son PASS. Con CI externa pendiente se conserva VALIDANDO y owner DEV05; A5 requiere otra Task Javier distinta. DAG/COLA sin modificaciones manuales.

Rollback: revert normal tras HEAD CAS, sin force-push. No QA/main/PROD, activacion de proveedores ni cambios de RRULE.
