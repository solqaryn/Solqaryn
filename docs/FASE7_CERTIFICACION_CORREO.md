# CORREO — contrato vigente y certificación

Este archivo sustituye las instrucciones históricas de SMTP básico de la antigua Fase 7.

## Contrato vigente

SOLQARYN usa:

- Outlook.com;
- `smtp-mail.outlook.com:587`;
- STARTTLS obligatorio;
- OAuth2/Modern Auth;
- identidad `solqaryn.platform@outlook.com`;
- refresh token independiente por entorno;
- sin contraseña SMTP;
- sin client secret OAuth2.

El inventario exacto de configuración desplegable vive en `docs/RENDER_ENVIRONMENT_CONTRACT.md`.

## Certificación

DEV se certifica antes de promover a PROD:

1. build y despliegue exitosos;
2. `/health/ready` HTTP 200;
3. diagnóstico real `POST /facturas/correo/probar` con resultado `SMTP_OK`;
4. envío real controlado de una factura;
5. recepción, remitente, Reply-To, contenido y PDF verificados;
6. ausencia de secretos legacy en el servicio.

PROD no se usa como entorno de experimentación.
