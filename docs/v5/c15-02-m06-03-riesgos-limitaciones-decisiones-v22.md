# C15-02.M06.03 — Registro de limitaciones, riesgos y decisiones (V22)

## A0 — Contrato institucional, estado y ambito

Accion `PLAN_MAESTRO!D2230`: «Documentar limitaciones, riesgos abiertos y decisiones tomadas». Este artefacto R0 **no agrega controles de runtime**, despliegues, configuracion de terceros, bases productivas ni permisos. PLAN E2230 es la meta acumulativa de revisión de Hostinger, Cloudflare, R2, MySQL, DNS y proveedores; **no está certificada** por estos documentos.

- Fuente exacta: `solqaryn/Solqaryn@dev` HEAD `c3f6241c545db19ec087f0a2547a668b68d76890`, censo UTC `2026-11-20T02:26:22.010Z`.
- Atribucion DEV05: `6adc2e55f47c81919f6374e561687076`, V17 lock `SQV17_C15_02_M06_03` id `2000002230`; A5 sera otra Task Javier.
- M05.01–M05.06: 6/6 PLAN H=LISTO y evidencia A5_PASS individual en K. M06.01 auditoria prueba-real-vs-mock y M06.02 manifiesto SHA/diff/logs constan LISTO **con A5 propia pendiente**; no se convierten en A5_PASS por este registro.

## A1 — Matriz verificable de riesgos, causas y decisiones

| Referencia y prueba | Estado comprobado / riesgo abierto | Decision de control vigente | Siguiente accion permitida |
|---|---|---|---|
| `C15-02.M05.01` [`a2dd7af7`](https://github.com/solqaryn/Solqaryn/commit/a2dd7af7d6f1a437d1d886ad4596a4e47720d9c0) | Tests unitarios Angular 4 PASS en job 136014234950; check global `release` y `Modulos...` **FAIL** | No elevar las pruebas aisladas a CI global ni declarar P0/P1 resueltos | Diagnosticar fallos globales por job/SHA sin desactivar gates |
| `C15-02.M05.02` [`01e4131f`](https://github.com/solqaryn/Solqaryn/commit/01e4131f03ffed3887ace004738c9018e2314211) | Vitest DTO 6 PASS, usa mock HTTP; V5 provenance original **FAIL** | Mantener fallo historico; correccion documental descendiente [`ec765931`](https://github.com/solqaryn/Solqaryn/commit/ec765931f852fbb7e8f24e5761dd7e6b274b8540) con V5 SUCCESS, sin inventar reejecucion origen | Verificar equivalencia de blobs antes de reusar PASS en futuras auditorias |
| `C15-02.M05.03` [`b9466fc0`](https://github.com/solqaryn/Solqaryn/commit/b9466fc06b4c2c882948fe12dae0ccdaf46c70b8) | 6/6 Oracle EF contra MySQL 8.4 efimero, **pero** migraciones globales **FAIL** job 136021980847; modelo de test aislado | No certificar app DbContext/proveedor real/migracion global por green test-only | Evaluar fallo de migraciones en su propia unidad con tests reales y nuevo SHA |
| `C15-02.M05.04` [`8865c6c4`](https://github.com/solqaryn/Solqaryn/commit/8865c6c483157e67598d77436d2df58584ca7865) | 2 Chromium PASS en loopback con sesión sintetica; `Modulos...` global FAIL | No equiparar navegador con autenticacion real ni cobertura global | Conservar prueba E2E, usar JWT firmado/HTTP real solo donde PLAN D lo requiera |
| `C15-02.M05.05` [`ae7d06e0`](https://github.com/solqaryn/Solqaryn/commit/ae7d06e00db9aa647792911f2648833af61775e5) | 5/5 JWT real HTTP tenant A/B/401/403 en CI aislada; `Modulos...` global FAIL | La autenticacion real *aislada* se acredita en este test, no a proveedores externos ni a toda la plataforma | Reparar solo fallos causales y conservar la falla global como bloqueo donde sea requisito |
| `C15-02.M05.06` [`2ca545ea`](https://github.com/solqaryn/Solqaryn/commit/2ca545eaa8eb9a75e0b909fff0f6404281dcc6ab) | Chromium real 3/3 y cuatro tamaños; 403 de fixture, DCL/FCP de laboratorio; LCP/CLS/INP campo NO_MEDIDO; migraciones globales IN_PROGRESS al corte original | No certificar CWV de campo/JWT backend desde fixture | Campo queda NO_MEDIDO; para runtime real exigir evidencia dirigida nueva |
| PLAN E M06 / proveedores | Sin autorizacion ni comprobacion de servicios live Hostinger/Cloudflare/R2/DNS, dominios y secretos; contratos en docs ≠ operaciones reales | **R3 NO_GO** para PROD, QA/main, restauracion destructiva, compras o secretos | Solicitar permiso **especifico** solo si una futura unidad exige expresamente accion R3; evitar aprobacion humana rutinaria para docs |
| `C15-02.M06.04` rollback / `M06.05` review / `M06.06` cierre | PENDIENTE, gates por DAG; los dos actas anteriores no prueban rollback de proveedor ni revisión independiente posterior de esta unidad | No liberar globalmente por un A5 de otro SHA o por `VALIDANDO`; no tocar formulas | Seguir orden de microtareas, rollback reversible solo en entornos efimeros/dev, A5 otra Task |

La matriz deja las limitaciones **expuestas**, no corrige silenciosamente P0/P1 ni sustituye test con prosa. Las severidades exactas de los fallos globales son `POR_CLASIFICAR` hasta lectura causal del job, no se inventa una etiqueta P0/P1. Las metricas sin denominador o sin usuarios son `NO_MEDIDO`.

## A2/A3 — Alcance tecnico y superficie de seguridad

Este diff es solo de documentacion, por tanto nuevas pruebas HTTP, JWT, MySQL, Angular/Chromium no son requisito causal de esta unidad R0. Los tests real/JWT/MySQL mencionados pertenecen a SHAs previos, y no se reutilizan para afirmar cobertura de providers, backups o apps production. Riesgos multiempresa, revocacion y R3 restauracion persisten hasta que una prueba propia demuestre lo contrario; ningun permiso de secretos/QA/main/PROD.

## A4 — Exclusividad, fuente, readback y rollback

Preflight `PLAN!H2230=PENDIENTE/I2230` vacio, `COLA!G2230=SI/O2230=SI`, `DAG_TECNICO!A2191=C15-02.M06.03/F2191=VALIDADO/G2191=SI` y ningun `EN_PROGRESO` propio; se obtuvo lock `2000002230` y CAS H→EN_PROGRESO + owner DEV05 con readback. Publicar exclusivamente este Markdown + seccion autocontenida en CHANGELOG y exigir `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` **completed SUCCESS del SHA nuevo**, luego CAS H a VALIDANDO y LISTO con evento BITACORA/idempotencia y readback; A5 posterior por revisor Javier distinto. Un CI `FAIL/SKIPPED/CANCELLED/IN_PROGRESS` exigido **no** es PASS.

Rollback del documento: `git revert` normal del commit; hojas solo bajo nuevo evento y lock. Nunca `force-push`, cambio de QA/main/PROD, borrado de bitacora o forzado de `DAG!G` / `COLA!G/O`.

**Salida atomica:** registro de decisiones y riesgos por ID/SHA/CI real, con los FAIL y NO_MEDIDO expresos, sin falsas certificaciones.
