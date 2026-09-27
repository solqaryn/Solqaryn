# Detalles pendientes — SOLQARYN

Este archivo concentra decisiones que el propietario ha decidido aplazar deliberadamente. No deben bloquear trabajo no dependiente y no deben reinterpretarse como fallos de DEV o PROD.

## 1. Dominio personalizado / corte DNS

**Estado:** PENDIENTE

**Decisión vigente:** no migrar, activar ni cortar el dominio personalizado en este momento.

DEV puede continuar usando las URLs administradas actuales de Vercel y Render. La activación/cambio de dominio se retomará más adelante, cuando el entorno productivo correspondiente esté preparado y exista autorización explícita para el corte de DNS/dominio.

**Acción actual:** ninguna.

---

## 2. Certificación SMTP real de DEV

**Estado:** PENDIENTE

**Servicio afectado:** `solqaryn-api-dev`

**Contrato configurado:** Outlook.com, `smtp-mail.outlook.com:587`, STARTTLS obligatorio, OAuth2/Modern Auth, identidad `solqaryn.platform@outlook.com`.

**Evidencia actual:**

- `/health/ready` responde HTTP 200 y confirma `database=connected`.
- El diagnóstico real `POST /facturas/correo/probar` llegó al servicio SMTP de SOLQARYN.
- El intento terminó con `SMTP_TIMEOUT` después de aproximadamente 60 segundos al intentar conectar con `smtp-mail.outlook.com:587`.
- El servicio Render DEV permanece en plan Free.
- En la configuración actual, la restricción de salida SMTP del plan Free impide completar la certificación real por el puerto 587.

**Decisión vigente:** mantener Render DEV en plan Free y no contratar un plan de pago únicamente para cerrar esta prueba.

**Se retoma cuando:** exista una decisión posterior para habilitar conectividad SMTP real (por cambio de plan autorizado o por una arquitectura de correo distinta aprobada).

**Al retomar, validar:** `SMTP_OK`, autenticación OAuth2, envío real controlado, recepción, remitente, Reply-To y PDF adjunto.

---

## 3. Vercel PROD corporativo

**Estado:** PENDIENTE

**Evidencia actual:** el team corporativo Vercel conectado contiene únicamente el proyecto `solqaryn-dev`; no existe todavía un proyecto `solqaryn-prod`.

**Bloqueo:** el conector Vercel disponible permite inspección/despliegues de proyectos existentes, pero no expone creación/eliminación de proyectos. No se puede cerrar este punto automáticamente desde este entorno.

**Se retoma cuando:** el propietario cree `solqaryn-prod` dentro del mismo team corporativo de SOLQARYN o habilite una herramienta que permita crear proyectos.

**Al retomar, validar:** ownership corporativo, rama `main`, variables PROD, routing al backend PROD, deploy estable y ausencia de proyectos legacy duplicados.

---

## 4. Certificación SMTP real de PROD

**Estado:** PENDIENTE — OAuth2 certificado; transporte SMTP no completado

**Contrato configurado:** Outlook.com, `smtp-mail.outlook.com:587`, STARTTLS obligatorio, OAuth2/Modern Auth, identidad `solqaryn.platform@outlook.com`.

**Evidencia runtime PROD del 2026-09-27:**

- Render PROD obtuvo correctamente un access token OAuth2 desde su configuración: **PASS**.
- El diagnóstico real de transporte desde el runtime intentó conectar a `smtp-mail.outlook.com:587` con STARTTLS obligatorio.
- Resultado del transporte: `SMTP_TIMEOUT` después de ~60 segundos; no alcanzó autenticación SMTP.
- El servicio `solqaryn-api-prod` continúa en plan Free.
- El probe temporal de certificación quedó desactivado después de capturar la evidencia.

**Bloqueo restante:** no está en la obtención del token OAuth2. Falta conectividad SMTP saliente suficiente para completar conexión + STARTTLS + autenticación y posteriormente un envío/recepción real.

**Decisión vigente:** mantener este punto como pendiente no bloqueante y no cambiar de plan únicamente para esta prueba.

**Al retomar, validar:** `SMTP_OK`, autenticación SMTP OAuth2, envío real controlado, recepción en Outlook, remitente, Reply-To, adjunto PDF y trazabilidad sin exponer secretos.

---

## 5. Retiro final de recursos legacy personales

**Estado:** PENDIENTE

**Evidencia actual:** el repositorio personal privado `jmejia31/VariStorehn` todavía existe fuera de la organización `solqaryn`. También permanecen ramas temporales de auditoría/certificación en el repositorio corporativo.

**Bloqueo:** el conector GitHub disponible no expone eliminación de repositorios ni borrado de refs/ramas. No se ejecutará una sustitución destructiva sin una operación explícita soportada.

**Se retoma cuando:** exista herramienta con capacidad de borrar repositorios/refs o el propietario realice la retirada manual después de confirmar el backup requerido.

**Al retomar, validar:** ausencia de repositorios personales legacy que dupliquen SOLQARYN, eliminación de ramas temporales ya fusionadas y conservación únicamente de `dev`/`main` más las ramas operativas realmente necesarias.

---

## Regla de uso

Agregar aquí únicamente pendientes deliberadamente pospuestos por decisión del propietario. Cada pendiente debe indicar su estado, motivo, condición de reanudación y no debe bloquear trabajo independiente.
