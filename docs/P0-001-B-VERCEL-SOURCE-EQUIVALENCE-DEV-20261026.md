# P0-001.B — Regla DEV de equivalencia de fuente frontend para commits de gobierno (26-10-2026)

## Problema reproducido, evidencia nativa
- Repositorio `solqaryn/Solqaryn`, rama protegida `dev`, HEAD observado `3d2f0436b7c0c7bfc3eff07f8b01f3121dc3aa38`.
- Vercel DEV proyecto `solqaryn-dev` (`prj_1Anhx5mWyXEBX89lWC24Py6JXe7A`), deployment `dpl_Bi9YNxDrMXXBqMLPHCa6aCtyA1NV` del HEAD actual: `CANCELED`. Logs reales: `NON_RUNTIME_GOVERNANCE_ONLY_SKIP files=5 branch=dev`, exit 0 del Ignored Build Step. No es fallo de compilación.
- Último frontend previamente publicado: deployment `dpl_9wHGTwW1YNGucpmG58EqtYXtWsGs`, `READY`, SHA `35c67e330f876ee66f8015e889ffa151d6d4643a`, alias oficial `solqaryn-dev.vercel.app`.
- Comparación GitHub `35c67e33..3d2f0436`: un commit y exactamente cinco archivos de CI, documentación, baseline y script de validación; cero cambios en `frontend/`, `backend/`, `render.yaml` o configuración de aplicaciones. El manifiesto Git de todas las entradas de `frontend/` es idéntico en ambos SHA.
- Render DEV `solqaryn-api-dev`: deployment `dep-dbfqjt29lscs73bmvs10` en estado `live` para `3d2f0436`; confirmado mediante lectura nativa. Checks obligatorios GitHub del HEAD: `verify-solqaryn-project-scope`, `SOLQARYN - Seguridad multiempresa`, `SOLQARYN - Build y pruebas backend`, todos SUCCESS en SHA exacto.

## Decisión técnica delimitada: no generar builds artificiales
La política existente `frontend/scripts/vercel-ignore-build.mjs` **se conserva**: en DEV ignora únicamente cambios de control-plane. QA y PROD no se modifican.

Para la lógica **de ahorro/continuidad del release frontend DEV**, `scripts/dev-vercel-release.mjs` puede reconocer `DEV_FRONTEND_SOURCE_EQUIVALENCE=PASS`, sin solicitar otro build, sólo si:
1. GitHub demuestra que el SHA READY es antecesor lineal de HEAD DEV, sin divergencias, con 1–50 commits y diff completo no vacío (máximo 300 archivos). Todos los nombres actuales y anteriores deben ser documentación, CI o scripts fuera del frontend/backend bajo allowlist; cualquier archivo runtime, desconocido o comparación ambigua implica NO_GO.
2. GitHub devuelve manifiestos completos del directorio `frontend/` para ambos SHA con nombres, tipos y SHAs Git idénticos; la prueba falla cerrada ante cambio, duplicado, omisión o hashes inválidos.
3. Vercel confirma directamente un deployment `READY` en el proyecto DEV, rama `dev`, identidad GitHub correcta, target interno de ese proyecto y alias oficial `solqaryn-dev.vercel.app`. No se admite un deployment `CANCELED`, ERROR ni de otro proyecto como fuente de equivalencia.
4. Se comprueba el HEAD vigente de `dev`, los tres checks obligatorios y la autorización de esta intervención antes de operaciones con proveedores. No usar snapshots para fingir resultados actuales.

El marcador **`SOURCE_EQUIVALENT` no significa `READY` del SHA actual**, ni prueba la identidad de configuración/variables externas, servicios backend, CORS, E2E, SSO o matriz RBAC. El script imprime explícitamente `VERCEL_EXACT_SHA_READY=NO FUNCTIONAL_CERTIFICATION=REQUIRED`. Cambios en variables o configuración del proyecto Vercel requieren comprobación independiente, aunque el árbol fuente coincida.

## Límite comercial: el plan original todavía exige comparación exact-SHA
La fuente única `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` incluye para P0-001 el negativo obligatorio de bloquear cuando el SHA difiere de Render o Vercel. **Este PR no modifica ese plan ni inventa una nueva autorización comercial.** Hasta que su autoridad admita formalmente equivalencia condicionada, `P0-001.B` conserva `EN_PROGRESO` y el verificador histórico `scripts/verify-p0-001-b-parity.mjs` sigue siendo exact-SHA y fail-closed.

Aun con esa eventual autorización, no marcar `LISTO` sin las comprobaciones funcionales pendientes de CORS/preflight, E2E/SSO frontend y RBAC correspondientes, CI exacto final, revisión independiente, merge protegido y readback de `PLAN_MAESTRO/COLA/BITACORA`.

## Validación y rollback
- `node scripts/vercel-dev-runtime-equivalence.mjs --self-test`: 20 negativas + 1 positiva de equivalencia de fuente.
- `node scripts/dev-vercel-release.mjs --self-test`: se preservan política, check 3/3, límite Hobby y no-READY ante cancelación.
- El workflow `solqaryn-dev-vercel-consolidated.yml` ejecuta ambas pruebas en PR y antes de trabajo con tokens en `dev`.
- Rollback: revert protegido del PR; ningún cambio a `frontend/vercel.json`, `frontend/scripts/vercel-ignore-build.mjs`, QA, PROD, datos MySQL, secretos, dominios ni infraestructura.
