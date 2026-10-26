# P0-008.A — Inspección de metadatos del registro de secretos (DEV, PARCIAL)

**Estado de evidencia:** `EN_PROGRESO / PARTIAL_NO_GO`. **Task:** PRIMARY_DEV_01 Javier :00, ID `6adbb9fff1548191a1bd5b30d9e9984a`. **Actor GitHub:** `jmejia31`. **Dependencia:** P0-012.B LISTO. **Base auditada:** `dev@785911f1095f13c0b6c0e859c35e0a7b733fcb3c`. **Fuente comercial:** Plan Inicial V3, PLAN_MAESTRO fila 16, Doc Drive ID `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`.

## Alcance / fuente reproducible

Inspección **solo de nombres, ámbitos y tipos declarados**, sin descargar, descifrar, registrar ni comparar valores. La evidencia es una instantánea de metadatos de proveedores conectados; **NO** acredita que la aplicación haya usado estos valores, que estén vigentes, ni que exista paridad/seguridad real. El snapshot versionado `docs/baselines/P0-008-A-DEV-METADATA-20261026.json` es histórico y no reemplaza nuevas lecturas del proveedor.

- **Vercel:** proyecto `solqaryn-dev` (`prj_1Anhx5mWyXEBX89lWC24Py6JXe7A`), consulta de metadata con `decrypt=false`. Se observaron **6 nombres** declarados: cuatro para los targets `production/preview/development` (`SEO_INDEXING_ENABLED`, `PUBLIC_ORIGIN`, `API_UPSTREAM`, `SOLQARYN_ENV`) y dos **exclusivamente production** (`SOLQARYN_PUBLIC_STORE_SIGNING_KEY`, `SOLQARYN_PUBLIC_STORE_PROOF_ENABLED`). El atributo de target `production` de una variable del proyecto llamado DEV **NO** significa producción autorizada ni confirma su presencia en `development`.
- **Render:** workspace `SOLQARYN` (`tea-dapjfom7bikc73fncodg`), servicio `solqaryn-api-dev` (`srv-daqvla49v7es738up9vg`), asociado a rama `dev`. El conector consultado expuso metadatos de servicio **pero no el registro de claves de entorno**. No se concluye que faltan secretos; la evidencia de claves Render es `NO_VERIFICADA`.
- **GitHub:** HEAD `dev` y CI comprobados; el endpoint sensible de secretos de repositorio no está disponible mediante la lectura aprobada. Nombres/valores de secretos GitHub son `NO_VERIFICADOS`.
- **Aiven, Cloudinary, SMTP:** fuera de esta lectura; cualquier validación exige canal propio autorizado y sin valores sensibles en informes.

## Prueba negativa, seguridad y límites

`node scripts/verify-p0-008-a-secret-metadata.mjs --self-test` valida la estructura y **12 mutaciones negativas sintéticas**: decryption habilitada, valores insertados, duplicados, targets incorrectos, fuentes faltantes y conclusiones `GO` indebidas. La ejecución nativa depende del workflow de PR `p0-008-a-secret-metadata.yml`; la prueba de snapshot **no consulta servicios reales** ni sustituye un readback actual de proveedor.

**Hallazgo y decisión:** `PARTIAL_NO_GO`. No usar este inventario como prueba de secretos completos, consistencia Render/Vercel, autenticación, despliegue, readiness, revisión de rotación o liberación de P0-001.B. **No declarar LISTO** hasta completar por canal permitido el inventario exigido y verificar pruebas, PR, revisión independiente, CI final, merge protegido y readback Sheets. No capturar valores o hashes de credenciales en archivos o logs.

## Impacto y rollback

Cambios solo en documentación, snapshot de nombres, script Node de comprobación offline y workflow read-only. No se modifican UI Angular, API/DTO, MySQL, migraciones, RBAC, secretos, provider settings, dominios, deployments ni QA/main/PROD. El rollback consiste en revertir exclusivamente este changeset mediante PR protegido, preservando evidencia histórica.
