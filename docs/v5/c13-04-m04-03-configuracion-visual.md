# C13-04.M04.03 - configuracion visual permitida

Scope solqaryn/Solqaryn@dev, PROJECT_SCOPE_LOCK=STRICT, PLAN row 2002, Task DEV04. Riesgo R2 conservador por ruta visual privada. Alcance: configuracion cosmetica local marca institucional/contraste y densidad comoda/compacta con lista blanca tipada, sin CSS/HTML/URL externos, nuevas operaciones API, JWT, RBAC, tenant, EF, MySQL, persistencia o acceso a QA/main/PROD.

A0: nueva ruta privada con plataformaGuard existente, componente standalone Angular 22.2.1, HTML/CSS seguros, tests unitarios/adversariales, Chromium dirigido y workflow GitHub SHA-aislado. No implementa el inventario integral distinto M04.01 ni los estados de backlog M04.02.

A1: pruebas de ruta, positivo toggles, roles denegados, revocacion, HTML hostil, getter mutable TOCTOU, Proxy revocado y excepcion; test browser anonimo/403 sintetico, presets UI, Tab real, foco >=2px, targets >=44px, responsive 320. Asserts capaces de fallar.

A2: no nuevas concesiones, el guard existente controla la ruta y componente oculta presets con sesion no confirmada. El test usa token y respuestas simuladas, NO acredita JWT HTTP, MFA, RBAC real, tenants A/B backend, SQL ni pertenencia actual live.

A3: npm ci + lint + build Angular y Playwright Chromium real en GitHub-hosted; MySQL/EF, proveedor y HTTP backend real no se modifican y NO_APLICA a este diff, nunca certificados.

A4: exigir checks dirigidos y requeridos completed/success en SHA exacto, Scope Lock y procedencia V5, PLAN/BITACORA readback; QUEUED/IN_PROGRESS/CANCELLED/SKIPPED no PASS. A5: otra Task Javier autorizada revisa codigo/tests. Rollback git revert normal y bitacora aditiva.

## Reparación del gate de calidad ERP
El primer job de Priority4 rechazó la ruta con UNGUARDED por lista explícita obsoleta, aunque canActivate=[plataformaGuard] estaba presente. Se añade exclusivamente la ruta privada exacta a la lista cerrada de scripts/quality/priority4_quality_audit.py; el validador sigue exigiendo guard, JWT y sesión de plataforma verificada por backend. No se permiten prefijos, comodines, authGuard ERP ni eliminación de pruebas. Nuevo job ERP/Security/ScopeLock/V5 en SHA de reparación requerido SUCCESS antes de LISTO.
