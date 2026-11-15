# SOLQARYN V17/V15 — C05-01.M03.03 fence de autorización efectiva de design tokens

**Microtarea:** C05-01.M03.03 (fila PLAN 736), autor DEV05 JAVIER `6adc2e55f47c81919f6374e561687076`. Riesgo R2: diseño por tenant, identidad, RBAC, revocación, sesión y tokens. Repositorio solqaryn/Solqaryn en dev, Scope Lock STRICT. Predecesora inmediata C05-01.M03.02 recibió A5 independiente DEV05 antes de claim V17 de esta nueva unidad (lock `SQV17_C05_01_M03_03`, ID `2000000736`).

## Implementación real y límites

`C0501DesignTokenCurrentAuthorizationFence` añade una puerta Application que reutiliza el `IdentidadUniversalAuthorizationGuard` vigente para revalidar, antes de consultar el catálogo, la membresía y el permiso **relacional** empresarial `ModuloSistema.Configuracion`/`AccionPermiso.Editar`. Nunca usa rol global legacy ni un tenant procedente del DTO. `IC0501VerifiedThemeStateReader` exige que un futuro adaptador backend obtenga tenant, propietario, catálogo ordinal, revisión, sesión y JWT vigente de fuentes confiables; no hay implementación de proveedor ni registro de DI en esta etapa.

Los datos confiables se convierten a snapshots de la Application M03.02 y reglas Domain M03.01 existentes, que validan sesión, versión, tipo CSS, clave allowlisted, idempotencia y cambios ilegales. Tenant B se rechaza antes del primer acceso; revocación de membresía/RBAC y carencia de fuente no consultan el catálogo; falso origin o JWT caducado deniega. El resultado nominal `AUTHENTICATED_THEME_ADAPTER_REQUIRED` **jamás modifica CSS, theme, tenant, base de datos o archivos**, ni concede JWT o sesión. Es preflight no autorizante; autorización HTTP y persistencia deberán validarse al momento de efecto por una microtarea posterior.

## A0–A5, pruebas dirigidas y rollback

- **A0:** solo nueva clase Application e interfaz de evidencia, 12 xUnit adversariales, este documento y CHANGELOG autocontenido. No se modifican la identidad persistida, EF, migraciones, controllers, Angular ni configuración CI.
- **A1:** pruebas xUnit positivas sin grant y negativas de identidad/intent nulos, tenant B, grant cliente, membresía/rol revocados, falta de RBAC explícito, catálogo ajeno y clave ordinal, JWT no actual, sesión/revisión obsoletas, CSS inseguro, ausencia/error de fuente y cancelación.
- **A2:** aislamiento A/B y denegación por defecto; solo el guard real reconsulta fuente de membresía/permiso relacional, pero la autenticidad JWT final dependerá del provider backend no implementado. Nunca usar un booleano de test como autorización HTTP real.
- **A3:** composición Domain–Application–guard real en memoria; **NO_APLICA MySQL Category=Integration, HTTP/Chromium/Playwright**, sin tocar EF, DB, rutas o UI. Esta excepción no certifica la plataforma completa ni un provider de JWT.
- **A4:** SHA publicado fast-forward/CAS, Scope Lock, procedencia V5, Backend Release/xUnit, seguridad multiempresa, Authorization y readback PLAN/COLA/DAG/BITACORA requieren SUCCESS exact-SHA o descendiente con blobs y pruebas equivalentes. QUEUED/IN_PROGRESS/FAILED/CANCELLED no PASS; conservar VALIDANDO.
- **A5:** independencia obligatoria: otra de las cinco Tasks Javier, distinta de DEV05, debe inspeccionar código, tests, evidencia y permisos.

Rollback `git revert` ordinario con revalidación de HEAD, sin force-push. No se crean permisos globales, tokens activos, rutas API, tablas, cambios de producción, QA/main/PROD, proveedores, tareas nuevas ni secretos.
