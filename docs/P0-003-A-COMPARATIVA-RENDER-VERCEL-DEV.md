# P0-003.A — Comparativa causal Render y Vercel DEV

**Proyecto:** SOLQARYN; **fuente comercial:** Plan Comercial Inicial Unificado `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES`, fila P0-003.A (L5/L9/L15). **Dependencia:** P0-012.B=LISTO. **Ejecutor:** PRIMARY_DEV_02 / JAVIER_CHATGPT, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`, cuenta GitHub `jmejia31`. **Corte de consulta:** 2026-10-25T19:20:06.609Z. **HEAD de base:** `bafcde53ce2562f70d5b4115184df51e5c48483d`. Los valores son snapshot, nunca garantía para otro SHA ni prueba de producción.

## Comparación basada en proveedores conectados

| Dimensión | Render API DEV | Vercel frontend DEV |
| --- | --- | --- |
| Identidad | `solqaryn-api-dev`, service `srv-daqvla49v7es738up9vg` | `solqaryn-dev`, project `prj_1Anhx5mWyXEBX89lWC24Py6JXe7A` |
| Rama | `dev` | `dev` |
| Deploy observado | `dep-dbf5age7bikc73c8467g` | Alias `solqaryn-dev.vercel.app` apunta a `dpl_8UsFYE5z3qUYdpsj8wj4kYXaMRJv` |
| Estado | `live` (terminó `2026-10-25T19:16:59.908841Z`) | `READY` en el deployment del alias |
| Commit publicado | `bafcde53ce2562f70d5b4115184df51e5c48483d` | `29dbcb06fe8034c01e508b08f13bfc171e08f4c9` |
| Coincide con HEAD de corte | Sí | **No** |

Vercel registra asimismo `dpl_Gsbx2wDBrbMKcBXtKV6HYLrhs8SX`, `CANCELED`, asociado al HEAD `bafcde53ce2562f70d5b4115184df51e5c48483d`. No cuenta como `READY`. La política consolidada vigente `docs/DEV_VERCEL_CONSOLIDATED_RELEASE.md` y `frontend/vercel.json` establece `git.deploymentEnabled.dev=false` para evitar despliegue automático por cada push. Un intento cancelado **puede ser el resultado deliberado de dicha política**, y no evidencia por sí solo falla de la aplicación. El workflow protegido consolidado tiene su propia cadencia, cuota y guardas; esta Task no lo dispara.

**Dictamen de paridad exact-SHA al corte: NO_ACREDITADA.** Render sirve `bafcde53ce2562f70d5b4115184df51e5c48483d` pero el alias Vercel `READY` acredita SHA anterior. La paridad de P0-001.B y certificación P0-003.B no se cierran por este informe.

## Contratos de upstream, PUBLIC_ORIGIN y CORS

- Fuente `frontend/server/environment-binding.js`: Vercel project ID DEV selecciona `environment=DEV`, `apiUpstream=https://solqaryn-api-dev-fxx8.onrender.com`, `publicOrigin=https://solqaryn-dev.vercel.app`, `seoIndexingEnabled=false`. Cualquier override de entorno incompatible falla cerrado.
- `frontend/vercel.json`: `/api/:path*` se resuelve en el proxy server-side `/api/backend-proxy?path=:path*`, no en el hostname de frontend como selector de ambiente.
- `render.yaml`: `Cors__AllowedOrigins__0=https://solqaryn-dev.vercel.app`; `AppSettings__BackendPublicUrl=https://solqaryn-api-dev-fxx8.onrender.com`; entorno `Development`, rama `dev`.
- `backend/src/API/Program.cs` rechaza `Cors:AllowedOrigins` vacío y registra lista explícita de orígenes. **Verificación estática**, sin afirmar CORS efectivo en memoria o pruebas HTTP preflight.

## GET públicos de salud (sin credenciales)

- `https://solqaryn-api-dev-fxx8.onrender.com/health`: el lector web devolvió el cuerpo `{"status":"ok","service":"Solqaryn API"}`.
- `https://solqaryn-api-dev-fxx8.onrender.com/health/ready`: el lector devolvió el cuerpo `{"status":"ready","database":"connected"}`.

El conector de lectura **no devolvió código HTTP numérico ni SHA embebido**: no se inventa `HTTP=200` ni se confunde salud backend con prueba de identidad de frontend, CORS efectivo, login, RBAC o tenancy.

Una lectura anónima de `https://solqaryn-dev.vercel.app` devolvió `login_required`; no equivale a humo autenticado ni a error demostrado. La API autenticada de Vercel permitió obtener el estado real del alias y deployment IDs.

## Negativos y regresiones de criterio

1. **SHA incompatible**: deployment `READY` en `29dbcb0...` frente a `dev@bafcde53ce2562f70d5b4115184df51e5c48483d` ⇒ rechazar `PARIDAD=PASS`.
2. **SHA correcto pero `CANCELED`**: deployment `dpl_Gsbx...` ⇒ rechazar `READY`.
3. **API saludable**: `Render live` y cuerpos de liveness/readiness sanos ⇒ no inferir un frontend `READY` en el mismo SHA.
4. **Configuración declarativa**: coincidencia de upstream/origen/CORS en fuentes ⇒ no afirmar valores efectivos del proveedor ni `OPTIONS` autenticado.
5. **Frontend protegido**: `login_required` de lector anónimo ⇒ no afirmar indisponibilidad funcional ni validación de UI.

**Salida de P0-003.A:** acta comparativa verificable con brecha externa acotada; no publicación ni paridad certificada. Las microtareas que exijan despliegue exacto permanecen sujetas a un nuevo Vercel DEV `READY` del HEAD protegido, readback de alias y smokes de su propio scope.

## Impacto, pruebas y rollback

Cambios del PR: **solo** esta acta y `CHANGELOG_AI.md`. No modifica frontend/backend de producto, API, DTO, datos, migraciones, auth, RBAC, tenants, despliegues, secretos, DNS, QA/main/PROD. Consumidores de la comparación: P0-003.B y el gate de paridad externo P0-001.B. Observabilidad: metadatos frescos de proveedores, dos consultas GET y evidencia de configuración vigente. **Rollback:** revert protegido del PR documental; conservar los despliegues tal cual.

`P0-003.A=LISTO` solo después de CI requerido 3/3 del SHA final, revisión GitHub de otra identidad, merge protegido dev, readback `PLAN_MAESTRO` + `COLA` por fórmula + `BITACORA`. La aprobación del acta nunca sustituye la paridad futura.

## Referencias trazables

- GitHub HEAD: https://github.com/solqaryn/Solqaryn/commit/bafcde53ce2562f70d5b4115184df51e5c48483d
- Scope Lock SUCCESS del SHA: https://github.com/solqaryn/Solqaryn/actions/runs/40224051213
- Render dashboard DEV: https://dashboard.render.com/web/srv-daqvla49v7es738up9vg ; deployment `dep-dbf5age7bikc73c8467g`
- Vercel READY antiguo: https://vercel.com/solqaryn/solqaryn-dev/8UsFYE5z3qUYdpsj8wj4kYXaMRJv
- Vercel CANCELED HEAD: https://vercel.com/solqaryn/solqaryn-dev/Gsbx2wDBrbMKcBXtKV6HYLrhs8SX
- Fuente de política: `docs/DEV_VERCEL_CONSOLIDATED_RELEASE.md` en `bafcde53ce2562f70d5b4115184df51e5c48483d`.
