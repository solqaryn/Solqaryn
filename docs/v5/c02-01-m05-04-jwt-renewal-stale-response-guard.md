# C02-01.M05.04 — Respuesta de renovación JWT obsoleta (V30, 2026-11-23)

## Contrato real entregado
Al recibir el resultado HTTP de `POST /auth/renovar`, `AuthService` comprueba que tanto el token de sesión de Angular como el token persistido siguen siendo exactamente los que iniciaron la operación. El cierre de sesión, un nuevo login y el cambio de token por otra pestaña invalidan el resultado anterior. Se rechaza el resultado tardío sin volver a instalar JWT, usuario ni roles. También se deniega el POST si el estado inicial ya perdió consistencia. Esto corrige una carrera real de autenticación en el runtime Angular, no una simulación.

## Alcance, seguridad y rollback
- Riesgo R2: modifica únicamente el guard de renovación dentro de `frontend/src/app/core/auth/auth.service.ts`; la API, firmas JWT, esquema SQL, grants, DI, control de tenant, rutas y permisos permanecen sin cambios.
- El guard protege el instante de aceptación del refresh y funciona junto a la revalidación server-side `TenantContextService` + permisos de `SessionActivityService` existente. La UI no concede membresías.
- Regresiones dirigidas Vitest: éxito de token vigente, logout antes de respuesta, login de actor B frente al refresh de A, cambio de token en otra pestaña. Los fixtures no certifican MySQL/JWT firmado ni Chromium.
- Rollback permitido: `git revert` normal del commit únicamente en `dev`, con inspección de HEAD y protección de cambios concurrentes; sin force/reset.

## Estado institucional y pendientes
Esta es una reparación funcional de la microtarea existente `C02-01.M05.04` en `VALIDANDO`, propietario histórico DEV03. A0/diff y casos A1 escritos; su ejecución real, backend exact-SHA y A4 dependen de GitHub Actions. No declarar A0–A4 completos por este incremento. Sigue faltando preferencia de última empresa **duradera por actor en MySQL**, reemisión/verificación backend del JWT para cambio de tenant y navegación Angular/Chromium real hasta la pantalla autorizada, con denegación tenant A/B y revocación. A5 debe hacerla otra Task Javier elegible. Sin QA, main ni PROD.

Referencias aplicadas: gobernanza local verificada y SKREF-10 Angular Developer (origen `angular/angular`, pin `6808966993bf2b2d455d9b6245e960a59a01b9a9`, `skills/dev-skills/angular-developer/SKILL.md`, `references/http-client.md` y `references/testing-fundamentals.md`). SKREF-03 UI/Impeccable NO_APLICA (no hay cambio visual); SKREF-11 threat-model formal NO_APLICA (no se solicitó modelado formal); SKREF-12 performance NO_APLICA.
