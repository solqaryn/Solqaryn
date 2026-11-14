# SOLQARYN V5 FINAL — C00-03.M02.01: entidades, ownership, relaciones e invariantes de cobertura integrada

**Clase:** TIPO 1, especificación documental de la matriz única requisito → mejora → contrato → prueba → evidencia y cierre. **Autor:** SOLQARYN PRIMARY DEV 05 — JAVIER, TASK_ID 6adc2e55f47c81919f6374e561687076. **Único repositorio/rama:** solqaryn/Solqaryn@dev; PROJECT_SCOPE_LOCK=STRICT.

**Autoridades:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL, Google Doc 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA; Sheet 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU (PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA); Skill y Scope Lock del HEAD vivo. **Predecesor:** C00-03.M01.06 LISTO y matriz documental R01–R12 leída en docs/v5/c00-03-m01-06-cierre-matriz-integrada.md. **Preflight:** este ID PENDIENTE, dueño vacío, COLA G/O=SI; no registrado individualmente en DAG_TECNICO, por lo que se aplica puerta conservadora de M01 y no se anticipan gates de M03–M06.

**Resultado de fase M02:** convertir cada requisito previo en mejora, contrato, prueba y criterio de cierre dentro del V5 FINAL con evidencia mínima de matriz integrada; esta M02.01 define **modelo de identidad y ownership**, no llena las 2.340 coberturas ni realiza implementación.

## 1. Entidades del modelo lógico (no se crean tablas)

| Entidad | Clave y autoridad | Relaciones y atributos mínimos |
| --- | --- | --- |
| FuenteRequisitoV5 | referencia/versionado del Doc V5 FINAL | descripción original, riesgo, alcance, no-alcance, procedencia |
| MejoraV5 | ID de microtarea vigente PLAN_MAESTRO!A | objetivo concreto, programa C00–C15, resultado exigido, criterios de salida |
| Cobertura | par (referenciaRequisito, microtareaID, versiónFuente) | justificación causal, estado de correspondencia, no duplicación |
| Contrato | ID microtarea + artefacto Git blob/URI fuente | interfaz/API/DTO/seguridad o regla documental, condiciones pre/post |
| Evidencia | commit SHA, check-run exacto, readback Sheet o vínculo verificable | naturaleza DOCUMENTAL / PRUEBA_REAL / A5, fecha y alcance |
| Prueba | par (microtareaID, tipo prueba, fingerprint) | positivo, negativo, herramienta real cuando aplica y resultado |
| Dependencia | microtarea actual → ID predecesor canónico | DAG_TECNICO y gates COLA G/O calculados; ciclos prohibidos |
| Ownership | PLAN_MAESTRO!I + TASK_ID físico | único dueño efectivo, historia inmutable, recuperación CAS verdadera |
| Estado | PLAN_MAESTRO!H | PENDIENTE → EN_PROGRESO → VALIDANDO → LISTO |
| RevisionIndependiente | (microtareaID, SHA, revisor TASK_ID distinto) | A5_PENDIENTE/PASS/FAIL y alcance, nunca autopromoción |
| EventoAuditoria | BITACORA A:Z, EVENT_ID + IDEMPOTENCY_KEY | 26 campos y append sin truncar, nunca source de autorización |
| ReporteEjecucion | TAREAS_PROGRAMADAS AA:AN por TASK_ID | fotografía de última corrida, no autoridad para modificar H/I |

El vínculo Cobertura permite que **un requisito se descomponga en varias mejoras existentes** y que **una microtarea responda a varias restricciones**, pero no genera nuevos IDs ni otro backlog. El identificador de fuente documental NO reemplaza el ID institucional. No inventar unicidad uno-a-uno si la relación es muchos-a-muchos.

## 2. Ownership y fronteras de autoridad

- La fuente comercial es exclusivamente V5 FINAL; los 2.340 IDs van de C00-01.M01.01 a C15-05.M06.06. Requerimientos de versiones anteriores solo tienen trazabilidad documental, no ejecución separada.
- PLAN_MAESTRO H/I gobierna estado/autoría. COLA E replica H por fórmula; COLA G/O y DAG_TECNICO ofrecen elegibilidad calculada, no reserva ni decisión de seguridad. Una cuota MAX(24,3*N) NO concede elegibilidad.
- Los únicos actores ejecutores son las seis Tasks Javier del allowlist vigente, vinculadas a su identidad real y no a nombres textuales. Un registro histórico de cuenta retirada no le concede nuevas operaciones o revisiones.
- CLAIM: H=PENDIENTE, I vacío, G/O=SI, dependencias reales, capacidad disponible y archivos sin colisión. H=EN_PROGRESO con I=TASK_ID, evento A:Z y readback. **Read-modify-write de Google Sheets por sí solo no es CAS transaccional**; relevo de owner requiere exclusión efectiva y evidencia de ausencia de escrituras simultáneas, o NO_GO.
- VALIDANDO conserva el owner y se alcanza al estar completo el candidato, ANTES de A0–A3. A4 exige commit directo fast-forward y SHA exacto publicado. A5 independiente de otra Task Javier no es condición para LISTO_DEV, sí para promover con autorización nueva.
- Datos de tenant A/B, JWT, BCrypt, RBAC, secretos y PII permanecen fuera del modelo de cola y protegidos por backend; un requisito de IAM nunca se da por resuelto por redactar su correspondencia.

## 3. Invariantes de trazabilidad

| Regla | Invariante y criterio de rechazo |
| --- | --- |
| I01 – Identidad | Cada mejora usa exactamente un ID vigente; si falta, es desconocida, NO crear un 2.341 |
| I02 – Fuente única | No tomar un PR/plan archivado como segunda autoridad de tareas |
| I03 – Cobertura | Una correspondencia contiene requisito, mejora, contrato, prueba, evidencia y estado explícito; ausencia de una pieza se marca pendiente |
| I04 – No doble implementación | Referencias legadas que convergen al mismo resultado no crean dos microtareas ejecutables |
| I05 – Dueño único | H/I reales y readback, sin dos escritores; la evidencia histórica no reabre ownership |
| I06 – Dependencias | Para cualquier gate comprobar COLA G/O y DAG/prerrequisitos; VALIDANDO provisional no libera IAM, DB, cierre de fase, QA/PROD |
| I07 – Calidad | A0–A4 aplicables deben PASS real antes de LISTO_DEV; SKIPPED/CANCELLED/IN_PROGRESS no son PASS requerido |
| I08 – Causalidad Git | Commit dev fast-forward, parent, diff y check-runs head_sha exactamente igual al SHA del cambio |
| I09 – Historial | BITACORA 26 columnas A:Z, claves únicas; no truncar a A:U ni sobrescribir A5_FAIL |
| I10 – Independencia | Revisor A5 distinto del autor, mismo allowlist Javier vigente; no A5 por cuenta ajena |
| I11 – Seguridad | No inferir permisos de cliente, flags o nombre de empresa; verificar backend JWT/RBAC/tenancy cuando hay cambios reales |
| I12 – Migración futura | Hostinger/Cloudflare/R2 son objetivo no autorizado; cero imágenes históricas = migración inexistente NO_APLICA |
| I13 – Cierre terminal | La unidad C15-05.M06.06 requiere las 2.339 anteriores certificadas y gates, sin nueva microtarea posterior |
| I14 – Integridad de alcance | Documento describe una mejora futura pero no afirma implementación, entorno vivo ni pruebas que no se ejecutaron |

## 4. Muestra trazable de cobertura por familias R01–R12 (sin estados inventados)

La matriz documental M01.06 definió doce **referencias**, no doce nuevas microtareas. Esta M02.01 fija cómo se relacionarán con los ID reales al continuar M02/M03:

| Ref | Familia de requisitos | Contrato / prueba exigible al implementador | Riesgo de inferencia |
| --- | --- | --- | --- |
| R01 | Fuente única V5 y 2.340 IDs | comprobar catálogo y PLAN/COLA unívocos | no reabrir otro plan |
| R02 | Identidad y autorización de Tasks | owner real + allowlist + negativo actor ajeno | no usar credenciales de otra cuenta |
| R03 | Concurrencia | un solo claim ganador y evento causal | Sheets no garantiza CAS mediante lectura |
| R04 | DAG y gates | precedentes LISTO, G/O SI, ruta validada | objetivo de reserva no habilita despacho |
| R05 | Validación de stack | .NET/Angular/MySQL según cambio ejecutable | CI genérica no valida cada resultado |
| R06 | A0 estática | diff, secretos, dependencias y contratos | sin bypass antes del commit |
| R07 | A1 y A2 | aceptación positiva/negativa, RBAC y tenant A/B | no usar mock como evidencia live |
| R08 | A3 integración | consumidores, migraciones y jobs exact SHA | no SKIPPED=PASS |
| R09 | A4 publicación | FF dev, parent/diff/check-run exacto, readback | no certificar CI previa |
| R10 | A5 independiente | revisor distinto del autor, SHA y casos | no auto-revisión |
| R11 | Auditoría institucional | A:Z 26 campos, claves idempotentes | no truncar ni inventar RUN_ID |
| R12 | Rollback | revert aditivo, preservación de historia | no force-push ni borrado de eventos |

**Ejemplo de correspondencia**, no ejecución nueva: para R11, el contrato físico está anclado a BITACORA!A:Z; las microtareas C00-02.M02.01–05 del catálogo V5 definen aspectos del control y se vinculan por evidencia, sin duplicar una nueva «R11-Task». Si una solución en C00-02 define eventos, eso no prueba que C00-03, R11 o todos los otros requisitos estén implementados.

## 5. Pruebas de aceptación documental (oráculos, NO runtime)

| Caso | Signo | Entrada/resultado esperado |
| --- | --- | --- |
| P01 | + | Una referencia R01 se mapea a un ID existente con contrato y evidencia → cobertura identificada sin nuevo ID |
| P02 | + | Un requisito afecta dos microtareas V5 existentes → relación muchos-a-muchos documentada sin duplicar trabajo |
| P03 | + | PLAN H/I, COLA E/G/O y DAG coherentes → estado y dueño provienen solo de PLAN |
| P04 | + | Revisión ajena A5 del mismo SHA con caso positivo/negativo → registro independiente sin promocionar entorno |
| N01 | - | Requisito histórico propone crear ID adicional → denegar con fuente V5 |
| N02 | - | Correspondencia sin evidencia o prueba → marcar pendiente, no implementar/certificar |
| N03 | - | COLA G/O SI pero prerequisito real no LISTO → no claim |
| N04 | - | Propuesta de asignación sobre VALIDANDO con dueño diferente → rechazar colisión, exigir exclusión/CAS |
| N05 | - | Un job skipped/otro SHA pretende validar A4 → rechazar |
| N06 | - | Reviewer Task=autor o ajeno a seis Javier → A5 rechazado |
| N07 | - | Evento A:U de 21 campos o clave duplicada → no agregar ni declarar cierre |
| N08 | - | Prueba de documentación pretende certificar API/MySQL/E2E no ejecutados → rechazar |
| N09 | - | Una mejora sobre tenant B se habilita por flag frontend de A → rechazar por RBAC backend |
| N10 | - | C15-05.M06.06 se habilita anticipadamente o con A5_FAIL causal → bloquear |
| N11 | - | Matriz propone cambios de Cloudflare/Hostinger/QA/PROD sin autorización → bloquear |
| N12 | - | Fila de historial externo se interpreta como Task activa → rechazar como autoridad |

Estos son criterios de integridad de la **definición** documental; no simulan llamadas reales a API, ejecución MySQL, autenticación de usuarios o un caso Playwright.

## 6. Impacto, auditorías y rollback

**Componentes:** documentación C00-03 M02 y CHANGELOG aditivo; consumidores lógicos M02–M06 del mismo plan, sin código, formularios, rutas, tablas, migraciones, secretos ni despliegues. **API/DTO:** ninguno ejecutable. **Seguridad:** JWT/RBAC/tenancy A/B y datos sensibles permanecen sujetos a pruebas de implementación, no a esta matriz. **A0:** diff/secretos/enlaces/código muerto no aplicable; **A1:** P01–P04/N01–N12 revisados como contrato; **A2:** límites de actor, A5 y tenant; **A3:** consistencia con M01.01–06 y planes M02, sin cambios ejecutables. Para .NET/Angular/Node/MySQL/Docker/E2E, subpruebas NO_APLICA por cambio documental, no aprobación de tecnología. **A4:** commit directo FF, Scope Lock completed/success del SHA exacto, diff/readback; **A5:** segunda Task Javier con casos causales documentales antes de cualquier propuesta de promoción.

**Reversión de esta entrega:** git revert aditivo del commit documental en dev, sin reset, force-push, modificación de PLAN/BITACORA histórica ni promoción QA/main/PROD. Las relaciones y contratos definidos aquí son **preparación**, no aceptación final de la plataforma universal.