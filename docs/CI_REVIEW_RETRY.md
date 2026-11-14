# SOLQARYN - Reintento automático de Scope Lock tras aprobación

## Problema reproducido

Cuando un PR de gobierno abre sus checks antes de obtener revisión independiente, Scope Lock puede fallar con `GOVERNANCE_REQUIRES_INDEPENDENT_FORMAL_REVIEW`. Una aprobación posterior sobre el mismo SHA no reejecuta automáticamente la ejecución histórica; repetirla manualmente ha generado bloqueos recurrentes en PR #3815 y #3816.

## Mecanismo seguro

El workflow `.github/workflows/retry-scope-on-approval.yml` escucha únicamente revisiones `APPROVED` enviadas para PR hacia `dev`. Corre código de la rama predeterminada `dev`, nunca código del PR revisado; con permiso mínimo `actions:write` únicamente reejecuta el job exacto de Scope Lock mediante la API GitHub. No agrega aprobaciones, commits ni ejecuta merges.

El script `scripts/security/retry-scope-lock-after-review.mjs` verifica actor y revisor independiente de la lista permitida, repo y base DEV originales, PR abierto, revisión vigente del HEAD exacto, no-autorrevisión ni colaboración en commits, aprobación obligatoria de Javier para autor institucional, workflow y job exactos, fallo concreto de gobernanza y número acotado de intentos. Ante ausencia o contradicción de evidencia, no reejecuta.

El job reintentado vuelve a aplicar todas las reglas preexistentes; si otro control falla, no certifica. La fusión de PR sigue exigiendo checks requeridos, revisión independiente y branch protection. El workflow no sustituye ninguna Task programada.

## Bootstrap de instalación sin ejecutar código de PR

Este workflow puede recibir una revisión en su propio PR de instalación antes de que el script exista en `dev`. En ese escenario, el checkout confiable `dev` comprueba la existencia del script y registra `SCOPE_REVIEW_RETRY_RUNTIME=BOOTSTRAP_DEFERRED_UNTIL_MERGE`. Las etapas de prueba y reintento no se ejecutan. El resultado del job no acredita aún operación en vivo del reintento; el Scope Lock original y la revisión exact-HEAD siguen siendo bloqueantes y, para este PR de instalación, requieren el último re-run normal del job original. Solo después del merge, cuando el script esté integrado en `dev`, se ejecuta el runtime protegido y puede emitir `SCOPE_REVIEW_RETRY=STARTED`. No se descarga ni ejecuta código desde la rama del PR con `actions:write`.
## Pruebas y despliegue

- `SCOPE_REVIEW_RETRY_SELF_TEST=PASS` incluye 16 casos positivos y negativos: SHA desactualizado, revisión no vigente, actor ajeno, auto-revisión, branch distinta, fork, fallo no relacionado y exceso de reintentos.
- Auto-rerun de una aprobación futura solo tras merge protegido de este cambio; su propia aprobación de bootstrap podría necesitar una última repetición manual.
- No se cambian secretos, entornos, migraciones, ERP, QA/main/PROD, Aiven, Render ni Vercel.
- Reversión mediante PR protegido que retire el workflow y script y restaure la referencia de Scope Lock, con CI y readback.
