# SOLQARYN V5 — C00-02.M06.03: riesgos, limitaciones y decisiones

**Resultado exigido:** documentar limitaciones técnicas reales, riesgos abiertos y decisiones tomadas del modelo de cola/autoría/estados C00-02. Entrega **documental**, no implementación ni liberación de M06.06, QA/main/PROD. Fuente: [V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN/COLA/DAG/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Gobierno: `solqaryn/Solqaryn`, `dev`, PROJECT_SCOPE_LOCK=STRICT.

**Snapshot inicial**: `1a44eeef8e545e67aef31c42a5044ea3d9a579b1`; `PLAN_MAESTRO!H62:H67=LISTO` para las seis antecesoras M05.01–06; `DAG_TECNICO` de M06.03 `F=VALIDADO;G=SI`, y COLA G/O=SI antes del claim. Son puertas de elegibilidad, **no** certificaciones live. Verificar nuevamente HEAD y Sheet antes de cada cierre.

## Fuentes causales cotejadas

| M05 | SHA original | Alcance demostrado; exclusiones obligatorias |
| --- | --- | --- |
| .01 | `c563ec107fd80327d45e586c364b43f0748934c9` | Reglas puras xUnit; NO CAS/lease Sheets ni MySQL |
| .02 | `97e9f14dc7b78b2890dfaef660afa4dc4785f4f4` | HTTP TestServer con endpoint stub 200/400/409/500; NO JWT real; CI autorización original CANCELLED |
| .03 | `cb2bd3254b0bf4aa8a9fa92cd115f36809f666a7` | Selector + memory adapter; NO proveedor distribuido ni migración |
| .04 | `9214b5190152c4fb76a53b7737905ea9b74b028c` | Test positivo in-memory, blob conservado y probado por SHA causal `bf3b370d1da783d016e5de2753032bdf44830d68`; flags A0–A4 de fixture NO son auditorías institucionales; jobs originales CANCELLED no PASS |
| .05 | `bf3b370d1da783d016e5de2753032bdf44830d68` | Guard rechaza scope/tenant/binding/autor retirado/autorrevisión con fixtures; NO JWT ni datos tenant persistidos |
| .06 | `a061bfc35e8609c6aa6cbb1a4a18d1581538f6d4` | Chromium y UI read-only, 403 y sesión interceptados; NO autenticación live ni MySQL |

Fuentes completas: `docs/v5/c00-02-m06-01-test-sufficiency-audit.md` (blob `f70761725397e275432847a6d510b70d85e0b09b`), `docs/v5/c00-02-m06-02-reproducibility-manifest.md` (blob `b1978b5223c4c6badd105b1fc4c157f49f84703d`). Los jobs citados ya fueron ejecutados en sus SHA históricos: esta microtarea NO los reejecuta ni inventa nuevas pruebas.

## Riesgos abiertos y decisión / dueño para eventual cierre

| Riesgo | Evidencia/límite | Decisión de tratamiento | Owner técnico sugerido |
| --- | --- | --- | --- |
| R01 concurrencia/claims | Adaptador de memoria no excluye dos escritores reales | Exigir PLAN H/I + CAS exclusivo y readback institucional, no liberar despachador | Gobernanza/Sheets |
| R02 JWT/RBAC/tenant A/B | Guards con bindings y fixtures simulados | JWT server-side y denegaciones entre empresas deben probarse sobre API real | Seguridad/API |
| R03 persistencia | Sin provider durable MySQL para cola V5 | No declarar estado duradero/recuperación hasta integración MySQL 8.4 con transacciones | Backend/datos |
| R04 HTTP | TestServer ofrece ruta stub, no endpoint institucional | Mantener etiqueta TEST; exigir consumidor real y auth antes de integración | API |
| R05 interfaz | Playwright Chromium usa sesión/respuesta 403 sintéticas | Certificar UX sólo; no llamar a pruebas E2E de JWT/tenant live | Frontend/QA |
| R06 CI cancelada | SHA original de M05.02/.04 tuvo jobs CANCELLED | No contar como PASS; conservar comparación de blob y CI causal de descendiente | Autor y revisor SHA |
| R07 HEAD/CHANGELOG concurrente | Múltiples Tasks escriben `dev` y encabezados | Actualizar por fast-forward CAS exact-HEAD y section encabezado+viñetas juntas; gate V5 | Autor GitHub |
| R08 independencia A5 | LISTO_DEV y A5_PENDIENTE no son A5_PASS | A5 distinta Task entre las seis Javier; conservar fallos por SHA | Revisor independiente |
| R09 BITACORA y telemetría | Se exigen A:Z 26 campos, EVENT_ID e IDEMPOTENCY_KEY únicos | Append contiguo, RUN_ID sólo si peek lo acredita; no editar historial | Owner institucional |
| R10 fase final | DAG G=SI de M06.03 no equivale a liberar M06.06 | Conservar comprobación propia de aceptación/cierre y dependencia, sin promoción | Owner M06.06 |
| R11 despachos externos | No hay autorización de webhooks de tareas ni cuentas retiradas | Mantener allowlist seis Javier; prohibir dispatcher/run_now autónomo | Gobierno |

## Decisiones ejecutivas y pendientes

- **D01 alcance:** esta entrega registra limitaciones y tratamiento con dueños; **no** corrige R01–R11 ni introduce código, secrets, API, esquema, workers o proveedor productivo.
- **D02 pruebas y clasificación:** `requiere_dotnet=NO`, `requiere_node_angular=NO`, `requiere_mysql=NO`, `requiere_docker=NO`, `requiere_e2e=NO` para el **diff documental**; `requiere_security=SI` por fronteras de autorización/PII/tenancy; `requiere_scope_lock=SI` en SHA publicado. Las pruebas runtime de las fuentes originales NO se confunden con pruebas ejecutadas aquí.
- **D03 auditorías:** A0 diff Markdown/CHANGELOG, secretos, procedencia de encabezados; A1 verificar seis IDs/casos positivos y negativos (in-memory no CAS real; CANCELLED no PASS); A2 JWT/RBAC/tenant y allowlist preservados; A3 consumir manifiesto, auditoría y DAG de seis antecesoras LISTO; A4 comprobar git diff, commit fast-forward, Scope Lock y provenance exact-SHA, PLAN/COLA/BITACORA readback y rollback. A5 pendiente de otra Task Javier, nunca del autor.
- **D04 reversión:** `git revert <SHA_DOCUMENTAL>` ordinario en dev, tras verificar HEAD, sin reescritura de historia, borrar eventos, tocar QA/main/PROD ni datos legítimos. No constituye ensayo de rollback: M06.04 requiere evidencia separada.
- **D05 siguiente acción:** M06.04 ensayar desactivación/rollback; M06.05 formalizar solicitud de revisión independiente; M06.06 gate terminal distinto, nunca liberación por esta nota.

## Matriz de aceptación verificable

- **P1 positivo:** los seis `C00-02.M05.01–06` constan LISTO, sus seis SHA se corresponden con manifiesto/fuentes, cada riesgo tiene decisión y responsable.
- **N1 negativo:** adaptador in-memory **NO** equivale a CAS Sheets ni a MySQL duradero.
- **N2 negativo:** TestServer stub y sesión interceptada **NO** equivalen a JWT/RBAC live ni a prueba tenant A/B persistido.
- **N3 negativo:** `CANCELLED`/`SKIPPED`/`IN_PROGRESS` **NO** son PASS requerido; no usar verde de otro SHA.
- **N4 negativo:** un A5 propio, RUN_ID no verificable, claims simultáneos o desbloqueo M06.06/QA/main/PROD deben rechazarse.

**Salida certificable de este ID:** documento de riesgos/decisiones con fuentes contrastadas; no certificación funcional de integraciones o desbloqueo de fase por inferencia.
