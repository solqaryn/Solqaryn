# C15-02.M06.02 — Manifiesto reproducible de versiones, diffs y artefactos (V22)

## A0 — Contrato de salida

PLAN D2229: «Registrar versión, diff, artefactos y resultado reproducible». Alcance R0, **manifiesto documental y enlaces a GitHub Actions**: no toca runtime, JWT, RBAC, MySQL, secretos, pagos o proveedores. PLAN E2229 define objetivo acumulativo de infraestructura/proveedores, **no declarado aprobado** en esta unidad.

- Fuente de preflight: `solqaryn/Solqaryn@dev` HEAD `f665ee71e9acc34696d1733ca8481c9fe171737a`, fecha UTC 2026-11-20T02:23:09.637Z.
- Autor DEV05 `6adc2e55f47c81919f6374e561687076`, candado V17 `SQV17_C15_02_M06_02` ID `2000002229`; owner en PLAN I y evento de BITACORA.
- Predecesoras: M05.01–M05.06 `LISTO+A5_PASS` 6/6 en K, M06.01 `LISTO` con su auditoria [real vs mock](https://github.com/solqaryn/Solqaryn/blob/f665ee71e9acc34696d1733ca8481c9fe171737a/docs/v5/c15-02-m06-01-real-vs-mock-scope-audit.md); **su A5 sigue pendiente**, no se simula.

## Versiones y evidencia CI real por unidad

| ID | Commit SHA causal de codigo/prueba | Resultado dirigido y tecnologia | GitHub-hosted RUN/JOB | Exclusion, falla historica o limite |
|---|---|---|---|---|
| M05.01 | [`a2dd7af7d6f1a437d1d886ad4596a4e47720d9c0`](https://github.com/solqaryn/Solqaryn/commit/a2dd7af7d6f1a437d1d886ad4596a4e47720d9c0) | 4/4 Vitest unidad Angular | [44653594400 / 136014234950](https://github.com/solqaryn/Solqaryn/actions/runs/44653594400/job/136014234950) | Global release y Modulos FAIL; no se acreditan |
| M05.02 | [`01e4131f03ffed3887ace004738c9018e2314211`](https://github.com/solqaryn/Solqaryn/commit/01e4131f03ffed3887ace004738c9018e2314211) | 6/6 Vitest DTO cliente | [44654801182 / 136018264999](https://github.com/solqaryn/Solqaryn/actions/runs/44654801182/job/136018264999) | V5 FAIL origen; reparacion provenance ec765931f852fbb7e8f24e5761dd7e6b274b8540 |
| M05.03 | [`b9466fc06b4c2c882948fe12dae0ccdaf46c70b8`](https://github.com/solqaryn/Solqaryn/commit/b9466fc06b4c2c882948fe12dae0ccdaf46c70b8) | 6/6 xUnit Oracle EF MySQL 8.4 efimero | [44655887915 / 136021869603](https://github.com/solqaryn/Solqaryn/actions/runs/44655887915/job/136021869603) | Migraciones EF global FAIL job 136021980847 |
| M05.04 | [`8865c6c483157e67598d77436d2df58584ca7865`](https://github.com/solqaryn/Solqaryn/commit/8865c6c483157e67598d77436d2df58584ca7865) | 2/2 Playwright Chromium loopback | [44654113784 / 136016023781](https://github.com/solqaryn/Solqaryn/actions/runs/44654113784/job/136016023781) | Sesion local simulada; Modulos global FAIL |
| M05.05 | [`ae7d06e00db9aa647792911f2648833af61775e5`](https://github.com/solqaryn/Solqaryn/commit/ae7d06e00db9aa647792911f2648833af61775e5) | 5/5 Playwright Chromium + JWT API tenant A/B real en CI | [44654745490 / 136018083744](https://github.com/solqaryn/Solqaryn/actions/runs/44654745490/job/136018083744) | Sin proveedor externo; Modulos global FAIL |
| M05.06 | [`2ca545eaa8eb9a75e0b909fff0f6404281dcc6ab`](https://github.com/solqaryn/Solqaryn/commit/2ca545eaa8eb9a75e0b909fff0f6404281dcc6ab) | 3/3 Playwright Chromium acceso/teclado/perf lab | [44657965081 / 136028793145](https://github.com/solqaryn/Solqaryn/actions/runs/44657965081/job/136028793145) | 403 local sintetico; campo CWV NO_MEDIDO; migraciones globales IN_PROGRESS |

Todos los RUN/JOB son identificadores **observados de GitHub Actions**. Un status `success` del test dirigido corresponde a la unidad concreta, **no** a todo el flujo global. Cada link de commit expone su diff (no inventar cambios hechos por otro autor). Se conservan `CHANGELOG_AI.md` y la atribucion de cada tarea sin reescribir historia.

## A1 — Reproduccion y comprobaciones negativas

1. Abrir commit SHA causal de cada fila; verificar el diff en pestaña *Files changed* y que se trate de tests/workflow/doc. En M05.02 el SHA de test es `01e4131f...` y la correccion de CHANGLELOG esta en descendiente `ec765931...`; **no asignar nuevo resultado de test a un archivo cambiado sin verificar blob equivalence**.
2. Abrir el RUN/JOB exacto, revisar pasos de checkout Node/.NET, `npm ci` / `dotnet test` / Chromium / servicio `mysql:8.4`, y el resumen de pruebas **4, 6, 6, 2, 5, 3** con `0 fail` para el test dirigido. La auditoria M06.01 conserva los limites entre infraestructura real, fixture y validacion UI.
3. En el listado de check-runs del mismo commit distinguir `completed/success` de `failure/cancelled/skipped/in_progress`. Negativo reproducible: M05.02 V5 original **FAIL** corregido despues; M05.03 migraciones globales **FAIL**; M05.01 `release` y `Modulos...` **FAIL**; M05.04/M05.05 `Modulos...` **FAIL**; M05.06 migraciones globales no completadas al censo. Ninguno es **PASS** por esta acta.
4. Condicion de frontera: cambiar SHA de un test o configuracion de CI, o usar solamente una lista de PASS sin job/log/codigo, invalida cualquier equivalencia inferida. En pruebas UI con sesión simulada no convertir 403 fixture en JWT firmado real. Mediciones FCP/DCL son de laboratorio; Core Web Vitals de campo y proveedor real `NO_MEDIDO`.

La evidencia anterior se ha recopilado leyendo commits, check-runs y logs en GitHub-hosted. Los comandos de CI son los de sus YAML originales; esta acta no los reejecuta sobre un SHA nuevo y no simula equivalencia global. Cada futura correccion necesita su nuevo SHA y check exacto.

## A2/A3 — Seguridad y proveedor sin sobrerreclamar

**NO_APLICA a este diff R0** una nueva prueba de HTTP/JWT/MySQL/Chromium porque solo se agrega manifiesto y changelog, sin modificar codigo. Al hablar de real, M05.03 usó MySQL 8.4 efimero y EF modelo de pruebas; M05.05 usó HTTP JWT firmado/tenant A/B en CI, no datos productivos; M05.01/.02/.04/.06 son unitarias o browser con mocks parciales. `M05.03` falla el gate global de migraciones, y las demas fallas globales permanecen abiertas. **No certificar** Hostinger, Cloudflare, R2, DNS, dominio ni MySQL productivo. R3/prod/qa/main prohibidos sin autorizacion separada.

## A4 — Estado V17, CI y rollback

`PLAN H2229=PENDIENTE`, `I2229` libre, `COLA G/O=SI`, `DAG_TECNICO A2190=C15-02.M06.02/F=VALIDADO/G=SI` fueron revalidados antes de tomar candado y CAS PENDIENTE→EN_PROGRESO. Publicacion autorizada: un archivo Markdown y seccion autocontenida CHANGELOG, fast-forward/CAS sobre HEAD vivo de dev. Tras publicar se exige `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` `completed/success` para ese SHA, readback PLAN/COLA/DAG y BITACORA evento idempotente; posteriormente A5 por Task distinta al autor. `SKIPPED/CANCELLED/FAIL/IN_PROGRESS` no acreditan el check requerido.

Rollback de artefactos: `git revert` normal del commit documental; no `force-push`, resets, formulas alteradas ni borrado BITACORA. Para reabrir una fila, nuevo evento y lock CAS. Los fallos globales pendientes no se borran.

**Resultado atomico D2229:** versiones, diffs/artefactos, enlaces y resultados dirigidos **trazables y reproducibles**, con decisiones negativas por SHA; no se atribuye al M06 completo ni certifica proveedores externos.
