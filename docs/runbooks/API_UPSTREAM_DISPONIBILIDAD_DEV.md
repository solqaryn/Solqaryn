# SOLQARYN — API upstream intermitente: diagnóstico y recuperación DEV

Fecha del diagnóstico: 2026-10-12 (hora local CST) / 2026-10-13 UTC.
Alcance de esta corrección: **únicamente rama `dev`**. QA, PROD, DNS `solqaryn.com`, secretos, planes y recursos Aiven no se modifican.

## Evidencia real (no inferencia de disponibilidad permanente)

1. Los tres servicios corporativos `solqaryn-api-dev`, `solqaryn-api-qa` y `solqaryn-api-prod` usan el plan **Render Free**. El plan suspende instancias por inactividad; un `READY` en Vercel sólo verifica su frontend, no la disponibilidad continua de Render.
2. El proxy `frontend/api/backend-proxy.js` usaba timeout de socket de **30000 ms** y traducía errores de conexión a `502 API upstream no disponible.`, sin código estructurado ni causa segura de observabilidad. No se atribuyen todos los fallos a un único proveedor sin traza correlacionada.
3. Render QA: `GET /health/ready` terminó en **503** luego de **24488 ms** (2026-10-12T21:16:30Z); en ese arranque el GET público `/tienda/bootstrap` tardó **13707 ms**, finalmente 200.
4. Render PROD: `GET /health/ready` **200** luego de **28626 ms** (2026-10-12T22:50:37Z); el `/tienda/bootstrap` tardó **6106 ms** en ese período.
5. En DEV, Vercel registró intentos `POST /api/auth/login` a las 01:31 y 01:33 UTC. Render registró una petición `POST /auth/login` iniciada a las 01:33:38Z y terminada **200** en **7928 ms**. No se infiere de estos registros que las credenciales fallaran; el mensaje del proxy puede originarse antes de que ASP.NET reciba la petición.
6. El enrutamiento Vercel se resuelve por `VERCEL_PROJECT_ID` (DEV, QA y PROD no tienen fallback entre sí). Se conserva el binding canónico y la política Render `/health` de liveness, sin sustituirla por `/health/ready`.

## Mitigación incorporada en candidato DEV

- `ApiAvailabilityService`: sonda **GET /api/health/ready** sólo al pulsar ingresar; usa `HttpBackend` para evitar JWT/tenant, no incorpora usuario ni contraseña, y no es una tarea periódica ni keep-alive.
- Reintenta **únicamente readiness** ante códigos 0, 502, 503 o 504. Máximo tres GET con espera progresiva (2.5 y 5 segundos, más la duración efectiva de cada request). Cualquier 401/403 u otro fallo no transitorio falla cerrado inmediatamente.
- `AuthService.login()` envía exactamente **un POST** después de un readiness exitoso. Ningún retry automático de login, renovación de sesión, mutation, checkout o envío de documentos.
- La UI muestra que está recuperando la conexión y ofrece un error comprensible al agotarse el intento. No asegura que Render Free pase a ser always-on.
- El proxy conserva el timeout de 30 segundos, preserva los headers y estados de respuestas backend y clasifica **errores propios de socket** como `503` con `Retry-After: 3`, `code: API_UPSTREAM_UNAVAILABLE`, `Cache-Control: private, no-store` y attestation de ambiente. Sólo registra ambiente, método y código de error acotado; jamás body, JWT, query, contraseña o tokens.
- Las pruebas incluyen condiciones de timeout, desconexión y binding desconocido, `401` backend intacto, máximo de GET, ausencia de credenciales en readiness y **cero POST duplicados**.

## Gate para certificar el cambio

1. PR a `dev`: verificar scope lock, diff, `npm run lint` (incluye `scripts/validate-api-availability.mjs`), build Angular, unitarios Angular/Vitest, validación project-bound y los Dictámenes Fase 6/7 requeridos.
2. Fusionar **únicamente** a `dev` tras checks obligatorios; actualizar HEAD real. Un merge de código **no equivale a deploy**.
3. Confirmar que el despliegue Vercel del proyecto `solqaryn-dev` esté `READY` y corresponda al SHA nuevo, **no al build anterior**. Si Vercel reporta límite de despliegues, el runtime seguirá viejo hasta que el proveedor habilite el deploy; no marcar `LISTO` antes.
4. Smoke de `/api/health` y `/api/health/ready`, login real y selección de empresa; ante cold start, mostrar estado de espera, recuperar GET y realizar como máximo un POST login. No utilizar credenciales reales en CI ni publicarlas.
5. Correlacionar logs Vercel (código `API_UPSTREAM_UNAVAILABLE`) con eventos Render; confirmar `P0/P1=0` relativo al cambio. Revertir PR si existe regresión de JWT, RBAC, routing, cookies, streaming, caché o logout.

## Límite operativo y decisiones posteriores

**La causa de fondo de disponibilidad no desaparece mientras Render permanezca Free.** Esta corrección reduce errores de interacción de login y mejora observabilidad, pero no garantiza SLA ni elimina interrupciones para otras rutas. Para un servicio PROD comercial always-on se requiere decisión expresa del propietario sobre plan pago; no autorizar upgrade mediante este PR.

QA → PROD y el dominio `solqaryn.com` permanecen aplazados. Nunca copiar variables DEV hacia esos entornos ni reorientar `API_UPSTREAM` como fallback. No usar pings, crons o automatizaciones para mantener artificialmente viva la instancia Free.
