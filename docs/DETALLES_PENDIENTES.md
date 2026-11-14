# Detalles pendientes — SOLQARYN

Este archivo es la fuente canónica de pendientes deliberadamente aplazados o decisiones todavía no ejecutadas por el propietario. No deben bloquear trabajo no dependiente y no deben reinterpretarse como fallos de DEV, QA o PROD.

Última reconciliación: **2026-10-22**.

## Estado resumido

- DEV, QA y PROD tienen infraestructura corporativa configurada; la **promoción del HEAD modernizado DEV → QA → PROD sigue aplazada**. La certificación base no equivale a certificación exact-head de la promoción.
- Render DEV/QA/PROD: contrato canónico **28/28 PASS**.
- Aiven: bases y usuarios aislados por entorno.
- Vercel -> API: binding fail-closed y smoke certificados por entorno.
- Los seis puntos numerados de este archivo representan únicamente trabajo deliberadamente diferido o futuro. No son autorización para comprar, reactivar VAEP ni tocar el dominio.
- **Cloudinary DEV LISTO (2026-10-12):** 11/11 imágenes normalizadas, auditoría read-only con 0 referencias no canónicas/incorrectas, worker desactivado y aceptación manual del propietario de descargas individual/ZIP realizadas después de la operación. Evidencia en `docs/CLOUDINARY_AISLAMIENTO.md`.

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

## 4. Sistema de automatización retirado

**Estado:** RETIRADO / SIN TASKS AUTORIZADAS / NO BLOQUEANTE PARA EL DESARROLLO MANUAL.

El propietario eliminó las diez Tasks de ChatGPT y la hoja operativa antigua. GitHub retiró el scheduler y el despacho comercial. **No existe una cohorte de cinco o diez Tasks pendiente de reactivación.** Los IDs, horarios, leases y epochs anteriores no tienen validez operativa.

**Decisión vigente:** no buscar, recrear, recuperar ni habilitar las antiguas Tasks. Cualquier sistema futuro deberá diseñarse como instancia independiente, con autorización expresa y basarse únicamente en referencias históricas verificadas, nunca en las políticas ejecutivas de un plan anterior.

**Estado documental:** `docs/AUTOMATION_STATUS.md`. La seguridad, las pruebas funcionales y el respaldo del ERP continúan independientes.

**Acción actual:** no existe activación que ejecutar. El desarrollo manual solicitado por el propietario puede continuar en `dev` con seguridad, revisión y pruebas proporcionales.

**Reapertura:** exclusivamente mediante una orden nueva para diseñar e implementar automatizaciones nuevas.
---

## 5. Plan Comercial Inicial / Maestro futuro

**Estado:** PLAN COMERCIAL INICIAL ACTIVO COMO FUENTE; MAESTRO FUTURO DIFERIDO

**Naturaleza:** este punto no es deuda de DEV/QA/PROD ni una falla de infraestructura. El Plan Comercial Inicial aprobado define el alcance ejecutable previo al Maestro futuro.

**Autoridad comercial actual:** Drive ID `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; visión futura pospuesta y fuera de la cola.

**Decisión vigente:** continuar la ejecución únicamente desde el Plan Comercial Inicial actual, el estado vivo y las dependencias técnicas reales. Históricos, planes retirados, fases viejas o receipts previos no gobiernan trabajo nuevo.

**Acción actual:** ninguna fuera del trabajo que el propietario autorice/active desde el Plan Comercial Inicial.

**Se retoma/continúa cuando:** el propietario solicite implementar trabajo concreto del Plan Comercial Inicial o autorice un motor nuevo.

---

## Imágenes históricas Cloudinary DEV — LISTO (no pendiente)

El inventario previo encontró 11 imágenes de producto no canónicas. La normalización de 11/11 con validación de hashes y 0 originales eliminados fue ejecutada en Render DEV; MySQL confirmó `remaining=0` mediante auditoría independiente. El normalizador está `false` y Render DEV LIVE. El propietario confirmó la descarga individual y ZIP autenticados después de la normalización: **aceptación manual PASS**. Ver evidencias en `docs/CLOUDINARY_AISLAMIENTO.md`. No afecta QA/PROD.

## Promoción modernizada DEV → QA → PROD — APLAZADA por instrucción del propietario

La promoción permanece sin ejecutar: PR #3583 (`dev → qa`) y PR #3574 (`qa → main`) siguen en borrador. El propietario solicitó dejar esta actividad para después. No fusionar, desplegar ni certificar QA/PROD como parte del cierre Cloudinary; retomar con revisión y gates propios cuando vuelva a autorizarse.

## Dictamen de pendientes

Los seis puntos numerados de este archivo y la promoción modernizada diferida conservan su tratamiento actualizado. La coordinación antigua fue retirada, no pausada. **La normalización y prueba individual/ZIP de las 11 imágenes Cloudinary DEV ya no son pendientes.**

Esto no significa que el producto esté terminado: el punto 5 engloba el trabajo futuro normal del Plan Maestro/ERP. Los puntos 1-4 son decisiones operativas o de infraestructura diferidas explícitamente.

## Regla de uso

Agregar aquí únicamente pendientes deliberadamente pospuestos, decisiones diferidas o trabajo futuro expresamente reconocido por el propietario. Cada punto debe indicar estado, motivo, condición de reanudación y si bloquea o no trabajo independiente.

## 6. Evaluación futura de html5-qrcode

**Estado:** EVALUACIÓN TÉCNICA ACTUAL COMPLETADA 2026-10-12 / SEGUIMIENTO CONDICIONAL NO BLOQUEANTE

**Versión vigente:** `html5-qrcode@2.3.8`, última publicación estable disponible al ejecutar Fase 5.

**Decisión vigente:** mantener la librería sin cambios porque Fase 5 certifica compatibilidad funcional completa y no existe una actualización publicada que aplicar.

**Motivo de seguimiento:** la ausencia prolongada de nuevas releases justifica evaluar mantenimiento upstream, compatibilidad futura de navegador/cámara, postura de seguridad, alternativas activamente mantenidas y costo/riesgo de migración.

**Evidencia de revisión 2026-10-12:** npm continúa publicando `html5-qrcode@2.3.8` como etiqueta `latest` (https://www.npmjs.com/package/html5-qrcode). La ficha Snyk indica ausencia de vulnerabilidades **directas conocidas** en 2.3.8, pero mantenimiento inactivo (https://security.snyk.io/package/npm/html5-qrcode). La auditoría npm de todo el frontend continúa sometida a los gates permanentes de GitHub, incluidas dependencias transitivas; ausencia de aviso directo no equivale a garantía de seguridad futura.

**Decisión de revisión:** mantener `2.3.8` por compatibilidad y falta de actualización estable. La evaluación solicitada queda realizada; sólo se reabre bajo los disparadores especificados a continuación. No se ejecuta reemplazo sin pruebas de cámara, QR/barra, accesibilidad y móviles.

**Acción actual:** ninguna sustitución ni rediseño del escáner.

**Se retoma cuando:** exista una incompatibilidad real, una vulnerabilidad relevante, un cambio de APIs web/cámara o una alternativa claramente superior que justifique migración controlada.

**Al retomar, validar:** contratos de escaneo, permisos de cámara, dispositivos móviles, códigos QR/barra soportados, accesibilidad, rendimiento, privacidad, bundle y Playwright E2E.
