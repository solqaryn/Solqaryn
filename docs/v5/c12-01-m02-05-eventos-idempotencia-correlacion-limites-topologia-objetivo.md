# SOLQARYN V17 — C12-01.M02.05: eventos, idempotencia, correlación y límites conceptuales de topología

**ID:** C12-01.M02.05 · **PLAN_MAESTRO:** fila 1740 · **Autor:** PRIMARY DEV04 Javier (6aeacb7820ac8191b7d768a6f33dfa09) · **Riesgo:** R0 documental · **Repositorio:** solqaryn/Solqaryn@dev · **PROJECT_SCOPE_LOCK:** STRICT.

**Autoridad ejecutable:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL y PLAN/COLA/DAG_TECNICO. **Fuentes:** C12-01.M01.01–06 (G01–G16, rollback sin ejecutar), M02.01 (E01–E16/I01–I20), M02.02 (OP01–08, D01–12, ER01–12/V01–16) y M02.04 (CFG01–16/F01–12). Este documento describe futuros contratos de diseño; **ningún evento, cola, broker, endpoint, job o integración se implementa ni se ejecuta**.

## A0 — Alcance, fronteras y rollback

Diseñar un catálogo inactivo de señales de propuesta/revisión de topología y sus reglas de correlación, idempotencia, límites, redacción, seguridad tenant A/B, revocación y compensación. Se requiere una implementación posterior con pruebas reales y autorización independiente antes de conectar un proveedor. La topología en operación sigue siendo Vercel, Render, Aiven y Cloudinary; Hostinger, DNS, VPS, MySQL destino y Cloudflare no están habilitados por este alcance.

**Capas y archivos ejecutables alterados:** ninguno. **Datos/EF/MySQL:** sin esquema, migraciones, escritura ni transacciones reales. **JWT/RBAC/tenant:** invariantes conceptuales, no reemplazan comprobación backend. **Integraciones/secretos:** ningún mensaje a redes, claves o valores sensibles. **Consumidores futuros:** orquestación, seguridad, continuidad, operación y auditoría. **Rollback de este documento:** git revert normal tras comprobar HEAD y preservar BITACORA, nunca reescribir historia.

## EV01–EV12 — Catálogo de eventos FUTUROS_NO_EMITIDOS

| ID | Evento de diseño propuesto | Emisor futuro | Efecto máximo permitido en diseño / rechazo |
| --- | --- | --- | --- |
| EV01 | TopologyProposalDrafted | propietario/arquitectura | Registrar borrador E01 inactivo; no publicar despliegue |
| EV02 | EnvironmentBoundaryEvaluated | custodio de ambientes | Documentar E02 segregado; rechazar cruce DEV/QA/PROD |
| EV03 | WorkloadPlacementReviewed | arquitectura/red | Dictamen E03/E04/E05; nunca aprovisionar |
| EV04 | IngressPolicyReviewed | red/seguridad | Dictamen E06/E07 TLS/CORS; no cambiar DNS |
| EV05 | AccessReferenceReviewed | custodio IAM | Referencias E08/E09 opacas; rechazar secretos literales |
| EV06 | DatabaseCompatibilityProposed | equipo datos | Plan E10 sujeto a EF/MySQL 8.4 efímero real posterior |
| EV07 | BackupPolicyReviewed | continuidad | E11/E12 y checksums propuestos; no declarar restore |
| EV08 | MediaScopeReviewed | custodia de medios | E13 tenant scoped, sin tocar Cloudinary vivo |
| EV09 | ObservabilityLimitsReviewed | operación/privacidad | E14 con PII/JWT redactados y cuotas no medidas |
| EV10 | MigrationWaveEvaluated | propietario cambio | E15 requiere gates G01–G16, no aprobar cutover |
| EV11 | RollbackCheckpointReviewed | continuidad | E16 con pruebas futuras; evidencia ausente => NO_GO |
| EV12 | TopologyProposalRejected | revisor autorizado | Rechazo terminal del borrador; nunca activar flags |

**Semántica:** los nombres son etiquetas de catálogo documental, no contratos publicados ni eventos en un broker. El evento EV12 conserva motivos redactados, versionado y correlación para una propuesta futura; no borra su historia. EV10 y EV11 no significan ejecución de despliegue o recuperación. Un resultado aceptado para un documento no activa F12 cutoverEnabled de M02.04.

## ENV01–ENV12 — Campos de envoltura futura tipada

| ID | Campo conceptual | Restricción obligatoria futura |
| --- | --- | --- |
| ENV01 | eventId (ID único) | Imposible reusar entre envelopes distintos |
| ENV02 | eventType (enum EV01–EV12) | Evento desconocido => rechazo |
| ENV03 | schemaVersion (versión explícita) | No realizar downgrade silencioso |
| ENV04 | topologyRevisionId (referencia E01) | Owner y revisión comprobados |
| ENV05 | environmentKey (enum E02) | DEV/QA/PROD aislados |
| ENV06 | actorRef (referencia opaca) | JWT/RBAC/tenant y revocación validados por backend |
| ENV07 | tenantScopeRef (referencia opaca opcional) | No confiar en header/host/slug del cliente |
| ENV08 | correlationId (cadena generada servidor) | Trazar operación sin datos sensibles |
| ENV09 | causationId (referencia a evento precedente) | Evitar ciclos y referencias cross-tenant |
| ENV10 | idempotencyKey (digest canónico propuesto) | Scope por actor + ambiente + tenant + operación + versión |
| ENV11 | occurredAtUtc (timestamp UTC servidor) | Rechazar reloj inválido/futuro fuera de ventana |
| ENV12 | payloadDigest (hash de contenido redactado) | No transportar secretos/PII ni payload original |

Estas claves no son columnas, DTO compilados ni mensajes realmente transmitidos. El digest de idempotencia se construiría con canonización y hash criptográfico revisados en una microtarea R2 posterior; nunca almacenar el secreto de firma ni confiar en digest calculado por cliente.

## I01–I12 — Invariantes de idempotencia, orden, reintento y límites

| Regla | Invariante exigible en futura implementación |
| --- | --- |
| I01 | Misma clave+digest+scope => misma decisión durable; nunca dos efectos |
| I02 | Misma clave y payload distinto => conflicto, no sobrescritura |
| I03 | Tenant A y B no comparten espacio de idempotencia ni mensajes |
| I04 | Environment DEV/QA/PROD no comparte evento, secreto ni consumer lease |
| I05 | Roles revocados o JWT inválidos no se validan por una caché positiva antigua |
| I06 | Correlación/causación no habilita autorización ni acceso a otro tenant |
| I07 | Un consumidor futuro confirma efecto y ack en frontera transaccional comprobada |
| I08 | Reintentos con backoff acotado, límite y dead-letter; no infinito |
| I09 | Mensaje inválido, viejo, desordenado o de esquema desconocido => cuarentena |
| I10 | Cancelación o timeout no puede dejar una transición de topología aprobada a medias |
| I11 | Logs conservan IDs opacos y regla, nunca JWT, PII, DSN, secreto o carga completa |
| I12 | Activación de proveedor/cutover requiere orden R3, G01–G16 y ensayo reversible |

Un evento de revisión no es un comando para producir efectos. La deduplicación no debe basarse en memoria volátil, nombre del servidor, tiempo local ni cabecera del cliente. Aprobación de borrador, observabilidad y entrega del evento son estados distintos. No declarar "exactly once" ni restauración exitosa sin CI de integración real posterior.

## LIM01–LIM08 — Límites propuestos; valores operativos NO_MEDIDO

| ID | Dimensión | Condición segura propuesta |
| --- | --- | --- |
| LIM01 | Tamaño máximo de sobre | Definir umbral mediante pruebas; hasta entonces ninguna publicación |
| LIM02 | Frecuencia / tasa por identidad y tenant | Cuotas separadas con rechazo, sin cifra inventada |
| LIM03 | Reintentos | Cota finita y jitter; no retry infinito |
| LIM04 | Caducidad y TTL | Rechazar mensajes caducados y replay |
| LIM05 | Cardinalidad de correlationId | Limitar explosión de métricas/PII |
| LIM06 | Tamaño de dead-letter | Cuota de almacenamiento y política de retención aprobada |
| LIM07 | SLI/SLO, p95, RTO/RPO | NO_MEDIDO sin muestra y responsable |
| LIM08 | Concurrencia/cambio de versión | CAS de propuesta y un solo escritor efectivo |

Los valores numéricos de cuota, SLA y retención quedan pendientes de fuentes y aprobación; no asumir límites del plan del proveedor, de Azure/Redis/RabbitMQ ni de infraestructura todavía no creada. La definición futura de un broker y adaptador exigirá decisión arquitectónica y tests pertinentes.

## ER-EV01–ER-EV08 — Errores de diseño, sin API implementada

| Código | Disparador | Resultado futuro |
| --- | --- | --- |
| ER-EV01 | UNKNOWN_EVENT_OR_VERSION | Rechazo sin fallback de versión |
| ER-EV02 | DUPLICATE_KEY_PAYLOAD_CONFLICT | Conflicto, sin nuevo efecto |
| ER-EV03 | TENANT_OR_ENVIRONMENT_MISMATCH | Denegar sin exposición cross-tenant |
| ER-EV04 | ROLE_REVOKED_OR_UNAUTHENTICATED | Denegar/auditar redactado |
| ER-EV05 | CORRELATION_CYCLE | Rechazar cadena de causación |
| ER-EV06 | RATE_OR_SIZE_LIMIT_EXCEEDED | Rechazo con cota finita |
| ER-EV07 | UNVERIFIED_PROVIDER_OR_SECRET | NO_GO para llamada externa |
| ER-EV08 | UNAUTHORIZED_CUTOVER | Mantener propuesta y F12 inactivos |

## P01/N01–P12/N12 — Pares de aceptación FUTUROS_NO_EJECUTADOS

| Par | Positivo de laboratorio autorizado futuro | Negativo adversarial obligatorio |
| --- | --- | --- |
| P01/N01 | EV01 versionado y scoped | Borrador sin propietario procesado |
| P02/N02 | Misma clave+payload devuelve misma decisión | Replay produce efecto duplicado |
| P03/N03 | Misma clave con payload distinto genera conflicto | Payload cambiado sobrescribe registro |
| P04/N04 | Tenant A/B segregados en cola y métricas | Mensaje de B procesado en contexto A |
| P05/N05 | DEV y QA con identidad separada | Credencial DEV usada para PROD |
| P06/N06 | Autor autenticado con rol vigente | Principal revocado acepta EV10 |
| P07/N07 | CorrelationId vincula eventos del mismo scope | CausationId crea ciclo o cruza tenant |
| P08/N08 | Timeout reintenta acotadamente | Reintento infinito u operación doble |
| P09/N09 | Mensaje desconocido va a cuarentena | Se interpreta desconocido como permitido |
| P10/N10 | Registro de decisión no contiene PII | JWT o DSN escrito en dead-letter |
| P11/N11 | Límite validado rechaza tamaño excesivo | Cuota ausente permite saturación |
| P12/N12 | EV10 permanece documental con F12=false | Mensaje activa DNS/Hostinger sin R3 |

**Resultado de esta entrega R0:** 0 de 12 pares ejecutados, 0 mensajes enviados y 0 pruebas reales de .NET, Angular, MySQL, HTTP, broker, Playwright, VPS, DNS, backup o proveedor. A1 es inspección documental de EV01–12, ENV01–12, I01–12, LIM01–08, ER-EV01–08 y 12 pares. A2 revisa aislamiento conceptual tenant A/B, IAM, roles, revocación, secretos, integridad y límites. A3 de runtime = NO_APLICA porque no hay código ni infraestructura modificada. La existencia de tablas y escenarios no prueba que los controles funcionen.

## A4, gates, A5, recuperación y reversión

- **Reclamación:** PLAN H=PENDIENTE/I vacío, COLA G/O=SI, DAG F=VALIDADO/G=SI y candado V17 SQV17_C12_01_M02_05 (ID 2000001740) con compare-and-swap y readback. Máximo un EN_PROGRESO propio.
- **Publicación:** un commit fast-forward/CAS en dev con este documento y sección autocontenida de CHANGELOG; checks Scope Lock y procedencia V5 completed/success exact-SHA o equivalencia auditada; readback PLAN/COLA/BITACORA. Una CI queued/in_progress no acredita PASS.
- **A5:** la revisión independiente pertenece a otra de las seis Tasks Javier y puede rechazar. M02.06 requiere gates verdaderos de M02.01–05 y sus A5; VALIDANDO no libera sucesoras técnicas.
- **Seguridad:** G01–G16 de M01.06 siguen sin acreditar migración, red privada, DNS, proveedor, datos, TLS, restore, cutover o producción. No modificar QA/main/PROD, secretos, configuración física de Tasks ni pagos.
- **Rollback:** git revert normal después de leer HEAD, preservando el código y documentación de terceros; cualquier defecto se registra en BITACORA con SHA y caso negativo. No borrar historia ni desactivar validadores.

**Salida:** solo contrato de preparación R0 y matriz de pruebas futuras. No existe decisión sobre bus, scheduler, webhook o proveedor, y no se emiten mensajes reales.