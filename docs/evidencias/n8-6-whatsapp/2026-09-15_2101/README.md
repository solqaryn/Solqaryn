# Addendum final N8.6 — UX veraz del handoff WhatsApp

## Anomalía residual

Después del cierre correctivo anterior se detectó que el aviso de error de auditoría decía que WhatsApp se había abierto. Esa callback no conoce el resultado de `window.open`, por lo que podía aparecer junto al aviso de popup bloqueado.

## Root cause y cambio exacto

`FacturaViewComponent` ahora delega el flujo en `ejecutarFacturaWhatsAppHandoff`. La función obtiene un único booleano `aperturaAceptada`, abre el destino sin esperar la auditoría y hace que el fallback dependa exclusivamente de ese booleano. El aviso de auditoría es neutral: **No fue posible registrar la auditoría del intento de apertura de WhatsApp.**

El payload sigue llevando el teléfono normalizado solo de forma transitoria; el backend enmascara exactamente una vez antes de persistir. No se agregaron logs de body ni estados de envío, entrega, lectura u apertura confirmada.

## Casos cubiertos

- Popup aceptado: auditoría recibe payload normalizado y estado `WHATSAPP_CLIENT_OPEN_REQUESTED`.
- Popup bloqueado: se conserva el enlace `wa.me` como fallback y no se afirma apertura.
- Auditoría fallida con popup bloqueado: wording neutral, sin “abierto”, “enviado”, “entregado” o “leído”.
- Auditoría fallida con popup aceptado: el handoff permanece aceptado y el aviso sigue siendo neutral.

## Validación y alcance

- Backend WhatsApp: 26/26.
- Política frontend: 5/5.
- Contrato Factura→Service: 4/4.
- Checkout WhatsApp E2E: 7/7.
- Lint, build productivo y `git diff --check`: PASS.
- P0: 0. P1: 0.
- Mensajes reales: 0. Meta/Twilio: 0. Automatizaciones modificadas: 0.
- `N8.7.A`: no interferida; lease y scope ajenos intactos.
- CUA visual: `NOT_AVAILABLE`; no se fabricaron screenshots.

HEAD inicial: `7d221e36539db2cebdede8719a3e7a84a749822b`  
HEAD de la corrección: `7d736f4b`  
Rama: `Desarrollo`
