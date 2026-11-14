# P0-001.B — Certificación negativa de HEAD y paridad DEV (corte 25-10-2026)

**Resultado del corte: `NO_GO / PARIDAD_EXACT_SHA_NO_ACREDITADA`.** No es un informe de despliegue ni una autorización de promoción. Fuente exclusiva: Plan Comercial Inicial Unificado (Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`), ficha **P0-001.B**. Dependencias `P0-001.A` y `P0-012.B` verificadas **LISTO** en PLAN_MAESTRO/COLA. Ejecutora autorizada: `PRIMARY_DEV_03`, Task ID `6adc2e55f47c81919f6374e561687076`, cuenta GitHub `jmejia31`. Rama exclusiva `work/solqaryn-p0-001-b`.

## Evidencia de identidad, SHA y proveedores (solo lectura)

| Recurso | Referencia comprobada | Resultado |
| --- | --- | --- |
| GitHub protegido | `solqaryn/Solqaryn` `dev@6a6d6e8870b0a53db9b3dc19f197be409082b6e6` | Scope Lock, Seguridad multiempresa y Build backend **3/3 SUCCESS** sobre este SHA. No certifica despliegue. |
| Render API DEV | Servicio declarado `solqaryn-api-dev`, ID `srv-daqvla49v7es738up9vg` | **NOT_VERIFIED**: el conector rechazó lectura sin `workspaceId` previamente confirmado; no se eligió cuenta/workspace a ciegas ni se infirió SHA live. |
| Vercel proyecto DEV | `solqaryn-dev`, ID `prj_1Anhx5mWyXEBX89lWC24Py6JXe7A` | Último intento visible: `CANCELED` `dpl_CJoQyqx56iM3GKVCz1GXdm2Wra3u` sobre `bafcde53ce2562f70d5b4115184df51e5c48483d`, distinto del HEAD. |
| Vercel despliegue READY | `dpl_8UsFYE5z3qUYdpsj8wj4kYXaMRJv` | `READY` para SHA **obsoleto** `29dbcb06fe8034c01e508b08f13bfc171e08f4c9`. No acredita que el HEAD actual esté desplegado. |
| Snapshot reproducible | `docs/baselines/P0-001-B-DEV-20261025-REFRESCO-PRIMARY03.json` | Captura con UTC original, IDs, SHAs, URLs, resultados CI y valores expresamente desconocidos. |

Los tres checks de GitHub fueron leídos en [Scope Lock](https://github.com/solqaryn/Solqaryn/actions/runs/40243330357/job/121319605641), [Seguridad multiempresa](https://github.com/solqaryn/Solqaryn/actions/runs/40243330365/job/121319606103) y [Build backend](https://github.com/solqaryn/Solqaryn/actions/runs/40243330365/job/121319606095). Los dos registros de Vercel se consultaron en el proyecto DEV, sin desplegar ni modificar servicios: [CANCELED](https://vercel.com/solqaryn/solqaryn-dev/CJoQyqx56iM3GKVCz1GXdm2Wra3u) y [READY antiguo](https://vercel.com/solqaryn/solqaryn-dev/8UsFYE5z3qUYdpsj8wj4kYXaMRJv). El valor `target=production` dentro del proyecto **denominado DEV** es metadato Vercel, no permiso para tocar PROD.

**Negativo causal obligatorio:** si GitHub HEAD, Render live SHA y Vercel READY SHA no son idénticos, la certificación de paridad se rechaza. Aquí Vercel carece de READY sobre HEAD y Render es desconocido: `NO_GO`. Un HTTP 200, un CI verde o el READY de otro SHA tampoco suplen backend health, CORS preflight, autenticación/RBAC o aislamiento multiempresa; estos últimos quedan `NO_VERIFICADO`.

## Reutilización e idempotencia

El snapshot histórico `docs/baselines/P0-001-B-DEV-20261025.json` corresponde a `dev@05e6a67a7bbbd2a94b4aeaf1322258ece90c730b` en un corte anterior y **NO** se sobrescribió ni se interpretó como evidencia actual. Se reutiliza `scripts/verify-p0-001-b-parity.mjs` (sin crear un validador comercial paralelo) con soporte para un segundo snapshot cuyo resultado esperado es negativo. La prueba `--self-test` usa un positivo estrictamente sintético y mutaciones negativas; jamás certifica un deployment real.

## Impacto, riesgos y cierre

Solo documentación, evidencia JSON y tests Node read-only. **No** se alteran Angular, API/DTO, Domain/Application/Infrastructure, MySQL, migraciones, tenant context, JWT/RBAC/CORS, datos personales, contraseñas, proveedores, DNS, dominios, QA/main/PROD ni secretos. La clasificación negativa evita prometer paridad inexistente y preserva el control del backend como frontera de autorización. Consumidores: certificaciones DEV y P0-003.B, manteniendo sus propios scopes independientes.

**Siguiente evidencia externa faltante:** selección `workspaceId` de Render confirmada por el propietario para leer servicio DEV; un despliegue Vercel READY del HEAD exacto y comprobaciones de salud/preflight/tenant, sin inferirlos de respuestas 200. No se requiere contratar ni activar servicios. La **microtarea documental** sólo puede declararse LISTO con PR protegido, CI 3/3 y self-test del SHA final, APPROVED GitHub por un login independiente, merge a dev y readback de `PLAN_MAESTRO!H3:M3`, `COLA!E3` por fórmula y BITACORA. El dictamen del entorno sigue NO_GO mientras falte esa evidencia. Rollback: revert del changeset mediante PR protegido, sin borrar snapshots históricos ni modificar proveedores.
