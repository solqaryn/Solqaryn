# SOLQARYN V17 — C09-02.M02.05: contratos futuros de eventos, idempotencia y límites

**Microtarea:** \`C09-02.M02.05\`. **Task ejecutora:** PRIMARY DEV 02 / JAVIER_CHATGPT \`6af16bb9492c8191b8e8c7d606f11507\`. **PLAN_MAESTRO:** fila 1344; **candado V17:** SQV17_C09_02_M02_05 / 2000001344. **Ámbito:** solqaryn/Solqaryn rama dev, PROJECT_SCOPE_LOCK=STRICT. **Clasificación V15:** R0 documental, sin código runtime.

**Autoridad:** STANDARD DE IMPLEMENTACIÓN DE AUTOMATIZACIONES (Drive 1KdunQg6g6ge9Q6rdgWlYex8f6OvlRodb9xxgW51JDtA); Skill canónica de dev, A0–A5 y dependencias PLAN/COLA/DAG. C09-02 tiene como objetivo puntos, promociones, reseñas, respuestas y atención posventa, con antifraude, moderación y trazabilidad. Esta entrega **no implementa** eventos, broker, outbox, workers, webhooks, API ni envíos; son contratos inertes para una microtarea posterior autorizada.

## 1. Frontera de seguridad y origen

- El modelo conceptual C09-02.M02.01 define CuentaPuntos, MovimientoPuntos, AcumulacionVenta, CanjePuntos, BeneficioPromocion, Resena, DecisionModeracion, ApelacionModeracion, CasoPosventa, ConsentimientoCanal y EventoAuditoriaFidelidad. El presente documento no los materializa ni presupone tablas nuevas.
- Venta y Cliente legados pueden tener EmpresaId NULL; ningún evento puede declarar pertenencia por inferencia. Validar recurso, EmpresaId y UsuarioEmpresa activo desde backend cuando se implemente.
- El ledger de puntos no es un flujo de dinero, crédito comercial ni factura. Ningún evento propuesto acredita saldo ni modifica ventas hasta que transacción/consistencia reales sean probadas.
- JWT/RBAC y permisos se verifican en la operación original y de nuevo en cada procesamiento sensible; un payload, header, frontend o webhook no son fuente autoritativa de tenant.
- Los pasos futuros de persistencia, integraciones, mensajería, consentimiento, idempotencia y concurrencia requieren pruebas R2 en .NET 10 y MySQL 8.4 efímero; este documento no demuestra integración.

## 2. Sobre lógico propuesto (no ejecutable)

| Campo futuro | Regla obligatoria |
| --- | --- |
| EventId | UUID opaco único por evento lógico, no generarlo a partir de PII |
| EventType / SchemaVersion | catálogo allowlisted, versión explícita y compatibilidad comprobable |
| EmpresaId | obligatoria y validada contra contexto backend/autorización; nunca aceptar NULL, cabecera o dato cliente como permiso |
| AggregateType / AggregateId / AggregateVersion | entidad y versión para ordenar efectos, sin mezclar tenants |
| OccurredAtUtc | UTC y precisión definida; expiración/orden versionados |
| CorrelationId / CausationId | traza causal opaca, sin JWT ni contenido sensible |
| OperationId / IdempotencyHash | clave acotada a tenant + operación + actor/recurso, ligada a digest de payload |
| ActorId / RoleVersion | referencias mínimas para revalidar permiso/revocación en consumidor |
| Purpose / ConsentVersion | explícitos para notificaciones, opt-out domina sobre reintentos |
| PayloadDigest / PayloadSchema | hash de contenido validado, no cuerpo de factura, token ni dato PII completo |
| RetryCount / NextAttemptUtc / DeadlineUtc | límites finitos, sin reintento infinito ni bypass de expiración |
| EvidenceRef | referencia controlada a auditoría protegida, no URL pública ni secreto |

El sobre es una **propuesta**: sin paquete NuGet, serializador, cola, DTO ni esquema instalado. No activar event dispatcher entre Tasks.

## 3. Doce contratos de eventos FUTUROS

| ID | Tipo semántico propuesto | Publicador/circunstancia futura | Consumidor/acción permitida futura |
| --- | --- | --- | --- |
| EV01 | ProgramaFidelidadDefinido | administración A con RBAC válido | invalidar caché de configuración A |
| EV02 | VentaElegibleVerificada | venta confirmada y tenant verificado | programar acreditación idempotente, no acreditar aquí |
| EV03 | PuntosAcreditados | transacción ledger confirmada | actualizar proyecciones del mismo tenant |
| EV04 | PuntosCompensados | reversión compensatoria comprometida | reconstruir saldo sin eliminar historial |
| EV05 | CanjeSolicitado | saldo/cupo/versión verificados | iniciar evaluación transaccional una sola vez |
| EV06 | CanjeConfirmado | commit atómico con CAS ganado | actualizar cupo/proyección del tenant |
| EV07 | PromocionPublicada | política allowlisted vigente | invalidar caché A sin activar promoción de B |
| EV08 | AlertaAntifraudeDetectada | evaluación autorizada y trazable | revisión por rol antifraude, sin autoaprobación |
| EV09 | ResenaPendienteModeracion | autor/recurso saneado | cola de moderación privada del tenant |
| EV10 | DecisionModeracionRegistrada | moderador con membresía vigente | proyectar visibilidad y apelación con historia |
| EV11 | CasoPosventaActualizado | ticket/cliente/venta mismo tenant | notificar internamente bajo RBAC |
| EV12 | ConsentimientoCanalRevocado | opt-out confirmado y versionado | cancelar pendiente/retry del canal y auditar |

Ninguno de EV01–EV12 está implementado ni conecta a servicios; términos como «publicador» y «consumidor» describen solamente responsabilidad de diseño.

## 4. Reglas de idempotencia, concurrencia y privacidad

| ID | Condición contractual verificable en implementación posterior |
| --- | --- |
| ID01 | unique lógico (EmpresaId, EventType, OperationId, IdempotencyHash); transacción en motor real |
| ID02 | misma clave + digest igual devuelve efecto único; digest distinto rechaza conflicto sin efecto |
| ID03 | misma clave en A y B no comparte estado ni respuesta; pruebas A/B y partición física/logical |
| ID04 | al menos una vez y retry nunca implican doble acreditación/canje o doble envío |
| ID05 | AggregateVersion debe aumentar por CAS; dos writers compiten y solo uno confirma |
| ID06 | reversión genera movimiento nuevo con causa y link al original, no UPDATE/DELETE ledger |
| ID07 | expiración y ventanas de promoción UTC se reevalúan en retry, no confiar en cache |
| ID08 | opt-out/revocación JWT/RBAC/membresía prevalece al momento de procesar y enviar |
| ID09 | poison-event: cuarentena acotada con auditoría, sin dead-letter pública ni loop infinito |
| ID10 | correlación, logs y errores omiten JWT, secretos, PII y datos cruzados; límites de retención |
| ID11 | reordenación/duplicados no hacen retroceder estado ni restaurar acceso revocado |
| ID12 | no se aceptan eventos de tenant/recurso no verificables; fail-closed sin fallback global |

**Límites futuros**: máximo de intentos, tamaño y retención deberán fijarse mediante presupuesto y medición real; en este contrato permanecen **NO_MEDIDO**. Queda prohibido introducir cifras inventadas como cumplimiento de rendimiento. Operaciones financieras y mensajería real son riesgo R2/R3 sujeto a autorización separada.

## 5. Doce pares de escenarios FUTUROS, NO EJECUTADOS

| Caso | Positivo esperado futuro | Negativo/error cerrado esperado futuro |
| --- | --- | --- |
| P01/N01 | evento A con owner verificado se acepta una vez | evento B/NULL consumido por A se rechaza |
| P02/N02 | venta confirmada acredita una vez | replay y venta anulada no suman puntos |
| P03/N03 | reversión compensa con historial | delete o reversión duplicada no altera saldo |
| P04/N04 | canje con CAS vigente confirma | carrera/versión vieja/saldo insuficiente no gasta |
| P05/N05 | promoción A válida respeta ventana/cupo | B, UTC caducada o sin cupo no aplica |
| P06/N06 | misma clave/digest da misma respuesta | clave con payload distinto genera conflicto |
| P07/N07 | antifraude registra alerta revisable | fallo de regla no activa premio por defecto |
| P08/N08 | reseña saneada entra moderación A | XSS/PII/suplantación no se publica |
| P09/N09 | moderación A legítima deja historia | rol revocado/moderador B no cambia visibilidad |
| P10/N10 | caso posventa A notifica rol válido | IDOR/venta B o lector no recibe payload |
| P11/N11 | consentimiento específico permite canal | opt-out o retry no vuelve a enviar |
| P12/N12 | error temporal usa backoff acotado | poison/reintento infinito no filtra PII ni doble efecto |

Los doce pares son especificaciones, no asserts xUnit ni pruebas de proveedor ejecutadas. Para M03/M05 se exigirán build/test de .NET 10, negativos JWT/RBAC/tenant A/B, pruebas transaccionales Category=Integration en MySQL 8.4 efímero y verificaciones de API/Playwright solo donde procedan. Métricas **NO_MEDIDO**.

## 6. Criterios de calidad A0–A5, observabilidad y rollback

- **A0 (presente):** diff de este documento y sección autocontenida de CHANGELOG_AI.md; doce eventos EV, doce reglas ID, doce casos P/N, límites, dependencias y reversibilidad; ninguna nueva capa ni servicio.
- **A1 (presente, documental):** revisión de oráculos positivos/negativos/fronteras/error; runtime 0/12 ejecutado. No confundir escenarios con evidencia de comportamiento real.
- **A2 (presente, documental):** amenaza cross-tenant/IDOR, replay, actor revocado, revocación de consentimiento, ledger, privacidad y moderación; rechazo por defecto especificado; no certificación runtime.
- **A3:** NO_APLICA_R0 a HTTP, MySQL/EF, colas, brokers, Angular y Playwright en este cambio, pues no se integra ningún componente.
- **A4:** exigir commit fast-forward dev, Scope Lock y V5 procedencia \`completed/success\` en SHA causal, comparar blobs/diff, readbacks de PLAN/COLA/DAG/BITACORA y candado V17 por fila. CI FAIL/CANCELLED/SKIPPED/IN_PROGRESS requerida NO PASS.
- **A5:** otro TASK_ID de las cinco Tasks Javier distinto del autor; evaluación independiente de artefacto/alcance/sha, puede rechazar. LISTO DEV R0 no implica A5_PASS ni habilita M03 si DAG no lo permite.
- **Observabilidad futura:** correlationId y resultados minimizados, no secretos ni PII. Registros idempotentes con referencia causal, retención aprobada; no backend/runtime o alertas nuevos.
- **Rollback R0:** revertir exclusivamente documento y sección de CHANGELOG mediante commit inverso normal sobre HEAD verificado; nunca force-push, editar bitácora histórica, desbloquear gates, QA/main/PROD o cancelar CI ajena.

**Resultado limitado:** contratos conceptuales futuros para gobernanza de eventos de fidelización y reputación. CERO eventos ejecutables, CERO proveedores, CERO listeners, CERO workers y CERO efectos sobre usuarios/datos.
