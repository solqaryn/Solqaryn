# C01-03.M04.03 — marca dinámica de empresa verificada (DEV)

## A0 / riesgo / modificación
PLAN D238 manda branding, configuración dinámica y componentes permitidos. La ruta privada existente /configuracion/dinamica-v5 añade un nombre visual de tenant desde GET autenticado /empresa-configuracion/tenant/:id. Lee SOLO empresaIdVerificada de TenantContextService, compara ID de respuesta, sanea texto con c0201NombreBrandingSeguro, muestra nombre sólo tras éxito y limpia/cancela peticiones tardías al cambiar tenant. No usa cabecera, URL o localStorage para atribuir marca ni convierte esta UI en autoridad JWT/RBAC. No integra CSS ni scripts arbitrarios. Angular 22.2.1, riesgo R2, mismo contrato real, sin modificación de API, EF, MySQL de negocio, secrets ni producción. No se certifica meta global M04.

## A1–A3
Nueva suite Playwright real para anónimo, usuarios JWT firmados de A/B, identidad visual positiva y exclusión B en A, denegación 403/404 de GET ajeno, JWT manipulado 401, capturas DESPUÉS en 320/390/768/1280/1440, teclado Tab y ausencia de body overflow. Workflow GitHub-hosted con API .NET10, MySQL 8.4 efímero, Angular y Chromium; sin HTTP mocks. El contraste WCAG numérico, zoom nativo 200%, screenshot ANTES y carrera A→B de API artificialmente retardada aún NO están acreditados: LIMITACION, no PASS. Si el test de marca real falla, resolver causa, no inventar éxito.

## A4/A5
Requiere CI dirigida real exact SHA, Scope Lock V5, backend/frontend/security y revisión de capturas SUCCESS antes de LISTO. A5 distinta de autor/reparadores con revisión genuina. Mantener H=VALIDANDO mientras corre CI. Rollback git revert normal FF/CAS. Sin QA/main/PROD.

## SKREF V23
Governance local coincide canónica dev; SKREF-03 Impeccable pbakaus/impeccable@2149fcce39a90bb409df5f16515f316a76dc6199 y SKREF-10 Angular angular/angular@6808966993bf2b2d455d9b6245e960a59a01b9a9 SKILL.md originales verificados (Operate, señales y effect cleanup). 04/05/06/11/12 NO_APLICA, sin movimiento, landing, copy marketing, threat modeling formal o medición .NET. No se instalaron skills.


## V24 P2 adversarial logout: A HTTP tardío frente a empresa B (DEV04)

Se añadió una prueba Playwright de regresión a la suite existente. Utiliza las dos identidades reales de CI, JWT firmados, verificaciones GET /tenant-context/1 y /tenant-context/2 autorizadas y GET de configuración A realmente servido por API .NET 10 + MySQL 8.4 efímero. Playwright obtiene la respuesta A auténtica y retrasa únicamente su entrega; durante la espera, cierra la sesión A y autentica B con JWT legítimo en el mismo navegador. Una respuesta A entregada tarde —o cancelada por Angular al salir— jamás debe reemplazar la marca B. Exige nombre B visible y ausencia de A, con captura de estado B. No se falsifica autorización, no se fuerza un HTTP 200 sintético ni se modifica backend, datos de negocio o código de producción.

**Alcance preciso:** prueba adversarial de cambio entre dos sesiones autenticadas y tenants diferentes, NO verificación de selección A→B dentro de la misma identidad multiempresa. Captura "DESPUÉS" es evidencia del cambio de identidad, NO captura histórica de la interfaz ANTES del changeset. El zoom nativo 200% y la captura histórica ANTES continúan NO_ACREDITADO. Este changeset es reparación de pruebas A5_FAIL, no concesión de A5_PASS ni nuevo cierre; el FAIL histórico se conserva. Revisión A5 correctiva independiente por Task Javier distinta de DEV04 y de los demás reparadores, solo después de CI GitHub-hosted exact SHA SUCCESS y evidencia faltante. Rollback normal mediante nuevo git revert FF/CAS en dev. No QA/main/PROD.

La bitácora de cambio completa previa de CHANGELOG_AI.md (historial de gran tamaño) se preserva sin reescritura: el añadido causal queda documentado aquí y en BITACORA institucional, sin riesgo de truncar el CHANGELOG histórico por el conector.
