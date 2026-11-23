# C02-04.M06.02 — registro reproducible de versión, diff y artefactos V24

**Unidad y autorización.** La columna D de `PLAN_MAESTRO`, fila 429, exige «Registrar versión, diff, artefactos y resultado reproducible». Esta es una entrega documental R0 en `solqaryn/Solqaryn@dev`, con `PROJECT_SCOPE_LOCK=STRICT`; no cambia código, suites, API, migraciones ni despliegues. La meta general de `PLAN!E` sobre MFA y acceso excepcional sigue acumulativa, no se certifica con este documento.

## Identificadores causales

| Evento comprobable | Referencia fija | Evidencia y estado |
| --- | --- | --- |
| Reparación de laboratorio `C02-04.M05.06` | [Commit d64b19a4](https://github.com/solqaryn/Solqaryn/commit/d64b19a40983ea3e59d21c5de00652a8635e5daf) | Nuevo job MySQL 8.4 efímero + JWT firmado de CI. `9 PASS / 0 FAIL / 0 OMITIDOS`, TRX archivo `14780789691`; ocho pruebas Chromium reales con sesión HTTP sintética, `8 PASS / 0 FAIL / 0 OMITIDOS`. |
| Jobs de laboratorio reales, mismo SHA | [MySQL/JWT](https://github.com/solqaryn/Solqaryn/actions/runs/45091176575/job/137420827601) y [Angular/Chromium](https://github.com/solqaryn/Solqaryn/actions/runs/45091176575/job/137420827336) | Ambos `completed/success` para d64b19a4. Hubo 24 check-runs registrados del commit; los tres `SKIPPED` de retry son auxiliares, NO se computan como PASS. |
| Test Playwright (Chromium) | `frontend/e2e/c02-04-m05-06-regression.spec.ts`, blob `1d2200ca79e37cc799747d821b38f65898aaa409` | Navegación 15/15 rutas; Tab/Enter natural, estado de carga/503/403 interceptado y contraste parcial. **No prueba navegador con JWT real.** |
| Test backend | `backend/tests/Solqaryn.Tests/API/Controllers/C0204M0502QaHttpContractTests.cs`, blob `4b844ce140acde653b7aad7e1889d327895277dc` | TestServer HTTP, JWT firmado de pruebas, roles de plataforma, denegaciones por empresa A/B, revocación, persistencia en MySQL aislado. **No usa credenciales de clientes.** |
| Ejecutor CI reproducible | `.github/workflows/v19-c02-04-m05-06-regression.yml`, blob `a8ae3c894d4762f5bdb8240decc57e28cc2c1aad` | Los dos jobs, servicio `mysql:8.4`, filtro xUnit exacto, exigencia TRX 9/9 sin omitidos, `cancel-in-progress=false` y `github.sha`. |
| Contexto y rollback de laboratorio | `docs/v5/c02-04-m05-06-regression-a11y.md`, blob `a99b88b34a3f32c2ec74d5edb316a0232bc826df` | Distingue backend JWT/MySQL real de simulación UI y preserva `A5_FAIL` anterior. |
| A5 correctiva M05.06 | `BITACORA` evento `SQV24-J40-7F6629A4-C0204M0506-A5CORRECTIVE-D64B19A4-20261122-R11`, fila 11719 | Revisión independiente de DEV05, diferente de autor histórico DEV04 y reparador DEV06; `PLAN!H427=LISTO`, `I427` no alterado, fallo histórico conservado. `A5_PASS` sólo para el alcance de la prueba M05.06, sin certificar MFA ni M05 completa. |
| Límites y decisiones M06.03 | [Commit 362a2895](https://github.com/solqaryn/Solqaryn/commit/362a2895d8a16fe886bd9beac0259ab1aaa72fc9); `docs/v5/c02-04-m06-03-limitaciones-riesgos-decisiones-v24.md`, blob `bdbf2cd3a2b232872c0b9529c2968f1dc8d7b032` | Separación de evidencias, riesgos abiertos y alternativas. En el corte 2026-11-23T01:47Z, Scope Lock y frontend habían pasado, backend/seguridad seguían `in_progress`; por tanto `M06.03` quedó `VALIDANDO`, no LISTO. |

**Regla contra falsas equivalencias:** un SHA descendiente sólo sustituye una evidencia anterior si los blobs de los archivos del scope, pruebas y dependencias causales siguen equivalentes. Los blobs anteriores se contrastaron en `dev` al evaluar el A5 de M05.06; no se extrapolan a módulos distintos. `SKIPPED`, `CANCELLED`, `QUEUED` e `IN_PROGRESS` no se convierten en PASS.

## Cómo reproducir la revisión sin modificar fuentes

1. Inspeccionar el [diff d64b19a4](https://github.com/solqaryn/Solqaryn/commit/d64b19a40983ea3e59d21c5de00652a8635e5daf); verificar que el cambio se limita al workflow de laboratorio, el test HTTP/JWT, documentación y sección aditiva de `CHANGELOG_AI.md`. El changelog previo conserva sus encabezados y viñetas.
2. Consultar [run 45091176575](https://github.com/solqaryn/Solqaryn/actions/runs/45091176575), abrir el job MySQL/JWT y confirmar en sus logs `QA_REAL_LAB_TOTAL=9 PASSED=9 FAILED=0 OMITTED=0` y contenedor MySQL `8.4.13`. Revisar el artefacto TRX `14780789691`, sin usar `9 PASS` de otro SHA.
3. Abrir el job Angular/Chromium del mismo run; comprobar `8 passed`, sus nombres de test y la nota explícita `SYNTHETIC` sobre la sesión interceptada. La prueba visual de responsive/contraste no sustituye un JWT/browser E2E real.
4. Para `M06.03`, revisar el commit `362a2895` y [los checks exactos](https://github.com/solqaryn/Solqaryn/commit/362a2895d8a16fe886bd9beac0259ab1aaa72fc9/checks). Un check pendiente no aprueba A4. El documento de límites de ese commit es solamente R0, sin nueva validación JWT/MySQL/Angular.
5. Correlacionar por ID exacto con `PLAN_MAESTRO!H/I/J/K/L/M`, `COLA!G/O`, `DAG_TECNICO!F/G` y `BITACORA!A:Z`. La elegibilidad dinámica sólo permite CLAIM bajo lock propio V17 y CAS. No editar fórmulas ni eventos anteriores.

## Riesgos, decisiones y rollback

- Los riesgos R2 que siguen **no certificados** incluyen MFA real, origen de sesión browser→API con JWT firmado, BOLA/IDOR transversal, Core Web Vitals y cobertura WCAG integral. No trasladar el PASS documental de esta unidad a esos ámbitos ni a QA/main/PROD.
- Este registro `M06.02` **no crea nuevo test ni cambia runtime**. A0 es contraste de fuentes/diff; A1 documental incluye positivos identificables y negativos explícitos de lo que NO se probó; A2 JWT/RBAC/tenant y A3 MySQL/browser son `NO_APLICA` al *diff documental*, aunque sus pruebas históricas reales se citen. A4 exige verificación Scope Lock/V5 pertinente del SHA de esta entrega y readback. A5 debe efectuarla otra Task Javier distinta del autor DEV05.
- Referencias: Governance canónica; `SKREF-06` Humanizer, origen original `blader/humanizer`, pin `9862685f575c65a8247f90369951df1b3416e3d6` / `SKILL.md`, aplicada a precisión del texto. `SKREF-03` y `SKREF-10` NO_APLICA al no cambiar UI/Angular; `SKREF-11` NO_APLICA al no abrir un nuevo modelo de amenazas; `SKREF-12` NO_APLICA por ausencia de mediciones.
- Rollback exclusivo de esta entrega: `git revert` normal del commit que añade este archivo, después de comparar el HEAD. Conserva la BITACORA y el A5 previo. Prohibido `force-push`, producción, migraciones de clientes o limpieza destructiva de evidencia.

**Estado documental al publicarse:** no dar por hecho `LISTO` ni `A5_PASS` de `M06.02` antes de CI causal, CAS y revisión independiente posterior.
