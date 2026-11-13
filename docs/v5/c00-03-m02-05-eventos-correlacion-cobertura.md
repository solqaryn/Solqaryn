# SOLQARYN V5 FINAL — C00-03.M02.05: eventos, idempotencia, correlación y límites de la cobertura integrada

**Tipo:** 1, contrato documental, sin código ejecutable. **Autor:** PRIMARY DEV 02 — JAVIER_CHATGPT; TASK_ID `6af16bb9492c8191b8e8c7d606f11507`. **Ámbito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`.

**Fuente única:** `STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES` V5 FINAL, Google Doc `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; Sheet `1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU` (PLAN_MAESTRO, COLA, DAG_TECNICO, BITACORA). No se reactiva un plan histórico ni se agregan IDs fuera de los 2.340.

**Resultado exigido:** convertir requisitos previos en mejoras, contratos, pruebas y criterios de cierre dentro de la matriz de cobertura V5. Esta unidad M02.05 **define** eventos de cambio de cobertura, claves de idempotencia, correlación con evidencia y límites de volumen. No implementa un dispatcher, webhook, worker, endpoint, cola, transacción SQL, instalación de proveedor ni ejecución entre Tasks. No declara resueltas las 2.340 coberturas.

## 1. Dependencias comprobables y frontera del alcance

El lote `C00-03.M01.01–06` quedó LISTO y `C00-01.M06.06` LISTO en PLAN_MAESTRO durante el preflight. `C00-03.M02.05` estaba PENDIENTE/I vacío, con COLA G/O=SI; no tenía entrada DAG_TECNICO individual VALIDADA y se conservó el gate conservador del lote anterior. `C00-03.M02.04`, de configuración, era trabajo de otra Task y no se modifica ni se presupone completado.

| Fuente de cobertura V5 en dev | Blob SHA leído | Alcance incorporado |
| --- | --- | --- |
| `docs/v5/c00-03-m01-06-cierre-matriz-integrada.md` | `8f83554e0526089c82cf730abb144e26d0b7da66` | Matriz R01–R12 y exclusiones de M01 |
| `docs/v5/c00-03-m02-01-entidades-ownership-invariantes-cobertura.md` | `b6b16dfa1775ef07666d108580f6b21efe9b081a` | Identidad de requisito, mejora, evidencia y dueño |
| `docs/v5/c00-03-m02-02-contratos-api-dto-errores-cobertura.md` | `803685cc695660ce2fadee3175b5d97e40e3f3ab` | Contratos lógicos; no API existente |
| `docs/v5/c00-03-m02-03-persistencia-cobertura.md` | `745e9a3eae0514ee83fe2de12a535fb084548c74` | Modelo propuesto, no migración ejecutada |
| `docs/v5/c00-02-m02-05-eventos-idempotencia-correlacion.md` | `58ee748ae7c83bf801a3ad737f4ec43b145979f5` | Contrato de eventos de estado de las Tasks; distinto de cambios de cobertura |

**Distinción esencial:** C00-02.M02.05 define eventos de la *ejecución/estado* de una microtarea. Este documento define eventos **lógicos de correspondencia requisito↔microtarea** en la matriz integrada. Pueden correlacionarse mediante ID y SHA, pero ninguno es un bus/servicio operante.

## 2. Contrato lógico de evento de cobertura — NO implementado

| Campo mínimo | Tipo / regla | Procedencia y validación |
| --- | --- | --- |
| `event_kind` | `COVERAGE_PROPOSED | COVERAGE_VERIFIED | COVERAGE_GAP_FOUND | COVERAGE_SUPERSEDED` | Discriminante conceptual, no enum compilado |
| `source_ref`, `source_version` | cadenas referenciables | Requisito del documento V5 FINAL, sin convertir V3/V13 en cola |
| `microtask_id` | ID canónico, no nullable | `PLAN_MAESTRO!A` — uno de 2.340 |
| `coverage_key` | `source_ref | source_version | microtask_id | contract_ref` | Identidad lógica estable; misma correspondencia sin duplicado |
| `idempotency_key` | hash/fingerprint de `coverage_key | event_kind | evidence_sha` | Una reentrega idéntica no produce segundo cambio semántico |
| `contract_ref`, `test_ref` | referencia con versión, o `null` | Ausencia explícita → cobertura INCOMPLETA |
| `evidence_sha`, `git_head_sha` | SHA distintos cuando corresponda | Prueba del artefacto y HEAD de consulta nunca se confunden |
| `proof_kind` | DOCUMENTAL / RUNTIME / A5_INDEPENDIENTE / PENDIENTE | Documento no puede etiquetarse como prueba API/DB |
| `owner_task_id`, `reviewer_task_id` | autor de PLAN I y revisor distinto | Sólo seis Task Javier autorizadas actualmente; no suplantación |
| `observed_at_utc` | UTC real de lectura; nullable sin fuente | Prohibido crear marcas de tiempo de runners ficticios |
| `outstanding` | lista de pruebas/contratos faltantes | No presentar como COMPLETO mientras existan entradas |

El contrato no incluye credenciales, JWT, datos personales, secretos, URL firmadas ni payload multiempresa. Los ejemplos no transfieren registros de tenant A hacia B. Si una futura solución almacena eventos, debe definir transacción, índices únicos, límites de retención y aislamiento por empresa, con pruebas .NET/MySQL de integración **antes** de afirmar que existe.

## 3. Correspondencia y correlación sin duplicar ejecución

1. Una fuente R01–R12 puede corresponder a uno o varios IDs V5 existentes; no se crea una microtarea adicional. `coverage_key` distingue las correspondencias, no sustituye `EVENT_ID` de BITACORA.
2. `COVERAGE_PROPOSED` registra únicamente intención de trazabilidad; `COVERAGE_VERIFIED` exige contrato, prueba y evidencia causal de la unidad correspondiente; si falta evidencia, `COVERAGE_GAP_FOUND` mantiene el faltante visible.
3. Dos propuestas idénticas usan el mismo `idempotency_key`; una modificación de evidencia crea una versión **supersesora**, sin sobrescribir la versión antigua. Deduplicación es **requisito de diseño**, no garantía CAS de Sheets.
4. La causalidad real de cada ejecución se extrae de `PLAN_MAESTRO!H/I`, `COLA!E/G/O`, `DAG_TECNICO`, BITACORA A:Z y Git commit/check-runs `head_sha` exacto. La telemetría TAREAS_PROGRAMADAS AA:AN es último reporte, no autoridad de estados.
5. La bitácora física tiene **26 campos** A:Z: EVENT_ID A, TASK_ID G, RUN_ID H sólo nativo, ID de microtarea N, IDEMPOTENCY_KEY R, BASE_HEAD S, RESULT_HEAD T, EVIDENCIA W y TIMESTAMP UTC Z. No truncar a 21, no hacer append duplicado ni modificar eventos existentes.
6. La independencia A5 exige revisor de otro TASK_ID Javier; un A5_FAIL causal anterior no desaparece al cambiar etiquetas. LISTO_DEV no promueve QA/main/PROD sin A5_PASS independiente y autorización aparte.
7. `MAX(24,3*N)` es una meta de inventario con N ejecutores físicos habilitados, no una cuota de eventos, una puerta de DAG o permiso para reclamar. No hay dispatcher V5 desplegado ni garantía de ejecución continua.

## 4. Límite de capacidad y seguridad del futuro consumidor

**Diseño, no activación:** si alguna microtarea posterior implementara consumidor, éste necesitaría validación de payload versionado, autenticación autorizada, cuotas de eventos por origen, deduplicación durable, bloqueo transaccional, backoff acotado, retención, auditoría y compensación/rollback. Sin esa implementación y pruebas efectivas, ninguna Task realiza llamadas a otras Tasks ni declara entrega automática.

- No inferir autorización para cambiar recursos JWT/RBAC/tenant por eventos de cobertura; backend es la frontera de seguridad.
- Conflicto `PLAN!I` / doble escritor / fuente obsoleta / `DAG!F!=VALIDADO` → denegar efecto, conservar diagnóstico, no hacer CAS ficticio.
- `SKIPPED`, `CANCELLED`, `IN_PROGRESS` o SHA diferente en prueba requerida → conservar pendiente, no certificar A4.
- Un evento con información sensible o un ID no canónico → rechazar como evidencia publicable.

## 5. Oráculos documentales de aceptación (no tests runtime)

| Caso | Signo | Entrada documental y resultado exigido |
| --- | --- | --- |
| P01 | + | R03 y un ID V5 existente con contrato y prueba → COVERAGE_PROPOSED único, `coverage_key` estable |
| P02 | + | Mismo `coverage_key/event_kind/evidence_sha` reentregado → una sola identidad semántica; no exigir segundo append |
| P03 | + | Contrato + prueba + evidencia SHA causal verificables → COVERAGE_VERIFIED con proof_kind correcto |
| P04 | + | Evidencia incompleta → COVERAGE_GAP_FOUND con `outstanding` explícito, sin certificar implementación |
| N01 | − | ID 2.341 o source V3 histórico → rechazo como cola/autoridad ejecutable |
| N02 | − | Mismo evento y clave idempotente duplicada → no segundo efecto/append |
| N03 | − | Revisor igual al autor o de cuenta retirada → A5_PASS rechazado |
| N04 | − | G/O=SI pero Q=NO, DAG no comprobado o dependencia incompleta → no despacho |
| N05 | − | RUN_ID inferido del horario o evento A:U/21 columnas → rechazar registro |
| N06 | − | Check-run de SHA distinto/pendiente/skipped → no A4 de ese cambio |
| N07 | − | Documento que afirma handler/DB/CAS/segregación A/B probados sin runtime → no certificación |
| N08 | − | Token real, PII o tenant B en evidencia de A → rechazar exposición |
| N09 | − | Intento de editar QA/main/PROD, secreto, DNS o proveedor de pago → NO_GO |
| N10 | − | Otro owner EN_PROGRESO/VALIDANDO → no escritura concurrente ni relevo sin exclusividad |

Los ejemplos anteriores son **oráculos especificados y revisables**, no reportes de ejecución sobre una cola, MySQL o una API. Las pruebas reales de cualquier implementación futura se hacen en GitHub Actions sobre el SHA de esa implementación y según su impacto.

## 6. Impacto y validación proporcional

**Archivos previstos:** este Markdown y entrada aditiva en `CHANGELOG_AI.md`. **Capas sin cambios:** Domain, Application, Infrastructure, API, Angular, CI, SQL, workers, rutas, DTO y secretos. **Consumidores:** las seis Tasks Javier, lectores de C00-03.M02.01–06 y futuros scopes M03/M05. `C00-03.M02.04` ajeno conserva propiedad. **Riesgos:** doble claim, duplicado de evidencia, fuente obsoleta, identificación de autor, filtración de PII y confusión de pruebas.

`requiere_dotnet=NO`; `requiere_node_angular=NO`; `requiere_mysql=NO`; `requiere_docker=NO`; `requiere_e2e=NO`; `requiere_security=SI_DOCUMENTAL`; `requiere_scope_lock=SI`.

- **A0:** comparar texto/diff con contratos, sin secretos añadidos, encabezados y viñetas con atribución válida; inspeccionar CHANGELOG sin desplazar secciones ajenas.
- **A1:** revisar P01–P04 y N01–N10 como oráculos documentales y el RESULTADO_EXIGIDO M02.05.
- **A2:** comprobar denegaciones por autoría, JWT/RBAC/tenant A/B, PII, proveedor/QA y ninguna nueva autoridad.
- **A3:** verificar los cinco blobs de referencia, predecesor M01.06 LISTO, PLAN/COLA/DAG reales y BITACORA A:Z/26; sin reclamar PASS de .NET/MySQL/Angular/E2E inexistentes.
- **A4:** al publicar `dev` fast-forward con HEAD actualizado, verificar parent/diff, blob, `verify-solqaryn-project-scope` `completed/success` del SHA exacto y readback PLAN/COLA/BITACORA.
- **A5:** otro TASK_ID Javier distinto verifica SHA publicado, caso documental positivo/negativo y rollback; registrar A5_PENDIENTE antes de LISTO_DEV.

**Rollback:** `git revert` normal tras revisar HEAD y consumidores, sin force-push, reset destructivo ni borrado de BITACORA. No hay despliegues, cambios de datos ni promoción a QA/main/PROD.
