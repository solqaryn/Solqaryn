# SOLQARYN V5 — C06-02.M02.05: contrato conceptual de eventos, idempotencia y correlación de landing por tenant

**Alcance:** SOLAMENTE documentación de un posible contrato futuro. No implementar ni invocar webhook, dispatcher, cola, worker, broker, eventos entre Tasks, reintentos de red ni infraestructura. **Autor:** PRIMARY DEV 05 — JAVIER_CHATGPT, TASK_ID 6adc2e55f47c81919f6374e561687076. **Repositorio:** solqaryn/Solqaryn, rama dev, PROJECT_SCOPE_LOCK=STRICT; fecha 2026-11-15.

## 1. Propósito y autoridad

Unidad C06-02.M02.05 de PLAN_MAESTRO fila 912, definición de eventos, idempotencia, correlación y límites para una landing pública administrable por empresa. Fuente única: Standard de Implementación de Automatizaciones V5 FINAL en Drive (ID 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA), PLAN_MAESTRO/COLA/DAG_TECNICO y documentación M01.06/M02.01 del repositorio. La aptitud exige PLAN PENDIENTE sin responsable, COLA G=SI/O=SI, DAG F=VALIDADO/G=SI, y CAS exclusivo antes del trabajo.

M01.06 contiene G01–G16 y RB01–RB08; M02.01 describe las entidades, roles editor/publicador, restricciones de Host y control de versiones. M02.03 documenta persistencia futura sin migraciones. M02.02/M02.04 tienen owners distintos y no son objeto de edición por esta tarea. Las definiciones presentes no autorizan M03, publicación, DNS, Cloudflare/R2, QA/main/PROD ni traspaso de autoridad.

## 2. Envoltorio conceptual futuro, NO ESQUEMA DESPLEGADO

Un evento de negocio de landing solo podría emitirse **después** de una decisión backend autenticada y confirmada, no desde Angular, Host, slug, encabezado forwarded, cookie o localStorage. Propuesta de contrato versionado:

| Campo futuro | Regla de validación, privacidad y control |
| --- | --- |
| EventId | Identificador opaco estable, generado en origen autenticado; no aceptado como concesión de privilegios |
| EventType y SchemaVersion | Tipo permitido cerrado y versión explícita; desconocido se rechaza, nunca deserialización genérica ejecutable |
| TenantId y AggregateId | Tenant resuelto desde contexto validado en servidor; ambos forman ámbito lógico del recurso |
| OccurredAtUtc | Momento backend UTC; no usar reloj del cliente para ordenar decisiones |
| ActorReference y ActorRole | Identificador minimizado y permiso RBAC vigente; no tokens ni perfil completo en payload |
| CorrelationId | Traza acotada a operación/scope, opaca y sin datos secretos; no crea confianza de tenant |
| CausationId | Referencia a operación previa acreditada, evita bucles y deja rastro sin copiar JWT |
| RevisionId y ExpectedVersion | Versión de contenido/revisión, CAS obligatorio para cambios de publicación o rollback |
| IdempotencyKey | Hash/digest derivado del ámbito tenant, tipo, recurso y versión, sin serializar secretos |
| SourceCommitOrContractVersion | Referencia auditable de contrato/publicación, no credencial de despliegue |
| PublicPayload | Solo campos permitidos para consumidor; preview/draft/PII nunca se hacen públicos |
| ResultCode y AuditReference | Resultado mínimo, auditoría interna asociada sin exponer detalles internos a clientes |

En una implementación futura habría autorización antes de construcción de evento, y atomicidad entre estado de publicación y registro idempotente. Este documento **no configura** outbox, bus, dispatcher, webhook ni job: esas soluciones requieren su propia microtarea técnica, pruebas y autorización, no una suposición por M02.05.

## 3. Catálogo de doce intenciones de evento (NO EMITIDAS)

| ID | Tipo documental hipotético | Emisor y fronteras de seguridad |
| --- | --- | --- |
| EV01 | LandingDraftCreated | Editor autorizado en tenant vigente; nunca anónimo ni identidad desde host |
| EV02 | LandingDraftChanged | Versión CAS y whitelist de bloques; sin HTML activo |
| EV03 | PreviewAccessRequested | Actor autorizado, grant vigente y respuesta noindex/no-store; no URL privada en payload |
| EV04 | PreviewAccessRevoked | Revocación de token y auditoría minimizada; nunca mostrar preview a público |
| EV05 | HostOwnershipVerificationRequested | Solo solicitud administrativa; no habilitar binding DNS/TLS |
| EV06 | HostOwnershipVerified | Requiere evidencia de ownership server-side, proveedor explícito y aprobación; no creado aquí |
| EV07 | PublicationApprovalRequested | Editor solicita revisión, no autoaprueba |
| EV08 | PublicationApproved | Publicador distinto con RBAC y revision esperada; prohibición fail-open |
| EV09 | PublicationActivated | Solo tras transacción CAS exitosa; se invalida cache tenant-bound |
| EV10 | PublicationReverted | Puntero a versión previamente aprobada con respaldo y control de integridad |
| EV11 | PublicSeoProjectionChanged | Canonical/robots/sitemap solo de versión pública, sin borradores |
| EV12 | LandingAuditEvidenceRecorded | Correlación y resultado sin JWT, PII detallada ni URL firmada |

Cada nombre representa un concepto de futuro, no un contrato de API vigente ni un trigger de base de datos. No hay emisión automática, infraestructura habilitada o acción de Task programada por listar estos eventos.

## 4. Idempotencia, orden, tiempos y límites propuestos

**ID01 — ámbito:** clave de deduplicación compuesta lógicamente por TenantId validado + tipo + aggregate + revision/version + operación, con hash resistente; nunca compartir resultados entre A/B. **ID02 — atomicidad:** el registro y la transición de publicación futura deben coincidir con una transacción local verificable, sin declarar exactly-once distribuido. **ID03 — CAS:** un cambio de versión obsoleta retorna conflicto explícito sin reintento ciego. **ID04 — duplicados:** una misma solicitud autorizada y equivalente devuelve resultado original minimizado; distinta carga con la misma clave produce conflicto. **ID05 — expiración:** retención y TTL se medirán y documentarán según riesgo, carga y privacidad; valores numéricos no se inventan desde esta etapa.

**ID06 — entrega:** futuros consumidores deberán tolerar at-least-once sin repetir efectos (SEO, caché, notificación). **ID07 — orden:** ordenar por aggregate + versión, no timestamp cliente ni orden global entre tenants. **ID08 — reintentos:** presupuesto acotado, backoff y dead-letter conceptual sujeto a futura autorización; error permanente JWT/RBAC/tenant nunca se reintenta con otro tenant. **ID09 — correlación:** causationId permite detectar ciclos, pero un mismo trace no autoriza acceder a otro scope. **ID10 — rate limit:** límites separados por actor, tenant, host aprobado y operación, fail-closed cuando se desconozca el contexto.

**ID11 — seguridad:** prohibido tomar un header, query, claim de frontend o etiqueta visual como tenant autoritativo; secretos y PII nunca viajan en eventos públicos. **ID12 — aislamiento:** consumidor de empresa B no debe leer, confirmar ni reintentar una clave de A; no inferir tenant predeterminado ante excepción de proveedor.

## 5. Interacciones conceptuales con persistencia y seguridad

- El modelo M02.01 distingue Site, Revision, PreviewGrant, PublicationApproval y AuditEvidence; el contrato M02.03 propone índices compuestos tenant-bound, no una outbox creada.
- Una publicación futura verifica JWT/membresía activa, permisos editor/publicador separados, host aprobado y revisión CAS antes de cualquier efecto. API en .NET 10 e Infrastructure/EF Core/MySQL deberán construir sus propias pruebas funcionales, transaccionales y A/B; no se implementan en M02.05.
- Una petición pública solo lee versión autorizada y activa; SEO, cache y assets se particionan por empresa+host+versión. Un preview no se incorpora al sitemap, no se almacena en cache pública y requiere grant explícito.
- Un error de entrega/evento no produce fallback a tenant ajeno, host alternativo ni publicación sin aprobación. El dashboard puede mostrar estado de error seguro, no asumir éxito.
- La eventual integración con terceros, Cloudflare/R2, SMTP, workers o notificaciones requiere autorización, contrato separado, cuotas y rollback; no activarla aquí.

## 6. Doce pares de aceptación positivos/negativos FUTUROS_NO_EJECUTADOS

| Caso | Positivo esperado futuro | Negativo/falla segura futura |
| --- | --- | --- |
| PN01 | Draft A genera intención EV01 tras autorización backend | Tenant B no crea Draft A ni recibe su EventId |
| PN02 | Actualización CAS correcta queda correlacionada | ExpectedVersion desfasada se rechaza sin efecto |
| PN03 | Preview A con grant activo emite solo evidencia privada | Sin permiso, token vencido o anónimo no recibe contenido |
| PN04 | Host A verificado mantiene ámbito A | Forwarded-Host falsificado no acredita tenant ni ownership |
| PN05 | Publicador distinto aprueba revisión | Editor que se autoaprueba recibe rechazo |
| PN06 | Retransmisión idéntica retorna resultado idempotente | Misma clave con payload distinto genera conflicto |
| PN07 | Reintento temporal conserva tenant+idempotencyKey | Error RBAC/tenant no se reintenta como otro rol |
| PN08 | Cache publicada de A se invalida por versión de A | Cambio A no invalida ni sirve HTML de B |
| PN09 | SEO actualizado solo de revisión aprobada | Preview o draft jamás entra en robots/sitemap |
| PN10 | Auditoría registra actor/UTC/resultado minimizados | JWT, contraseña, URL firmada y PII no aparecen |
| PN11 | Consumo futuro repetido tolera at-least-once | Doble efecto de publicación/rollback es rechazado |
| PN12 | Error de proveedor devuelve señal controlada | No hay bucle infinito, fallback cross-tenant ni éxito ficticio |

Los pares PN son oráculos de diseño, **no** pruebas reales ejecutadas de .NET, Vitest, MySQL, HTTP, navegador, worker o red. La viabilidad del flujo end-to-end se acredita solo con CI causal de una implementación técnica posterior.

## 7. Controles A0–A5, rollback y no-alcance

**A0:** preflight exacto de PLAN H/I, COLA G/O, DAG F/G, seguridad STRICT, correspondencia con M01 y M02.01, archivo propio y CHANGELOG sin intercalado. **A1 documental:** comprobar doce tipos EV, doce reglas ID y doce pares PN consistentes, sin declarar ejecución de pruebas. **A2:** revisar tenant A/B, Host no confiable, JWT/RBAC separado, noindex de preview, límite de payload y secretos; sin alterar runtime. **A3:** compatibilidad conceptual con Domain/Application/Infrastructure/API, contratos M02 sin cambiar owners ni implementar bus. **A4:** commit fast-forward con CAS de HEAD, diff exclusivamente dos Markdown, blobs verificados y checks Scope Lock + procedencia V5 completed/success exact SHA, readbacks institucionales. **A5:** pendiente de otro TASK_ID del allowlist Javier, no del autor.

**NO_APLICA por causa:** build .NET/Angular, MySQL efímero, Playwright y CI de workers no verifican un diff documental sin runtime; queued/cancelled/skipped/failed no se pueden considerar PASS. **Rollback documental:** git revert normal preservando commits de otras Tasks y evento compensatorio aditivo si procediera. **Prohibido:** emitir eventos reales, webhook/dispatcher, run_now entre Tasks, schedules nuevos, bucles server-side, migraciones, modificaciones de DNS/Hostinger/Cloudflare, QA/main/PROD, pagos y claves.
