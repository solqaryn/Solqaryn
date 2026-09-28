# Detalles pendientes — SOLQARYN

Este archivo concentra decisiones que el propietario ha decidido aplazar deliberadamente. No deben bloquear trabajo no dependiente y no deben reinterpretarse como fallos de DEV o PROD.

## 1. Dominio personalizado / corte DNS

**Estado:** APLAZADO / NO BLOQUEANTE

**Evidencia vigente:** `solqaryn.com` ya está delegado correctamente a Cloudflare y Cloudflare fue certificado previamente. No existe cutover activo del dominio hacia frontend/backend PROD.

**Decisión vigente:** no migrar, activar ni cortar el dominio personalizado en este momento. DEV y PROD continúan usando las URLs administradas actuales de Vercel y Render.

**Acción actual:** ninguna.

**Se retoma cuando:** el propietario decida asignar el dominio a PROD y autorice explícitamente el cutover DNS/frontend/backend.

**Al retomar, validar:** hostnames finales, DNS, TLS/certificados, Vercel PROD, backend PROD, CORS/orígenes permitidos, redirects, smoke end-to-end y rollback del corte.

---

## 2. Certificación SMTP real de DEV

**Estado:** APLAZADO / NO BLOQUEANTE

**Servicio afectado:** `solqaryn-api-dev`

**Contrato configurado:** Outlook.com, `smtp-mail.outlook.com:587`, STARTTLS obligatorio, OAuth2/Modern Auth, identidad `solqaryn.platform@outlook.com`.

**Evidencia actual:**

- `/health/ready` responde HTTP 200 y confirma `database=connected`.
- El diagnóstico real `POST /facturas/correo/probar` llegó al servicio SMTP de SOLQARYN.
- El intento terminó con `SMTP_TIMEOUT` después de aproximadamente 60 segundos al intentar conectar con `smtp-mail.outlook.com:587`.
- El servicio Render DEV permanece en plan Free.
- En la configuración actual, la restricción de salida SMTP del plan Free impide completar la certificación real por el puerto 587.

**Decisión vigente:** mantener Render DEV en plan Free y no contratar un plan de pago únicamente para cerrar esta prueba.

**Acción actual:** ninguna.

**Se retoma cuando:** exista una decisión posterior para habilitar conectividad SMTP real (por cambio de plan autorizado o por una arquitectura de correo distinta aprobada).

**Al retomar, validar:** `SMTP_OK`, autenticación OAuth2, envío real controlado, recepción, remitente, Reply-To y PDF adjunto.

---

## 3. Certificación SMTP real de PROD

**Estado:** APLAZADO / NO BLOQUEANTE — OAuth2 certificado; transporte SMTP no completado

**Contrato configurado:** Outlook.com, `smtp-mail.outlook.com:587`, STARTTLS obligatorio, OAuth2/Modern Auth, identidad `solqaryn.platform@outlook.com`.

**Evidencia runtime PROD del 2026-09-27:**

- Render PROD obtuvo correctamente un access token OAuth2 desde su configuración: **PASS**.
- El diagnóstico real de transporte desde el runtime intentó conectar a `smtp-mail.outlook.com:587` con STARTTLS obligatorio.
- Resultado del transporte: `SMTP_TIMEOUT` después de ~60 segundos; no alcanzó autenticación SMTP.
- El servicio `solqaryn-api-prod` continúa en plan Free.
- El probe temporal de certificación quedó desactivado después de capturar la evidencia.

**Bloqueo restante:** no está en la obtención del token OAuth2. Falta conectividad SMTP saliente suficiente para completar conexión + STARTTLS + autenticación y posteriormente un envío/recepción real.

**Decisión vigente:** mantener este punto aplazado y no bloqueante; no cambiar de plan únicamente para esta prueba. El servicio `solqaryn-api-prod` fue revalidado en plan Free el 2026-09-27.

**Acción actual:** ninguna.

**Al retomar, validar:** `SMTP_OK`, autenticación SMTP OAuth2, envío real controlado, recepción en Outlook, remitente, Reply-To, adjunto PDF y trazabilidad sin exponer secretos.

---

## 4. Retiro final de infraestructura legacy personal / rollback histórico

**Estado:** CERRADO — POSTCHECK PASS

**Cierre confirmado (2026-09-28):**

- El propietario retiró los recursos personales legacy de SOLQARYN/VariStoreHN en Aiven, Render, Vercel y Cloudinary.
- Los endpoints Render legacy `solqaryn-api-desarrollo.onrender.com` y `solqaryn-api.onrender.com` responden HTTP 404.
- El alias Vercel legacy `varistorehn.vercel.app` responde HTTP 404.
- Vercel corporativo conserva únicamente `solqaryn-dev` y `solqaryn-prod`; Render corporativo conserva únicamente `solqaryn-api-dev` y `solqaryn-api-prod`.
- DEV y PROD responden readiness con base conectada.
- PROD mantiene `totalCount=73` y las imágenes observadas usan `res.cloudinary.com/riyrzmob/.../solqaryn_prod/...`.
- Los logs corporativos posteriores al retiro no muestran referencias activas a `defaultdb`, `varistorehn_desarrollo`, `vyijnqzq` ni a los hosts Render legacy inspeccionados.
- El repositorio personal legacy `jmejia31/VariStorehn` ya no aparece entre los repositorios instalados/accesibles.
- GitHub Actions run `36298199171` ahora devuelve `artifacts: []`: eliminado el artifact `10924897018` (`solqaryn-prod-empty-rollback-36298199171`).
- GitHub Actions run `36228394479` ahora devuelve `artifacts: []`: eliminado el artifact `10901905430` (`solqaryn-legacy-prod-defaultdb-backup-36228394479`).

**Resultado:** no quedan artifacts de rollback/histórico asociados a estos dos runs. Los workflow runs se conservan únicamente como trazabilidad de ejecución exitosa.

**Acción actual:** ninguna.

---

## Regla de uso

Agregar aquí únicamente pendientes deliberadamente pospuestos por decisión del propietario. Cada pendiente debe indicar su estado, motivo, condición de reanudación y no debe bloquear trabajo independiente.
