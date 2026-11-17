# C09-04.M06.01 — Auditoría causal M05.01–06: pruebas frente a mocks (V19)

Fecha: 17-11-2026. Autor DEV06 (TASK_ID `6af16bbad26881918c283abe06e1f32a`). Repositorio único `solqaryn/Solqaryn@dev`; `PROJECT_SCOPE_LOCK=STRICT`. PLAN_MAESTRO fila 1436. Candado V17 `SQV17_C09_04_M06_01` / `2000001436`.

## A0 — alcance y decisión de evidencia
Microtarea R0 de **revisión documental**: discriminar qué verifican las pruebas del lote previo `C09-04.M05.01–06` y qué sigue sin acreditarse para mensajería multicanal. Cambios exclusivamente en este documento y `CHANGELOG_AI.md`. No se modifica API, frontend runtime, worker, EF, MySQL, contratos, secretos ni integraciones; ninguna revisión documental autoriza SMTP, WhatsApp, webhooks reales, QA, main o PROD. Dependencia del plan: seis M05 en `LISTO` con A5 independiente confirmado en PLAN_MAESTRO, incluida M05.06 (evento `SQV19-J50-C0904M0506-A5PASS-3DD2B7BE-E7012917-20261117-R11`).

## A1 — matriz verificada contra seis SHA y ejecuciones GitHub-hosted

| Predecesora | SHA probado y GitHub Actions | Prueba real y veredicto | Frontera NO demostrada |
| --- | --- | --- | --- |
| [M05.01](https://github.com/solqaryn/Solqaryn/commit/0263060c4f6042e634f26bd5f6c5119babb5bebf) | [44121223928](https://github.com/solqaryn/Solqaryn/actions/runs/44121223928) | 7/7 xUnit de worker outbox: consentimiento, revocación, no duplicación de idempotencia y sin despacho cross-tenant; `completed/success`. | Los repositorios/proveedores sustituidos no demuestran red SMTP/WhatsApp real ni entrega efectiva. |
| [M05.02](https://github.com/solqaryn/Solqaryn/commit/9a0dab0695a8212b29222c68db57ade9a11beffa) | [44124315470](https://github.com/solqaryn/Solqaryn/actions/runs/44124315470) | 8/8 xUnit de controlador/DTO: 400/403/409, tenant A/B, idempotencia y errores opacos; `completed/success`. | Invocación directa del controlador y servicios sustituidos, no HTTP real con JWT firmado ni proveedor conectado. |
| [M05.03](https://github.com/solqaryn/Solqaryn/commit/a44a227f4bc4e5ee6a77762b997afadccdee86e2) | [44126557626](https://github.com/solqaryn/Solqaryn/actions/runs/44126557626) | **3/3 xUnit Category=Integration** y TRX en **MySQL 8.4 efímero**, EF Oracle, aislamiento A/B, CAS/reintentos, rechazo de host/puerto indebidos; `completed/success`. | Integra persistencia aislada, NO Aiven productivo ni webhooks de terceros, JWT HTTP o entrega final. |
| [M05.04](https://github.com/solqaryn/Solqaryn/commit/655a28e7633bd5b3d32a56a8e2e7b7cd4d731323) | [44126350846](https://github.com/solqaryn/Solqaryn/actions/runs/44126350846) | 3/3 xUnit de aceptación de intención outbox: reintentos, dos tenants y aplicación→controlador; `completed/success`. | Repositorio falso; no entrega a proveedor, webhook ni endpoint HTTP autenticado extremo a extremo. |
| [M05.05 corregida](https://github.com/solqaryn/Solqaryn/commit/76ddaee9990cb1dece9af8813c62612915255b8c) | [44129286222](https://github.com/solqaryn/Solqaryn/actions/runs/44129286222) | 11/11 xUnit negativos: suplantación empresa/rol, revocación, replay/payload distinto, rechazo de caracteres de control y repositorio malicioso; `completed/success`. | La corrección de aserción Moq sobre SHA previo `443707d9` no transforma mocks en prueba HTTP/JWT/MySQL real. |
| [M05.06](https://github.com/solqaryn/Solqaryn/commit/3dd2b7be2787c87dcf021eecd628e7238503217d) | [44140941910](https://github.com/solqaryn/Solqaryn/actions/runs/44140941910) | **4/4 Playwright Chromium reales** contra Angular localhost: reflow 320/375/768/1280, Tab/Enter, 403 revocación simulada, 503/error y ausencia de escrituras; `completed/success`. | La sesión HTTP es interceptada, no demuestra JWT servidor; umbral DOMContentLoaded de 20 s es de regresión local, NO SLO de producto. |

Los seis jobs de prueba dirigidos fueron consultados en GitHub Actions como `completed/success`, con logs que registran respectivamente 7, 8, 3, 3, 11 y 4 tests aprobados. Para cada SHA fuente se comprobó `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacíos ni atribución cruzada` en `completed/success`. Los jobs auxiliares `SKIPPED` y los checks de otros scopes `IN_PROGRESS` **no se contabilizan PASS**. Las seis transiciones a LISTO y sus seis A5 independientes constan en el plan; A5 nunca autocertifica al autor.

## A1 adversarial — falsos positivos que se rechazan
- N1: `xUnit 7/8/3/11 PASS` con mocks de proveedor o repositorio NO equivale a integración de entrega o contrato HTTP con JWT firmado.
- N2: `MySQL 8.4 Category=Integration 3/3` demuestra persistencia de ensayo efímera, NO proveedor externo/Aiven real, ni webhooks firmados y replay remoto.
- N3: `4/4 Chromium` no demuestra revocación server-side ni permisos persistidos porque la sesión se intercepta.
- N4: la corrección del Moq de M05.05 se acredita por SHA probado `76ddaee9`, no por SHA previo `443707d9` sin equivalencia.
- N5: `SKIPPED`, `CANCELLED`, `QUEUED`, `IN_PROGRESS` o `FAILURE` nunca equivalen a PASS de un control requerido.
- N6: seis M05 `LISTO+A5` habilitan **esta auditoría documental**, no liberan automáticamente los objetivos amplios M06 o producción.

## A2/A3 — riesgos, límites y decisiones
**R01 proveedores reales:** SMTP/WhatsApp/adaptadores de terceros no ejecutados en esta auditoría; `NO_MEDIDO`. **R02 webhooks:** firma, reintento, replay, caducidad y recibos de terceros no acreditados extremo a extremo; `NO_MEDIDO`. **R03 privacidad:** la ausencia de fuga en fixtures/controlador/Chromium no certifica tratamiento PII en envío real, logs de proveedor o retención; `NO_MEDIDO`. **R04 deduplicación:** CAS e idempotencia aislados sí ejercitados; duplicados ante red real, callbacks y concurrencia de proveedor no certificados. **R05 seguridad:** denegaciones tenant A/B en tests y MySQL aislado no equivalen a JWT firmado y RBAC en endpoint HTTP completo. **R06 rendimiento/accesibilidad:** cobertura acotada Angular/Chromium, no WCAG global ni SLO producción.

Decisión D01: aceptar únicamente la **revisión R0 de causalidad** y sus enlaces directos, no declarar cobertura global del lote. D02: separar tests con mocks de tests con MySQL 8.4/Chromium ejecutados. D03: mantener abiertos riesgos de webhook, replay, PII y duplicación real para las siguientes unidades autorizadas del plan. D04: no inventar integraciones/credenciales ni habilitar QA/main/PROD o cambiar DAG/COLA manualmente. D05: requerir A5 por Task Javier distinta de DEV06 antes de cualquier gate posterior que la exija.

A2 y A3 de nuevas rutas/DB/HTTP son `NO_APLICA` a este diff exclusivamente documental: se auditan resultados previos sin fingir repetición o ampliación de integración. Ningún dato productivo tocado.

## A4/A5 — cierre y reversión
Publicar exclusivamente documento + sección autocontenida de CHANGELOG mediante fast-forward CAS al HEAD vigente. Verificar Scope Lock y procedencia V5 `completed/success` del SHA propio, más readback PLAN/COLA/DAG/BITACORA, sin usar check de otro SHA como evidencia automática. `A5_PENDIENTE`: revisión por otra Task Javier de distinto ID; conservar autoría de cada antecedente. Rollback: `git revert` normal del commit documental con HEAD y concurrencia revalidados; sin force-push ni reescritura. No autoriza liberar M06.06 o entornos productivos.
