# C01-03.M04.03 — marca dinámica de empresa verificada (DEV)

## A0 / riesgo / modificación
PLAN D238 manda branding, configuración dinámica y componentes permitidos. La ruta privada existente /configuracion/dinamica-v5 añade un nombre visual de tenant desde GET autenticado /empresa-configuracion/tenant/:id. Lee SOLO empresaIdVerificada de TenantContextService, compara ID de respuesta, sanea texto con c0201NombreBrandingSeguro, muestra nombre sólo tras éxito y limpia/cancela peticiones tardías al cambiar tenant. No usa cabecera, URL o localStorage para atribuir marca ni convierte esta UI en autoridad JWT/RBAC. No integra CSS ni scripts arbitrarios. Angular 22.2.1, riesgo R2, mismo contrato real, sin modificación de API, EF, MySQL de negocio, secrets ni producción. No se certifica meta global M04.

## A1–A3
Nueva suite Playwright real para anónimo, usuarios JWT firmados de A/B, identidad visual positiva y exclusión B en A, denegación 403/404 de GET ajeno, JWT manipulado 401, capturas DESPUÉS en 320/390/768/1280/1440, teclado Tab y ausencia de body overflow. Workflow GitHub-hosted con API .NET10, MySQL 8.4 efímero, Angular y Chromium; sin HTTP mocks. El contraste WCAG numérico, zoom nativo 200%, screenshot ANTES y carrera A→B de API artificialmente retardada aún NO están acreditados: LIMITACION, no PASS. Si el test de marca real falla, resolver causa, no inventar éxito.

## A4/A5
Requiere CI dirigida real exact SHA, Scope Lock V5, backend/frontend/security y revisión de capturas SUCCESS antes de LISTO. A5 distinta de autor/reparadores con revisión genuina. Mantener H=VALIDANDO mientras corre CI. Rollback git revert normal FF/CAS. Sin QA/main/PROD.

## SKREF V23
Governance local coincide canónica dev; SKREF-03 Impeccable pbakaus/impeccable@2149fcce39a90bb409df5f16515f316a76dc6199 y SKREF-10 Angular angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9 SKILL.md originales verificados (Operate, señales y effect cleanup). 04/05/06/11/12 NO_APLICA, sin movimiento, landing, copy marketing, threat modeling formal o medición .NET. No se instalaron skills.
