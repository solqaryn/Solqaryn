# SOLQARYN V5 FINAL — C02-03.M02.05: contratos conceptuales de eventos, idempotencia, correlación y límites de lifecycle tenant

**Microtarea:** C02-03.M02.05. **Autor:** PRIMARY DEV 06, JAVIER_CHATGPT, TASK_ID 6af16bbad26881918c283abe06e1f32a. **Repositorio:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT. **Fecha:** 2026-11-15.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES V5 FINAL (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), PLAN_MAESTRO/COLA/DAG_TECNICO/BITACORA, skill canónica, AGENTS.md, documentos C02-03.M01.01–06 y modelo C02-03.M02.01. PLAN H372=PENDIENTE/I372 vacío, COLA G/O=SI y DAG F=VALIDADO/G=SI al claim; owner único después del claim.

**Clasificación:** DOCUMENTAL solamente. Este archivo NO crea eventos reales, colas, webhooks, Topics, triggers entre Tasks, workers, API, base de datos, migraciones, IAM, secretos ni envíos a servicios externos. Todos los mensajes y operaciones aquí definidos son PROPUESTOS_FUTUROS_NO_EMITIDOS. El documento no presupone que exista bus, dispatcher ni outbox.

## 1. Baseline y frontera de confianza

- Empresa actualmente permite Activar/Desactivar; Empresa.Eliminado=false [NotMapped] no representa baja lógica de empresa. UsuarioEmpresa vincula rol tenant y no otorga privilegios de plataforma.
- El futuro estado SOLICITADO/PROVISIONANDO/ACTIVO/SUSPENDIDO/ARCHIVADO/BAJA_LOGICA/FALLA_REPARABLE no se afirma existente en runtime. Lo definen los contratos M01 y el modelo M02.01, sujetos a diseño e integración posteriores.
- Cada intención global de lifecycle requiere operador con autorización de plataforma validada server-side, no rol admin de empresa, JWT, EmpresaId, DSN o host proporcionado por cliente.
- El diseño mantiene separación por EmpresaId, versionado/fencing e idempotencia durable futura. Cualquier consumidor futuro debe volver a verificar autoridad, origen, versión y pertenencia; no aceptar únicamente la firma de un mensaje.

## 2. Catálogo conceptual de señales S01–S10

| ID | Señal futura NO EMITIDA | Emisor permitido a futuro | Consumidor y efecto condicionado |
| --- | --- | --- | --- |
| S01 | TenantAltaSolicitada | Servicio plataforma autorizado | Preflight de unicidad, presupuesto y grants, sin activar comercio |
| S02 | TenantPreflightAceptado | Preflight transaccional validado | Preparar checkpoint idempotente, no reservar infraestructura automáticamente |
| S03 | TenantAltaConfirmada | Commit durable de aprovisionamiento | Permitir lectura del estado tras revalidar identidad y binding |
| S04 | TenantSuspensionSolicitada | Operador plataforma con razón validada | Comparar versión y bloquear nuevas operaciones antes del ACK |
| S05 | TenantSuspendido | Escritura durable y revocación confirmadas | Invalidar acceso/caché con versión; nunca borrar datos |
| S06 | TenantReactivacionSolicitada | Operador plataforma autorizado | Revalidar membresías, políticas y secretos; no resucitar sesiones |
| S07 | TenantReactivado | Transición durable y accesos reevaluados | Publicar nueva versión solo tras checks obligatorios |
| S08 | TenantArchivado | Retención/auditoría y CAS certificados | Prohibir operaciones nuevas preservando historia |
| S09 | TenantBajaLogicaRegistrada | Transición expresamente autorizada | Retener registros y FKs, sin eliminar DB/objetos |
| S10 | TenantTransicionFallida | Control de fallo/compensación | Estado reparable y alerta redactada, no ciclo infinito de reintento |

Estas señales no forman un scheduler ni eventos entre seis Tasks ChatGPT. No constituyen autorización para implementar dispatcher; su emisión es futura y necesita nuevas evaluaciones de arquitectura, límites de cuota y aislamiento.

## 3. Sobre lógico mínimo V01–V16 (propuesto, NO serializado)

| Campo | Contrato requerido | Regla de rechazo |
| --- | --- | --- |
| V01 schemaVersion | Entero/versionado estable | Tipo o versión no reconocida |
| V02 eventId | Identificador único no reciclado | Evento duplicado no idempotente |
| V03 operationId | Identificador de operación durable | Falta correlación con comando validado |
| V04 correlationId | Traza transversal redactada | Colisión inconsistente entre empresas |
| V05 empresaId | Identificador interno positivo, emitido server-side | Empresa distinta del contexto autorizado |
| V06 actorId | Referencia interna del operador | Operador desconocido o sin grant plataforma |
| V07 actorScope | Concesión verificable fuera del payload | Booleano/rol cliente usado como autoridad |
| V08 commandType | Tipo cerrado (alta, suspender, reactivar, archivar, baja lógica) | Comando desconocido o ilegal |
| V09 stateBefore | Estado anterior autoritativo, no del cliente | Desfase de versión / transición inválida |
| V10 stateAfter | Estado destino autorizado | Estado ilegal o sin política de retención |
| V11 expectedVersion | Versión CAS monotónica | Version stale o replay fuera de orden |
| V12 idempotencyKeyDigest | Referencia a clave con huella segura | Reutilización con payload distinto |
| V13 payloadHash | Huella canonicalizada sin secreto | Hash no corresponde al comando original |
| V14 occurredAtUtc | Tiempo de commit, no tiempo de solicitud cliente | Evento prematuro antes de commit durable |
| V15 sourceCommitOrRevision | Revisión de código o registro rastreable | Evidencia no verificable o revisiones incompatibles |
| V16 redactionPolicyVersion | Norma vigente para logs y trazas | PII, JWT, DSN o credenciales explícitas |

Se prohíbe incluir access token, refresh token, contraseña, correo completo, DSN, claves proveedor, payload legal sin redacción y URLs de almacenamiento privadas en sobre, error y logs. La exposición de identificadores se limita a la autorización de cada consumidor.

## 4. Invariantes de entrega, reintento y correlación L01–L14

| ID | Regla futura | Falla que debe abortar |
| --- | --- | --- |
| L01 | Una solicitud corresponde a una Empresa y comando inequívocos | Evento de A en contexto B |
| L02 | Idempotencia aislada por EmpresaId+comando+clave+hash payload | Misma clave para payload diferente |
| L03 | Registrar resultado durable antes de ACK y antes de emitir | ACK sin transacción confirmada |
| L04 | Outbox si se elige posteriormente, atómico con estado de negocio | Publicar evento sin estado persistente |
| L05 | Reintento seguro usa backoff acotado y presupuesto futuro | Bucle infinito / tormenta de eventos |
| L06 | Consumidor deduplica evento y mantiene resultado por versión | Segunda ejecución con mismo eventId |
| L07 | CAS/fencing de estado impide suspensión/reactivación competidoras | Dos ganadores concurrentes |
| L08 | Revocación de acceso y caché precede confirmación de suspensión | Token stale habilita escritura de tenant suspendido |
| L09 | Reactivación exige grants y membresías actuales | Resucitar UsuarioEmpresa revocada |
| L10 | Archivo y baja lógica requieren retención y hold resueltos | DELETE/DROP físico o pérdida de contabilidad |
| L11 | Error parcial produce checkpoint auditable y compensación idempotente | Tenant aparentemente ACTIVO sin preflight |
| L12 | Toda correlación mantiene tenant y actor sin PII sensible | Logs conectan datos privados de A y B |
| L13 | 409 por conflicto idempotente/versión es fallo seguro sin efectos | Éxito ficticio por retry |
| L14 | Un mensaje no invoca nuevas Tasks ni publica en QA/main/PROD | Dispatch externo no autorizado o autoaprobación A5 |

## 5. Pruebas contractuales P01–P04 / N01–N10, FUTURAS NO EJECUTADAS

| Caso | Tipo | Entrada hipotética | Resultado obligatorio futuro |
| --- | --- | --- | --- |
| P01 | POS | Alta con grant real, key y hash válidos | Un único commit y evento S03 correlacionado |
| P02 | POS | Replay de mismo comando y key | Respuesta idéntica y ningún efecto duplicado |
| P03 | POS | Suspensión A tras revocación validada | S05 correlacionado y rechazo de API/jobs privados |
| P04 | POS | Fallo transitorio desde checkpoint | Reanudación acotada, sin duplicados ni fuga B |
| N01 | NEG | Actor empresa A intenta suspender B | Denegación antes de crear evento |
| N02 | NEG | Mismo key, payload distinto | 409 lógico y cero nuevas escrituras |
| N03 | NEG | EventId duplicado | Consumidor deduplica y no vuelve a aplicar |
| N04 | NEG | expectedVersion stale | CAS falla y no emite S03/S05/S07 |
| N05 | NEG | JWT revocado o rol desactualizado | Denegar antes de usar outbox o jobs |
| N06 | NEG | Host, storage o DSN B en evento de A | No derivar autoridad ni acceso de ese dato |
| N07 | NEG | Log contiene token, password o email | Redacción y test de seguridad falla cerrado |
| N08 | NEG | Retención contable sin resolver y baja lógica | Bloquear S09; conservar filas y adjuntos |
| N09 | NEG | CI obligatoria CANCELLED/SKIPPED/FAIL | No promover LISTO por check erróneo |
| N10 | NEG | Intento de emitir desde Task o dispatcher no autorizado | No activar webhook/worker nuevo ni alterar programación |

Estos son contratos FUTUROS_NO_EJECUTADOS: no se ejecutaron MySQL, .NET, Playwright, HTTP, colas, worker ni eventos para redactar M02.05.

## 6. Impacto, rollback y A0–A5

**Impacto actual:** nuevo Markdown docs/v5 y sección autocontenida de CHANGELOG_AI.md. Sin cambios de Domain, Application, Infrastructure, API, frontend, migraciones, configuración, proveedores, secretos, datos, despliegue, Tasks ni schedules.

**A0:** revisar diff limitado a 2 archivos, sección de CHANGELOG sin encabezados vacíos, fuentes canónicas, IDs y lenguaje NO EMITIDO. **A1:** validar señales S01–S10, campos V01–V16, reglas L01–L14 y casos positivos/negativos, sin reportar PASS runtime. **A2:** JWT/RBAC/tenancy A/B, actor plataforma, cifrado/reducción de identificadores, idempotencia, revocación, compensación, no secretos. **A3:** contratos M01, M02.01–04 sólo en su estado real, DAG F/G de este ID y PLAN/COLA; no confundir documento de etapa previa con implementación de dispatcher. **A4:** publicación FF/CAS dev y checks Scope Lock + V5 completed/success sobre SHA exacto, readback PLAN/COLA/DAG/BITACORA. Checks de compilación y DB no causales se marcan NO_APLICA, nunca PASS. **A5:** revisión independiente de otra Task Javier, autor distinto, antes de usar el documento para desbloquear M02.06.

**Rollback de documentación:** git revert normal del commit en dev, revalidar HEAD y checks; BITACORA sigue aditiva. **Siguiente etapa:** M02.06 únicamente cuando sus predecesoras cumplan LISTO+A5 y DAG/COLA calculen SI, sin habilitar runtime, QA/main/PROD ni servicios externos.
