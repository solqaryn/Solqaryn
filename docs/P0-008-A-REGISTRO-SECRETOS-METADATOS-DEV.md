# P0-008.A — Inspección de metadatos del registro de secretos (DEV, PARCIAL)

**Estado de evidencia:** `EN_PROGRESO / PARTIAL_NO_GO`. **Task:** PRIMARY_DEV_01 Javier :00, ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Actor GitHub:** `jmejia31`. **Dependencia:** P0-012.B LISTO. **Base auditada:** `dev@785911f1095f13c0b6c0e859c35e0a7b733fcb3c`. **Fuente comercial:** Plan Inicial V3, PLAN_MAESTRO fila 16, Doc Drive ID `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`.

## Alcance / fuente reproducible

Inspección **solo de nombres, ámbitos y tipos declarados**, sin descargar, descifrar, registrar ni comparar valores. La evidencia es una instantánea de metadatos de proveedores conectados; **NO** acredita que la aplicación haya usado estos valores, que estén vigentes, ni que exista paridad/seguridad real. El snapshot versionado `docs/baselines/P0-008-A-DEV-METADATA-20261026.json` es histórico y no reemplaza nuevas lecturas del proveedor.

- **Vercel:** proyecto `solqaryn-dev` (`prj_1Anhx5mWyXEBX89lWC24Py6JXe7A`), consulta de metadata con `decrypt=false`. Se observaron **6 nombres** declarados: cuatro para los targets `production/preview/development` (`SEO_INDEXING_ENABLED`, `PUBLIC_ORIGIN`, `API_UPSTREAM`, `SOLQARYN_ENV`) y dos **exclusivamente production** (`SOLQARYN_PUBLIC_STORE_SIGNING_KEY`, `SOLQARYN_PUBLIC_STORE_PROOF_ENABLED`). El atributo de target `production` de una variable del proyecto llamado DEV **NO** significa producción autorizada ni confirma su presencia en `development`.
- **Render:** workspace `SOLQARYN` (`tea-dapjfom7bikc73fncodg`), servicio `solqaryn-api-dev` (`srv-daqvla49v7es738up9vg`), asociado a rama `dev`. El conector consultado expuso metadatos de servicio **pero no el registro de claves de entorno**. No se concluye que faltan secretos; la evidencia de claves Render es `NO_VERIFICADA`.
- **GitHub:** HEAD `dev` y CI comprobados; el endpoint sensible de secretos de repositorio no está disponible mediante la lectura aprobada. Nombres/valores de secretos GitHub son `NO_VERIFICADOS`.
- **Aiven, Cloudinary, SMTP:** fuera de esta lectura; cualquier validación exige canal propio autorizado y sin valores sensibles en informes.

## Readback adicional de metadatos de proveedor (2026-10-26T06:07:42.411Z)

- Render DEV: consulta autenticada de metadatos del servicio `srv-daqvla49v7es738up9vg` en el workspace `tea-dapjfom7bikc73fncodg`: `environmentId=evm-dapkqge7bikc73fros3g`, `branch=dev`, `autoDeployTrigger=checksPass`, `healthCheckPath=/health` (configuración declarada, **NO** prueba de health). El conector nativo **no expone un listado de nombres de variables de entorno**, por lo que `RENDER_ENV_KEYS=NO_VERIFICADAS` y `MYSQL_BIND=NO_VERIFICADO` permanecen inalterados. No se abrió ni escribió el registro de secretos.
- Vercel DEV: lectura metadata-only repetida con `decrypt=false`, manteniendo **6 nombres** y el mismo reparto de targets; ninguna variable de `production` se considera automáticamente vinculada a `development`. No se conservan valores cifrados, descifrados, IDs sensibles de credenciales ni payloads de proveedor.
- Resultado: esta lectura aumenta la trazabilidad del **servicio y entorno** Render, pero **NO** completa el inventario de bindings Render/MySQL. El PR continúa `DRAFT` y la microtarea `EN_PROGRESO / PARTIAL_NO_GO` hasta que exista evidencia específica permitida. No se autoriza deploy ni se altera entorno alguno.

## Comprobación negativa adicional: vínculo CI → proveedor (2026-10-26T07:07:34Z)

- En el SHA protegido `dev@785911f1095f13c0b6c0e859c35e0a7b733fcb3c`, el workflow programado **SOLQARYN DEV - Vercel consolidated release**, [run #40283854509](https://github.com/solqaryn/Solqaryn/actions/runs/40283854509), pasó el self-test de política pero falló al publicar al proyecto DEV: `MISSING_PROVIDER_TOKEN_IN_ACTIONS_SECRETS` y salida 1. El valor de `VERCEL_TOKEN` no estaba disponible para **esa ejecución**. Esta es una observación de disponibilidad de binding en un job, **no** una prueba de que el secreto esté ausente globalmente ni de que existan o falten valores en Vercel.
- La lectura autenticada de metadatos del proyecto `solqaryn-dev` devolvió `live=false` y `latestDeployment.readyState=CANCELED` (`target=production`); no acredita `READY` ni paridad DEV y no concede permiso para producción. La brecha operativa de P0-001.B continúa **NO_GO**. No se intentó reejecución, despliegue, descifrado, creación ni corrección de secretos.
- El alcance P0-008.A permanece **PARTIAL_NO_GO**: la lista de nombres de variables Render y el binding físico MySQL siguen sin verificarse; ninguna evidencia documental sustituye metadatos reales. Acción institucional externa delimitada: revisar por canal autorizado la disponibilidad del token para el job DEV sin revelar su valor, y facilitar acceso metadata-only a los bindings faltantes. No cambiar credenciales ni proveedores desde esta microtarea.

## Prueba negativa, seguridad y límites

`node scripts/verify-p0-008-a-secret-metadata.mjs --self-test` valida la estructura y **12 mutaciones negativas sintéticas**: decryption habilitada, valores insertados, duplicados, targets incorrectos, fuentes faltantes y conclusiones `GO` indebidas. La ejecución nativa depende del workflow de PR `p0-008-a-secret-metadata.yml`; la prueba de snapshot **no consulta servicios reales** ni sustituye un readback actual de proveedor.

**Hallazgo y decisión:** `PARTIAL_NO_GO`. No usar este inventario como prueba de secretos completos, consistencia Render/Vercel, autenticación, despliegue, readiness, revisión de rotación o liberación de P0-001.B. **No declarar LISTO** hasta completar por canal permitido el inventario exigido y verificar pruebas, PR, revisión independiente, CI final, merge protegido y readback Sheets. No capturar valores o hashes de credenciales en archivos o logs.

## Impacto y rollback

Cambios solo en documentación, snapshot de nombres, script Node de comprobación offline y workflow read-only. No se modifican UI Angular, API/DTO, MySQL, migraciones, RBAC, secretos, provider settings, dominios, deployments ni QA/main/PROD. El rollback consiste en revertir exclusivamente este changeset mediante PR protegido, preservando evidencia histórica.
