# Estándar canónico de compartición por WhatsApp

Solqaryn soporta actualmente compartición WhatsApp iniciada por el usuario mediante el cliente oficial de WhatsApp/WhatsApp Web. Este modo no requiere API de proveedor ni costo por mensaje y no declara delivery/read receipts. La integración automática mediante proveedor/API se considera capacidad opcional futura y no bloquea el ERP Core.

## Flujo vigente

1. El backend autoriza el documento dentro del tenant activo y genera únicamente datos server-authoritative y, cuando existe, un enlace temporal seguro al PDF.
2. El frontend usa `WhatsAppShareService` (`frontend/src/app/core/services/whatsapp-share.service.ts`) para normalizar el teléfono, codificar Unicode y construir `https://wa.me/<numero>?text=<mensaje>`.
3. El usuario pulsa **Abrir WhatsApp y continuar** y decide si pulsa Enviar en la aplicación oficial o WhatsApp Web.

El enlace `wa.me` no adjunta binarios. El mensaje puede incluir el enlace público temporal autorizado al PDF; nunca incluye JWT, secretos, rutas internas ni datos de otro tenant.

## Teléfonos y estados

El fallback documentado para Honduras acepta formatos locales e internacionales (por ejemplo, `+504 XXXX-XXXX`) y produce el número E.164 sin signos. La política está encapsulada y permite otro prefijo por tenant en el futuro. Teléfonos ausentes, inválidos o con prefijo duplicado no abren WhatsApp.

La auditoría usa destinatario enmascarado y uno de estos estados:

- `WHATSAPP_HANDOFF_GENERATED`
- `WHATSAPP_CLIENT_OPEN_REQUESTED`

No se registran `WHATSAPP_SENT`, `DELIVERED`, `READ` ni `SUCCESSFULLY_DELIVERED`: abrir `wa.me` no demuestra entrega ni lectura.

## Seguridad y permisos

El endpoint de preparación conserva `Facturacion:Compartir`, valida la factura con el servicio tenant-aware y no confía en totales o números manipulados en el navegador. El historial conserva canal, actor, documento, timestamp, correlación de la petición y resultado técnico sin guardar el texto completo del mensaje.

## Cobertura

Factura, catálogo público y checkout consumen la misma política reusable. No se agregan botones artificiales a módulos sin flujo de compartir existente. La API automática de Meta/Twilio queda como `WHATSAPP_AUTOMATED_PROVIDER_API = OPTIONAL_FUTURE_CAPABILITY` y no es requisito ni dependencia del ERP Core.

## Pruebas y límites

Las pruebas cubren normalización, invalidación, prefijo duplicado y enmascarado (`backend/tests/Solqaryn.Tests/WhatsAppSharePolicyTests.cs`), además de los contratos existentes de autorización del controlador. La validación E2E abre únicamente el destino externo en Desarrollo; no envía mensajes reales sin autorización explícita.
