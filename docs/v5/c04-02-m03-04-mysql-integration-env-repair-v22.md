# V22 - C04-02.M03.04 - reparacion causal CI MySQL 8.4 (DEV03)

Scope: `C04-02.M03.04` en VALIDANDO; owner historico DEV01 permanece sin cambios; actor reparador DEV03. Riesgo R2 (GitHub-hosted CI, EF/MySQL) solo rama dev, PROJECT_SCOPE_LOCK=STRICT; sin infraestructura real ni cambios a JWT/RBAC, DB productiva o entornos QA/main/PROD.

## Causa real
El workflow `.github/workflows/dev-ci.yml` en SHA `47d30fd7fd5badd4b5d210bd80df069873a4dcd6` ya agregó trece variables para integrar tests heredados. En run `44907720517`, job `136856824875`, se ejecutaron 140 tests `Category=Integration` contra MySQL 8.4.13 efímero: 139 PASS, 1 FAIL, cero SKIP. El único error fue `Solqaryn.Tests.Integration.C0601M0304IsolatedLandingMysqlTests.IsolatedTenantABDraftMetadataAndCorruptionDenyByDefault`: `InvalidOperationException EPHEMERAL_MYSQL84_REQUIRED`. El test exige explícitamente `C0601_M0304_MYSQL_TEST` y no la recibía: el workflow omitía el nombre. No es un defecto demostrado del adaptador MySQL ni autoriza omitirlo.

## Reparación y seguridad
Se añade solamente la variable `C0601_M0304_MYSQL_TEST` apuntando a `127.0.0.1:3306/solqaryn_ci` del servicio desechable del propio job (credenciales de CI existentes). El test construye una base `c0601_m0304_<GUID>` y la elimina en `finally`, verificando MySQL 8.4, aislamiento tenant A/B, estado corrupto, cancelación y denegación segura. Se preservan los tests positivos/negativos y los demás 13 nombres, sin desactivar checks ni cambiar ejecutables de producción. No agregar servidores ni usar Aiven.

## A0-A5 y handoff
- A0: diff de workflow limitado a su entorno CI, caso y bitácora enlazados, rollback por `git revert` normal.
- A1-A3: reejecutar en GitHub Actions los 140 `Category=Integration`; evidencia requerida 140/140 PASS, 0 FAIL, 0 SKIP, MySQL 8.4.x físico efímero y scripts de migración; no asumir PASS por la edición declarativa.
- A4: exigir `verify-solqaryn-project-scope`, procedencia V5 y gates backend/seguridad pertinentes `completed/success` en SHA publicado, además de readback PLAN/COLA/BITACORA y no-conflicto de blobs con scope previo. `queued/in_progress/skipped/cancelled/fail` no es PASS requerido.
- Mientras CI no acredite lo anterior, conservar `PLAN_MAESTRO!H629=VALIDANDO` y `I629=DEV01`, sin A5 ni liberación de DAG. La prueba aislada de adapter C04 debe conservar cobertura real; no confundir esa prueba con certificación de toda la etapa M03 ni servicios de producción.
- A5: tercera Task Javier distinta del autor DEV01 y reparador DEV03, revisión independiente genuina. Sin QA/main/PROD y sin borrar FAIL histórico.
