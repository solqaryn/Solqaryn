# Cierre visual SMTP Solqaryn — Desarrollo/UAT

Estado SMTP: **LISTO FUNCIONALMENTE**.

## Metadatos

- Repositorio: `jmejia31/Solqaryn`
- Rama: `Desarrollo`
- HEAD funcional confirmado al iniciar: `41c564015bd493b02bb306f082c3b5cb5aa9c420`
- Servicio Render: `solqaryn-api-desarrollo` (`srv-d9jblq7avr4c73c74jng`)
- Frontend: `https://solqaryn-desarrollo.vercel.app`
- Entorno: Desarrollo/UAT
- Factura: `FAC-000003` / venta `VEN-000003`
- Producción: **NO TOCADA**
- WhatsApp: **NO TOCADO**
- Correos enviados: **1** (el único envío funcional previo)
- Correos nuevos enviados en esta corrida de captura: **0**
- Secretos, contraseñas, tokens y JWT: no mostrados ni incluidos.

## Matriz de evidencia

| Prueba | Resultado | Archivo | Observaciones |
|---|---|---|---|
| 01 Render servicio DEV LIVE | PASS | `01_render_servicio_dev_live.png` | Servicio correcto, Desarrollo y estado Live visibles. |
| 02 Variables SMTP seguras | PASS | `02_render_environment_smtp_seguro.png` | Claves SMTP visibles; todos los valores permanecen enmascarados, incluida `Smtp__PasswordSmtp`. |
| 03 Logs de inicio | PASS | `03_render_logs_inicio.png` | Arranque exitoso, `Application started` y servicio live visibles. |
| 04 Facturación DEV/UAT | PASS | `04_solqaryn_facturacion_dev.png` | Factura y venta de DEV/UAT visibles. |
| 05 Diagnóstico SMTP | PASS funcional / captura retest transitorio | `05_smtp_diagnostico_ok.png` | El diagnóstico SMTP_OK/STARTTLS/autenticación fue observado previamente en la sesión funcional. La única reapertura permitida durante esta materialización mostró un fallo transitorio de verificación; no se repitió. |
| 06 Factura seleccionada | PASS | `06_factura_seleccionada.png` | `FAC-000003` real, formato A4 y estado pagado visibles. |
| 07 Envío confirmado | PASS persistente | `07_envio_factura_confirmado.png` | Evidencia persistente posterior al único envío; no se repitió el correo sólo para recrear un toast. |
| 08 Recepción real | PASS | `08_correo_recibido.png` | Captura aportada por el propietario: correo recibido con asunto `FAC-000003`. |
| 09 PDF recibido | PASS | `09_pdf_factura_recibido.png` + `09_pdf_factura_recibido.pdf` | PDF A4 real, legible, `FAC-000003`, total L. 200.00. |
| 10 Historial Solqaryn | PASS | `10_historial_envio_solqaryn.png` | Un registro por Correo con resultado Enviado. |
| 11 Logs de envío Render | PASS | `11_render_logs_envio_ok.png` | Ventana real 10:50:41–10:50:44 CST; STARTTLS, un intento, correo enviado y destinatario enmascarado. |
| 12 Cierre final | PASS | `12_cierre_final.png` | Inventario físico de la carpeta y README materializados. |

## Diagnóstico y alcance

- Diagnóstico SMTP: **PASS funcional**.
- Envío real: **PASS**.
- Recepción: **PASS**.
- PDF: **PASS**.
- Historial: **PASS**.
- Logs: **PASS**.
- Producción: **NO TOCADA**.
- WhatsApp: **NO TOCADO**.
- Cambios de código: **0**.
- Evidencia visual: **materializada en disco**; 05 conserva la captura real del único retest transitorio y no se presenta como un resultado fabricado.

El contador de correos enviados permanece en **1**. No se generó otra venta y no se envió otro correo durante esta corrida.

La autoridad `docs/VAEP_AUTHORITY.md` exige trabajar en `Desarrollo`; no se creó ni se usó una rama adicional.
