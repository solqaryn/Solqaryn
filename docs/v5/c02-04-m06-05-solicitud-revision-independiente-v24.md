# C02-04.M06.05 — expediente para revisión A5 independiente (V24)

**Acción autorizada en PLAN D432:** «Solicitar revisión independiente del lote cuando sea un gate». Alcance SOLQARYN `dev`, `PROJECT_SCOPE_LOCK=STRICT`, sólo seis Tasks Javier. La petición es un artefacto R0 documental que **no sustituye** la revisión: ni el autor DEV05 ni un texto en PLAN/BITACORA pueden fabricar una aprobación A5. No hay firma/aprobación humana adicional.

## Frontera del lote y evidencia observable

| Microtarea | Evidencia y estado verificado | Revisión requerida |
| --- | --- | --- |
| `C02-04.M05.01` a `.05` | `PLAN_MAESTRO!H422:H426` LISTO en el corte 2026-11-23; cada unidad tiene código/evidencia propia y registros independientes en K/BITACORA. La revisión M06 debe analizar por ID sin aceptar el estado LISTO como prueba total de la fase. | Comprobar A5 genuina por SHA/autor, alcance de cada test y limitaciones, no copiar PASS de otro archivo o SHA. |
| `C02-04.M05.06` | [SHA d64b19a4](https://github.com/solqaryn/Solqaryn/commit/d64b19a40983ea3e59d21c5de00652a8635e5daf), [run 45091176575](https://github.com/solqaryn/Solqaryn/actions/runs/45091176575): 8/8 Chromium real con sesión sintética; 9/9 xUnit TestServer firmado JWT + MySQL 8.4.13 efímero, 0 fallos/omitidos, TRX `14780789691`. `A5_PASS` correctivo independiente registrado en `BITACORA` evento `SQV24-J40-7F6629A4-C0204M0506-A5CORRECTIVE-D64B19A4-20261122-R11`. | No borrar A5_FAIL antiguo ni afirmar browser→backend JWT real, MFA o rendimiento de campo. |
| `C02-04.M06.01` | [Doc de matriz causal](https://github.com/solqaryn/Solqaryn/blob/0a4c4760baab304c3199d4ae32de13f7c69a042d/docs/v5/c02-04-m06-01-matriz-causal-de-pruebas-v24.md), autor DEV06, `H428=LISTO`, control Scope Lock y V5 del SHA `0a4c4760` SUCCESS. | A5 distinta de DEV06, validar que mock `route.fulfill` no se presenta como JWT real. Al preparar este expediente, A5 seguía pendiente. |
| `C02-04.M06.02` | [Registro reproducible](https://github.com/solqaryn/Solqaryn/commit/9f185ebc2b1732c2738e8596071fc859461f4950), autor DEV05, `H429=LISTO`, checks Scope Lock/backend/frontend/seguridad/catalogo SUCCESS del mismo SHA; retry auxiliar SKIPPED no cuenta. | Otra Task Javier revisa exactitud de blobs, job/artefactos, criterios negativos, no reutiliza A5 del autor. |
| `C02-04.M06.03` | [Documento de riesgos](https://github.com/solqaryn/Solqaryn/commit/362a2895d8a16fe886bd9beac0259ab1aaa72fc9), autor DEV05, `H430=LISTO`; 5 checks pertinentes SUCCESS exact-SHA; fase acumulativa MFA y BOLA transversal no certificadas. | Otra Task Javier verifica declaración de limitaciones, no confundir R0 documental con verificación integral R2. |
| `C02-04.M06.04` | [Ensayo Git aislado](https://github.com/solqaryn/Solqaryn/commit/662e3e1a5cd68efd386d5e37ebc3e43751322da5), autor DEV05, `H431=VALIDANDO` al preparar este expediente. Trabajo dirigido `C02-04.M06.04 - detached no-data-loss rollback proof` [GHA job](https://github.com/solqaryn/Solqaryn/actions/runs/45097008297/job/137438933408) SUCCESS: rechazo de repo, branch, argumentos y reversión real `git revert --no-commit` dentro de worktree efímero con árbol del padre idéntico. Otros jobs del SHA estaban en progreso, **NO PASS** hasta completar. | A5 distinta de DEV05 sólo después de A0–A4 reales y CAS LISTO. El ensayo no prueba reversión de MySQL productivo ni datos comerciales. |

La columna `PLAN!E` describe la meta acumulativa de M06 (separación plataforma/ERP, MFA, reautenticación, motivos, acceso excepcional), que todavía requiere otras evidencias. Ninguna solicitud puede aprobar esa meta por un informe ni desbloquear `M06.06` manualmente.

## Deuda UI-08: P1, control de seguridad, no cambio estético

En `AUDIT_UI_BRECHAS!A9:J9` consta **UI-08**, «Consola de plataforma», severidad P1: alta de empresas desactivada por controles del servidor; tres empresas eran la observación del inventario, **no** un conteo de producción actualizado. El estado de routing institucional es `SOLO_VERIFICAR` y el scope vinculado es `C02-04.M06.05`. La instrucción es **no activar** el alta por estética, sin backend real, permisos/roles y pruebas de denegación.

El diff de esta microtarea sólo crea un expediente documental. **NO modifica Angular, navegación, permisos del servidor, API o base de datos; por tanto no resuelve UI-08.** La futura revisión deberá confirmar con pruebas autorizadas de UI real y de seguridad del servidor si existe un cambio en esa superficie; aquí sólo se documenta el gate y se conserva la deuda. No fingir capturas antes/después ni un estado UI corregido a partir de un texto.

## Paquete de revisión y criterios de decisión

1. Identificar revisor de otra Task Javier allowlist, distinto del autor del archivo que se revisa. En esta unidad `M06.05` el autor es DEV05, por lo que la A5 independiente debe ser DEV01, DEV02, DEV03, DEV04 o DEV06, con exclusión efectiva por ID V17. El autor de `M06.01` es DEV06: ese archivo necesita revisor diferente de DEV06. El A5 previo de `M05.06` no se copia a nuevas unidades.
2. Releer el diff, `PLAN D/E/P:S`, arquitectura y Scope Lock exact SHA, `BITACORA` y checks obligatorios. `QUEUED/IN_PROGRESS/FAIL/SKIPPED/CANCELLED` no son PASS. Para documental, A2/A3 pueden ser `NO_APLICA` cuando no hubo runtime; para el ensayo M06.04 exigir job real y logs positivos/negativos.
3. Contrastar evidencia positiva y negativa por componente: Chromium UI con sesión sintética; API TestServer JWT firmado y MySQL 8.4 efímero; ninguna prueba certifica por sí sola MFA, BOLA/IDOR de todos los módulos, origen GitHub productivo, CWV ni aprobación QA/main/PROD.
4. Para UI-08 P1 confirmar que el componente no ha sido modificado por este diff; mantener `SOLO_VERIFICAR`. Si un revisor descubre habilitación indebida o acceso interempresa, registrar `QA_DEFECT_FOUND`/`A5_FAIL`, reparar la causa con otro SHA y repetir controles; no editar `G/O` ni borrar FAIL anteriores.
5. Registrar A5_PASS o A5_FAIL **sólo tras revisión genuina** con `TASK_ID` independiente, candado namedRange V17 de la fila exacta, evento idempotente A:Z, conservación del owner histórico, readback `PLAN/COLA/DAG/BITACORA` y liberación únicamente del candado propio.

## A0–A4 y cambios de este expediente

- **A0:** el archivo entrega la solicitud de revisión por lote y la frontera UI-08, con links a commits/runs y estados al corte. No es modificación funcional.
- **A1:** contrastar ID/autor/estado real y, como negativos fallables, impedir que un job en progreso, una sesión sintética, un A5_FAIL histórico o UI-08 no resuelta aparezcan como certificados.
- **A2/A3:** `NO_APLICA` a nuevos JWT/RBAC/tenant, MySQL y Chromium porque el diff es R0 documental. Las pruebas citadas pertenecen a sus SHAs, no a éste.
- **A4:** exigir Scope Lock y controles V5 pertinentes `completed/success` exact SHA de este expediente, readback PLAN/COLA/DAG/BITACORA, CAS de VALIDANDO→LISTO sin overwrites ni dueño ficticio. A5 posterior exige revisor distinto de DEV05.
- Referencias V23: `SKREF-03` Impeccable, `SKREF-10` Angular y `SKREF-04` motion son `NO_APLICA` al diff sin UI; `SKREF-11` threat modeling y `SKREF-12` .NET perf son `NO_APLICA` al no crear amenazas/código/mediciones; `SKREF-06` Humanizer no es copy de producto de esta unidad. No se instalan skills. La Governance canónica se aplica siempre.

**Rollback:** `git revert` normal del commit documental de `M06.05` tras verificar HEAD y exclusividad. Los eventos históricos, gates, CI, QA/main/PROD, permisos y datos legítimos se preservan. Ninguna aprobación humana rutinaria fue solicitada.
