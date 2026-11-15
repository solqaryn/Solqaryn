# SOLQARYN V5 FINAL — C14-04.M02.01: entidades, propietarios y relaciones para puesta en marcha

**Autor:** PRIMARY DEV 03 — JAVIER_CHATGPT — `TASK_ID=6adc2e4bd8a48191ac7d8641ed95a1a3`. **Ambito:** `solqaryn/Solqaryn@dev`, `PROJECT_SCOPE_LOCK=STRICT`. **Clase:** contrato de diseño DOCUMENTAL M02.01; no crea tablas, API, deployments ni migraciones.

## 1. Fuentes, precedentes y límites

Autoridad: [STANDARD V5 FINAL](https://docs.google.com/document/d/1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA/edit) y [PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA](https://docs.google.com/spreadsheets/d/1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU/edit). Gobierno: `AGENTS.md`, `docs/PROJECT_SCOPE_LOCK.md`, `docs/AUTOMATION_STATUS.md` y skill canónica.

Predecesor directo: `C14-04.M01.01–06` LISTO con A5 independiente; `C14-04.M01.06` se publicó en SHA `9a6a4691d74af7df492e7225919163e4abd60d31`, blob `a793116efbdfe01c9db4e60c86cd9ca74b3bb27f`, y su A5 fue emitida por la Task :20, distinta de la autora :10. Al reclamar M02.01 se comprobó PLAN H2132=PENDIENTE/I vacía, COLA G/O=SI, DAG F=VALIDADO/G=SI; sólo este ID cambió a EN_PROGRESO con readback. Las fórmulas pueden cambiar después: no son permisos permanentes.

El alcance es **modelar artefactos de operación futura**, con propiedad verificable, relaciones y reglas de integridad para soporte, observabilidad y liberación. La topología vigente Vercel/Render/Aiven/Cloudinary no se sustituye por el destino conceptual Hostinger/Cloudflare/R2. Ningún contrato documental demuestra disponibilidad, backup, restauración, seguridad runtime, aceptación accesible ni RPO/RTO.

## 2. Registros lógicos y dueño de verdad

| ID | Registro conceptual | Clave de correlación propuesta | Autoridad y regla de propiedad |
| --- | --- | --- | --- |
| PM01 | ReleaseCandidate | release_id + commit_sha | Git `dev`; un SHA inmutable por candidato |
| PM02 | EnvironmentGrant | entorno + grant_id | Permiso explícito externo al plan; ausencia bloquea despliegue |
| PM03 | ArtifactManifest | commit_sha + artifact_digest | Build verificado; digest no se presume por nombre |
| PM04 | OperationalScope | scope_id + tenant_key opcional | Owner y límite de tenant explícitos, nunca inferidos del cliente |
| PM05 | HostRouteContract | host_pattern + route_id + entorno | Host allowlisted; ninguna redirección A→B sin validación |
| PM06 | SecurityCheck | commit_sha + check_name + run_id | GitHub Actions `completed/success` exact-SHA o equivalencia causal probada |
| PM07 | AcceptanceEvidence | case_id + evidence_sha | Caso observado, resultado, entorno, actor y timestamp; no mera especificación |
| PM08 | RolloutWindow | window_id + entorno | Responsable de operación y autorización de ventana distintos del A5 |
| PM09 | HealthReadinessSignal | service_id + observed_at | Observación real posterior al cambio; no SLO inventado |
| PM10 | AlertRule | service_id + rule_id + version | Dueño de umbral, notificación y severidad; sin alertas ficticias |
| PM11 | IncidentCase | incident_id | Estado, severidad, responsable y correlación; PII redactada |
| PM12 | SupportEscalation | incident_id + escalation_step | Destinatario autorizado, SLA propuesto no medido |
| PM13 | BackupEvidence | snapshot_id + checksum | Origen, integridad, acceso y retención, sin respaldo supuesto |
| PM14 | RestoreTrial | snapshot_id + trial_id | Ensayo aislado autorizado; no restore de producción implícito |
| PM15 | RollbackProcedure | release_id + procedure_version | Pasos reversibles, stop condition, owner y aprobación de entorno |
| PM16 | TransitionAuditEvent | event_id + idempotency_key | BITACORA A:Z append-only; evidencia de PLAN/COLA/DAG y autoría |

Son **entidades conceptuales**, no modelos EF Core ni tablas nuevas. La fuente institucional del estado de microtareas sigue siendo PLAN_MAESTRO!H/I/K; COLA y DAG derivan elegibilidad y BITACORA registra hechos sin reemplazar la fuente autoritativa. Un registro desconocido se marca `NO_ACREDITADO`, no se fabrica.

## 3. Relaciones, aislamiento y cardinalidad

- PM01 (1:N) PM03; cada manifiesto referencia exactamente un SHA de la versión publicada. PM01 (1:N) PM06 y PM07; la observación sólo es aplicable al SHA y al componente realmente probados.
- PM02 (1:N) PM08; PM08 (N:1) PM01. No existe relación automática desde A5_PASS o LISTO_DEV hacia PM02: la autorización QA/main/PROD sigue separada.
- PM04 (1:N) PM05, PM07 y PM11 cuando sean datos de un cliente. Un scope global se representa explícitamente y no permite lectura cross-tenant. La identidad verificada en backend gobierna el tenant, no `tenant_id`, slug, host o cabecera entregada por navegador.
- PM09 (N:1) PM04/servicio, PM10 (N:1) PM09 o servicio y PM11 (0:N) PM10. La ausencia de telemetría no equivale a estado saludable.
- PM11 (1:N) PM12; PM13 (1:N) PM14; PM15 (N:1) PM01 y (0:N) PM14. Ningún ensayo futuro autoriza cambios vivos sin PM02 y controles adicionales.
- PM16 (N:1) transición institucional, con evento e idempotencia únicos. Correlación no equivale a bus de eventos ni dispatcher instalado.

## 4. Invariantes exigibles para implementación futura

| ID | Invariante | Evidencia mínima |
| --- | --- | --- |
| INV01 | SHA y digest presentes antes de cualquier aceptación | PM01/PM03 comprobados |
| INV02 | `completed/success` y `head_sha` causal para checks requeridos | PM06; skipped/cancelled/in_progress no PASS |
| INV03 | Sin grant de entorno no existe despliegue | PM02 verificable |
| INV04 | Autor y revisor A5 son TASK_ID Javier diferentes | PLAN K + BITACORA |
| INV05 | Un solo responsable de escritura por microtask ID | PLAN I + mecanismo de exclusión; lectura/escritura Sheets sola no es CAS atómico |
| INV06 | Tenant A no puede consultar datos, medios o logs de B | Prueba positiva/negativa backend A/B |
| INV07 | Sin secretos o PII en repositorio, checks o historial | Escaneo y redacción |
| INV08 | Manifest, ruta y entorno no cambian por parámetro cliente | Verificación allowlist |
| INV09 | Salud no se declara mediante umbral no medido | PM09 y ventana de observación real |
| INV10 | Alertas sin prueba son diseño, no operación certificada | PM10 + correlación verificable |
| INV11 | IncidentCase preserva severidad, historial y responsable | PM11/PM12 append-only |
| INV12 | Backups y restore se acreditan separadamente | PM13/PM14 con hashes sintéticos y ensayo aislado |
| INV13 | Rollback tiene stop condition, responsable y autorización | PM15 + PM02 |
| INV14 | Historias de BITACORA no se reescriben | EVENT_ID/IDEMPOTENCY_KEY únicos |
| INV15 | Estado VALIDANDO no libera sucesor técnico sin LISTO+A5 requeridos | PLAN/COLA/DAG por ID |
| INV16 | Nada en este contrato autoriza QA, main, PROD, DNS, Cloudflare/R2 o datos reales | Permiso posterior explícito |

## 5. Matriz de escenarios positivos y negativos futuros

| Caso | Positivo a verificar | Negativo de rechazo |
| --- | --- | --- |
| T01 | PM01 y PM03 concuerdan por SHA/digest | Manifest huérfano o hash desigual bloquea |
| T02 | PM02 autoriza entorno explícito | A5 sin grant no despliega |
| T03 | JWT/RBAC válido accede al PM04 correcto | Tenant A no obtiene artefacto B |
| T04 | Ruta permitida por PM05 y backend | Header, slug o host manipulados no cruzan tenant |
| T05 | PM06 evidencia success exact-SHA | Cancelled/queued/skipped/failed no se promueven |
| T06 | PM07 contiene observado vs esperado | Caso escrito pero no ejecutado no es PASS |
| T07 | PM09/PM10 generan señal real y alerta correlacionada | Umbral/alerta no medidos no son SLO |
| T08 | PM11 escala a PM12 permitido | Destinatario no autorizado no recibe PII |
| T09 | PM13 restaura PM14 en entorno efímero | Snapshot sin integridad no acredita RPO/RTO |
| T10 | PM15 revierte sin pérdida en prueba autorizada | Sin owner/stop condition no se libera |

**T01–T10 son FUTUROS_NO_EJECUTADOS.** No se ejecutaron HTTP, MySQL, Docker, Playwright, accesibilidad, restauración, alertas, DNS, despliegues o validaciones de latencia. Las brechas conservan estado `NO_MEDIDO` o `NO_ACREDITADO`; ningún porcentaje o cumplimiento se infiere.

## 6. Validación documental A0–A5, impacto y rollback

- A0: enumerar PM01–PM16, INV01–INV16 y T01–T10, verificar sintaxis, enlaces, fuentes, no-secretos y ausencia de encabezados Markdown vacíos.
- A1: revisar un caso positivo/negativo por tipo de relación y denegación; son criterios verificables de diseño, NO pruebas runtime.
- A2: revisar denegación por defecto JWT/RBAC/tenant A/B y privacidad; el cambio es Markdown, por lo que tests de seguridad de ejecución son `NO_APLICA` al diff, no PASS.
- A3: cotejar M01.01–06, STANDARD, DAG/PLAN/COLA y separación entre contratos y futuras implementaciones.
- A4: publicar documento + sección autocontenida de CHANGELOG en `dev` con fast-forward y `expected_sha`, comprobar el diff y los checks obligatorios Scope Lock/V5 en ese SHA, además de los readbacks institucionales.
- A5: revisor independiente de otra de las cinco Tasks Javier; no autoaprobar ni liberar entornos. Estado al publicar: `A5_PENDIENTE`.

**Impacto:** sólo `docs/v5/` y `CHANGELOG_AI.md`. API/Application/Domain/Infrastructure/frontend, DB, JWT/RBAC, dominios y proveedores sin modificaciones. **Rollback:** `git revert` regular, condicionado al HEAD vivo y sin force-push ni borrar BITACORA. La adopción futura de PM01–PM16 exige otro alcance, autorización, CI funcional y migración reversible si procediera.
