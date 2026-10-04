# Detalles pendientes — SOLQARYN

Este archivo es la fuente canónica de pendientes deliberadamente aplazados o decisiones todavía no ejecutadas por el propietario. No deben bloquear trabajo no dependiente y no deben reinterpretarse como fallos de DEV, QA o PROD.

Última reconciliación: **2026-10-04**.

## Estado resumido

- DEV, QA y PROD están funcionalmente reconciliados y certificados sobre la infraestructura corporativa vigente.
- Render DEV/QA/PROD: contrato canónico **28/28 PASS**.
- Aiven: bases y usuarios aislados por entorno.
- Vercel -> API: binding fail-closed y smoke certificados por entorno.
- Los seis puntos siguientes son los únicos pendientes vigentes conocidos fuera del trabajo normal del roadmap.

---

## 1. Render paid / always-on

**Estado:** PENDIENTE / DEUDA TÉCNICA DE DISPONIBILIDAD / NO BLOQUEANTE POR DECISIÓN DEL PROPIETARIO

**Servicios afectados:**

- `solqaryn-api-dev`
- `solqaryn-api-qa`
- `solqaryn-api-prod`

**Estado actual:** los tres servicios continúan en Render `free`.

**Impacto aceptado:** el plan Free puede hacer spin-down por inactividad y producir cold starts. La plataforma permanece funcional y certificada bajo Free, pero no se declara `always-on` mientras los servicios sigan en ese plan.

**Opción evaluada:** migrar cada web service a `0.5c-512mb` (nombre legado: Starter), 512 MB RAM y compute always-on. Precio de referencia verificado el 2026-10-02: **USD 7/mes por servicio**, aproximadamente **USD 21/mes** por los tres servicios, antes de otros consumos aplicables.

**Decisión vigente:** mantener Free temporalmente mientras se evalúa el cambio de plan. Está prohibido introducir keep-alive artificial como sustituto de un plan always-on.

**Acción actual:** ninguna compra ni upgrade sin autorización explícita del propietario.

**Se retoma cuando:** el propietario autorice el gasto y ordene el upgrade.

**Al retomar, validar:** plan aplicado en los tres servicios, deploy sano, `RENDER_ENV_CONTRACT=PASS managed_keys=28`, `/health`, `/health/ready`, binding Vercel -> API, storefront y ausencia de regresiones de rendimiento/recursos.

---

## 2. SMTP real end-to-end

**Estado:** APLAZADO / NO BLOQUEANTE

**Entornos afectados:** DEV, QA y PROD.

**Contrato configurado:** Outlook.com, `smtp-mail.outlook.com:587`, STARTTLS, OAuth2/Modern Auth e identidad `solqaryn.platform@outlook.com`.

**Estado certificado actualmente:**

- las 28 variables canónicas están presentes en los tres entornos;
- no se usa contraseña SMTP básica ni client secret OAuth2;
- OAuth2 está configurado con refresh token independiente por entorno;
- DEV y PROD ya demostraron que la limitación pendiente está en el transporte SMTP saliente del runtime Free, no en el contrato de configuración;
- PROD obtuvo correctamente token OAuth2 durante la certificación previa;
- QA conserva el mismo contrato canónico, pero el envío end-to-end permanece deliberadamente sin certificar mientras se mantenga la restricción del plan Free.

**Bloqueo restante:** completar conexión SMTP + STARTTLS + autenticación OAuth2 + envío + recepción real controlada desde los runtimes desplegados.

**Decisión vigente:** mantener este punto aplazado mientras los servicios continúen en Render Free; no contratar un plan únicamente para cerrar la prueba SMTP.

**Acción actual:** ninguna.

**Se retoma cuando:** exista autorización de cambio de plan o se apruebe una arquitectura de correo distinta.

**Al retomar, validar:** `SMTP_OK`, autenticación OAuth2, envío y recepción reales, remitente, Reply-To, adjunto PDF, idempotencia y trazabilidad sin exposición de secretos.

---

## 3. Dominio personalizado `solqaryn.com` -> PROD

**Estado:** APLAZADO / NO BLOQUEANTE

**Evidencia vigente:** `solqaryn.com` está delegado correctamente a Cloudflare y la cuenta/DNS son corporativos. El cutover hacia PROD no está activo.

**Estado actual:** DEV, QA y PROD continúan operando con las URLs administradas de Vercel/Render; PROD está certificado mediante `https://solqaryn-prod.vercel.app`.

**Decisión vigente:** no ejecutar todavía el cutover DNS/frontend/backend.

**Acción actual:** ninguna.

**Se retoma cuando:** el propietario autorice explícitamente asignar el dominio a PROD.

**Al retomar, validar:** hostnames finales, DNS, TLS/certificados, Vercel PROD, backend PROD, CORS/orígenes permitidos, redirects, smoke end-to-end y rollback del corte.

---

## 4. Automatizaciones VAEP

**Estado:** PAUSADAS POR DECISIÓN DEL PROPIETARIO / NO ES FALLA DE INFRAESTRUCTURA

**Estado actual:** las **10 automatizaciones canónicas permanecen pausadas (0/10 habilitadas)**.

**Distribución canónica:**

- cinco Primary bajo responsabilidad operativa de Javier Mejía: `:00/:12/:24/:36/:48`;
- cinco Supervisor bajo responsabilidad operativa de Alex Morales: `:05/:17/:29/:41/:53`.

**Decisión vigente:** no reactivar, crear, eliminar ni sustituir automatizaciones sin autorización explícita de los responsables aplicables.

**Acción actual:** ninguna.

**Se retoma cuando:** el propietario autorice expresamente reanudar el runtime VAEP.

**Al retomar, validar:** IDs canónicos, ownership, slots, estado pausado -> habilitado, autoridad `docs/VAEP_AUTHORITY.md`, readback y ausencia de duplicados/automatizaciones legacy.

---

## 5. Plan Maestro / ERP

**Estado:** ROADMAP ACTIVO / TRABAJO FUTURO NORMAL DE PRODUCTO

**Naturaleza:** este punto no es deuda de DEV/QA/PROD ni una falla de infraestructura. El único Plan Maestro vigente continúa definiendo el trabajo funcional, técnico y arquitectónico futuro de SOLQARYN.

**Autoridad:** Google Doc `PLAN MAESTRO SOLQARYN`, ID `1YdQlNJ312HuziyKb9E-GEt55dcgSmuFGgfsHxyzPUaw`.

**Decisión vigente:** continuar el roadmap únicamente desde el Plan Maestro actual, el estado vivo y las dependencias técnicas reales. Históricos, planes retirados, fases viejas o receipts previos no gobiernan trabajo nuevo.

**Acción actual:** ninguna fuera del trabajo que el propietario autorice/active desde el Plan Maestro.

**Se retoma/continúa cuando:** se reactive la ejecución funcional/VAEP o el propietario solicite el siguiente alcance del roadmap.

---

## Dictamen de pendientes

Con la reconciliación del 2026-10-02, **no existe otro pendiente deliberadamente aplazado conocido fuera de estos cinco puntos** en las fuentes canónicas actuales de SOLQARYN.

Esto no significa que el producto esté terminado: el punto 5 engloba el trabajo futuro normal del Plan Maestro/ERP. Los puntos 1-4 son decisiones operativas o de infraestructura diferidas explícitamente.

## Regla de uso

Agregar aquí únicamente pendientes deliberadamente pospuestos, decisiones diferidas o trabajo futuro expresamente reconocido por el propietario. Cada punto debe indicar estado, motivo, condición de reanudación y si bloquea o no trabajo independiente.

## 6. Evaluación futura de html5-qrcode

**Estado:** DEUDA TÉCNICA NO BLOQUEANTE / SIN REEMPLAZO ACTUAL

**Versión vigente:** `html5-qrcode@2.3.8`, última publicación estable disponible al ejecutar Fase 5.

**Decisión vigente:** mantener la librería sin cambios porque Fase 5 certifica compatibilidad funcional completa y no existe una actualización publicada que aplicar.

**Motivo de seguimiento:** la ausencia prolongada de nuevas releases justifica evaluar mantenimiento upstream, compatibilidad futura de navegador/cámara, postura de seguridad, alternativas activamente mantenidas y costo/riesgo de migración.

**Acción actual:** ninguna sustitución ni rediseño del escáner.

**Se retoma cuando:** exista una incompatibilidad real, una vulnerabilidad relevante, un cambio de APIs web/cámara o una alternativa claramente superior que justifique migración controlada.

**Al retomar, validar:** contratos de escaneo, permisos de cámara, dispositivos móviles, códigos QR/barra soportados, accesibilidad, rendimiento, privacidad, bundle y Playwright E2E.
