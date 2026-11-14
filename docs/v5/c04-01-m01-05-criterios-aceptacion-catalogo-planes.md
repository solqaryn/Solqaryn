# SOLQARYN V5 — C04-01.M01.05: criterios de aceptación funcional y técnica del catálogo de planes

**Microtarea:** `C04-01.M01.05`, PLAN_MAESTRO fila 582. **Task autora:** PRIMARY DEV 02 — JAVIER `6af16bb9492c8191b8e8c7d606f11507`. **Rama:** `solqaryn/Solqaryn@dev` con `PROJECT_SCOPE_LOCK=STRICT`. **Fecha:** 2026-11-14. **Naturaleza:** especificación de aceptación M01; no modifica productos SaaS ni datos reales.

## 1. Objetivo, autoridad y alcance acotado

Autoridad: [STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit), sección `C04-01` y `PLAN_MAESTRO!A582`; Sheet de estados, elegibilidad y BITACORA `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU`. Preflight de `C04-01.M01.05`: `COLA G/O=SI`, `DAG_TECNICO!F543=VALIDADO/G543=SI`, PLAN PENDIENTE sin owner y claim CAS posterior. La condición de entrada habilita **redactar criterios**, no activa M01.06/M02 ni operaciones de facturación.

**Resultado de producto esperado a futuro:** planes SaaS versionados con capacidades, límites, precios, periodicidad y upgrade/downgrade auditables; ejecución de límites server-side en tenant legítimo. **Resultado real de esta M01.05:** matriz de aceptación con oráculos, condiciones de observación y evidencia requerida para desarrollo futuro. **No hay PASS de las pruebas runtime enumeradas en este documento**.

Dependencias documentales del mismo lote: `C04-01.M01.01` fija objetivos y NR01–NR10, `.02` identifica entradas/actores/superficies y `.03` inventaría código actual; `.04` registra riesgos R01–R12, supuestos S01–S08 y decisiones D01–D08 **pendientes**. Los criterios no suponen que estas decisiones hayan sido aprobadas.

## 2. Base técnica leída y restricciones de evidencia

| Superficie | Evidencia de código disponible | Certificación NO inferida |
| --- | --- | --- |
| Domain | `SuscripcionesSaaS.cs` blob `d9b5dbe19fb728293b00b748f10ac10336debd2d`: Plan, PlanLimite, Suscripcion | No se probó contrato de moneda, precio, versión comercial o impuestos |
| Application | `SuscripcionesSaaSService.cs` blob `bb501285f11cd4b32709a58a2c51efa19c3664ea`: tenant antes de repository, replay/conflicto | No garantiza autenticación end-to-end ni concurrencia MySQL |
| API | `SuscripcionesSaaSController.cs` blob `cab95db5a33c6a15448e2437481eaaac25ffc4fe`: Authorize + RequierePermiso | No crea rol de edición global del catálogo |
| EF | `SuscripcionesSaaSConfiguration.cs` blob `16631a0155a449ab71e7479c92e8ba8804303249` | Índices/config no demuestran migración aplicada en servicio real |
| Tests | `SuscripcionesSaaSServiceTests.cs` blob `090bf81e7a8f08e86dcdc7ddf5b0c20625ae2839` | Moq xUnit leído, no ejecutado aquí |
| UI | `suscripcion-saas-card.component.ts` blob `7909433c414089b89b87ebff20017c2704a7f085` | Lectura de plantilla no certifica accesibilidad/Playwright ni precios |

## 3. Matriz de aceptación — contratos de ejecución FUTURA

| Criterio | Oráculo positivo observable | Prueba negativa, protección y evidencia exigida |
| --- | --- | --- |
| AC01 — versión de plan | Un plan aprobado expone código, versión inmutable y fecha de vigencia consistente | N01: edición retroactiva de versión referenciada por suscripción se rechaza; test Domain/DB de historial |
| AC02 — capacidades | Límites y módulos provienen de fuente única por versión y tenant | N02: capacidad fuera del plan y permiso RBAC ausente se deniegan, aun con UI alterada |
| AC03 — límites | Nombres normalizados, null ilimitado definido, valores positivos | N03: 0, negativos, duplicados y clave inválida rechazan determinísticamente |
| AC04 — precios/comercial | Una moneda, impuesto, tarifa y periodicidad explícitamente aprobados se representan sin ambigüedad | N04: precio/moneda/impuesto sin aprobación no crea cargo o configuración activa |
| AC05 — upgrade/downgrade | Cambio autorizado conserva historial del tenant, vigencia y motivo auditable | N05: downgrade destructivo, suscripción cancelada o precio retroactivo se bloquean |
| AC06 — autoridad tenant | Tenant A autenticado con membresía vigente ve solo su propio contrato | N06: tenant B en ruta/claims/servicio, usuario revocado o sin JWT es denegado antes de I/O |
| AC07 — RBAC independiente | Usuario con acción permitida consulta o muta sólo según rol real | N07: entitlement comercial se usa para elevar `Configuracion/Administrar`: rechazo |
| AC08 — idempotencia | Dos solicitudes equivalentes con misma clave producen una sola alta | N08: mismo key payload distinto o carrera concurrente entre requests produce conflicto/ganador único |
| AC09 — consistencia MySQL | Índices/FK y cambio aditivo preservan suscripciones referenciadas | N09: migración con drop/cascade o pérdida de historial aborta; CI MySQL efímera y revert |
| AC10 — integración HTTP | Contrato de errores estable, paginación válida y problemas 400/403/404/409 tipados | N10: sin key, entrada malformada, permiso insuficiente o registros tenant B nunca devuelven datos |
| AC11 — UI accesible | Estado carga/error/vacío, labels, contraste y teclado coherentes con plan y límites | N11: interfaz concede permisos localmente, muestra precio no aprobado o filtra tenant B: fallo E2E |
| AC12 — operación resiliente | Reversión funcional/documentada conserva datos y no permite cobros duplicados | N12: caída/timeout/webhook duplicado (cuando se autoricen eventos futuros) no otorga acceso ni cobra |
| AC13 — auditoría y privacidad | Se registra decisión permitida/denegada con tenant, acción, motivo y versión sin secretos | N13: JWT, clave sensible, PII o datos de pago aparecen en logs; fail-closed |
| AC14 — release gate | Cada check causal sobre commit exacto termina completed/success y el readback institucional coincide | N14: queued/in_progress/skipped/cancelled/failed no cuentan como PASS de prueba requerida |

Estas AC/N son **futuras y no ejecutadas**. La condición de observación exige datos de prueba no productivos, casos A/B, aislamiento de tenants, test de regresión y artefactos por SHA.

## 4. Criterios para declarar cada etapa como apta

**Etapa M01 (documental):** objetivos, superficies, inventario y matriz de riesgos/criterios trazados, sin inventar importes ni activar funcionalidades. Cada microtarea se registra y revisa por separado; M01.06 tiene gate especial propio y nunca se libera por esta M01.05.

**Etapa M02 (contratos/modelo, FUTURA):** someter a revisión explícita semántica de versiones, precio, moneda, periodicidad, cuotas y transición; acordar autoridad para administrar catálogo. Documentación preliminar no equivale a autorización de cobro o permisos. Exigir A5 independiente si el gate institucional aplica.

**Etapa M03 (implementación, FUTURA):** entregar código real .NET 10, persistencia MySQL/EF y seguridad JWT/RBAC/tenant conforme a alcance; unitarios positivos/negativos, integración real con MySQL efímero, casos cross-tenant y replay; CI GitHub Actions sobre SHA exacto antes de LISTO DEV. No probar mocks como sustituto de integración cuando esta sea necesaria.

**Etapa M04/M05/M06 (superficies, QA y entrega, FUTURA):** Angular 22.2.1 con pruebas de servicio y E2E Playwright si se requiere; regresión, accesibilidad, auditoría y rollback verificables; no modificar QA/main/PROD sin autorización nueva.

## 5. Evidencia mínima por escenario (no fabricar resultados)

Cada prueba ejecutable posterior debe documentar `MICROTAREA_ID`, repo/dev, SHA publicado, suite/job/workflow, head_sha, status/conclusion, test positivo/negativo, datos de prueba, resultado y rollback. Cuando se compruebe aislamiento A/B, incluir tenant del token y de la solicitud en fixture anonimizado y una aserción **denegado** para cruce.

El trabajo documental M01.05 verifica solamente que cada AC incluya propósito, oráculo y negativo y que cada código probado se anote con límite de evidencia. En este commit ninguna de las AC01–AC14 se cuenta como PASS funcional del catálogo futuro, ni se realizan cobros, upgrades o versiones comerciales.

## 6. Cierre y reversibilidad

**Archivos impactados:** solo `docs/v5/c04-01-m01-05-criterios-aceptacion-catalogo-planes.md` y sección autónoma de `CHANGELOG_AI.md`; sin tocar Domain, Application, EF, API, Angular, mysql, secretos o usuarios. Los consumidores futuros deberán releer los archivos y HEAD actual antes de desarrollar.

**A0–A4 para este documento:** inspección estática del diff, revisión textual de los 14 AC y negativos, integridad tenant/RBAC/JWT y evidencia exacta del commit publicado, `verify-solqaryn-project-scope` y `V5 - Sin encabezados vacios ni atribucion cruzada` **completed/success** sobre su SHA. Las suites generales in_progress son NO_APLICA por Markdown y jamás PASS. Registrar PLAN/COLA/BITACORA y A5_PENDIENTE.

**A5:** Task Javier distinta de la autora; revisión independiente documental, sin autopromoción a QA/main/PROD. **Rollback de la microtarea:** git revert normal no destructivo; nunca borrar BITACORA histórica ni force-push.

**Resultado permitido:** criterios de aceptación definidos. **No** habilita M01.06, M02, migraciones, catálogos con tarifas reales, facturación, suscripciones nuevas, eventos, proveedores, QA o producción.
