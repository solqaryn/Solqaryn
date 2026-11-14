# P0-009.A — Inventario causal CI/checks y Vercel DEV

**Microtarea:** P0-009.A, Plan Comercial Inicial Unificado (Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA; fuentes L1/L14), PLAN_MAESTRO fila 18, dependencia P0-012.B=LISTO.
**Task:** PRIMARY_DEV_03 / JAVIER_CHATGPT / 6adc2e55f47c81919f6374e561687076 / GitHub jmejia31.
**Entorno:** únicamente dev, PROJECT_SCOPE_LOCK=STRICT / AUTOMATION_RUNTIME=ACTIVE_DEV / VAEP=FALSE / Jules=0.
**Corte causal:** 2026-10-25T20:47:18.880Z; dev SHA 72327d132ee69955c5bb13a03423b229a08bd432. Es una fotografía, NO un estado perpetuo.

## CI: hechos verificables para SHA exacto

- [Scope Lock run 40230716010](https://github.com/solqaryn/Solqaryn/actions/runs/40230716010): SUCCESS para 72327d13, incluido job verify-solqaryn-project-scope. En el log constan 9 pruebas negativas de frescura de Skill, 31 mutaciones negativas de autoridad y salvaguardas 3x3 PASS.
- [CI ERP run 40230716009](https://github.com/solqaryn/Solqaryn/actions/runs/40230716009): SUCCESS para ese mismo SHA. Jobs SOLQARYN - Seguridad multiempresa, SOLQARYN - Build y pruebas backend, y SOLQARYN - Frontend tipado y build: los tres SUCCESS. Logs reales: seguridad 943/943 pruebas PASS (0 fallos, 0 omitidas); backend 2902/2902 PASS (0 fallos, 0 omitidas); build backend 0 errores. Frontend produjo warning de stylesheet storefront-producto.component.scss: 17.59 kB sobre budget 16 kB (exceso 1.59 kB). Los workflows avisan sobre deprecación de runtime Node 20 de actions.
- Versiones de las fuentes: Angular/Material 22.2.1, Zone.js 0.16.3, API .NET 10, EF Core 10.0.12, Oracle MySql.EntityFrameworkCore 10.0.9. Source: frontend/package.json y csproj API/Infrastructure de ese SHA.
- [Ruleset dev 22829243](https://github.com/solqaryn/Solqaryn/rules/22829243): active, PR requerido, sin bypass actors, con tres checks de contexto requeridos: verify-solqaryn-project-scope; SOLQARYN - Seguridad multiempresa; SOLQARYN - Build y pruebas backend. El ruleset reporta required_approving_review_count=0; esto NO exime el contrato operativo de obtener APPROVED independiente real de otro login sobre commits finales.
- Workflows observados: .github/workflows/project-scope-lock.yml y .github/workflows/solqaryn-core-ci.yml. Otros workflows históricos o nombres de fases no reemplazan estos controles.

## Estado Vercel DEV, diferenciado de QA/PROD

**Proyecto consultado:** solqaryn-dev, branch GitHub dev. No se consultaron proyectos QA/PROD, ni se ejecutaron despliegues.

| Recurso Vercel del proyecto DEV | Estado | SHA origen | Paridad con dev@72327d13 |
| --- | --- | --- | --- |
| [dpl_8UsFYE5z3qUYdpsj8wj4kYXaMRJv](https://vercel.com/solqaryn/solqaryn-dev/8UsFYE5z3qUYdpsj8wj4kYXaMRJv) | READY | 29dbcb06fe8034c01e508b08f13bfc171e08f4c9 | NO |
| [dpl_CJoQyqx56iM3GKVCz1GXdm2Wra3u](https://vercel.com/solqaryn/solqaryn-dev/CJoQyqx56iM3GKVCz1GXdm2Wra3u) | CANCELED | bafcde53ce2562f70d5b4115184df51e5c48483d | NO |
| [dpl_Gsbx2wDBrbMKcBXtKV6HYLrhs8SX](https://vercel.com/solqaryn/solqaryn-dev/Gsbx2wDBrbMKcBXtKV6HYLrhs8SX) | CANCELED | bafcde53ce2562f70d5b4115184df51e5c48483d | NO |
| QA / PROD | NO_VERIFICADO, fuera del alcance | — | NO_DEDUCIBLE |

El atributo target=production en registros del proyecto llamado solqaryn-dev es una etiqueta de Vercel, no prueba de que esta Task consultara o modificara el proyecto PROD de SOLQARYN.

## Hallazgos y pruebas negativas

1. El Plan solicita guardar 48/48 y 21/21. **NO se comprobó que esas cifras correspondan al HEAD actual.** Los jobs de CI exactos exhiben 943/943 y 2902/2902; NO sustituir pruebas ni presentar 48/48 y 21/21 como resultados nuevos. Se conserva su condición histórica NO_REVALIDADA hasta obtener logs específicos.
2. No convertir CI SUCCESS en equivalencia Vercel/Render ni en READY para el SHA actual. Un READY de SHA anterior o CANCELED de SHA más reciente es negativo para paridad. El gate de P0-001.B permanece independiente.
3. No inferir aprobación formal independiente del parámetro 0 del ruleset. Exigir otro login real, commit SHA final, checks 3/3 y merge protegido.
4. Script reproducible: scripts/verify-p0-009-a-ci-evidence.mjs --self-test, un fixture positivo y ocho mutaciones negativas: check requerido ausente, check de SHA ajeno, deployment CANCELED, READY con SHA ajeno, revisión por el autor, revisión por el último contribuyente, aprobación de commit antiguo y PR sin SHA. PASS en Node efímero local el 25-10-2026. Es prueba sintética de fail-closed, NO lectura del proveedor; sin checkout/CI dirigido del script no afirmar que lo ejecutó GitHub Actions.

## Impacto y cierre

Solo esta acta, un script de pruebas puras y CHANGELOG_AI.md. Sin cambios de API, DTO, frontend, RBAC, MySQL, migraciones, secretos, datos, workflows, proveedor, QA/main/PROD o despliegues. Consumidores: revisión de CI y mantenimiento DEV; observabilidad por SHA, jobs y links. Rollback mediante revert protegido de esos tres archivos, sin reescribir historia.

**Estado:** candidato en VALIDANDO tras PR; LISTO requiere CI obligatorio 3/3 exact SHA del PR, validación independiente del script, APPROVED de otro GitHub login, merge protegido y readback PLAN_MAESTRO/COLA/BITACORA. No cerrar P0-001.B por esta acta.

## Prueba reproducible de este PR

Se incorpora un workflow mínimo de GitHub Actions (`.github/workflows/p0-009-a-ci-evidence.yml`) con `permissions: contents: read`, `actions/checkout@v4` y Node 24.21.0 para ejecutar `node scripts/verify-p0-009-a-ci-evidence.mjs --self-test` sobre el SHA exacto del PR. Este self-test valida un positivo y ocho mutaciones negativas sintéticas; no contacta proveedores. **El resultado nativo solo es PASS al comprobar el job SUCCESS**, no por existir el YAML. La revisión independiente debe observar el job y los tres checks obligatorios antes del merge y conciliación. Este cambio no crea despliegues, credenciales ni edita código ERP.
