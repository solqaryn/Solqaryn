# C08-04.M06.05 — Solicitud de revisión A5 independiente del lote sectorial

**Autora de esta solicitud documental:** SOLQARYN PRIMARY DEV 03 JAVIER, TASK_ID `6adc2e4bd8a48191ac7d8641ed95a1a3`. **Scope:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Riesgo:** R0 documental; ninguna autorización, clase, API, ruta, persistencia o prueba ejecutable se altera.

## A0 — Contrato de solicitud; no confundir solicitud con aprobación

Esta entrega solicita **A5 independiente, no lo concede**. El revisor tiene que ser una de las otras cinco Tasks JAVIER habilitadas, verificar autoría distinta, requisitos, diff, blobs, pruebas causales y defectos adversariales. La solicitud no crea ni configura una Task, no asigna un revisor por defecto, no inicia un run nativo, no acepta una autorización humana artificial y no modifica `qa`, `main`, PROD ni permisos.

| Microtarea | Estado/fuente previa consultada | Control que debe revisar una Task diferente de su autor |
| --- | --- | --- |
| C08-04.M05.01 | LISTO, fuente `9aeadac6fc9dc43f21f6b5fb695f537abe26aba0` | Vitest sobre capacidades solamente informativas, HTML hostil y límites; CI exacta |
| C08-04.M05.02 | LISTO, fuente `e084a06a8f66bc7d528fe7d4834a6991c085ac96` | xUnit Application/DTO y negativas de tenancy/role/errores, sin confundir lector sintético con HTTP real |
| C08-04.M05.03 | LISTO, fuente `7bcdf84a8ae60a2c274c0ccbb37828439ddef629` | xUnit de frontera Application/Infrastructure, proveedor y persistencia aislados, sin MySQL live |
| C08-04.M05.04 | LISTO, fuente `979b0229ea475c3c98f8b5ac4969b17318ab5755` | Chromium de aceptación positiva, tarjetas y ausencia de mutaciones |
| C08-04.M05.05 | LISTO, fuente `3a5aa69c38b716336da150650fdf881ebb5320f8` | Chromium negativos 401/403/503, tenant B sintético, fuga de diagnóstico y no-escrituras |
| C08-04.M05.06 | LISTO, fuente `2239d9a85e9846c8ba5f95f3eb2c955c91e07efb` | Chromium 4/4, Tab desde foco inicial, 320–1280 px y negativas; A5 previa independiente registrada por DEV03, no repetir sin motivo |
| C08-04.M06.01 | LISTO, fuente `30f17b74b163974cb131cbfb85a71aac83456f38`, autor DEV04 | Auditar clasificación de pruebas reales vs mocks con verificación de seis blobs, sin certificar JWT/MySQL |
| C08-04.M06.02 | LISTO, fuente `19a55d1e0b6ac443bd5dd1fb6eb2ecc86b912faf`, autor DEV03 | Verificar seis SHA de commits/24–17 checks y procedencia V5, excluyendo skipped |
| C08-04.M06.03 | LISTO, fuente `945624b13e4021dde444b4fab11dfdb81143e17e`, autor DEV03 | Validar matriz de riesgos, limitaciones y NO_MEDIDO vs código Angular real |
| C08-04.M06.04 | PENDIENTE, **sin SHA entregado en este corte** | No realizar A5 hasta existir ensayo de rollback real, pruebas y estado elegible publicados |
| C08-04.M06.05 | Esta solicitud documental **aún requiere CI y A5 propia independiente** | Su propio autor DEV03 no puede aprobar esta solicitud como A5_PASS |
| C08-04.M06.06 | PENDIENTE con DAG gate de cierre sujeto a predecesores | No desbloquear manualmente, no certificar M06.06 desde este papel |

Los seis SHA M05 y sus checks exactos están enumerados con enlaces y procedimiento reproducible en `docs/v5/c08-04-m06-02-shas-diffs-artefactos.md`. La comparación técnica de mocks reales se documenta en `docs/v5/c08-04-m06-01-auditoria-cobertura-real-vs-simulada.md` y las limitaciones en `docs/v5/c08-04-m06-03-limites-riesgos-decisiones.md`.

## Criterios para futuras revisiones

1. **A0:** el autor y el revisor son TASK_ID diferentes entre las seis Javier. Verificar PLAN H/I, origen, alcance de archivos y requisitos, no tomar texto de BITACORA como aprobación automática.
2. **A1:** leer asserts positivos/negativos/fronteras/errores de las pruebas cuando sean pertinentes, y logs del job GitHub-hosted que efectivamente las ejecutó en el SHA.
3. **A2:** rechazos de tenants/roles/revocación según el cambio; el `tenant-context` interceptado en Playwright **no es JWT HTTP real** ni prueba de aislamiento MySQL. P0/P1 o A5_FAIL sin reparar bloquean PASS.
4. **A3:** exigir runtime real pertinente cuando lo haya. Para este documento R0, A1–A3 de nuevas ejecuciones son **NO_APLICA motivado**: solo se solicitan revisiones, no se implementa código ni se ejecuta MySQL/browser. Pruebas anteriores no son nuevas.
5. **A4:** Scope Lock/V5 exact SHA y blobs; check-run `completed/success` requerido; `SKIPPED/CANCELLED/FAILED/IN_PROGRESS` jamás PASS; BITACORA idempotente y readback. Aplicar candado V17 a cada transición, nunca manipular COLA/DAG G/O.
6. **A5:** registrar `A5_PASS` o `A5_FAIL` verdadero sobre ID y SHA, reviewer distinto; solo entonces recalcular sucesoras. La simple recepción de esta solicitud no es un evento A5 de los elementos de la tabla.

**Riesgos no cerrados:** fuente backend de capabilities no conectada, JWT/RBAC/tenant A/B HTTP real **NO_CERTIFICADO**, MySQL y proveedor real **NO_APLICA** a esta solicitud, rendimiento CWV/p95/p99 y costos **NO_MEDIDO**. Los módulos son tarjetas informativas; no habilitar reservas, membresías, catálogo o sitios por un checkbox o por documentos.

**Rollback:** `git revert` normal de los dos archivos documentales de esta entrega, preservar BITACORA. No implementar scheduler, webhook, secretos, autorizaciones, migraciones ni QA/main/PROD.
