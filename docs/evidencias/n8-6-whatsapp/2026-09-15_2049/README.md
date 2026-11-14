# Erratum N8.6 — contrato de auditoría WhatsApp

## Defecto post-cierre detectado

La factura abría correctamente `wa.me`, pero el caller enviaba un destinatario ya enmascarado al endpoint de auditoría. El backend intentaba enmascararlo de nuevo, rechazaba los asteriscos y la auditoría fallaba silenciosamente.

## Corrección

- El frontend envía al API únicamente el número normalizado durante la petición autenticada.
- `WhatsAppSharePolicy` es la única fuente de enmascarado antes de persistir el historial.
- El handoff no se rompe si el registro de auditoría falla; se muestra un aviso estructurado sin afirmar envío ni entrega.
- No se escriben números crudos en logs, excepciones, telemetry, receipts ni esta evidencia.

## Cobertura añadida

- Test frontend del payload cross-layer: normalizado hacia API, nunca pre-enmascarado.
- Tests backend de persistencia enmascarada y rechazo de input pre-enmascarado.
- Regresión WhatsApp, checkout E2E, lint y build productivo.

## Estado

- HEAD inicial: `d8258ff318e0b2a7466a6ff65dfd7342636adf4e`
- Corrección: commit publicado en `Desarrollo` (receipt asociado).
- P0: 0. P1: 0.
- Mensajes reales: 0. Meta/Twilio: 0. Automatizaciones modificadas: 0.
- `N8.7.A`: no interferida; su lease y scope permanecen bajo su owner.
- CUA visual: `NOT_AVAILABLE`; no se fabricaron capturas.
