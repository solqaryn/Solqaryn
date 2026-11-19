# C06-04.M03.05 — Portal de cliente: frontera segura de despacho (incremento R2)

Autor: SOLQARYN PRIMARY DEV 01 JAVIER. Fuente V5 FINAL; dev exclusivo; PROJECT_SCOPE_LOCK=STRICT.

## A0 y alcance

Se incorpora una frontera Application `C0604PortalWorkDispatchBoundary` con puerto `IC0604PortalWorkQueue` y pruebas xUnit. Consume el gate existente `C0604PortalClienteAuthorizationGate`, cuya propiedad `GrantsWrite` es siempre falsa. No se registra un puerto en DI, no se crea API, worker alojado, cron, outbox MySQL, proveedor externo, email, pedido, reserva, pago o despliegue. El código no expone PII ni credenciales. La cola real queda deliberadamente **sin activación**, porque no existe una prueba de autorización runtime firmada y deduplicación persistente para este scope. Esta entrega es un checkpoint auténtico de código seguro, **no M03.05 terminado**.

## A1–A3

Se agregan xUnit con denegaciones sobre operaciones no permitidas, claves idempotentes inválidas/hostiles, UTC ausente, versión inválida, roles autoatribuidos, solicitud de escritura o entrega externa, error de evidencia y cancelación; se verifica que no llega ningún enqueue al puerto. El camino nominal también deniega en ausencia de permisos vigentes, incluso si la información estructural es aparentemente favorable. Las pruebas de backend y seguridad deben ejecutarse en GitHub Actions del SHA causal. Estas pruebas son in-process, NO_APLICA para MySQL/HTTP/Chromium de este diff, pero el cierre del scope R2 completo **sí exige** integración real: JWT CUSTOMER, tenant A/B, RBAC, ownership, revocación, CAS durable, idempotencia atómica, retry/dead-letter y worker con proveedor auditado. No simular un PASS.

## A4, A5 y reversión

Verificar Scope Lock, V5, .NET Release/xUnit y seguridad en el SHA exacto con logs, además de readback de PLAN/COLA/BITACORA. Mantener VALIDANDO mientras la integración productiva autorizada y los criterios P1 permanezcan sin acreditar. A5 por otra Task Javier distinta. Revertir con commit Git normal conservando trabajos concurrentes e historial, nunca force-push ni borrar registros.
