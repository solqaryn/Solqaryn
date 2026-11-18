# C02-02.M06.05 — Handoff de revisión independiente de lote placement shared/dedicated

**Fecha:** 2026-11-18. **Proyecto:** SOLQARYN `dev` exclusivamente, `PROJECT_SCOPE_LOCK=STRICT`. **PLAN:** C02-02.M06.05, fila 360. **Riesgo del diff:** R0 documental; **riesgo del lote auditado:** R2 tenancy, EF/MySQL, autorización y posible R3 de cutover. **Orden V20:** todas las Tasks Javier pueden realizar revisión cruzada sin reemplazar autor histórico; un único escritor por ID bajo candado V17/CAS.

## A0. Resultado de microtarea M06.05

El contrato institucional exige **“Solicitar revisión independiente del lote cuando sea un gate”**. Este registro entrega un handoff técnico reproducible para la **revisión por una Task Javier distinta** del autor de cada artefacto y de su reparador. No constituye una aprobación A5 nueva, no certifica C02-02.M06.06, no adelanta QA/main/PROD y no solicita intervención humana rutinaria. No modifica Domain, Application, Infrastructure, API, MySQL, frontend, migraciones, secretos ni flujos de producción; solo registro y CHANGELOG. Rollback de este diff: `git revert` normal tras verificar HEAD actual, con BITACORA histórica intacta.

## Matriz de revisión M05 con evidencia real

| Microtarea | Autor histórico de código o pruebas | A5 independiente ya registrado en PLAN K (no duplicar) | Pruebas/acreditación y límites que el revisor debe reexaminar |
| --- | --- | --- | --- |
| C02-02.M05.01 | DEV01 `6adbb9fff1548191a1bd5b30d9e9984a` | DEV05 `6adc2e55f47c81919f6374e561687076` | Pruebas xUnit reglas Domain, SHA `a3f3d0bb88863c9cd1697ed2f81cace70e89447f`; no JWT/MySQL |
| C02-02.M05.02 | DEV05 `6adc2e55f47c81919f6374e561687076` | DEV06 `6af16bbad26881918c283abe06e1f32a` | Application/DTO y error seguro, SHA `f6c945a86d255fd7c473ac10c149951b2b2a064a`; lector sintético |
| C02-02.M05.03 | DEV03 `6adc2e4bd8a48191ac7d8641ed95a1a3`; reparación P2 DEV05 | DEV06 `6af16bbad26881918c283abe06e1f32a` | Oracle EF/MySQL 8.4.13 efímero Category=Integration **3/3 PASS** sobre SHA `e3688574566e84e4def5d98fda8ec852d3158a1e`; no row-level enforcement JWT/API. A5_FAIL histórico P2 conservado |
| C02-02.M05.04 | DEV03 | DEV06 | Positive Domain/Application, SHA `c37158e030e7af47b3da975d4e66bd054a61a907`; jamás conceder conexión |
| C02-02.M05.05 | DEV03 | DEV06 | Negativos y error seguro Application, SHA `488aaa40001d0015d8268ae3de47b9212426c6f0`; no RBAC/JWT real |
| C02-02.M05.06 | DEV06 | DEV02 `6af16bb9492c8191b8e8c7d606f11507` | Reglas Domain sintéticas, SHA `2abd96b9f243eb2e499d5eb11b01b72a9cd38bf6`; **2/2 xUnit**; no p95/p99 ni DB/HTTP |

La comparación usa PLAN_MAESTRO H/I/K y BITACORA: cada A5 debe conservar **autor** y **revisor** distintos, test SHA/blobs válidos, defectos históricos y causa de corrección. La existencia de un A5 en PLAN K es fuente de seguimiento, **no** una A5 nueva de esta entrega. Revalidar si cualquiera de esos registros cambia.

## Handoff de puertas M06; no fingir avance

- M06.01: auditoría de límites de los mocks e integración MySQL real; requiere cierre A0–A4 exact-SHA de su documentación y A5 de otra Task distinta de DEV04.
- M06.02: manifiesto documental de artefactos con SHA y referencias; su A5 debe ser de Task distinta de DEV03.
- M06.03: registro de siete riesgos PL-01 a PL-07; A5 distinta de DEV03; ninguna evidencia autoriza runtime.
- M06.04: **PENDIENTE al crear este registro**; exigir rollback/desactivación real en entorno permitido con integridad previa/posterior, evitando tocar cliente o producción; no aceptar una explicación textual como prueba R2, no asignar PASS anticipado.
- M06.05 (este documento): **A5_PENDIENTE** de una Task Javier distinta de DEV04; verificar independientemente seis fuentes, autores y gating de los siguientes pasos. No autoaprobar.
- M06.06: solo su DAG vigente con M05+M06.01–05 LISTO y A5, más los predecesores de fase/bloque, puede permitir evaluación; no es autorización R3, QA/main/PROD o migración.

## A1–A3: verificaciones y límites

**A1 documental:** confirmar seis referencias M05, seis autores/revisores según PLAN K y BITACORA; registrar ausencia o contradicción, no rellenarla con suposiciones. **A2:** para este diff no hay modificación de autenticación, tenant, roles o secretos, por tanto **NO_APLICA al diff**, aunque los gates R2 de JWT firmado, RBAC real, IDOR/BOLA A/B, revocación, fallback server-side y DSN siguen abiertos en implementación. **A3:** no se introduce ni ejecuta integración nueva; MySQL/HTTP/Chromium/EF son **NO_APLICA al diff R0**, no certificados. La integración MySQL M05.03 está acreditada exclusivamente por [job 135157483625](https://github.com/solqaryn/Solqaryn/actions/runs/44404923031/job/135157483625); la regresión sintética M05.06 por [job 135166621371](https://github.com/solqaryn/Solqaryn/actions/runs/44407530043/job/135166621371). No confundir ambas.

## A4–A5: publicación, revisión y salida

Para cerrar esta unidad: commit fast-forward en dev con diff documental, checks exact-SHA `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` completed/success, readback H/I, evento BITACORA único y DAG/COLA sin manipulación. Mientras haya CI necesaria en progreso, conservar VALIDANDO con SHA y owner. A5 será ejecutada por otra Task autorizada, con adquisición exclusiva V17 sobre H:I, revisión independiente y evento idempotente; no registrar `A5_PASS` por este handoff.

**NO_GO específico:** migración/cutover live, secretos, eliminación irreversible, pagos, infra productiva, QA/main/PROD y cualquier interpretación de esta revisión como aprobación comercial. No fingir 100 % de seguridad, no borrar A5_FAIL históricos.
