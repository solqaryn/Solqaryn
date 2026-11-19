# C07-04.M06.05 — Solicitud de revisión técnica independiente del lote (V22)

PLATFORM=SOLQARYN
REPOSITORY=solqaryn/Solqaryn
BRANCH=dev
PROJECT_SCOPE_LOCK=STRICT
MICROTAREA_ID=C07-04.M06.05
AUTHOR_TASK_ID=6adc2e4bd8a48191ac7d8641ed95a1a3
RISK=R0_DOCUMENTAL
A5=SOLICITADA_A_OTRA_TASK_JAVIER_NO_AUTOFIRMA

## A0 — Acción atómica, ámbito y dependencias

La acción literal de PLAN_MAESTRO!D1152 es **«Solicitar revisión independiente del lote cuando sea un gate»**. El entregable de esta microtarea es un paquete trazable de solicitud y criterios de revisión para el lote C07-04 (pedidos, cotizaciones, facturas, caja, pagos, anulaciones y exportaciones); **no es** una aprobación independiente, restauración, emisión fiscal, cobro, despliegue ni cierre de la meta acumulativa PLAN!E («Completa M06»). Este commit agrega únicamente el presente documento y una sección aditiva de CHANGELOG_AI.md. No cambia Domain/Application/Infrastructure/API, DTO, Angular, EF/MySQL, controles JWT/RBAC/tenant, workflow, secretos, permisos ni datos.

El DAG exige predecesores del M05 con A5 genuina para las acciones M06. El 19 de noviembre de 2026, la lectura de PLAN mostró M06.01, .02 y .03 LISTO; sus A5 documentales independientes constan en BITACORA. M06.04 (ensayo rollback), M06.05 (esta solicitud) y M06.06 (cierre de etapa) **no** quedan certificados por el presente paquete. El gate técnico de M06.06 continúa condicionado por sus predecesores y A5, mediante fórmulas vivas de DAG_TECNICO y COLA. La solicitud no debe adelantar ninguna puerta ni convertirse en aprobación humana rutinaria.

## Evidencia verificable para el revisor independiente

| Entrega auditada | SHA de origen | Prueba o alcance conocido | Límite explícito |
| --- | --- | --- | --- |
| C07-04.M05.01 | `859b6575f584c416a8074b2ca140b15f1776e78c` | Vitest 5/5 de regla y servicio | HTTP, identidad y permisos simulados |
| C07-04.M05.02 | `ac6e42ac5b25ef86514fe5bc818d1778a0a3f35a` | xUnit 14/14 de contrato y controladores | Moq Strict; no petición autenticada real |
| C07-04.M05.03 | `4e0ee17ae7a48da368f21e801e378ddb83e6e2c9` | 4/4 integración MySQL 8.4 efímero, aislamiento e idempotencia | No PSP, JWT HTTP ni transacción integral de negocio |
| C07-04.M05.04 | `c9d6ad8275f9a6217c50fa3017ffae420a73797d` | 4/4 Chromium de navegación y UX con fixtures | Sesión y venta simuladas |
| C07-04.M05.05 | `d976f5d8f603dae350dbfcd35abf7d233aeb2256` | xUnit 9/9 de negativos de filtro/permisos | IPermisoService simulado, no revocación por HTTP |
| C07-04.M05.06 | `0f78a8f783c6afcf47e9a92d21fc67d178673683` | 15/15 Chromium reales, lint/build, Scope Lock/V5, [run 44607415955](https://github.com/solqaryn/Solqaryn/actions/runs/44607415955) | La comprobación global «Módulos, cargas, sesión, catálogos, variantes, accesibilidad, facturación y correo» quedó **completed/failure** en ese SHA. No atribuir PASS global ni pagos reales |

Las cifras anteriores representan **evidencia histórica auditada y no pruebas reejecutadas por esta solicitud**. Su origen son los documentos y bitácoras de M05/M06. Para verificar diferencias, blobs y pruebas, consultar el SHA y las comprobaciones reales, no un contador autorreportado.

Documentos de apoyo en el repositorio:
- [M06.01 — pruebas reales versus mocks](c07-04-m06-01-auditoria-pruebas-vs-mocks.md), commit `0a57a45fb80edbadc7e1deaa07c6877772bd9d79`, auditado A5 de forma independiente por DEV03 para el **documento R0**, sin certificar negocio R2/R3.
- [M06.02 — versionado y artefactos](c07-04-m06-02-version-diff-artefactos-reproducibilidad.md), con blob hashes, CI y fallo global no ocultado.
- [M06.03 — límites, riesgos y decisiones](c07-04-m06-03-limitaciones-riesgos.md), que retiene brechas de identidad, permisos, PSP, contabilidad, rollback y métricas.
- [M05.06 — ejecución dirigida real](https://github.com/solqaryn/Solqaryn/actions/runs/44607415955), con contraste directo frente a la comprobación global fallida del SHA de origen.

## Solicitud A5 formal — revisión independiente, sin aceptación ficticia

Solicitar a **una de las otras cinco Tasks JAVIER autorizadas**, distinta de DEV03 autora de este documento y distinta de cualquier reparador del mismo cambio, la revisión genuina del SHA de entrega de M06.05. No aceptar revisión de la Task autora, no convertir el presente texto en A5_PASS y no utilizar ejecutores ajenos al allowlist. El revisor debe inspeccionar el diff y el requisito D1152, verificar las seis pruebas históricas y sus límites, revisar el registro de M06.01–03, el estado de M06.04 y la evidencia de GitHub Actions en SHA causal. Puede decidir A5_FAIL con defecto P1/P2 concreto; todo FAIL histórico debe conservarse.

La revisión debe responder:
1. ¿Este documento solicita efectivamente una revisión **sin pretender haberla aprobado** ni simular firma del propietario? Confirmar autor, revisores elegibles, event_id e idempotencia.
2. ¿Los 6 SHA y las suites de M05 están identificados y se distingue MySQL/Chromium genuinos de Moq/sesiones sintéticas, sin extrapolar a JWT firmado, membresía server-side, PSP o auditoría fiscal?
3. ¿Se preserva el resultado `completed/failure` del check global histórico del SHA M05.06? Un job dirigido verde no lo sanea.
4. ¿Los estados, autores, revisores A5 y las puertas calculadas del DAG/COLA se leen de Sheets y BITACORA actualizadas, sin forzar «SI»?
5. ¿El SHA propio de M06.05 contiene **solo** documento + CHANGELOG aditivo y tiene `verify-solqaryn-project-scope` y «V5 - Sin encabezados vacíos ni atribución cruzada» en `completed/success`? Las comprobaciones `queued/in_progress/failed/skipped` requeridas **no** cuentan como PASS.
6. ¿Se mantiene M06.06 sin certificación integral ni QA/main/PROD, y los riesgos de rollback, identidad y dinero siguen abiertos hasta pruebas de sus unidades técnicas?

**Salida independiente requerida**: un evento idempotente `A5_PASS` o `A5_FAIL` en BITACORA con Task revisora diferente, SHA fuente, comprobaciones e inspección causal; readback PLAN K/L, COLA y DAG bajo lock V17 por ID, preservando I del autor. Los demás hitos de M06 dependen de sus propios gates. Este documento no crea aprobación humana adicional ni workflow permanente.

## A1–A4 del cambio documental y reversión

- **A0:** compara D1152, alcance R0, dependencias y fuentes; no añade ruta/API/servicio.
- **A1:** la matriz y los seis criterios negativos detectan confusiones de mocks versus integración, gates/autoría y CI fallida; no se presenta test nuevo de runtime.
- **A2:** `NO_APLICA` ejecutar JWT/RBAC/tenant en este diff **solo documental**. La autorización comercial pendiente no queda aprobada.
- **A3:** `NO_APLICA` ejecutar MySQL, Playwright, PSP, HTTP y rollback de datos aquí. Son pruebas pendientes únicamente cuando PLAN D de otra unidad técnica las requiere; M06.04 debe tratar su propio ensayo.
- **A4:** exigir GitHub Actions `verify-solqaryn-project-scope` y check V5 `completed/success` para SHA exacto del commit de este documento, diff correcto y readback H/I/K/L/M, BITACORA, COLA/DAG tras cada transición. A5 separada.
- **Reversión:** un `git revert` normal del commit documental en dev previa revalidación de HEAD/escritores, sin force-push ni borrado de BITACORA. No reescribir historia, no tocar qa/main/PROD, no secretos ni pagos.
