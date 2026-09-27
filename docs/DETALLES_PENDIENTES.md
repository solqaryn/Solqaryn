# Detalles pendientes — SOLQARYN

Este archivo concentra decisiones que el propietario ha decidido aplazar deliberadamente. No deben bloquear trabajo no dependiente y no deben reinterpretarse como fallos de DEV.

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

## 4. Certificación externa Cloudinary PROD

**Estado:** PENDIENTE

**Evidencia actual:** el código y el contrato de aislamiento exigen prefijo `solqaryn_prod`, pero el Environment `PROD` de GitHub no contiene credenciales Cloudinary bajo los nombres estándar auditados. El runtime de Render no expone lectura de valores de secretos mediante el conector disponible.

**Bloqueo:** no existe conector Cloudinary autenticado en este entorno para certificar control plane/ownership ni inventariar o retirar recursos legacy sin riesgo.

**Se retoma cuando:** se conecte Cloudinary corporativo o se aporten credenciales PROD mediante un canal autorizado.

**Al retomar, validar:** cuenta/product environment corporativo, prefijo `solqaryn_prod`, aislamiento respecto de DEV, inventario de activos, URLs legacy y retirada segura de duplicados.

---

## 5. Certificación SMTP real de PROD

**Estado:** PENDIENTE

**Contrato configurado:** Outlook.com, OAuth2/Modern Auth, identidad `solqaryn.platform@outlook.com`.

**Evidencia actual:** Outlook Email está instalado en ChatGPT, pero el Environment `PROD` de GitHub no contiene refresh token SMTP bajo los nombres estándar auditados. Render PROD está operativo, pero su conector no permite leer los valores secretos existentes y no se debe sobrescribir a ciegas.

**Bloqueo:** falta una prueba de envío/recepción real desde el runtime PROD. En DEV ya existe además una limitación conocida de salida SMTP en el plan Free de Render.

**Se retoma cuando:** exista conectividad SMTP saliente válida desde PROD o se adopte una arquitectura de correo distinta aprobada.

**Al retomar, validar:** autenticación OAuth2, envío real controlado, recepción en Outlook, remitente, Reply-To, adjunto PDF y trazabilidad sin exponer secretos.

---

## 6. Retiro final de recursos legacy personales

**Estado:** PENDIENTE

**Evidencia actual:** el repositorio personal privado `jmejia31/VariStorehn` todavía existe fuera de la organización `solqaryn`. También permanecen ramas temporales de auditoría/certificación en el repositorio corporativo.

**Bloqueo:** el conector GitHub disponible no expone eliminación de repositorios ni borrado de refs/ramas. No se ejecutará una sustitución destructiva sin una operación explícita soportada.

**Se retoma cuando:** exista herramienta con capacidad de borrar repositorios/refs o el propietario realice la retirada manual después de confirmar el backup requerido.

**Al retomar, validar:** ausencia de repositorios personales legacy que dupliquen SOLQARYN, eliminación de ramas temporales ya fusionadas y conservación únicamente de `dev`/`main` más las ramas operativas realmente necesarias.

---

## 7. Migración final de datos históricos a PROD

**Estado:** PENDIENTE DE EJECUCIÓN PRODUCTIVA

**Evidencia ya cerrada:** el respaldo histórico verificado de VariStoreHN fue descargado, validado por SHA-256, descifrado y restaurado en un MySQL 8.4 aislado. El ensayo completo aplicó las migraciones actuales de SOLQARYN y terminó en `PASS` con 137 tablas, 107 migraciones EF, 1 empresa, 73 productos y 6 usuarios; no hubo escrituras en PROD.

**Bloqueo actual:** el Environment `PROD` de GitHub está restringido a ejecución desde `main`. El intento desde `dev` fue rechazado antes de iniciar el job productivo. La promoción de un workflow que realiza la escritura final sobre PROD fue bloqueada por los controles de seguridad del entorno de ejecución de ChatGPT; no se forzará ni se eludirán esos controles.

**Estado del PROD actual:** `solqaryn_prod` permanece con el esquema canónico (137 tablas / 107 migraciones) y sin los datos históricos de VariStoreHN. Render PROD está desplegado y operativo sobre `main`.

**Se retoma cuando:** el workflow de migración final sea autorizado/ejecutado desde `main` por un canal permitido o por el propietario desde GitHub Actions.

**Al retomar:** crear primero respaldo cifrado del PROD vacío, restaurar el respaldo histórico cifrado, aplicar EF hasta 107 migraciones y verificar exactamente 1 empresa / 73 productos / 6 usuarios antes de cerrar.

---

## Regla de uso

Agregar aquí únicamente pendientes deliberadamente pospuestos por decisión del propietario. Cada pendiente debe indicar su estado, motivo, condición de reanudación y no debe bloquear trabajo independiente.
