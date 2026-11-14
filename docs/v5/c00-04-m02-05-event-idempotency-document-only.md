# SOLQARYN V5 FINAL — C00-04.M02.05: contrato documental de eventos, idempotencia, correlación y límites

**Ámbito:** SOLQARYN, rama dev, PROJECT_SCOPE_LOCK=STRICT. Autor PRIMARY DEV 04 Javier, TASK_ID 6aeacb7820ac8191b7d768a6f33dfa09. Este documento es un **diseño de contrato futuro**, no una implementación de eventos, webhook, dispatcher, worker, outbox, mensajería ni ejecución entre Tasks. No modifica SQL, JWT, RBAC, tenancy, secretos, API, Angular ni producción.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), PLAN_MAESTRO / COLA / DAG_TECNICO / BITACORA del Sheet 1VWJmV7udKk0UaNGL2VQJoEGmUYK9AMAmkqOrnrlmydU. El evento nunca sustituye los estados de PLAN_MAESTRO ni constituye una instrucción ejecutable.

## 1. Preflight, fuentes y fronteras

- Gate **individual** DAG_TECNICO fila 150: F=VALIDADO, G=SI, con los diez predecesores C00-04.M01.01–06 y C00-04.M02.01–04 en LISTO. El último, M02.04, fue revisado A5 independientemente por PRIMARY DEV 04 respecto de autor PRIMARY DEV 02, SHA f6e6dcfa697d4263da936e3fa4eccd609c21b9ad; PLAN K119 y BITACORA contienen evidencia con readback. Este gate solamente permite **documentar** M02.05, no liberar M02.06 ni implementación M03.
- Contratos fuente: docs/v5/c00-04-m02-01-entidades-ownership-invariantes.md, docs/v5/c00-04-m02-02-contratos-api-dto-errores-gobierno.md, docs/v5/c00-04-m02-03-persistencia-indices-migraciones-fixtures-gobierno.md, docs/v5/c00-04-m02-04-configuracion-tipada-ambitos-flags-seguros-gobierno.md. Los esquemas y entidades allí descritos son conceptuales; ningún DTO o endpoint nuevo se infiere de ellos.
- Superficie de este cambio: un Markdown nuevo y sección aditiva autocontenida en CHANGELOG_AI.md. Dependencias directas: gobernanza V5, fuentes institucionales y arquitectura Angular 22.2.1 / .NET 10 / MySQL EF Core 10.0.12 / proveedor Oracle 10.0.9. **No** se edita infraestructura, configuración, almacenamiento, interfaces de runtime, ni otros IDs.
- Seguridad: la autorización efectiva está en backend y políticas vigentes. El evento diseñado no concede roles ni permite que cliente elija EmpresaId, tenant, actor, ambiente, destinatario, Task o scope.

## 2. Sobre lógico propuesto (FUTURO, no emitido)

| Campo lógico | Reglas propuestas | Fallo cerrado |
| --- | --- | --- |
| EventId | Identificador global único inmutable de una emisión | Rechazar vacío, duplicado mal formado o reutilizado para contenido distinto |
| EventType / SchemaVersion | Tipo de catálogo allowlisted y versión inmutable | Rechazar tipo desconocido o evolución incompatible |
| SourceAuthority / SourceRevision | Fuente aprobada y revisión verificable | Sin revisión vigente, evento no habilita trabajo |
| OccurredAtUtc / RecordedAtUtc | Tiempos UTC distintos, trazables | Rechazar orden causal imposible y no inferir completitud por reloj |
| CorrelationId / CausationId | Identificar operación y causa parent, sin ciclo | Detectar bucles, duplicación y profundidad excesiva |
| TenantBinding | Contexto server-side enlazado al usuario/servicio autenticado | Nunca aceptar tenant desde headers, query, slug o payload |
| ActorTaskId / ActorRole | Identidad nativa verificada, dentro del allowlist vigente | Una mención histórica del actor no lo autoriza |
| SubjectScopeId | Exacto ID institucional existente y estado permitido | Rechazar scope ajeno, archivado o no habilitado por DAG |
| IdempotencyKey | Clave de intención estable por operación y scope, distinta de EventId | Duplicados idénticos equivalen a una sola intención |
| PayloadDigest / SanitizedPayload | Huella de bytes canonizados, mínima información no sensible | Un mismo key con digest diferente es CONFLICTO |
| EvidenceRef / AuditEventId | Evidencia SHA y evento BITACORA A:Z único | No confundir propuesta, ejecución y certificación |

**Invariante:** EventId identifica una emisión; IdempotencyKey identifica una intención deduplicable; CorrelationId agrupa el flujo; CausationId enlaza al padre. Ninguna de las cuatro claves es una credencial, un permiso, un lease ni un estado de PLAN. No incluir tokens JWT, secretos, contraseñas, PII innecesaria, SQL, URLs de ejecución o contenido ejecutable.

## 3. Semántica futura y garantías NO implementadas

1. **Intención:** solo un comando autorizado en su scope puede solicitar un registro conceptual. La intención debe validarse contra identidad, plan, DAG y versión efectiva. Un documento, un commit o un callback de Task no representa autorización.
2. **Persistencia hipotética:** si se autoriza después una outbox, la escritura del dato de negocio y la de outbox requerirían una transacción real del proveedor; dos escrituras de Sheet no constituyen CAS distribuido, ni este documento crea tablas ni transacciones.
3. **Deduplicación hipotética:** usar índice lógico compuesto (TenantBinding, SubjectScopeId, EventType, IdempotencyKey), verificando PayloadDigest; misma clave y mismo digest es reintento idempotente, misma clave y distinto digest causa CONFLICTO y auditoría sin ejecución.
4. **Entrega hipotética:** at-least-once exigiría consumidor idempotente y ack posterior a commit; no prometer exactly-once ni reordenamiento perfecto. La ausencia de ack no autoriza duplicar transiciones de PLAN.
5. **Correlación:** propagar ids opacos sin exponer contenido sensible; límites de profundidad y detección de ciclos; desacoplar relojes y tolerar entrega fuera de orden con validación de revisión.
6. **Retención y cancelación:** TTL de dedupe y retención legal deben aprobarse antes de cualquier runtime; prohibido destruir historia BITACORA o datos comerciales legítimos. Pausar un eventual mecanismo no borra historial ni activa otro ejecutor.
7. **Observabilidad:** eventual contador de aceptados, duplicados, conflictos, denegaciones, reintentos y descartes; registrar huellas y razones sanitizadas, nunca secretos. Especificación únicamente, sin telemetría viva nueva.

**No existe** worker, webhook, event dispatcher, cola, outbox, llamada Task→Task, programador cíclico ni trigger externo por este changeset. No se habilitan eventos todavía, conforme a V5.

## 4. Límites de cuota, permisos y abuso

- Rechazar actor fuera de las seis Tasks Javier, autorrevisión A5, scopes en manos de otro escritor, falsos PENDIENTE o DAG sin F=VALIDADO y G=SI. Un evento de BITACORA de autor antiguo es evidencia, no autoridad para rescatar.
- Un eventual consumidor deberá tener cuota explícita por tenant y por tipo, presupuesto global, backoff limitado con jitter, circuit breaker y dead-letter con revisión humana, sin retry infinito. La cuota no podrá rebajar checks de seguridad ni permitir compras o infraestructura.
- Sin autorización expresa, cualquier endpoint de administración, config flag, rol RBAC, IAM, migración MySQL, reactivación de Task, envío externo, QA/main/PROD, DNS y cobro son **DENY**.
- Reglas de payload: tamano maximo por schema, firma o autenticidad según arquitectura aprobada, rechazo de datos inesperados, sanitización de logs, no renderizar HTML crudo. Tenant A no lee, despacha ni correlaciona contenido de tenant B.
- Si la evidencia de un check exact-SHA es queued/in_progress/failed/cancelled/skipped, ese check requerido no acredita PASS. Un check de compilación irrelevante para Markdown se marca NO_APLICA con motivo, no PASS.

## 5. Oráculos documentales (NO tests runtime)

| Caso | Situación ilustrativa | Resultado que un futuro sistema debería garantizar |
| --- | --- | --- |
| P01 | Diez predecesores LISTO y A5 independiente, DAG150 F VALIDADO/G SI | Solo elegibilidad documental de este ID |
| P02 | Replay con misma IdempotencyKey + digest en el mismo scope/tenant | Una intención lógica y evidencia consistente |
| P03 | CorrelationId común y CausationId válido en dos pasos | Trazabilidad sin concesión de permisos |
| P04 | Pausa futura de dispatcher hipotético | Cero envíos nuevos, sin borrado de eventos legítimos |
| P05 | Eventos fuera de orden con SourceRevision antigua | Reconciliación o rechazo seguro, nunca avance silencioso |
| N01 | Tenant A proporciona TenantId de B desde cliente | DENY y no filtración |
| N02 | Mismo IdempotencyKey, digest distinto | CONFLICTO y auditoría, sin doble efecto |
| N03 | Task ajena, retirado o identidad no comprobada | NO_GO sin fallback |
| N04 | Autor del scope intenta su propio A5 | DENY |
| N05 | EventId reciclado para payload diferente | Rechazo de integridad |
| N06 | SchemaVersion desconocida, clave inexistente o contenido ejecutable | Rechazo sin default permisivo |
| N07 | Loop A→B→A o profundidad superior al límite | Detener propagación, registrar motivo |
| N08 | Falta ACK, reintento duplicado o consumidor reiniciado | Dedupe antes de efecto, sin promesa exactly-once |
| N09 | CI requerida skipped/cancelled/in_progress/failed | No PASS ni LISTO por inferencia |
| N10 | PENDIENTE con COLA O SI pero sin DAG individual válido | No claim |
| N11 | Webhook, Task→Task, evento directo a QA/PROD o activación de worker | Prohibido hasta autorización separada |
| N12 | Evento solicita elevar RBAC/JWT, migrar DB o cambiar secretos | DENY y alcance nuevo obligatorio |
| N13 | Dedupe caducado sin ventana de negocio válida | No reprocesar ciegamente, consultar fuente vigente |

Todos son **criterios de diseño inspeccionados**, no resultados de pruebas unitarias, E2E, API, MySQL, JWT o tenencia de verdad. La implementación futura debe elaborar pruebas positivas/negativas y medir resultados reales en CI del SHA pertinente.

## 6. Auditorías, reversión y sucesoras

- **A0:** diff limitado a dos Markdown, autoría/alcance correcto, no secretos, sección CHANGELOG nueva con encabezado y viñetas contiguas, sin mezclar autoría; comprobar patrón V5.
- **A1:** inspección conceptual P01–P05 / N01–N13; no reclasificar oráculos escritos como tests ejecutados.
- **A2:** límites de actor, RBAC, JWT, tenant A/B, firma/PII, cuota, replay y denegación; sin cambio en runtime.
- **A3:** verificar source docs M02.01–04, predecesores diez LISTO, revisión A5 de M02.04 y DAG150 VALIDADO/SI; sin dependencia de workers.
- **A4:** publicar en dev por commit fast-forward protegido con expected SHA, leer commit/tree/blobs y verificar Scope Lock y check V5 exact head_sha completed/success. Para Markdown puro .NET/Angular/MySQL/Playwright son NO_APLICA, no PASS causal.
- **A5:** revisión independiente posterior por una Task Javier distinta del autor J30, sin autopromoción. C00-04.M02.06 no se reclama por completar M02.05 si carece de gate individual, revisión o preflight adicional.
- **Rollback:** revert ordinario del commit documental si no pisa cambios concurrentes; error institucional requiere evento compensatorio aditivo en BITACORA, sin borrar evidencia histórica.

**Conclusión:** este alcance entrega exclusivamente el contrato documental de eventos, idempotencia, correlación y límites. No se instala, inicia o simula un sistema de eventos.
