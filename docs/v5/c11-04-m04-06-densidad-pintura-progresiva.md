# SOLQARYN V17 — C11-04.M04.06: densidad y pintado progresivo en observabilidad privada

**Autor:** PRIMARY DEV 05, TASK_ID 6adc2e55f47c81919f6374e561687076. **Riesgo:** R1 Angular UI. **Entorno:** solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT.

## A0: propósito y alcance

Esta unidad optimiza solamente la representación de las cuatro tarjetas descriptivas existentes de observabilidad. El componente Angular standalone mantiene ruta privada, autorización de plataforma comprobada por backend y estado de sesión/reactividad originales. El CSS aplica `content-visibility:auto` y `contain-intrinsic-size:auto 10rem` a cada tarjeta para permitir al navegador omitir pintura fuera de pantalla, con padding fluido `clamp(.8rem,1.6vw,1rem)`. No se agrega endpoint, telemetría viva, exporter, cache, listas virtuales, API, DTO, JWT/RBAC, EF/MySQL, dependencia npm ni proveedor. La estructura DOM y el contenido accesible permanecen completos; no se promete mejora medida de Core Web Vitals.

## A1 y A3: pruebas causales

Se agregan dos casos al Playwright real del workflow `v17-c11-04-m04-01-observabilidad.yml`, que ejecuta Chromium, lint y build Angular en GitHub-hosted. Caso positivo/frontera: sesión sintética autorizada, cuatro tarjetas DOM, CSS computado con `content-visibility:auto`, contención intrínseca y padding acotado; scroll real del último item y ausencia de solicitudes a exportadores. Caso negativo/error: 320/768/1280 px conservan rejilla 1/2 columnas sin overflow; token retirado/redirección a login oculta las tarjetas. El archivo existente también contiene 403/503, revalidación, doble GET, teclado Tab real, contraste y objetivos táctiles. Ninguna prueba con sesión sintética certifica autorización HTTP real.

## A2: fronteras

No se modifica la autoridad del backend; los estados no autorizados siguen ocultando toda guía privada. La mejora de pintura es sólo presentación: ningún resultado concede acceso, escribe datos, registra métricas ni habilita migraciones. MySQL, EF, servicio externo y autenticación firmada real: **NO_APLICA** porque no se tocan esas superficies.

## A4, A5 y rollback

Exigir en SHA publicado los checks requeridos `completed/success`: Scope Lock, procedencia V5, Angular tipado/build, seguridad pertinente, y workflow dirigido que ejecute estas dos pruebas Chromium. Ante CI QUEUED/IN_PROGRESS, mantener VALIDANDO con SHA, run y owner; no afirmar PASS. Leer PLAN/COLA/DAG/BITACORA antes de LISTO. A5 únicamente por otra Task Javier con código, diff y pruebas comprobados. Rollback por `git revert` normal en dev y readback; nunca force-push, QA/main/PROD ni secretos. Métricas de performance visual: **NO_MEDIDO** sin perfil de rendimiento causal.
