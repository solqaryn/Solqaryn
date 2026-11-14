# Evidencia N8.6 — WhatsApp user initiated handoff

Estado funcional: **PASS** para el modo `USER_INITIATED_HANDOFF` sin API de proveedor.

- Política reusable: `frontend/src/app/core/services/whatsapp-share.service.ts` y `backend/src/Application/Services/WhatsAppSharePolicy.cs`.
- Factura, catálogo público y checkout comparten normalización y construcción de `wa.me`.
- Estados auditables: `WHATSAPP_HANDOFF_GENERATED` y `WHATSAPP_CLIENT_OPEN_REQUESTED`.
- No se registran entrega, lectura ni envío automático; el usuario decide pulsar **Enviar**.
- RBAC y aislamiento de factura se mantienen en el endpoint existente.
- Validación: `WhatsAppSharePolicyTests` 4/4 tests, controlador/servicio backend dirigido 24/24 y `SOLQARYN-fase6` 7/7.
- `npm run lint` y `npm run build:prod` superados.

La sesión CUA no estuvo disponible para obtener capturas físicas nuevas en esta corrida. No se fabrican PNG; la validación visual queda explícitamente pendiente y no altera los resultados funcionales anteriores.

La API automática Meta/Twilio queda documentada como `WHATSAPP_AUTOMATED_PROVIDER_API = OPTIONAL_FUTURE_CAPABILITY` y no bloquea el ERP Core.
