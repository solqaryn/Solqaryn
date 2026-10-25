# P0-003.B — Certificación negativa de identidad DEV Render/Vercel

**Proyecto:** SOLQARYN, repositorio `solqaryn/Solqaryn`, rama protegida `dev`; única fuente de prioridad: Plan Comercial Inicial Unificado, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA` (L5/L9/L15). **Fila institucional:** `PLAN_MAESTRO!7`, ID `P0-003.B`; dependencias `P0-003.A=LISTO` y `P0-012.B=LISTO`, comprobadas mediante Sheet/COLA calculada. **Task:** PRIMARY_DEV_02, `JAVIER_CHATGPT`, TASK_ID físico `6adc2e4bd8a48191ac7d8641ed95a1a3`, cuenta GitHub `jmejia31`. **Corte de observación:** 2026-10-25T22:24:19.074Z, fotografía de proveedores y código, no garantía continua.

## Resultado del contraste REAL (sólo lectura)

**Dictamen:** `PARIDAD_RENDER_VERCEL_DEV=NO_ACREDITADA`; no hay base para declarar la paridad de `P0-001.B` ni una publicación Vercel DEV del HEAD exacto. Se certifica **el resultado negativo del procedimiento de comprobación** y el comportamiento fail-closed del validador, no el ambiente ni el release.

| Evidencia conectada | Proveedor DEV y recurso | Estado y SHA |
| --- | --- | --- |
| HEAD integrado protegido | GitHub `dev` posterior a PR #3754 | `05cfd207279fac6feca768f6e9cf743da513d812`, Scope Lock + Seguridad + Backend SUCCESS del mismo SHA |
| API DEV | Render `solqaryn-api-dev`, servicio `srv-daqvla49v7es738up9vg`, deploy `dep-dbf80stg1s2s73a353cg` | `live`, commit `05cfd207279fac6feca768f6e9cf743da513d812`, finishedAt `2026-10-25T22:21:15.939843Z` |
| Frontend DEV | Vercel proyecto `solqaryn-dev`, `prj_1Anhx5mWyXEBX89lWC24Py6JXe7A`; deployment `dpl_8UsFYE5z3qUYdpsj8wj4kYXaMRJv` | `READY`, pero commit `29dbcb06fe8034c01e508b08f13bfc171e08f4c9` — **NO** es el HEAD de este corte |
| Intentos frontend | Vercel `dpl_CJoQyqx56iM3GKVCz1GXdm2Wra3u` y `dpl_Gsbx2wDBrbMKcBXtKV6HYLrhs8SX` | `CANCELED`, commit `bafcde53ce2562f70d5b4115184df51e5c48483d` — **NO** son publicación READY |
| Contratos estáticos | `frontend/server/environment-binding.js`, `frontend/vercel.json`, `render.yaml`, `backend/src/API/Program.cs` | Origen `https://solqaryn-dev.vercel.app`, upstream `https://solqaryn-api-dev-fxx8.onrender.com`; configuración declarada **no** prueba CORS efectivo |

Se consultaron **sólo** servicios/proyecto identificados como DEV. `target=production` dentro de las entradas del proyecto denominado `solqaryn-dev` es una etiqueta de Vercel y no es autorización ni prueba de cambios en PROD. La política `docs/DEV_VERCEL_CONSOLIDATED_RELEASE.md` deshabilita publicación automática `dev`; un deployment `CANCELED` no prueba bug funcional, pero tampoco acredita paridad.

## Evidencia negativa y contrato reproducible

Archivo: `scripts/verify-p0-003-b-dev-parity.mjs`. Es **validador puro offline**, sin credenciales, acceso a red ni escrituras en entornos. La función `evaluateDevParity(evidence)` **rechaza** certificación cuando falta cualquiera de las identidades verificables: entorno DEV y SHA 40-hex de la rama; Render servicio DEV `live` sobre el mismo SHA; Vercel proyecto DEV `READY` con SHA coincidente; alias exacto hacia ese deploy; HTTP/origen real; readiness de API; upstream/allow-origin con preflight **efectivo**; smoke autenticado y aislamiento de tenants.

Comando: `node scripts/verify-p0-003-b-dev-parity.mjs --self-test`.

Pruebas sintéticas ejecutadas localmente con Node 22.16.0: **1 positivo y 13 negativos PASS**. Los casos negativos son: HTTP 200 aislado sin SHA/entorno; SHA no informado; Render SHA obsoleto; Render no live; Vercel SHA obsoleto; deployment CANCELED; proyecto Vercel ajeno; alias a otro deployment; HTTP 200 en origen equivocado; backend no ready; CORS preflight no observado; ausencia de smoke autenticado; ausencia de verificación de aislamiento multiempresa. Una prueba positiva sintética sólo comprueba la lógica del validador, **jamás** certifica el deployment real.

El proveedor no expuso en esta comprobación números HTTP verificables para todas las rutas, preflight OPTIONS efectivo ni smoke de sesión autenticada/RBAC/tenant; dichos controles quedan `NO_VERIFICADO`, no se suplanta su evidencia. **El caso negativo obligatorio del Plan — HTML 200 no es identidad completa del entorno — queda ejercitado explícitamente.**

## Dependencias, impacto y rollback

**Componente:** certificación y diagnóstico DEV, sin modificación de producto. **Capas:** documentación + script offline + workflow CI de validación aislada (si consta en el PR) + `CHANGELOG_AI.md`; no alteraciones de API, DTO, frontend runtime, base de datos, migraciones, auth/RBAC, secretos, pagos o deployments. **Consumidores:** P0-001.B (paridad externa) y certificaciones futuras de infraestructura DEV. **Observabilidad:** recurso del proveedor, SHA, estados y timestamps. **Seguridad:** no se obtiene información autenticada ni se ejecuta tráfico privado. **Rollback:** revert protegido del changeset mediante PR, preservando auditorías y estados de proveedores. No se borra evidencia ni se fuerza historia.

**Condiciones de cierre P0-003.B:** revisión independiente GitHub por login diferente de `jmejia31` sobre el SHA final; CI Scope Lock, Seguridad y Backend 3/3 SUCCESS exact SHA; validador nativo acreditado; merge protegido en dev y readback en `PLAN_MAESTRO!H7:M7`, `COLA!E7` (fórmula) y `BITACORA`. El cierre, si procede, acredita **la certificación negativa documental**, nunca que Render/Vercel tengan paridad ni que P0-001.B esté resuelto.

## Referencias contrastables

- PR antecedente P0-003.A: https://github.com/solqaryn/Solqaryn/pull/3754
- Commit DEV de referencia: https://github.com/solqaryn/Solqaryn/commit/05cfd207279fac6feca768f6e9cf743da513d812
- Render DEV: https://dashboard.render.com/web/srv-daqvla49v7es738up9vg
- Vercel READY SHA anterior: https://vercel.com/solqaryn/solqaryn-dev/8UsFYE5z3qUYdpsj8wj4kYXaMRJv
- Vercel CANCELED: https://vercel.com/solqaryn/solqaryn-dev/Gsbx2wDBrbMKcBXtKV6HYLrhs8SX
