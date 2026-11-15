# SOLQARYN V17 — C09-01.M02.05: contratos futuros de eventos, idempotencia y correlación CRM

**ID:** C09-01.M02.05 · **PLAN_MAESTRO fila 1308** · **Autor:** PRIMARY DEV 02 JAVIER_CHATGPT, TASK_ID `6af16bb9492c8191b8e8c7d606f11507` · **Rama:** `solqaryn/Solqaryn@dev` · **Riesgo:** R0 documental · **PROJECT_SCOPE_LOCK=STRICT**. Fuente única STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES Drive `1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA`; mandatos V15–V17.

## Resultado y frontera de autoridad

Definir exclusivamente contratos **conceptuales e inertes** de eventual auditoría, interacciones de timeline, oportunidades, segmentación y consentimientos del CRM universal. **No se implementa ni habilita** bus, webhook, scheduler, worker, publicación de eventos, cola, outbox, integración externa, notificación, envío de campañas, API, EF/MySQL, Angular, secretos ni cambios de datos. Los tipos abajo son propuestas para M03/M04/M05; no clases C#, tablas, endpoints ni mensajes reales. La documentación no puede activar tareas ni constituir un event dispatcher. Una oportunidad no crea Venta, factura, crédito ni movimiento monetario.

Empresa/tenant se resuelven desde identidad y membresía verificadas en **backend**. `EmpresaId`, `ActorId`, cabeceras, host y ruta enviados por cliente no son autoridad. Cualquier legado `Cliente.EmpresaId=NULL` se rechaza/aisla hasta reconciliación formal, jamás se considera recurso global. Opt-out o membresía revocada bloquean efectos posteriores incluso cuando un mensaje se reintenta.

## Precedentes y alcance documental

Preflight de este ID: PLAN H1308=PENDIENTE e I1308 vacío, COLA G/O=SI, DAG_TECNICO F/G=VALIDADO/SI; V17 candado `SQV17_C09_01_M02_05`, namedRangeId `2000001308`. `C09-01.M01.01–06` y A5 necesarias permiten sólo el documento de diseño. `C09-01.M02.01` define 16 entidades/conceptos y 20 invariantes futuros; M01.04 conserva D01–D08 PENDIENTES y M01.05 AC01–AC14 y NFR01–NFR10 FUTUROS_NO_EJECUTADOS. No inferir que M02.02–04, si son PENDIENTE/EN_PROGRESO, ya están implementadas o aprobadas. Una Task activa en otra M02 mantiene su propio owner.

**Contrato inerte:** cada evento se expresa como intención futura, no una señal emitida al registrar este Markdown.

## Catálogo de doce mensajes semánticos (FUTUROS_NO_IMPLEMENTADOS)

| Ref | Mensaje conceptual | Agregado propuesto / causa | Rechazo obligatorio |
| --- | --- | --- | --- |
| EV01 | ClienteCRMReconciliado | atribución autorizada de Cliente a Empresa | owner nulo/ajeno sin reconciliación |
| EV02 | InteraccionCRMRegistrada | actividad asociada a Cliente y actor válidos | replay, actor sin membresía, Empresa B |
| EV03 | ActividadCRMAsignada | asignación a responsable con permiso vigente | usuario revocado o recurso B |
| EV04 | ActividadCRMCompletada | transición válida y versionada | transición duplicada/inválida |
| EV05 | OportunidadCRMCreada | propuesta comercial sin venta durable | creación automática de factura/pago |
| EV06 | EtapaOportunidadCambiada | CAS de versión y etapa allowlisted | versión obsoleta o rol lector |
| EV07 | SegmentoCRMDefinido | filtros tipados y alcance empresa | consulta SQL/JS libre, cross-tenant |
| EV08 | SegmentoCRMRecalculado | snapshot paginado bajo límite de costo | datos de B o lectura ilimitada |
| EV09 | PreferenciaContactoActualizada | finalidad/canal versionados y propios | actuar sin base o consentimiento |
| EV10 | ConsentimientoRevocado | opt-out prioritario e irreversible por retry | reactivar canal por replay |
| EV11 | ExportCRMPreparada | solicitud con permiso de export específico | generar archivo con PII B |
| EV12 | OperacionCRMRechazada | auditoría mínima del rechazo | incluir tokens, texto PII o stack |

Nada en EV01–EV12 declara contrato comercial definitivo. Los nombres, estados y políticas de retención han de aprobarse mediante D01–D08, A5 y pruebas de seguridad antes de una implementación.

## Envelope propuesto, minimización e idempotencia

| Campo futuro | Semántica/validación | Control de seguridad |
| --- | --- | --- |
| `EventId` | UUID opaco único en ámbito autorizado | no derivar de PII ni confiar en ID cliente |
| `EventType` | enum allowlisted y versionada | rechazar tipos arbitrarios |
| `SchemaVersion` | entero positivo compatible | no interpretar payload desconocido |
| `EmpresaRef` | tenant resuelto en servidor | denegar A/B cruzado, nunca tomar cabecera |
| `ActorRef` | usuario de sesión verificada | denegar revocación/rol sin permiso |
| `AggregateRef` | Cliente/Actividad/Oportunidad propios | verificar existencia y owner antes de publicar |
| `AggregateVersion` | versión CAS monotónica | conflicto detectado, no pérdida silenciosa |
| `OccurredAtUtc` | instante de efecto confirmado por backend | no sustituir por reloj navegador |
| `CorrelationId` | identificador opaco acotado | no contener token, correo o notas |
| `CausationId` | evento padre verificado, opcional | evitar bucles y referencias de B |
| `IdempotencyKey` | clave por tenant+actor+operación+huella | replay no duplica, clave conflicto falla |
| `PayloadKind` | sólo DTO mínimo versionado | no serializar Cliente completo/PII |
| `Outcome` | aceptado/rechazado y código seguro | no filtrar DB, permisos secretos o stack |
| `RetentionClass` | clasificación pendiente de autoridad | no prometer retención concreta ni borrar historia |

**Regla de deduplicación futura:** una transacción debe registrar la operación de negocio y la clave idempotente en el **mismo boundary durable** cuando exista EF/MySQL autorizado; reintento con igual tenant+actor+operación+key+huella devuelve un solo efecto; igual clave con distinta huella genera conflicto, y claves idénticas de tenants A/B no comparten respuesta. `EventId` aislado no es garantía de exactly-once. Publicar después de commit mediante outbox verificado/worker solo si una microtarea posterior lo implementa y autoriza.

**Orden futuro:** ordenar por AggregateRef/Version, no asumir orden global entre empresas. Reintentos limitados con backoff/jitter y DLQ **sólo como diseño**; nunca crear timers o reintentos ahora. Consumidores idempotentes verifican nuevamente ámbito y control, y una revocación prevalece sobre un consentimiento más antiguo.

## Dieciséis negativas y controles de fallo futuro

| Control | Escenario de rechazo que debe tener prueba causal posterior |
| --- | --- |
| NG01 | Actor A introduce EmpresaRef B o AggregateRef de B |
| NG02 | Cliente con `EmpresaId=NULL` legado se publica a todos |
| NG03 | JWT expirado, membresía o permiso revocado publica evento |
| NG04 | Key repetida con mismo payload duplica actividad |
| NG05 | Key repetida con payload distinto sobrescribe resultado |
| NG06 | Una misma key en A permite leer resultado de B |
| NG07 | Version obsoleta cambia etapa de oportunidad |
| NG08 | Filtro de segmentos incorpora SQL/JS o campo no permitido |
| NG09 | Opt-out se ignora por reintento posterior |
| NG10 | Export falla permiso pero genera archivo con PII |
| NG11 | Mensaje/log incluye JWT, correo, teléfono, notas o secretos |
| NG12 | Fallo MySQL entre evento y operación deja efecto huérfano |
| NG13 | Timeout de consumer confirma éxito sin commit |
| NG14 | Reordenación causa estado inconsistente o loop |
| NG15 | Compensación CRM crea/borra Venta o saldo ERP |
| NG16 | Envío o webhook externo se activa sin aprobación |

**Todos NG01–NG16: FUTUROS_NO_EJECUTADOS.** Una prueba negativa debe poder fallar ante un defecto real; revisar por ID, SHA, timestamp y logs redactados. No declarar éxito porque el caso está escrito.

## Validación futura por tecnología y reversión

- **Domain/Application .NET 10:** asserts de ownership, versión, idempotencia, payload mínimo y transiciones rechazadas con xUnit reales.
- **API/JWT/RBAC:** operaciones autenticadas sin guard Angular, revocación, 401/403/404, Empresa A/B y recursos ajenos, sin secrets en logs.
- **EF/MySQL 8.4 efímero:** al crear outbox/idempotencia real, ejecutar `Category=Integration` y comprobar transacción, concurrencia, unique indexes, compensación y 0 FAIL; un build o migración sola no acredita DB.
- **Angular/Playwright:** si cambia UI en una fase posterior, navegación/teclado/errores y ausencia de doble submit con test Chromium por SHA.
- **Reversión:** en ESTE diff R0 un `git revert` normal y evento aditivo; en fases R2 futuras, rollback transaccional y backup/restore probado en DB aislada, no tocar Aiven productiva.
- **No desplegar:** no crear colas, webhooks, eventos entre Tasks, secrets, DNS, schedules o servicios pagados sin autorización específica. R3 producción/irreversibles siguen NO_GO.

## A0–A5, condición de salida y dependencias

**A0:** validar la unidad exacta R0 y los antecedentes en PLAN/COLA/DAG; una sola EN_PROGRESO propia, candado V17; diff de únicamente este documento y sección íntegra de `CHANGELOG_AI.md` con fuente V5. **A1:** coherencia semántica de EV01–EV12, 14 campos, NG01–NG16 y contratos M01/M02.01, todos NO_IMPLEMENTADOS. **A2:** modelado negativo de tenant A/B, JWT/RBAC/revocación, PII, consentimientos, replay, CAS, fraude y contabilidad; sin certificación de runtime. **A3:** implementación de .NET/HTTP/MySQL/Angular/Playwright NO_APLICA al cambio documental, nunca PASS implícito. **A4:** commit fast-forward CAS sólo dev, Scope Lock y V5 procedencia `completed/success` exact-SHA, BITACORA A:Z idempotente/readback, liberar named range V17 una sola vez. **A5:** revisor de otra de las cinco Tasks Javier, con distinto TASK_ID, revisa diff, evidencia, límites y pruebas futuras; `LISTO_DEV` no equivale a A5_PASS.

M03 requerirá código real por microtarea, pruebas y gates de dependencias sin bypass. Este documento **no habilita** una tarea técnica por sí solo ni completa el capítulo C09.
