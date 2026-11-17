# C08-04.M06.01 — Auditoría de evidencia real frente a mocks

**Autor:** DEV04 JAVIER, TASK_ID `6aeacb7820ac8191b7d768a6f33dfa09`. **Rama:** `dev`. **Scope Lock:** STRICT. **Riesgo de la entrega:** R0 documental, aunque las verificaciones de autorización/tenant estudiadas son R2; no se modifica runtime ni se autoriza despliegue. **Alcance:** constatar que los tests de C08-04.M05.01–06 corresponden a las capacidades actualmente disponibles, sin atribuir a fixtures simulados resultados de proveedores o seguridad real.

## A0: fuentes, alcance y fronteras

La UI actual `SectorModulesReadonlyComponent` (blob `76aba1b896fa341c4308222508f6b6eeb953d306`) inicia con estado `vacio`, presenta cuatro tarjetas estáticas `No conectado`, no llama API de activación y no otorga permisos. Su HTML (blob `260b26df0e618d4e974fa24e2ff6108faa50aa07`) declara las capacidades no conectadas. El `C0804SectorModuleApplicationGate` (blob `f3d152bd0a8731aad87a038abf0189de5a9c7e61`) exige `IC0804SectorModuleEvidenceReader` y nunca concede acceso ni escribe DB. El adaptador `C0804IsolatedTenantModuleSnapshotAdapter` (blob `453428e34a0a8d78b8a91e4676caa89cb0722a8c`) fija en falso evidencia de lectura persistida y audit fence; `ReadsLiveMysql=false`, `ReadsJwtOrRbac=false`, `PerformsDurableCas=false`. El `C0804SectorModuleReadOnlyIntegrationBoundary` (blob `f1706126ff79f9a3b48377d444c129fbacbea1a3`) tampoco certifica proveedor runtime.

No cambia Domain, Application, Infrastructure, API, frontend, rutas, tenant, JWT, RBAC, DI, datos, migraciones ni dependencias. Rollback: `git revert` normal del changeset documental. Ningún check de un SHA anterior suplanta CI nueva.

## Matriz de pruebas inspeccionadas

| Microscopo | Qué ejecutó realmente y qué detecta | SHA causal; test/blob | Qué **no** prueba |
| --- | --- | --- | --- |
| M05.01 | 4 Vitest Angular TestBed positivos/negativos: 4 keys, estados de preview, revocación visual y HTML hostil. Job dirigido completed/success. | `9aeadac6fc9dc43f21f6b5fb695f537abe26aba0`; `sector-capability-contract.spec.ts` / `43a1d4872dae7a40f4db11392faf4a1559e554ad` | Fuente de capabilities backend ni módulos activados |
| M05.02 | xUnit Release con `Reader` en memoria: petición válida no autoriza, override, tenant B, SHA inválido, permisos y errores rechazan. Job dirigido success. | `e084a06a8f66bc7d528fe7d4834a6991c085ac96`; `C0804SectorModuleApplicationGateTests.cs` / `151c731ba02182231799803483332b98a751a245` | Middleware JWT HTTP, membresía/RBAC persistida |
| M05.03 | xUnit Release entre **clases reales** de Application/Infrastructure, 6 hechos: actor/proveedor sintéticos, tenant B, revocación, revisión obsoleta, cancelación y 0 llamadas al provider. Job dirigido success. | `7bcdf84a8ae60a2c274c0ccbb37828439ddef629`; `C0804SectorModuleRealAdapterContractIntegrationTests.cs` / `1e6350506562161bbc225cf371903ab1bff2d801` | MySQL 8.4, EF/migración, JWT, CAS durable, provider conectado |
| M05.04 | Playwright Chromium **real** contra UI Angular: 320/1280, 4 tarjetas, ninguna mutación/API sectorial y 403. Job dirigido success. | `979b0229ea475c3c98f8b5ac4969b17318ab5755`; `c08-04-m05-04-positive.spec.ts` / `b97378d6d631c4d24465dcaea6160ddbb88a7c83` | 403 del servidor real: `page.route(tenant-context)` simula y token local no es JWT |
| M05.05 | Chromium real, 5 tests: anónimo, 401, tenant B 403, 503, flag local sin grant; no escrituras. Job dirigido success. | `3a5aa69c38b716336da150650fdf881ebb5320f8`; `c08-04-m05-05-negative.spec.ts` / `ba80dd9660615c8c5a206b47e8879bc1db2620fc` | Tenant A/B en API real, revocación server-side |
| M05.06 | Chromium real, 4 tests: 320/375/768/1280, Tab desde inicio sin `.focus()`, 403/503 y cero mutaciones. Job dirigido success. | `2239d9a85e9846c8ba5f95f3eb2c955c91e07efb`; `c08-04-m05-06-regression.spec.ts` / `a3466cd8f55e274d6f1b8ccd3a9a1a8d1b628423` | `page.route` continúa siendo fixture sintético, no prueba HTTP/RBAC/DB |

## A1, A2, A3 — Dictamen proporcional

**Positivos**: la UI renderiza cuatro previews y los boundaries reales devuelven rechazo seguro. **Negativos**: tenant B, 401/403, revocación, source SHA, overrides y XSS como texto. **Frontera**: 320–1280 px, revisión de módulo, longitud de SHA, Tab desde foco inicial. **Error**: 503 y cancelación en xUnit. Son assertions falsables ejecutadas en el test runner real, **no** una simulación de despliegue. A2: los tests de tenant/permiso son **lógicos o de UX con HTTP interceptado**; no certifican JWT HTTP, RBAC relacional ni aislamiento A/B end-to-end. A3: las clases de las capas son reales en xUnit, y el browser/Angular es real en Chromium, pero los puertos de actor y proveedor son sintéticos. MySQL EF 8.4 y proveedor externo **NO_APLICA al diff de auditoría** y **NO_CERTIFICADOS para activación**.

## A4 — Check-runs verificables por SHA fuente

Se consultó `/repos/solqaryn/Solqaryn/commits/<SOURCE_SHA>/check-runs?per_page=100` para los seis SHAs causales. `SUCCESS/SKIPPED` respectivamente: **22/2, 22/5, 19/1, 15/1, 15/1, 15/2**. En todos, el check dirigido, backend y `verify-solqaryn-project-scope` concluyeron `completed/success`. Los `SKIPPED` son auxiliares **no contados como PASS**. Las rutas/blob hashes listadas se inspeccionaron en el árbol de base `43ce84f67d5d944cdba7bc22b5ebcee7a8518bfc`. Exigir checks propios `SUCCESS` del SHA que publique este documento o equivalencia demostrada.

**Veredicto:** SUFICIENTE para los contratos actuales de vista informativa y fallos cerrados **solo**; INSUFICIENTE para afirmar módulos habilitados, proveedor live, MySQL, tenancy/RBAC/JWT reales o cumplimiento global. No se encontró un defecto de runtime nuevo en esta auditoría; sí un **límite de evidencia explícito**, sin levantar gates. M06.06 permanece sujeta al DAG y revisión A5 independiente por otra Task Javier. No modifica QA/main/PROD.
