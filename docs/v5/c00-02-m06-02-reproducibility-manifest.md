# SOLQARYN V5 — C00-02.M06.02: manifiesto reproducible de versión, diff y evidencias

**Propósito.** Consolidar exclusivamente la versión, árbol Git, hashes, artefactos de pruebas, límites y comandos de reproducción para la fase C00-02 M05, y demostrar cómo se verificó la auditoría documental M06.01. **No** añadir dispatcher, integraciones externas ni controles de autorización nuevos.

## Autoridades de la revisión

- Repositorio: `solqaryn/Solqaryn`, rama `dev`; GitHub autenticado `jmejia31`. La identidad comercial se verifica en la Sheet institucional, no se deduce de un commit.
- Documento V5 FINAL: Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`. Operación: `PLAN_MAESTRO!H` gobierna, `COLA!G/O` gates calculados, `DAG_TECNICO!F/G` autoridad de elegibilidad técnica; BITACORA tiene 26 columnas `A:Z`.
- Seis IDs de C00-02 M05.01–06 están LISTO DEV en PLAN_MAESTRO al preflight. Las revisiones independientes A5 se acreditan por otra Task, no por esta documentación.
- Arquitectura actual comprobada: Angular 22.2.1, .NET 10, MySQL 8.4; el presente change no modifica ninguna de ellas.

## Tabla causal exacta: commits, fuentes y verificaciones

| ID | Commit original | Fuente original identificable | Suite y alcance comprobado |
| --- | --- | --- | --- |
| M05.01 | `c563ec107fd80327d45e586c364b43f0748934c9` | `V5C0002M0501LifecycleRegressionTests.cs` blob `2af2d7b64ca4d9154b59dd465baf6c12be1c8ce7` | Reglas puras xUnit; evidencia institucional backend 3526/3526, seguridad 1124/1124; no claims Sheets |
| M05.02 | `97e9f14dc7b78b2890dfaef660afa4dc4785f4f4` | `V5C0002M0502SafeHttpErrorContractTests.cs` blob `1b286b8a2c4f7a444bd8f713c483838d6072e15e` | HTTP TestServer stub 200/400/409/500; backend job `132272525216` SUCCESS, seguridad `132272525110` SUCCESS; autorización de SHA origen CANCELLED y NO PASS |
| M05.03 | `cb2bd3254b0bf4aa8a9fa92cd115f36809f666a7` | `V5C0002M0503ProviderIntegrationTests.cs` blob `66efd52484297bc92c88403b102fbdcdcb93dfc0` | Selector + proveedor in-memory; xUnit 3577/3577 registrado, seguridad 1149/1149; Scope Lock SUCCESS; no migración MySQL |
| M05.04 | `9214b5190152c4fb76a53b7737905ea9b74b028c` | `V5C0002M0504PositiveAcceptanceTests.cs` blob `679a5cc90fc17229c911aca25b79e5f1906111c9` | Test positivo de memoria; SHA origen con backend/autorización/seguridad CANCELLED, causal descendiente `bf3b370d1da783d016e5de2753032bdf44830d68` conserva blob, compara ahead_by=2 |
| M05.05 | `bf3b370d1da783d016e5de2753032bdf44830d68` | `V5C0002M0505NegativeIsolationTests.cs` blob `43257846018dd16acd513b1b45b689c1bef33666` | Guard de tenant/scope con fixtures, actor retirado, binding y auto-A5 denegados |
| M05.06 | `a061bfc35e8609c6aa6cbb1a4a18d1581538f6d4` | `frontend/e2e/v5-plataforma-browser-guards.spec.ts` blob `72e10a1cc76141bc6d84618e532fbbf2148d2b9e` | Chromium verdadero con sesión/403 sintéticos, teclado y responsive; Scope Lock y Chromium SUCCESS, backend/security CANCELLED y no adjudicados |

En el SHA causal `bf3b370d1da783d016e5de2753032bdf44830d68` se observaron **directamente** logs backend 3554/3554 (job `132276816220`), seguridad 1133/1133 (job `132276816626`) y release 3554/3554 (job `132276731951`), además de autorización/secrets `132276811504`, Scope Lock `132276731949` y CHANGELOG `132276731496`: `completed/success`, head_sha exacto. Las pruebas de M05.04 son causales por blob conservado, **no** por jobs CANCELLED del SHA 9214.

## Reproducción externa y límites

Para verificar en GitHub Actions y distinguir estado de éxito:
```text
GET https://api.github.com/repos/solqaryn/Solqaryn/commits/<SHA_EXACTO>/check-runs?per_page=100
```
Revisar en cada check aplicable `head_sha == <SHA_EXACTO>`, `status=completed` y `conclusion=success`; revisar pasos/logs del job y la fuente/árbol del mismo commit. `SKIPPED`, `CANCELLED`, `queued` o `in_progress` **nunca** acreditan PASS requerido. Comparar blobs, no confundir descendiente con SHA original.

Si se dispone de checkout *del mismo SHA exacto* y herramientas autorizadas, los comandos de comprobación (guía, **no son nueva ejecución en esta microtarea documental**) incluyen:
```bash
node scripts/verify-project-scope.mjs
node scripts/verify-solqaryn-skills.mjs --self-test
node scripts/quality/verify-v5-changelog-provenance.mjs
dotnet restore backend/Solqaryn.sln
dotnet build backend/Solqaryn.sln --configuration Release --no-restore
dotnet test backend/Solqaryn.sln --configuration Release --no-build --filter "Category!=Integration"
```
La existencia de comandos no equivale a que se hayan ejecutado localmente ni a probar persistencia MySQL o autenticación HTTP live. Determinar scripts/rutas definitivos contra workflows de la versión a ejecutar y su propia autoridad, sin instalar dependencias ajenas.

El documento precedente de auditoría de suficiencia se publicó con commit directo dev `82dfa08ef58c888ef9c16976c39ed5330bfcd445`, árbol `8c7a76894e8cf8fef4f52701881a741746af10b0`, parent `cb2bd3254b0bf4aa8a9fa92cd115f36809f666a7`, blobs de auditoría `f70761725397e275432847a6d510b70d85e0b09b` y CHANGELOG `77f5734022a84f440b95b5bf0b84b85a20474d7c`. Scope Lock exact-SHA job `132294060861` SUCCESS y CHANGELOG V5 job `132294060383` SUCCESS. Tests backend/frontend/security de ese SHA eran in_progress; no se usan para certificar este documento.

## Evaluación proporcionada de este alcance

- Tipo 1: inventario de versión/diff/artefactos y guía reproducible; `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO` para este **cambio documental**, aunque las pruebas de fuentes originales sí requirieron sus runners.
- `requiere_security=SI` para detectar falsa atribución de pases y certificar no alteración de límites JWT/RBAC/tenant; `requiere_scope_lock=SI` para publicación dev.
- A0 revisar íntegramente diff, metadatos y secretos, atribución de encabezados CHANGELOG; A1 contrastar seis IDs/fuentes, casos positivos y negativos y manifest de evidencias; A2 no elevar mocks/fixtures a pruebas live; A3 asegurar fuente institucional y dependencias 6/6 LISTO. A4 sólo tras fast-forward commit propio, exact-SHA Scope Lock + CHANGELOG, lectura de archivos y PLAN/COLA/BITACORA.
- Riesgo remanente: **no hay certificación** de Google Sheets CAS distribuido, JWT real, permisos tenant A/B sobre MySQL, cargas productivas o Cloud. No autoriza finalizar M06.06 o posteriores gates sólo por haber registrado este manifiesto.
- No se manipulan archivos históricos de BITACORA, permisos, secretos, ramas, QA/main/PROD. Rollback normal: revertir el commit documental preservando trazas de auditoría.
