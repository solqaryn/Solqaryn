# Validación productiva — SOLQARYN

PROD no es laboratorio. Esta lista se ejecuta sólo después de certificar el mismo cambio en DEV.

## Infraestructura

- [ ] `main` contiene únicamente cambios certificados.
- [ ] Render `solqaryn-api-prod` LIVE.
- [ ] `/health` responde correctamente.
- [ ] `/health/ready` confirma MySQL.
- [ ] `solqaryn_prod` usa usuario productivo dedicado.
- [ ] Vercel `solqaryn-prod` existe y está conectado a `main`.
- [ ] CORS apunta al frontend PROD real.
- [ ] Cloudinary usa prefijo `solqaryn_prod`.
- [ ] DNS/Cloudflare apunta únicamente al frontend PROD certificado cuando se active.

## Correo

- [ ] OAuth2 Outlook configurado.
- [ ] refresh token PROD almacenado sólo en Render.
- [ ] `SMTP_OK`.
- [ ] envío real controlado recibido.
- [ ] remitente y Reply-To correctos.
- [ ] factura/PDF correcto.

## Aplicación

- [ ] login y RBAC.
- [ ] dashboard empresa.
- [ ] catálogos/productos.
- [ ] inventario.
- [ ] compras/ventas/facturación.
- [ ] Cloudinary real.
- [ ] auditoría y aislamiento tenant.

## Migración histórica

Sólo después de completar lo anterior:

- [ ] verificar de nuevo el respaldo histórico de VariStoreHN;
- [ ] ejecutar ensayo de migración;
- [ ] migrar al tenant VariStoreHN en la nueva base `solqaryn_prod`;
- [ ] reconciliar conteos, totales y relaciones;
- [ ] ejecutar smoke post-migración;
- [ ] conservar evidencia y rollback de datos.

Ninguna infraestructura legacy se reactiva para este proceso.
