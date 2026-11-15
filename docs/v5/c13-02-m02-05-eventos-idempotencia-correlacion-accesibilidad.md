# SOLQARYN V17 — C13-02.M02.05: eventos conceptuales, correlación e idempotencia de accesibilidad

**Microtarea:** C13-02.M02.05, fila PLAN_MAESTRO 1920. **Autor:** PRIMARY DEV 06, TASK_ID `6af16bbad26881918c283abe06e1f32a`. **Clase:** R0 documental, sin runtime. **Repo:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. Fuentes: STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y su V17, calidad V15 y pipeline V16.

## Decisión y fronteras

Definir nombres, esquema conceptual, correlación, deduplicación, privacidad y reglas de emisión de eventos para **evidencia futura** de interacciones accesibles, errores, foco, teclado, contraste, reflow, pantalla lectora, resultados de pruebas y recuperación. **Ningún evento está implementado o emitido en esta microtarea**: no hay workers, webhooks, scheduler, broker, publicaciones HTTP, nueva API, EF/MySQL, Angular, proveedores, secretos o infraestructura. No existe prueba WCAG certificada ni resultados de navegador aquí.

Predecesores: M01.01–06 y sus A5 pertinentes (incluye `docs/v5/c13-02-m01-06-gates-rollback-liberacion-accesibilidad.md`), M02.01 `docs/v5/c13-02-m02-01-modelo-conceptual-accesibilidad.md` (entidades y invariantes), M02.03 `docs/v5/c13-02-m02-03-persistencia-indices-migraciones-fixtures-accesibilidad.md` (persistencia **propuesta**, no MySQL), M02.04 `docs/v5/c13-02-m02-04-configuracion-tipada-flags-accesibilidad.md` (flags **propuestos**, no controles activos). M02.02 se verifica como scope independiente con owner/estado/DAG propio; este documento no lo reemplaza. La dependencia técnica de M02.06 sigue gobernada por A5 de M02.01–05 y reglas de otras fases/bloques.

## Doce familias de eventos futuros — NO IMPLEMENTADOS

| Evento conceptual | Carga mínima propuesta, siempre redactada | Consumidor futuro y límite |
| --- | --- | --- |
| A11Y.SurfaceVisited | surface_key, revision, tenant_scope, timestamp | telemetría agregada; no guardar URL con query/tokens |
| A11Y.FocusTransitionObserved | source_control, target_control, expected? | prueba teclado con SHA; no grabar contenido de formulario |
| A11Y.KeyboardSequenceChecked | sequence_key, case_key, observed_code | evidencia de Tab/Enter/Escape, no eventos globales capturadores de texto |
| A11Y.DialogFocusRestored | dialog_key, return_control, result | pruebas de modal; no incluir contenido de diálogo |
| A11Y.FormErrorAnnounced | field_semantic_key, message_category, result | solo categoría redactada; nunca valores de usuario |
| A11Y.ReflowChecked | viewport, zoom, case_key, result | matriz browser real futura; no declarar PASS desde una captura |
| A11Y.ContrastMeasured | theme_key, contrast_metric?, standard_version? | resultado solo si medición real/autoridad; NO_MEDIDO si no hay |
| A11Y.ReducedMotionChecked | preference_state, case_key, result | no revelar preferencias personales identificables |
| A11Y.AssistiveReaderReviewed | reader_profile, language, evidence_digest | pruebas humanas/futuras; no atribuir conformidad universal |
| A11Y.AuthorizationDenied | reason_category, surface_key, tenant_scope | sin actorID/PII/tokens y sin datos de otro tenant |
| A11Y.EvaluationCompleted | run_key, source_sha, verdict, test_count | PASS únicamente si pruebas reales con denominador y evidencia |
| A11Y.RollbackVerified | change_sha, checkpoint_key, outcome | evidencia de recuperación; nunca despliegue automático |

**No existe ningún webhook o worker autorizado para generar o propagar estos eventos.** El contrato no habilita consumidores reales ni une Tasks programadas mediante llamadas entre ellas.

## Envelope conceptual y reglas de partición

Envelope propuesto: `event_id: UUID`, `schema_version: semver`, `event_type: allowlist`, `timestamp_utc`, `source_sha`, `run_key?`, `correlation_id`, `causation_id?`, `tenant_scope: PUBLIC|TENANT`, `tenant_key?`, `surface_key`, `case_key?`, `result: PASS|FAIL|NOT_RUN|NO_MEDIDO`, `payload_redacted`, `payload_digest`, `idempotency_key`, `retention_policy_id`. El `tenant_key` será obligatorio para eventos privados y deberá comprobarse en la capa de autorización backend si alguna implementación lo necesitara. No incluir JWT, cookies, IP completa, texto digitado, capturas crudas, PII, secretos, SQL o contenido empresarial en logs.

**Idempotencia:** clave futura `(tenant_scope,tenant_key?,run_key,event_type,surface_key,case_key?,attempt)` o UUID definido por emisor confiable; consumidor no puede confiar sólo en timestamp. Duplicados se reconocen sin duplicar veredictos o artefactos. **Orden:** correlation_id agrupa acciones de la misma prueba, causation_id vincula transición a operación, revision impide aceptar eventos tardíos como estado vigente. **Retención:** TTL definido por autoridad, cifrado en reposo y control de acceso; no asumir proveedor, frecuencia ni cuota disponibles.

## Invariantes de seguridad y causalidad

1. I01: un evento de TENANT_A no puede consultarse desde TENANT_B, ni mezclarse en resultados agregados con filtrado defectuoso.
2. I02: actor revocado o rol insuficiente no emite ni consulta evidencias privadas.
3. I03: `PUBLIC` nunca permite incluir `tenant_key`, ID de recursos privados ni sus contenidos.
4. I04: evento nuevo que carece de source_sha/case/evidencia no puede subir a PASS.
5. I05: NOT_RUN, NO_MEDIDO, CANCELLED, SKIPPED e IN_PROGRESS jamás equivalen a PASS.
6. I06: repetir el mismo idempotency_key no duplica auditoría ni incrementa métricas de pruebas.
7. I07: correlación no autoriza replay de eventos fuera de tenant, usuario o versión.
8. I08: eventos tardíos o fuera de orden no sobrescriben veredicto sellado o versión posterior.
9. I09: todos los payloads se validan contra allowlist de schema, tamaño, versión y tipo.
10. I10: ninguna telemetría transmite JWT/cookies/PII, credenciales o datos de formularios.
11. I11: toda excepción de consentimiento/retención tiene autoridad documentada, vigencia y registro.
12. I12: cambios de esquema tienen compatibilidad versionada; consumidores desconocidos rechazan evento inseguro.
13. I13: `EvaluationCompleted` depende de evidencia de `run_key` y test_count real con denominator, no resumen inventado.
14. I14: cualquier integración posterior se prueba con timeouts, dead-letter, reintentos y backpressure sin duplicar escritura.
15. I15: Domain/Application no se convierten en productor HTTP paralelo sin arquitectura aprobada.
16. I16: el registro institucional A0–A5/BITACORA no puede sustituirse por eventos operativos inventados.
17. I17: release de módulo no depende de un evento `A11Y` cuya prueba o owner sea desconocido.
18. I18: evidencias de accesibilidad nunca alteran permisos JWT/RBAC ni ownership de recurso real.

## Casos de aceptación futuros (no ejecutados)

| Caso | Positivo a probar | Negativo que debería fallar |
| --- | --- | --- |
| P01/N01 | A emite/consulta evidencia A | B lee evento o finding de A |
| P02/N02 | rol vigente emite permiso válido | actor revocado emite evento privado |
| P03/N03 | mismo event_id es idempotente | replay duplica PASS o contador |
| P04/N04 | correlation/causation resuelven run | evento huérfano se considera aprobado |
| P05/N05 | payload de error está redactado | payload filtra JWT, PII o texto |
| P06/N06 | schema_version compatible se acepta | versión o tipo no reconocido se aplica |
| P07/N07 | evento tardío se ordena sin pérdida | evento antiguo pisa estado nuevo |
| P08/N08 | PASS requiere resultado medido y SHA | NO_MEDIDO o NOT_RUN da PASS |
| P09/N09 | consumo falla cerrado ante interrupción | timeout duplica confirmación |
| P10/N10 | retención autorizada purga sin fuga | artifact privado se expone públicamente |
| P11/N11 | error de formulario queda semántico | se guarda valor privado del usuario |
| P12/N12 | rollback preserva auditoría inmutable | revert borra eventos históricos |

P01–P12/N01–N12 son **FUTUROS_NO_EJECUTADOS**. Se requieren tests reales cuando se implemente un emisor/consumidor, con SHA causal, tenant A/B, permisos/revocación, JSON schema, retry/timeout, logs redactados y fallos reales. No hay A3 de integración o proveedor ejecutada aquí.

## Límites, A0–A5, pruebas reales posteriores y rollback

**A0 R0:** revisar fuente M01, contrato M02, 12 eventos, envelope, claves/correlación, 18 invariantes, pruebas futuras y diff limitado a Markdown/CHANGELOG. **A1 R0:** coherencia estática de pares P/N futuros, no PASS funcional. **A2 R0:** restricciones JWT/RBAC/tenant A/B/revocación, PII, integridad e idempotencia documentales, no permisos implementados. **A3 R0:** `NO_APLICA` .NET/API, MySQL/EF, Angular/Playwright, broker, servicios y CI de runtime al no haber código. **A4 R0:** commit fast-forward/CAS sobre `dev`, Scope Lock/V5 `completed/success` SHA exacto, readback PLAN/COLA/DAG/BITACORA y secciones CHANGELOG autocontenidas. **A5:** revisión técnica independiente por otra Task Javier distinta de autora, con facultad de rechazo y sin autopase.

Implementaciones futuras (R2) exigirían pruebas negativas de segregación A/B, IDOR, roles/revocación, backpressure, idempotencia/replay, eventos tardíos, timeout y rollback; integración MySQL efímera si cambia EF o DB; Playwright si cambia UI. **R3 producción, secretos y operaciones irreversibles no autorizadas.**

Rollback de este documento: `git revert` normal y verificación del HEAD, sin reescribir historia ni BITACORA. No editar QA/main/PROD, cuentas, servicios externos, reglas ni programación de seis Tasks. El resultado de esta M02.05 sólo habilita un **contrato conceptual**, nunca una autorización de publicación de eventos.
