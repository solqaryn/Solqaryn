# SOLQARYN V22 — C04-01.M06.03: limitaciones, riesgos y decisiones abiertas

**DEV únicamente, rama `dev`, `PROJECT_SCOPE_LOCK=STRICT`**. Microtarea `C04-01.M06.03`, criterio literal `PLAN_MAESTRO!D610`: «Documentar limitaciones, riesgos abiertos y decisiones tomadas». Riesgo del presente cambio **R0 documental**; riesgos comerciales subyacentes **R2/R3**. Autor documental: JAVIER DEV01 (`6adbb9fff1548191a1bd5b30d9e9984a`). Baseline para este registro: `a532bbf3bb2723b11e2ba51dec7247d5d171a194` (2026-11-21). No supone que `E610` «Completa M06» sea certificación automática de toda la fase.

## Hechos comprobados y alcance real

Las seis entregas `C04-01.M05.01–06` poseen pruebas *dirigidas* y sus revisiones independientes A5 de desarrollo, pero sus contratos son distintos y no deben agregarse como una aprobación de negocio. [M06.01](./c04-01-m06-01-test-scope-audit-v22.md) distingue mocks de integraciones genuinas; [M06.02](./c04-01-m06-02-version-diff-artifacts-v22.md) contiene seis SHAs, jobs, artefactos y reproducciones. Esta microtarea **solo registra** las limitaciones, decisiones y riesgos restantes; no implementa precios, motor de suscripción ni cobros.

## Registro de riesgos y señales de cierre

| Clave / riesgo | Evidencia y alcance *actual* | Señal exigible para mitigar / no declarar PASS prematuro |
| --- | --- | --- |
| R01 / **P1** tarifa y vigencia no aplicadas server-side | UI de referencia y TestServer de DTO no exponen un servidor de tarifas de suscripción comerciales | Pricing autorizado desde fuente/versiones institucionales, negativas por vigencia, tenant y rangos, API firmada, xUnit/HTTP real |
| R02 / **P1** cuotas y capacidades solo descritas | Invariantes TestBed no prueban cuotas bajo concurrencia de clientes | Enforce transaccional con IdempotencyKey, límites por tenant y plan, doble gasto/reintentos, integración MySQL 8.4 aislada |
| R03 / **P1** cambios de plan y prorrateo no evaluados | No hay máquina comercial activada por M05.01–06 | Reglas de upgrade/downgrade/renovación y cálculo auditable positivo/negativo/frontera con rollback, sin procesar fondos reales |
| R04 / **P0** pago/PSP real NO autorizado (R3) | M05.05 prueba HTTP JWT y MySQL efímero, **no** contratos del procesador | Autorización R3 específica, sandbox formal, webhooks firmados/idempotentes, reconciliación y no fuga de secretos; nunca producción sin permiso |
| R05 / **P1** JWT/RBAC real confundido con fixture | M05.01/M05.04/M05.06 usan sesiones sintéticas; M05.02 usa actor sintético; M05.05 sí cubre JWT firmado solo para sus negativos dirigidos | Pruebas backend firmadas/expiradas/revocadas, roles sin permiso, tenant A→B, IDOR/BOLA y ausencia de filtración en recursos comerciales reales |
| R06 / **P1** MySQL 8.4 de laboratorio elevado a base comercial | M05.03 prueba transacción/rollback y M05.05 negativa firmada sobre MySQL efímero | Modelo/índices/migraciones reversibles, estados de suscripción, tenant y auditoría persistentes con 0 fallos y rollback de esquema en CI |
| R07 / **P2** accesibilidad declarada demasiado amplia | M05.06 usa Chromium real con Tab/Enter, foco y cuatro anchos, pero sin evaluación WCAG integral | Auditoría automatizada/manual de contraste, lectores de pantalla, gestión de foco y estados de error; no afirmar WCAG AA total |
| R08 / **P2** rendimiento de CI confundido con experiencia real | M05.06 comprueba DOM/timing en GitHub-hosted, sin hardware/dispositivos ni métricas de campo | Core Web Vitals con metodología autorizada, conjunto de dispositivos y límites medidos; marcar NO_MEDIDO si falta laboratorio |
| R09 / **P1** evidencias obsoletas de otro SHA | GitHub Actions pueden tener jobs failed/cancelled/queued; un build no acredita todos los contratos | Correlación por SHA y blobs, trabajos obligatorios *completed/success*, scripts dirigidos y logs, histórico FAIL intacto, readback Sheets |
| R10 / **P1** liberar DAG o A5 sin independencia | `LISTO` de cada microtarea no es A5 ni permiso QA/PROD | A5 por Task Javier distinta del autor/reparador, namedRange V17, CAS único, K/Bitácora genuinos, DAG solo fórmulas |

Las claves P0/P1/P2 son **prioridad de investigación**, no afirmación de vulnerabilidad ya explotada. Las mitigaciones requieren microtareas concretas elegibles del Plan y no son trabajos implícitamente autorizados por este documento.

## Decisiones ya tomadas para este entregable

- **D01 — No activar cobros, PSP, webhook financiero ni entornos qa/main/PROD.** El contrato R3 permanece NO_GO sin autorización separada.
- **D02 — Documentar sin alterar runtime, test, migraciones o CHANGELOG_AI.md**. La evidencia de historia queda íntegra, y solo se añade este Markdown bajo un commit serializado/FF-CAS en `dev`.
- **D03 — Tratar como específicos los checks de cada SHA**, nunca llamar PASS a `SKIPPED`, `CANCELLED`, `FAILURE`, `QUEUED` o `IN_PROGRESS`; jobs no aplicables al diff R0 se documentan como NO_APLICA con causa.
- **D04 — Mantener los propietarios históricos en PLAN I y los fallos/A5 pasados en BITACORA.** Revisiones nuevas A5 requieren Task distinta.
- **D05 — Bloqueo de sucesoras**: `C04-01.M06.06` no puede declararse LISTO ni liberar siguientes fases por documentos M06.01/02/03 en solitario; su DAG exige M06.01–05 y A5 según fuente calculada.
- **D06 — No presentar evidencia de simulador como integración real.** Angular fixture, HTTP TestServer con actor inyectado, MySQL efímero y proveedor de pagos real son categorías diferentes.

## Decisiones aún pendientes

- **PEND-01:** fuente contractual aprobada de precios, impuestos, periodicidad, límites y vigencias; dueño de producto/alcance requiere evidencia en fuente autorizada, no inferida de tests.
- **PEND-02:** alcance de transacciones y migraciones que demuestran aplicación server-side y rollback sin afectar producción; solo bajo Task R2 elegible y CI efímera.
- **PEND-03:** suite de accesibilidad/dispositivos real y estándares específicos; hasta entonces la evaluación de campo se registra **NO_MEDIDO**.
- **PEND-04:** integración de PSP/QA/PROD futura: queda **NO_AUTORIZADA_R3**, no se solicita ni simula en este DEV run.
- **PEND-05:** completar cierres A0–A4 y revisiones independientes A5 de M06.01–05; no sustituir la fuente DAG por texto ni forzar `COLA!O=SI`.

## Verificación del documento A0–A4

**A0**: requisito D610 y comparativa de fuentes M05/M06, componente, riesgos y dependencias; solo Markdown nuevo. **A1**: diez riesgos con criterios falsables, seis decisiones registradas y cinco pendientes; negativos de certificación falsa. **A2**: clasificación y límites JWT/RBAC/tenant sin modificar mecanismos de seguridad. **A3**: **NO_APLICA** ejecutar MySQL/HTTP/Chromium nuevo porque no se ha cambiado runtime; los tests ya ejecutados conservan su propia fuente/alcance. **A4**: verificar commit FF/CAS, `verify-solqaryn-project-scope` exact-SHA y otros checks causales pertinentes; BITACORA append-only + PLAN/COLA readback, reservar `A5_PENDIENTE` para Task distinta de DEV01. Si falta check obligatorio, dejar `VALIDANDO`. Los riesgos y decisiones no desbloquean G/O.

**Deshacer sin pérdida:** un `git revert` normal del commit documental, nueva CI y readback; nunca reescribir historial ni BITACORA, modificar fórmulas ni borrar datos reales. Resultado final del alcance D610: registro verificable de limitaciones, riesgos y decisiones, **no** cierre comercial de la fase.
